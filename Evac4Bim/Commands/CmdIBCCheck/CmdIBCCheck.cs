using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;



namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdIBCCheck : IExternalCommand
    {
        // member properties
        private double ReqExitWidthPerOccupant { get; set; }
        private int MaxOccupantLoadPerRoom_1006_2_1 { get; set; }
        private int hasSprinklers { get; set; }
        private double MaxCommonEgressDistance_Min_1006_2_1 { get; set; }
        private double MaxCommonEgressDistance_Max_1006_2_1 { get; set; }
        private double MaxExitAccessTravelDistance_1017_2 { get; set; }
        private Element projInfo { get; set; }

        // Project constants
        public const double MIN_EXIT_DOOR_WIDTH = 812.99; // mm
        public const double MIN_EXIT_DOOR_HEIGHT = 2032; // mm

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;
            this.projInfo = doc.ProjectInformation as Element;

           

            // Init project parameters 
            this.ReqExitWidthPerOccupant = Double.Parse(projInfo.LookupParameter("ReqExitWidthPerOccupant").AsString());
            this.MaxOccupantLoadPerRoom_1006_2_1 = Int32.Parse(projInfo.LookupParameter("1006_2_1_MaxOccupantLoadPerRoom").AsString());
            this.hasSprinklers = projInfo.LookupParameter("hasSprinklers").AsInteger();
            this.MaxCommonEgressDistance_Min_1006_2_1 = Double.Parse(projInfo.LookupParameter("1006_2_1_MaxCommonEgressDistance_Min").AsString());
            this.MaxCommonEgressDistance_Max_1006_2_1 = Double.Parse(projInfo.LookupParameter("1006_2_1_MaxCommonEgressDistance_Max").AsString());
            this.MaxExitAccessTravelDistance_1017_2 = Double.Parse(projInfo.LookupParameter("1017_2_MaxExitAccessTravelDistance").AsString());


            //TaskDialog.Show("Debug", "Exit  : " + MaxCommonEgressDistance_Min_1006_2_1.ToString());

            // Open transaction
            var tx = new Transaction(doc);
            tx.Start("Export IFC");

                       
            //Querry rooms which are not corridors
            FilteredElementCollector collector = new FilteredElementCollector(doc); 
            List<Element> rooms = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).WhereElementIsNotElementType().Where(room => room.GetType() == typeof(Room)).Where(room => room.LookupParameter("isCorridor").AsInteger() == 0).ToList();

            //Querry all doors in the model which are exits (can be a room exit or a discharge exit)
            IEnumerable<Element> doorsList = collector.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().Where(room => room.LookupParameter("isExitDoor").AsInteger() == 1); 
            List<Element> roomExitList = doorsList.ToList();
             List<Element> dischargeExitList = doorsList.Where(door => door.LookupParameter("isDischargeExit").AsInteger() == 1).ToList();

            // List of storeys 
            List<Element> storeys = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToList();


            //TaskDialog.Show("Debug", storeys.Count.ToString());

            


            /// 1 . Checking room egress capacity + occupant load 
            ibcCheckRooms(rooms, roomExitList);

            /// 2 . Check Building 
            ibcCheckBuilding();

            /// 3. Check storeys 
            ibcCheckStoreys(storeys, rooms, dischargeExitList);
            ibcCheckBuildingEgressCapacity(storeys, dischargeExitList);


            tx.Commit();
            return Result.Succeeded;
        }

        


        // member methods
        public void ibcCheckRooms (List<Element> rooms, List<Element> roomExitList)
        {

            //Loop through rooms 

            foreach (Element r in rooms)
            {
                // C.1 Find doors belonging to current room
                string roomName = r.LookupParameter("Name").AsString();

                List<Element> r_exits = roomExitList.OfType<FamilyInstance>().Where(door => door.FromRoom.LookupParameter("Name").AsString() == roomName).OfType<Element>().ToList();

                // C.2 Compute the required exit capacity 
                int roomOccupantLoad = Int32.Parse(r.LookupParameter("RoomOccupantLoad").AsString());
                double ReqExitWidth = ReqExitWidthPerOccupant * roomOccupantLoad;
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
                foreach (Element d in r_exits)
                {
                    FamilyInstance d_f = d as FamilyInstance;

                    // C.4.1 : Check if door has required minimum size 

                    double width = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Millimeters);
                    double height = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble(), UnitTypeId.Millimeters);

                    //TaskDialog.Show("Debug", "Exit  : " + height.ToString());

                    if (width >= MIN_EXIT_DOOR_WIDTH && height >= MIN_EXIT_DOOR_HEIGHT)
                    {
                        d.LookupParameter("hasMinRequiredSize").Set("True");
                    }
                    else
                    {
                        d.LookupParameter("hasMinRequiredSize").Set("False");
                    }

                    // C.4.2 : Append the number of exits belonging to this room 
                    availableNumberOfExit++;


                    // C.4.3 : Append the available egress capacity for this room
                    availableEgressWidth += width;

                    // C.5 Check if egress capacity is well balanced 
                    widths.Add(width);
                }

                // C.4.2 : Append the number of exits belonging to this room
                r.LookupParameter("AvailableNumberOfExits").Set(availableNumberOfExit.ToString());
                //hasSufficientNumberOfExits hasBalancedEgressCapacity
                string hasSufficientNumberOfExits = "True";
                if (availableNumberOfExit < RequiredNumberOfExits)
                {
                    hasSufficientNumberOfExits = "False";
                }
                r.LookupParameter("hasSufficientNumberOfExits").Set(hasSufficientNumberOfExits);

                // C.4.3 : Append the available egress capacity for this room
                r.LookupParameter("AvailableEgressCapacity").Set(availableEgressWidth.ToString());
                string hasSufficientEgressCapacity = "True";
                if (availableEgressWidth < ReqExitWidth)
                {
                    hasSufficientEgressCapacity = "False";
                }
                r.LookupParameter("hasSufficientEgressCapacity").Set(hasSufficientEgressCapacity);



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

                r.LookupParameter("hasBalancedEgressCapacity").Set(hasBalancedEgressCapacity);


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

                // C.6 Check if distances between exits are okay (if multiple exits)
                // { ... }




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
                List<Element> roomsInStorey = rooms.Where(room => room.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID).AsElementId()  == levelID).ToList();
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

                    // C.4.2 : Append the number of exits belonging to this room
                    s.LookupParameter("AvailableNumberOfExitsLevel").Set(availableNumberOfExit.ToString());
                    string hasSufficientNumberOfExits = "True";
                    if (availableNumberOfExit < RequiredNumberOfExits)
                    {
                        hasSufficientNumberOfExits = "False";
                    }
                    s.LookupParameter("hasSufficientNumberOfExitsLevel").Set(hasSufficientNumberOfExits);

                    // C.4.3 : Append the available egress capacity for this room
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

                // C.4.2 : Append the number of exits belonging to this room
                dischargeStorey.LookupParameter("AvailableNumberOfExitsLevel").Set(availableNumberOfExit.ToString());
                string hasSufficientNumberOfExits = "True";
                if (availableNumberOfExit < RequiredNumberOfExits)
                {
                    hasSufficientNumberOfExits = "False";
                }
                dischargeStorey.LookupParameter("hasSufficientNumberOfExitsLevel").Set(hasSufficientNumberOfExits);

                // C.4.3 : Append the available egress capacity for this room
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



    }
}
