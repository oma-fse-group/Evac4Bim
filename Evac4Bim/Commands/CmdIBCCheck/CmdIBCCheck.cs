using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;



namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdIBCCheck : IExternalCommand
    {
        // member properties
        Document doc { get; set; }
        private double ReqExitWidthPerOccupant { get; set; }
        private double ReqStairWidthPerOccupant { get; set; }
        private int MaxOccupantLoadPerRoom_1006_2_1 { get; set; }
        private int hasSprinklers { get; set; }
        private int hasAlarm { get; set; }
        double ReqDistanceBetweenExitDoorsFactor { get; set; }
        private double MaxCommonEgressDistance_Min_1006_2_1 { get; set; }
        private double MaxCommonEgressDistance_Max_1006_2_1 { get; set; }
        private double MaxExitAccessTravelDistance_1017_2 { get; set; }
        private Element projInfo { get; set; }

        // Project constants
        public const double MIN_EXIT_DOOR_WIDTH = 812.99; // mm
        public const double MIN_EXIT_DOOR_HEIGHT = 2032; // mm
        public const double MIN_RISER_HEIGHT = 102; // mm
        public const double MAX_RISER_HEIGHT = 178; // mm
        public const double MIN_TREAD_DEPTH = 279; // mm



        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            this.doc = uidoc.Document;
            var app = commandData.Application.Application;
            this.projInfo = doc.ProjectInformation as Element;

 

            // Init project parameters 
            this.ReqExitWidthPerOccupant = Double.Parse(projInfo.LookupParameter("ReqExitWidthPerOccupant").AsString());
            this.ReqStairWidthPerOccupant = Double.Parse(projInfo.LookupParameter("ReqStairWidthPerOccupant").AsString());
            this.MaxOccupantLoadPerRoom_1006_2_1 = Int32.Parse(projInfo.LookupParameter("1006_2_1_MaxOccupantLoadPerRoom").AsString());
            this.hasSprinklers = projInfo.LookupParameter("hasSprinklers").AsInteger();
            this.hasAlarm = projInfo.LookupParameter("hasAlarm").AsInteger();
            this.MaxCommonEgressDistance_Min_1006_2_1 = Double.Parse(projInfo.LookupParameter("1006_2_1_MaxCommonEgressDistance_Min").AsString());
            this.MaxCommonEgressDistance_Max_1006_2_1 = Double.Parse(projInfo.LookupParameter("1006_2_1_MaxCommonEgressDistance_Max").AsString());
            this.MaxExitAccessTravelDistance_1017_2 = Double.Parse(projInfo.LookupParameter("1017_2_MaxExitAccessTravelDistance").AsString());
            // distance between two doors = factor * diagonal 
            // factor = {0.5 0.33}
            this.ReqDistanceBetweenExitDoorsFactor = 0.5;
            if (this.hasSprinklers == 1 && this.hasAlarm == 1)
            {
                ReqDistanceBetweenExitDoorsFactor = 0.33;
            }


            //TaskDialog.Show("Debug", "Exit  : " + MaxCommonEgressDistance_Min_1006_2_1.ToString());

            // Open transaction
            var tx = new Transaction(doc);
            tx.Start("Export IFC");


            //Querry rooms which are not corridors
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<Element> rooms = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).WhereElementIsNotElementType().Where(room => room.GetType() == typeof(Room)).Where(room => room.LookupParameter("isCorridor").AsInteger() == 0).Where(room => room.LookupParameter("ExcludeComponent").AsInteger() == 0).ToList();

            //Querry all doors in the model which are exits (can be a room exit or a discharge exit)
            IEnumerable<Element> doorsList = collector.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().Where(room => room.LookupParameter("isExitDoor").AsInteger() == 1);
            List<Element> roomExitList = doorsList.ToList();
            List<Element> dischargeExitList = doorsList.Where(door => door.LookupParameter("isDischargeExit").AsInteger() == 1).ToList();

            // List of storeys 
            List<Element> storeys = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToList();

           
            // check if emergency stairs are included in a multistorey stair => unpin 
            List<Element> multiStoreyStairs = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_MultistoryStairs).ToList();
            // ask for user permission 
            IBCCheckUtils.unpinStairs(multiStoreyStairs, this.doc);

            // List of stairs 
            List<Element> stairs = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Stairs).Where(stair => stair.LookupParameter("isEmergencyStair").AsInteger() == 1).ToList();

             
            //TaskDialog.Show("Debug", storeys.Count.ToString());

            /// 1 . Checking room egress capacity + occupant load 
            ibcCheckRooms(rooms, roomExitList);

            /// 2 . Check Building 
            ibcCheckBuilding();

            /// 3. Check storeys 
            ibcCheckStoreys(storeys, rooms, dischargeExitList);
            ibcCheckBuildingEgressCapacity(storeys, dischargeExitList);

            /// 4. Check stairs 
            ibcCheckStairSystem(stairs,storeys);

           // Confirmation
           TaskDialog.Show("Result", "Check is over");

            tx.Commit();
            return Result.Succeeded;
        }




        // member methods      

        public void ibcCheckRooms(List<Element> rooms, List<Element> roomExitList)
        {

            //Loop through rooms 

            foreach (Element r in rooms)
            {
                // C.1 Find doors belonging to current room
                string roomName = r.LookupParameter("Name").AsString();

                List<Element> r_exits = roomExitList.OfType<FamilyInstance>().Where(door => door.FromRoom.LookupParameter("Name").AsString() == roomName).OfType<Element>().ToList();

                // C.2 Compute the required exit capacity 
                int roomOccupantLoad = 0;
                string roomOccupantLoadStr = r.LookupParameter("RoomOccupantLoad").AsString();
                if (roomOccupantLoadStr == "" || roomOccupantLoadStr==null)
                {
                    // if it is not set, use the IBC default value 
                    roomOccupantLoad = Int32.Parse(r.LookupParameter("IBCMaxOccupantLoad").AsString());
                    r.LookupParameter("RoomOccupantLoad").Set(roomOccupantLoad.ToString());
                }
                else
                {
                    roomOccupantLoad = Int32.Parse(roomOccupantLoadStr);
                }
                double ReqExitWidth = ReqExitWidthPerOccupant * roomOccupantLoad;
                if (ReqExitWidth < MIN_EXIT_DOOR_WIDTH)
                {
                    ReqExitWidth = MIN_EXIT_DOOR_WIDTH; // avoid too smal values 
                }
                r.LookupParameter("requiredEgressCapacity").Set(ReqExitWidth.ToString());
                double roomTravelDistance = Double.Parse(r.LookupParameter("TravelDistance").AsString());

                // C.3 Compute the requried number of exits 
                int RequiredNumberOfExits = 1;
                if (roomOccupantLoad >= 1000) { RequiredNumberOfExits = 4; }
                else if (roomOccupantLoad < 1000 && roomOccupantLoad >= 500) { RequiredNumberOfExits = 3; }
                else if (roomOccupantLoad > MaxOccupantLoadPerRoom_1006_2_1) { RequiredNumberOfExits = 2; }
                else if (hasSprinklers == 1)
                {
                    if (roomTravelDistance > MaxCommonEgressDistance_Min_1006_2_1) { RequiredNumberOfExits = 2; }
                }
                else if (hasSprinklers == 0)
                {
                    if (roomOccupantLoad > 30 && roomTravelDistance > MaxCommonEgressDistance_Min_1006_2_1) { RequiredNumberOfExits = 2; }
                    else if (roomOccupantLoad <= 30 && roomTravelDistance > MaxCommonEgressDistance_Max_1006_2_1) { RequiredNumberOfExits = 2; }

                }
                // else, it stays == 1
                r.LookupParameter("RequiredNumberOfExits").Set(RequiredNumberOfExits.ToString());

                // C.4 Loop through exit doors of current room
                int availableNumberOfExit = 0;
                double availableEgressWidth = 0;
                List<double> widths = new List<double>();
                List<XYZ> doorLocations = new List<XYZ>();

                foreach (Element d in r_exits)
                {
                    FamilyInstance d_f = d as FamilyInstance;

                    // C.4.1 : Check if door has required minimum size 

                    double width = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Millimeters);
                    double height = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble(), UnitTypeId.Millimeters);

                    

                    if (width >= MIN_EXIT_DOOR_WIDTH && height >= MIN_EXIT_DOOR_HEIGHT)
                    {
                        d.LookupParameter("hasMinRequiredSize").Set("True");
                    }
                    else
                    {
                        if (width == 0)
                        {
                            d.LookupParameter("hasMinRequiredSize").Set("Error. Width not found");

                        }
                        else
                        {
                            d.LookupParameter("hasMinRequiredSize").Set("False");

                        }
                    }

                    // C.4.2 : Append the number of exits belonging to this room 
                    availableNumberOfExit++;


                    // C.4.3 : Append the available egress capacity for this room
                    availableEgressWidth += width;

                    // C.5 Check if egress capacity is well balanced 
                    widths.Add(width);

                    // C.6 Store door location for later use 
                    XYZ doorLocation = null;
                    
                    if (d.Location == null)                   
                    {
                        // can be a curtain glass 
                        // fetch origin from family instnce
                        FamilyInstance exitEleFamIns = d as FamilyInstance;
                        doorLocation = exitEleFamIns.GetTransform().Origin;

                    }
                    else
                    {
                        doorLocation = (d.Location as LocationPoint).Point;
                    }
                    doorLocations.Add(doorLocation);
                    //TaskDialog.Show("Debug", XYZToString((d.Location as LocationPoint).Point));

                }

                // C.4.2 : check the number of exits belonging to this room
                r.LookupParameter("AvailableNumberOfExits").Set(availableNumberOfExit.ToString());
                //hasSufficientNumberOfExits hasBalancedEgressCapacity
                string hasSufficientNumberOfExits = "True";
                if (availableNumberOfExit < RequiredNumberOfExits)
                {
                    hasSufficientNumberOfExits = "False";
                }
                r.LookupParameter("hasSufficientNumberOfExits").Set(hasSufficientNumberOfExits);

                // C.4.3 : Check the available egress capacity for this room
                r.LookupParameter("AvailableEgressCapacity").Set(availableEgressWidth.ToString());
                string hasSufficientEgressCapacity = "True";
                if (availableEgressWidth < ReqExitWidth)
                {
                    hasSufficientEgressCapacity = "False";
                }
                r.LookupParameter("hasSufficientEgressCapacity").Set(hasSufficientEgressCapacity);


                // C.5 Check if egress capacity is well balanced 
                // only applicable if more than 1 exit

                string hasBalancedEgressCapacity = "True";

                if (availableNumberOfExit > 1)
                {
                    foreach (double w in widths)
                    {
                        double ratio = w / availableEgressWidth;
                        if (ratio > 0.5)
                        {
                            hasBalancedEgressCapacity = "False";
                            break;
                        }
                    }
                }

                r.LookupParameter("hasBalancedEgressCapacity").Set(hasBalancedEgressCapacity);


                // C.6 Check if distances between exits are okay (if multiple exits)
                // At least two doors are separated by a distance superior to ReqDistanceBetweenExitDoors
                // Get the diagonal length of the room = distance between two furthest points 

                double roomDiagonal = IBCCheckUtils.GetRoomDiagonalDist(r as Room); // in millimeters
                double ReqDistanceBetweenExitDoors = ReqDistanceBetweenExitDoorsFactor * roomDiagonal;
                bool hasExitDoorsConfigured = true; // by default - only one door

                //TaskDialog.Show("ReqDistanceBetweenExitDoors", ReqDistanceBetweenExitDoors.ToString());
                if (availableNumberOfExit > 1)
                {
                    hasExitDoorsConfigured = false; // by default if multiple doors 

                    // more than two exits, need to check distances
                    for (int i = 0; i < doorLocations.Count(); i++)
                    {
                        XYZ p_i = doorLocations[i];
                        for (int j = i + 1; j < doorLocations.Count(); j++)
                        {
                            XYZ p_j = doorLocations[j];
                            double distance = p_i.DistanceTo(p_j);
                            distance = Math.Round(UnitUtils.ConvertFromInternalUnits(distance, UnitTypeId.Millimeters), 0);

                            //TaskDialog.Show("Doors", distance.ToString());
                            if (distance > ReqDistanceBetweenExitDoors)
                            {
                                hasExitDoorsConfigured = true;
                                break;
                            }


                        }
                        if (hasExitDoorsConfigured)
                        {
                            break; // no need to look further
                        }

                    }

                    //TaskDialog.Show("Doors", hasExitDoorsConfigured.ToString());

                }
                r.LookupParameter("DiagonalLength").Set(roomDiagonal.ToString());
                r.LookupParameter("hasExitDoorsConfigured").Set(hasExitDoorsConfigured.ToString());


                
                // C.7 check if occupant load is exceeded
                string isMaxOccupantLoadExceeded = "False";
                int IBCMaxOccupantLoad = Int32.Parse(r.LookupParameter("IBCMaxOccupantLoad").AsString());
                if (roomOccupantLoad > IBCMaxOccupantLoad)
                {
                    isMaxOccupantLoadExceeded = "True";
                }

                r.LookupParameter("isMaxOccupantLoadExceeded").Set(isMaxOccupantLoadExceeded);


                // C.8 chck travel distance             

                double TravelDistance = Double.Parse(r.LookupParameter("TravelDistance").AsString()); // in mm ! 
                string isTravelDistanceExceeded = "False";
                if (TravelDistance > MaxExitAccessTravelDistance_1017_2)
                {
                    isTravelDistanceExceeded = "True";
                }
                r.LookupParameter("isTravelDistanceExceeded").Set(isTravelDistanceExceeded);


            }

        }
        public void ibcCheckBuilding()
        {
            // check if required sprinklers were provided
            string hasSprinklersRequiredNotProvided = "False"; // default



            if (hasSprinklers == 0 && MaxExitAccessTravelDistance_1017_2 == -1)
            {
                hasSprinklersRequiredNotProvided = "True";
            }

            projInfo.LookupParameter("hasSprinklersRequiredNotProvided").Set(hasSprinklersRequiredNotProvided);


        }
        public void ibcCheckStoreys(List<Element> storeys, List<Element> rooms, List<Element> dischargeExitList)
        {
            // Loop through storeys
            foreach (Element s in storeys)
            {

                ElementId levelID = s.Id;
                int LevelOccupantLoad = 0;

                // find rooms belonging to each storey 
                List<Element> roomsInStorey = rooms.Where(room => room.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID).AsElementId() == levelID).ToList();
                // Querry doors from that level
                List<Element> dischargeDoorsInStorey = dischargeExitList.Where(door => door.LevelId == levelID).ToList();
                //TaskDialog.Show("Debug", dischargeDoorsInStorey.Count.ToString());

                // Loop through rooms to collect number of occupants
                foreach (Element r in roomsInStorey)
                {
                    // Increment number of occupant
                    LevelOccupantLoad += Int32.Parse(r.LookupParameter("RoomOccupantLoad").AsString());

                }
                // Update occupant load of storey 
                s.LookupParameter("LevelOccupantLoad").Set(LevelOccupantLoad.ToString());



                // C.2 Compute the required exit capacity 
                double ReqExitWidth = ReqExitWidthPerOccupant * LevelOccupantLoad;
                if (ReqExitWidth < MIN_EXIT_DOOR_WIDTH)
                {
                    ReqExitWidth = MIN_EXIT_DOOR_WIDTH;
                }
                s.LookupParameter("requiredEgressCapacityLevel").Set(ReqExitWidth.ToString());

                // C.3 Compute the requried number of exits 
                int RequiredNumberOfExits = 2;
                if (LevelOccupantLoad > 1000) { RequiredNumberOfExits = 4; }
                else if (LevelOccupantLoad <= 1000 && LevelOccupantLoad > 500) { RequiredNumberOfExits = 3; }

                // else, it stays == 2
                s.LookupParameter("RequiredNumberOfExitsLevel").Set(RequiredNumberOfExits.ToString());

                // C.4 Loop through exit doors of current level
                int availableNumberOfExit = 0;
                double availableEgressWidth = 0;
                List<double> widths = new List<double>();
                foreach (Element d in dischargeDoorsInStorey)
                {
                    FamilyInstance d_f = d as FamilyInstance;

                    // C.4.1 : Check if door has required minimum size 
                    // Door min size was already checked in ibcCheckRooms

                    // C.4.2 : Append the number of exits belonging to this room 
                    availableNumberOfExit++;


                    // C.4.3 : Append the available egress capacity for this room

                    double width = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Millimeters);
                    double height = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble(), UnitTypeId.Millimeters);


                    availableEgressWidth += width;

                    // C.5 Check if egress capacity is well balanced 
                    widths.Add(width);
                }

                // C.4.2 : Check the number of exits belonging to this room
                s.LookupParameter("AvailableNumberOfExitsLevel").Set(availableNumberOfExit.ToString());
                string hasSufficientNumberOfExits = "True";
                if (availableNumberOfExit < RequiredNumberOfExits)
                {
                    hasSufficientNumberOfExits = "False";
                }
                s.LookupParameter("hasSufficientNumberOfExitsLevel").Set(hasSufficientNumberOfExits);

                // C.4.3 : Check the available egress capacity for this room
                s.LookupParameter("AvailableEgressCapacityLevel").Set(availableEgressWidth.ToString());
                string hasSufficientEgressCapacity = "True";
                if (availableEgressWidth < ReqExitWidth)
                {
                    hasSufficientEgressCapacity = "False";
                }
                s.LookupParameter("hasSufficientEgressCapacityLevel").Set(hasSufficientEgressCapacity);


                // C.5 Check if egress capacity is well balanced 
                string hasBalancedEgressCapacity = "True";

                if (availableNumberOfExit > 1)
                {
                    // only applicable if more than 1 exit
                    foreach (double w in widths)
                    {
                        double ratio = w / availableEgressWidth;
                        if (ratio > 0.5)
                        {
                            hasBalancedEgressCapacity = "False";
                            break;
                        }
                    }
                }

                s.LookupParameter("hasBalancedEgressCapacityLevel").Set(hasBalancedEgressCapacity);


                // C.6 Check if distances between exits are okay (if multiple exits)
                // { ... }

            }
        }
        public void ibcCheckBuildingEgressCapacity(List<Element> storeys, List<Element> dischargeExitList)
        {
            int BuildingOccupantLoad = 0;
            // Loop through storeys to find total occupant load of building
            foreach (Element s in storeys)
            {
                BuildingOccupantLoad += Int32.Parse(s.LookupParameter("LevelOccupantLoad").AsString());
            }



            // Update total occupant load of building
            projInfo.LookupParameter("BuildingOccupantLoad").Set(BuildingOccupantLoad.ToString());


            // Querry the discharge level 
            List<Element> dischargeStoreys = storeys.Where(s => s.LookupParameter("isDischargeLevel").AsInteger() == 1).ToList();

            if (dischargeStoreys.Count > 0)
            {

                Element dischargeStorey = dischargeStoreys.First();   // Only one per building is allowed ! 

                ElementId levelID = dischargeStorey.Id;

                // Querry doors from that level
                List<Element> dischargeDoorsInStorey = dischargeExitList.Where(door => door.LevelId == levelID).ToList();

                // C.2 Compute the required exit capacity 
                double ReqExitWidth = ReqExitWidthPerOccupant * BuildingOccupantLoad;
                if (ReqExitWidth < MIN_EXIT_DOOR_WIDTH)
                {
                    ReqExitWidth = MIN_EXIT_DOOR_WIDTH;
                }
                dischargeStorey.LookupParameter("requiredEgressCapacityLevel").Set(ReqExitWidth.ToString());

                // C.3 Compute the requried number of exits 
                int RequiredNumberOfExits = 2;
                if (BuildingOccupantLoad > 1000) { RequiredNumberOfExits = 4; }
                else if (BuildingOccupantLoad <= 1000 && BuildingOccupantLoad > 500) { RequiredNumberOfExits = 3; }

                // else, it stays == 2
                dischargeStorey.LookupParameter("RequiredNumberOfExitsLevel").Set(RequiredNumberOfExits.ToString());

                // C.4 Loop through exit doors of current level
                int availableNumberOfExit = 0;
                double availableEgressWidth = 0;
                List<double> widths = new List<double>();
                foreach (Element d in dischargeDoorsInStorey)
                {
                    FamilyInstance d_f = d as FamilyInstance;

                    // C.4.1 : Check if door has required minimum size 
                    // Door min size was already checked in ibcCheckRooms

                    // C.4.2 : Append the number of exits belonging to this level 
                    availableNumberOfExit++;


                    // C.4.3 : Append the available egress capacity for this level

                    double width = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Millimeters);
                    double height = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble(), UnitTypeId.Millimeters);

                    availableEgressWidth += width;

                    // C.5 Check if egress capacity is well balanced 
                    widths.Add(width);
                }

                // C.4.2 : Check the number of exits belonging to this room
                dischargeStorey.LookupParameter("AvailableNumberOfExitsLevel").Set(availableNumberOfExit.ToString());
                string hasSufficientNumberOfExits = "True";
                if (availableNumberOfExit < RequiredNumberOfExits)
                {
                    hasSufficientNumberOfExits = "False";
                }
                dischargeStorey.LookupParameter("hasSufficientNumberOfExitsLevel").Set(hasSufficientNumberOfExits);

                // C.4.3 : Check the available egress capacity for this level
                dischargeStorey.LookupParameter("AvailableEgressCapacityLevel").Set(availableEgressWidth.ToString());
                string hasSufficientEgressCapacity = "True";
                if (availableEgressWidth < ReqExitWidth)
                {
                    hasSufficientEgressCapacity = "False";
                }
                dischargeStorey.LookupParameter("hasSufficientEgressCapacityLevel").Set(hasSufficientEgressCapacity);


                // C.5 Check if egress capacity is well balanced 
                string hasBalancedEgressCapacity = "True";

                if (availableNumberOfExit > 1)
                {
                    // only applicable if more than 1 exit
                    foreach (double w in widths)
                    {
                        double ratio = w / availableEgressWidth;
                        if (ratio > 0.5)
                        {
                            hasBalancedEgressCapacity = "False";
                            break;
                        }
                    }
                }

                dischargeStorey.LookupParameter("hasBalancedEgressCapacityLevel").Set(hasBalancedEgressCapacity);


                // C.6 Check if distances between exits are okay (if multiple exits)
                // { ... }

            }



        }

        /// <summary>
        /// For multi-storey stairs ==> unpin stairs in the UI ! 
        /// Assumption : stair serves one floor only - multistoreystair is made up multiple single storey stairs
        /// Discharge level is ignored
        /// </summary>
        /// <param name="stairs"></param>
        /// <param name="storeys"></param>
        public void ibcCheckStairSystem(List<Element> stairs, List<Element> storeys)
        {
            // First - reset all storeys 
            // Compute required widths 
            foreach (Element storey in storeys)
            {

                storey.LookupParameter("AvailableStairCount").Set("n.a");
                storey.LookupParameter("AvailableStairCapacity").Set("n.a");
                storey.LookupParameter("RequiredStairCount").Set("n.a");
                storey.LookupParameter("RequiredStairCapacity").Set("n.a");
                storey.LookupParameter("hasSufficientStairCount").Set("n.a");
                storey.LookupParameter("hasSufficientStairCapacity").Set("n.a");
                storey.LookupParameter("hasBalancedStairCapacity").Set("n.a");

                
                int LevelOccupantLoad = Int32.Parse(storey.LookupParameter("LevelOccupantLoad").AsString());

                if (storey.LookupParameter("isDischargeLevel").AsInteger() != 1 && LevelOccupantLoad > 0)
                {
                    /// 2.1 for each storey => Compute RequiredStairCount and RequiredStairCapacity - occupant number and min width  !
                    /* Required capacity :
                   - 7.6 mm per occupant
                   - 5.1 mm if building class is not H / I-2 and there is sprinkler + voice alarm
                   - §1011.2 : If Level.occupantLoad< 50 ⇒ minimum width 36 in (914 mm)                
                   - If Level.occupantLoad > 50 ⇒ minimum width 44 in (1118 mm)*/



                    /// 2.1.1 Check RequiredStairCapacity
                    double MinRequiredStairCapacity = 0;
                    // Compute MinRequiredStairCapacity
                    if (LevelOccupantLoad < 50)
                    {
                        MinRequiredStairCapacity = 914; //mm
                    }
                    else
                    {
                        MinRequiredStairCapacity = 1118;
                    }


                    double RequiredStairCapacity = ReqStairWidthPerOccupant * LevelOccupantLoad;
                    if (RequiredStairCapacity < MinRequiredStairCapacity)
                    {
                        RequiredStairCapacity = MinRequiredStairCapacity; // avoid too smal values 
                    }
                    storey.LookupParameter("RequiredStairCapacity").Set(RequiredStairCapacity.ToString());

                    /// 2.1.2 Check RequiredStairCount
                    int RequiredStairCount = 2;
                    if (LevelOccupantLoad > 1000) { RequiredStairCount = 4; }
                    else if (LevelOccupantLoad <= 1000 && LevelOccupantLoad > 500) { RequiredStairCount = 3; }

                    // else, it stays == 2
                    storey.LookupParameter("RequiredStairCount").Set(RequiredStairCount.ToString());


                }



            }


            // TaskDialog.Show("Debug", stairs.Count.ToString());
            /// 1. Loop stairs 
            foreach (Stairs s in stairs)
            {
                               
                StairsType s_FI = doc.GetElement(s.GetTypeId()) as StairsType;

                /// 1.1 Get stair width 
                double s_width =  Math.Round(UnitUtils.ConvertFromInternalUnits(s_FI.MinRunWidth, UnitTypeId.Millimeters), 1); // mm
                // specify width in parameters 
                //s.LookupParameter("Width").Set(100);

               // TaskDialog.Show("Debug", s_width.ToString());
               /// 1.2 get stair construction 
               double s_riserHeight = Math.Round(UnitUtils.ConvertFromInternalUnits(s.ActualRiserHeight, UnitTypeId.Millimeters), 1); 
               double s_treadDepth = Math.Round(UnitUtils.ConvertFromInternalUnits(s.ActualTreadDepth, UnitTypeId.Millimeters), 1);
                //TaskDialog.Show("Debug", s_riserHeight.ToString() + " * " + s_treadDepth.ToString());

                /// 1.3 check against requirements 

                if (MIN_RISER_HEIGHT <= s_riserHeight && s_riserHeight <= MAX_RISER_HEIGHT)
                {
                    s.LookupParameter("hasSufficientRiserHeight").Set("True");
                }
                else
                {
                    s.LookupParameter("hasSufficientRiserHeight").Set("False");
                }
                if (MIN_TREAD_DEPTH <= s_treadDepth )
                {
                    s.LookupParameter("hasSufficientTreadDepth").Set("True");
                }
                else
                {
                    s.LookupParameter("hasSufficientTreadDepth").Set("False");
                }

                /// 1.4 Find serviced storeys 
                Level l = null;
                ElementId s_toplevel = s.LookupParameter("Top Level").AsElementId();
                foreach (Element storey in storeys)
                {
                    l = storey as Level;

                    if (l.Id == s_toplevel)
                    {
                        break;
                    }

                }
                
                     
                // 1.4.1 For each serviced storey => append AvailableStairCount and AvailableStairCapacity
                // skip DISCHARGE LEVEL
                if (l.LookupParameter("isDischargeLevel").AsInteger()!=1)
                {

                    int AvailableStairCount = 0;
                    int.TryParse(l.LookupParameter("AvailableStairCount").AsString(), out AvailableStairCount);
                    AvailableStairCount += 1;
                    l.LookupParameter("AvailableStairCount").Set(AvailableStairCount.ToString());

                    double AvailableStairCapacity = 0;
                    double.TryParse(l.LookupParameter("AvailableStairCapacity").AsString(), out AvailableStairCapacity);
                    AvailableStairCapacity+=s_width;
                    l.LookupParameter("AvailableStairCapacity").Set(AvailableStairCapacity.ToString());
                    
                    /// 2.3 Check AvailableStairCapacity is well balanced 
                    // ie No serving stair should have a width less than 50% of the required capacity
                    double RequiredStairCapacity = 0;
                    double.TryParse(l.LookupParameter("RequiredStairCapacity").AsString(), out RequiredStairCapacity);
                    string hasBalancedStairCapacity = "True";
                    double reducedStairCapacity = AvailableStairCapacity - s_width; // if this particular exit was lost ! 
                    if (reducedStairCapacity < 0.5 * RequiredStairCapacity)
                    {
                        hasBalancedStairCapacity = "False";
                        
                    }
                    l.LookupParameter("hasBalancedStairCapacity").Set(hasBalancedStairCapacity);



                }

            }

            /// 2. Loop all storeys in the model
            /// (for later use) 2.3 find OverallRequiredStairCount and OverallRequiredStairCapacity (max values)
            int OverallRequiredStairCount = 0;
            double OverallRequiredStairCapacity = 0.0;

            foreach (Element storey in storeys)
            {
                int LevelOccupantLoad = Int32.Parse(storey.LookupParameter("LevelOccupantLoad").AsString());

                if (storey.LookupParameter("isDischargeLevel").AsInteger() != 1 && LevelOccupantLoad>0)
                {

                    /// 2.2 Compare with AvailableStairCount and AvailableStairCapacity => hasSufficientStairCount and hasSufficientStairCapacity

                    int RequiredStairCount = 0;
                    int.TryParse(storey.LookupParameter("RequiredStairCount").AsString(), out RequiredStairCount);

                    double RequiredStairCapacity = 0;
                    double.TryParse(storey.LookupParameter("RequiredStairCapacity").AsString(), out RequiredStairCapacity);

                    int AvailableStairCount =0;
                    int.TryParse(storey.LookupParameter("AvailableStairCount").AsString(),out AvailableStairCount);

                    double AvailableStairCapacity = 0;
                    double.TryParse(storey.LookupParameter("AvailableStairCapacity").AsString(), out AvailableStairCapacity);

                    string hasSufficientStairCapacity = "True";
                    if (AvailableStairCapacity < RequiredStairCapacity)
                    {
                        hasSufficientStairCapacity = "False";
                    }
                    storey.LookupParameter("hasSufficientStairCapacity").Set(hasSufficientStairCapacity);

                    string hasSufficientStairCount = "True";
                    if (AvailableStairCount < RequiredStairCount)
                    {
                        hasSufficientStairCount = "False";
                    }
                    storey.LookupParameter("hasSufficientStairCount").Set(hasSufficientStairCount);





                    //2.3 find OverallRequiredStairCount and OverallRequiredStairCapacity (max values)
                    if (AvailableStairCount > OverallRequiredStairCount)
                    {
                        OverallRequiredStairCount = AvailableStairCount;
                    }
                    if (AvailableStairCapacity > OverallRequiredStairCapacity)
                    {
                        OverallRequiredStairCapacity = AvailableStairCapacity;
                    }


                }

            }


            /// 3. Ensure capacity is maintained : 
            string isOverallRequiredStairCapacityMaintained = "True";
            string isOverallRequiredStairCountMaintained = "True";
            /// 3.1 Loop storeys again ! 
            foreach (Element storey in storeys)
            {
                int LevelOccupantLoad = Int32.Parse(storey.LookupParameter("LevelOccupantLoad").AsString());

                if (storey.LookupParameter("isDischargeLevel").AsInteger() != 1 && LevelOccupantLoad > 0)
                {
                    /// 3.1.1 For each storey => AvailableStairCount and AvailableStairCapacity ==  OverallRequiredStairCount and OverallRequiredStairCapacity
                    int AvailableStairCount = 0;
                    int.TryParse(storey.LookupParameter("AvailableStairCount").AsString(), out AvailableStairCount);

                    double AvailableStairCapacity = 0;
                    double.TryParse(storey.LookupParameter("AvailableStairCapacity").AsString(), out AvailableStairCapacity);

                    if (AvailableStairCapacity != OverallRequiredStairCapacity)
                    {
                        isOverallRequiredStairCapacityMaintained = "False";
                    }
                    if (AvailableStairCount != OverallRequiredStairCount)
                    {
                        isOverallRequiredStairCountMaintained = "False";
                    }

                    

                }

            }


            // update proj info 
            doc.ProjectInformation.LookupParameter("isOverallRequiredStairCapacityMaintained").Set(isOverallRequiredStairCapacityMaintained);
            doc.ProjectInformation.LookupParameter("isOverallRequiredStairCountMaintained").Set(isOverallRequiredStairCountMaintained);
            doc.ProjectInformation.LookupParameter("OverallRequiredStairCount").Set(OverallRequiredStairCount.ToString());
            doc.ProjectInformation.LookupParameter("OverallRequiredStairCapacity").Set(OverallRequiredStairCapacity.ToString());

 
        }





    }

    public class IBCCheckUtils
    {
        /// <summary>
        /// Return the diagonal of a room = furthest distance between two rooms [millimeters]
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public static double GetRoomDiagonalDist(Room room)
        {

            // List of boundary points 
            List<string> vertices = new List<string>(); // Store coordiantes here first to ensure points are unique
            List<XYZ> verts = new List<XYZ>(); // then convert into XYZ

            double maxDist = 0;

            // Get list of vertices
            IList<IList<Autodesk.Revit.DB.BoundarySegment>> segments = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
            if (null != segments)  //the room may not be bound
            {
                foreach (IList<Autodesk.Revit.DB.BoundarySegment> segmentList in segments)
                {

                    foreach (Autodesk.Revit.DB.BoundarySegment boundarySegment in segmentList)
                    {


                        // Get curve start point
                        string start = XYZToString(boundarySegment.GetCurve().GetEndPoint(0));
                        if (!vertices.Contains(start))
                        {
                            vertices.Add(start);
                        }
                        // Get curve end point
                        string end = XYZToString(boundarySegment.GetCurve().GetEndPoint(1));
                        if (!vertices.Contains(end))
                        {
                            vertices.Add(end);
                        }

                    }
                }
            }

            // Convert to XYZ 
            foreach (string v in vertices)
            {
                verts.Add(StringtoXYZ(v));
            }


            // Get distances 
            for (int i = 0; i < verts.Count(); i++)
            {
                XYZ p_i = verts[i];
                for (int j = i + 1; j < verts.Count(); j++)
                {
                    XYZ p_j = verts[j];
                    double distance = p_i.DistanceTo(p_j);
                    distance = Math.Round(UnitUtils.ConvertFromInternalUnits(distance, UnitTypeId.Millimeters), 0);

                    if (distance > maxDist)
                    {
                        maxDist = distance;
                    }

                }

            }

            return maxDist;


        }

        // output the point's three coordinates
        public static string XYZToString(XYZ point)
        {
            return point.X + ";" + point.Y + ";" + point.Z;
        }
        public static XYZ StringtoXYZ(string point)
        {
            return new XYZ(Double.Parse(point.Split(';')[0]), Double.Parse(point.Split(';')[1]), Double.Parse(point.Split(';')[2]));
        }

        /// <summary>
        /// return results 
        /// return -1 : no stair was pinned 
        /// return 0 : user refused to unpin
        /// return 1 : success 
        /// </summary>
        /// <param name="multiStoreyStairs"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static int unpinStairs (List<Element> multiStoreyStairs, Document doc)
        {
            int result = -1;
            if (multiStoreyStairs.Count > 0)
            {
                TaskDialogResult res = TaskDialogResult.Cancel; // default value - palceholder
                // check if any of the emergency stairs is in a multistoreystair
                // if so => unpin 
                // run the function twice to ensure all stairs are unpinned
                for (int i = 0; i < 2; i++)
                {
                    foreach (Element mst in multiStoreyStairs)
                    {
                        MultistoryStairs m = mst as MultistoryStairs;
                        List<ElementId> m_stairsIds = m.GetAllStairsIds().Where(sId => doc.GetElement(sId).LookupParameter("isEmergencyStair").AsInteger() == 1).ToList();
                        //TaskDialog.Show("Debug", m_stairsIds.Count.ToString());

                        if (m_stairsIds.Count > 0)
                        {
                            if (res == TaskDialogResult.Cancel) // only ask once
                            {
                                // ask user's permission first
                                // offer to remove previous paths of travel 
                                TaskDialog dialog = new TaskDialog("Decision");
                                dialog.MainContent = "Emergency stairs are pinned to multi storey stairs and cannot be processed proeprly. Unpin emergency stairs ?";
                                dialog.AllowCancellation = true;
                                dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

                                res = dialog.Show();

                            }
                            
                            if (res == TaskDialogResult.Yes)
                            {
                                // Yes
                                foreach (ElementId sId in m_stairsIds)
                                {
                                    Stairs s = doc.GetElement(sId) as Stairs;
                                    // TaskDialog.Show("Debug", s.LookupParameter("Base Level").AsElementId().ToString());
                                    m.Unpin(s.LookupParameter("Base Level").AsElementId());

                                }
                                result = 1;

                            }
                            else
                            {
                                // No
                                result = 0;
                            }
                        }

                        else
                        {
                            result = - 1;
                        }
                       
                    }
                }

            }
            return result;
        }




        public static List<XYZ> getRoomVertices(Room room)
        {

            // List of boundary points 
            List<string> vertices = new List<string>(); 
            // Store coordiantes as a string list to ensure points are unique
            // then convert the unique ones into <XYZ
            List<XYZ> verts = new List<XYZ>(); // then convert into XYZ

            // Get list of vertices
            IList<IList<Autodesk.Revit.DB.BoundarySegment>> segments = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
            if (null != segments)  //the room may not be bound
            {
                foreach (IList<Autodesk.Revit.DB.BoundarySegment> segmentList in segments)
                {

                    foreach (Autodesk.Revit.DB.BoundarySegment boundarySegment in segmentList)
                    {

                        // Get curve start point
                        string start = XYZToString(boundarySegment.GetCurve().GetEndPoint(0));
                        if (!vertices.Contains(start))
                        {
                            vertices.Add(start);
                        }
                        // Get curve end point
                        string end = XYZToString(boundarySegment.GetCurve().GetEndPoint(1));
                        if (!vertices.Contains(end))
                        {
                            vertices.Add(end);
                        }

                    }
                }
            }


            // Convert to XYZ 
            foreach (string v in vertices)
            {
                verts.Add(StringtoXYZ(v));
            }
            return verts;



        }
    }

}

