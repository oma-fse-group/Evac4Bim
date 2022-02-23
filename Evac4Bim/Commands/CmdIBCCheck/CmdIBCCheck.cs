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
        private double EgressCapacityPerOccupant { get; set; }
        private double StairCapacityPerOccupant { get; set; }
        private int MaxOccupantLoadPerRoom_1006_2_1 { get; set; }
        private int SprinklerProtection { get; set; }
        private int EmergencyCommunication { get; set; }
        double ReqDistanceBetweenExitDoorsFactor { get; set; }
        private double EgressPathTravelDistanceLimitLowOccupancy { get; set; }
        private double EgressPathTravelDistanceLimitHighOccupancy { get; set; }
        private double EgressPathTravelDistanceLimit { get; set; }
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
            this.EgressCapacityPerOccupant = Double.Parse(projInfo.LookupParameter("EgressCapacityPerOccupant").AsString());
            this.StairCapacityPerOccupant = Double.Parse(projInfo.LookupParameter("StairCapacityPerOccupant").AsString());
            this.MaxOccupantLoadPerRoom_1006_2_1 = Int32.Parse(projInfo.LookupParameter("OccupancyNumberLimitSingleExitSpace").AsString());
            this.SprinklerProtection = projInfo.LookupParameter("SprinklerProtection").AsInteger();
            this.EmergencyCommunication = projInfo.LookupParameter("EmergencyCommunication").AsInteger();
            this.EgressPathTravelDistanceLimitLowOccupancy = Double.Parse(projInfo.LookupParameter("EgressPathTravelDistanceLimitLowOccupancy").AsString());
            this.EgressPathTravelDistanceLimitHighOccupancy = Double.Parse(projInfo.LookupParameter("EgressPathTravelDistanceLimitHighOccupancy").AsString());
            this.EgressPathTravelDistanceLimit = Double.Parse(projInfo.LookupParameter("EgressPathTravelDistanceLimit").AsString());
            // distance between two doors = factor * diagonal 
            // factor = {0.5 0.33}
            this.ReqDistanceBetweenExitDoorsFactor = 0.5;
            if (this.SprinklerProtection == 1 && this.EmergencyCommunication == 1)
            {
                ReqDistanceBetweenExitDoorsFactor = 0.33;
            }


            //TaskDialog.Show("Debug", "Exit  : " + EgressPathTravelDistanceLimitLowOccupancy.ToString());

            // Open transaction
            var tx = new Transaction(doc);
            tx.Start("Export IFC");


            //Querry rooms which are not corridors
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<Element> rooms = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).WhereElementIsNotElementType().Where(room => room.GetType() == typeof(Room)).Where(room => room.LookupParameter("isCorridor").AsInteger() == 0).Where(room => room.LookupParameter("ExcludeComponent").AsInteger() == 0).ToList();

            //Querry all doors in the model which are exits (can be a room exit or a discharge exit)
            IEnumerable<Element> doorsList = collector.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().Where(room => room.LookupParameter("FireExit").AsInteger() == 1);
            List<Element> roomExitList = doorsList.ToList();
            List<Element> dischargeExitList = doorsList.Where(door => door.LookupParameter("DischargeExit").AsInteger() == 1).ToList();

            // List of storeys 
            List<Element> storeys = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToList();

            // BuiltInCategory.OST_TextNotes 

            // check if emergency stairs are included in a multistorey stair => unpin 
            List<Element> multiStoreyStairs = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_MultistoryStairs).ToList();
            // ask for user permission 

           IBCCheckUtils.unpinStairs(multiStoreyStairs, this.doc);

            // List of stairs 
            List<Element> allStairs = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Stairs).ToList();

            List<Element> stairs = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Stairs).Where(stair => stair.LookupParameter("FireEgressStair").AsInteger() == 1).ToList();

            List<Element> travelPaths = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_PathOfTravelLines).ToList();

            //TaskDialog.Show("Debug", storeys.Count.ToString());

            /// 1 . Checking room egress capacity + occupant load 
           ibcCheckRooms(rooms, roomExitList);

            /// 2 . Check Building 
            ibcCheckBuilding();

            /// 3. Check storeys 
            ibcCheckStoreys(storeys, rooms, dischargeExitList);
            ibcCheckBuildingEgressCapacity(storeys, dischargeExitList);

           // /// 4. Check stairs 
           ibcCheckStairSystem(stairs, storeys);

            /// 5. Display/Highlight results 
            makeTextNotes(storeys,rooms);
            makeTextNotesBuildingSummary(storeys, rooms);
            highlightDoors(storeys, doorsList.ToList());
            highlightTravelPaths(travelPaths);
            highlightStairs(allStairs);

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
                int OccupancyNumberSpace = 0;
                string roomOccupantLoadStr = r.LookupParameter("OccupancyNumberSpace").AsString();
                if (roomOccupantLoadStr == "" || roomOccupantLoadStr == null)
                {
                    // if it is not set, use the IBC default value 
                    OccupancyNumberSpace = Int32.Parse(r.LookupParameter("OccupancyNumberLimit").AsString());
                    r.LookupParameter("OccupancyNumberSpace").Set(OccupancyNumberSpace.ToString());
                }
                else
                {
                    OccupancyNumberSpace = Int32.Parse(roomOccupantLoadStr);
                }
                double EgressCapacityRequirement = EgressCapacityPerOccupant * OccupancyNumberSpace;
                if (EgressCapacityRequirement < MIN_EXIT_DOOR_WIDTH)
                {
                    EgressCapacityRequirement = MIN_EXIT_DOOR_WIDTH; // avoid too smal values 
                }
                r.LookupParameter("EgressCapacityRequirement").Set(EgressCapacityRequirement.ToString());


                double EgressPathTravelDistance = -1;
                Double.TryParse(r.LookupParameter("EgressPathTravelDistance").AsString(), out EgressPathTravelDistance);

                // C.3 Compute the requried number of exits 
                int ExitCountRequirement = 1;
                if (OccupancyNumberSpace >= 1000) { ExitCountRequirement = 4; }
                else if (OccupancyNumberSpace < 1000 && OccupancyNumberSpace >= 500) { ExitCountRequirement = 3; }
                else if (OccupancyNumberSpace > MaxOccupantLoadPerRoom_1006_2_1) { ExitCountRequirement = 2; }
                else if (SprinklerProtection == 1)
                {
                    if (EgressPathTravelDistance > EgressPathTravelDistanceLimitLowOccupancy) { ExitCountRequirement = 2; }
                }
                else if (SprinklerProtection == 0)
                {
                    if (OccupancyNumberSpace > 30 && EgressPathTravelDistance > EgressPathTravelDistanceLimitLowOccupancy) { ExitCountRequirement = 2; }
                    else if (OccupancyNumberSpace <= 30 && EgressPathTravelDistance > EgressPathTravelDistanceLimitHighOccupancy) { ExitCountRequirement = 2; }

                }
                // else, it stays == 1
                r.LookupParameter("ExitCountRequirement").Set(ExitCountRequirement.ToString());

                // C.4 Loop through exit doors of current room
                int ExitCount = 0;
                double EgressCapacity = 0;
                List<double> widths = new List<double>();
                List<XYZ> doorLocations = new List<XYZ>();

                foreach (Element d in r_exits)
                {
                    FamilyInstance d_f = d as FamilyInstance;

                    // C.4.1 : Check if door has required minimum size 

                    double width = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Millimeters);
                    double height = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble(), UnitTypeId.Millimeters);

                    if (width == 0)
                    {
                        // could be a curtain door 
                        width = UnitUtils.ConvertFromInternalUnits(d.LookupParameter("Width").AsDouble(), UnitTypeId.Millimeters);
                    }
                    if (height == 0)
                    {
                        height = UnitUtils.ConvertFromInternalUnits(d.LookupParameter("Height").AsDouble(), UnitTypeId.Millimeters);
                    }



                    if (width >= MIN_EXIT_DOOR_WIDTH && height >= MIN_EXIT_DOOR_HEIGHT)
                    {
                        d.LookupParameter("DimensionAdequate").Set("True");
                    }
                    else
                    {
                        if (width == 0)
                        {
                            d.LookupParameter("DimensionAdequate").Set("Error. Width not found");

                        }
                        else
                        {
                            d.LookupParameter("DimensionAdequate").Set("False");

                        }
                    }


                    // C.4.2 : Append the number of exits belonging to this room 
                    ExitCount++;


                    // C.4.3 : Append the available egress capacity for this room
                    EgressCapacity += width;

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
                r.LookupParameter("ExitCount").Set(ExitCount.ToString());
                //ExitCountAdequate EgressCapacityBalance
                string ExitCountAdequate = "True";
                if (ExitCount < ExitCountRequirement)
                {
                    ExitCountAdequate = "False";
                }
                r.LookupParameter("ExitCountAdequate").Set(ExitCountAdequate);

                // C.4.3 : Check the available egress capacity for this room
                r.LookupParameter("EgressCapacity").Set(EgressCapacity.ToString());
                string EgressCapacityAdequate = "True";
                if (EgressCapacity < EgressCapacityRequirement)
                {
                    EgressCapacityAdequate = "False";
                }
                r.LookupParameter("EgressCapacityAdequate").Set(EgressCapacityAdequate);

                // C.5 Check if egress capacity is well balanced 
                // If, by substracting one of the doors, the residual capacity is still 50% or more of the initial capacity 
                // Total capacity 
                double total_w = widths.Sum();
                string EgressCapacityBalance = "True";

                if (ExitCount > 1)
                {
                    // only applicable if more than 1 exit
                    foreach (double w in widths)
                    {
                        double residual_w = total_w - w;
                        if (residual_w < 0.5 * total_w)
                        {
                            EgressCapacityBalance = "False";
                            break;
                        }
                    }
                }

                r.LookupParameter("EgressCapacityBalance").Set(EgressCapacityBalance);




                // C.6 Check if distances between exits are okay (if multiple exits)
                // At least two doors are separated by a distance superior to ReqDistanceBetweenExitDoors
                // Get the diagonal length of the room = distance between two furthest points 

                double roomDiagonal = IBCCheckUtils.GetRoomDiagonalDist(r as Room); // in millimeters
                double ReqDistanceBetweenExitDoors = ReqDistanceBetweenExitDoorsFactor * roomDiagonal;
                bool EgressComponentsPlacement = true; // by default - only one door

                //TaskDialog.Show("ReqDistanceBetweenExitDoors", ReqDistanceBetweenExitDoors.ToString());
                if (ExitCount > 1)
                {
                    EgressComponentsPlacement = false; // by default if multiple doors 

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
                                EgressComponentsPlacement = true;
                                break;
                            }


                        }
                        if (EgressComponentsPlacement)
                        {
                            break; // no need to look further
                        }

                    }

                    //TaskDialog.Show("Doors", EgressComponentsPlacement.ToString());

                }
                r.LookupParameter("DiagonalLength").Set(roomDiagonal.ToString());
                r.LookupParameter("EgressComponentsPlacement").Set(EgressComponentsPlacement.ToString());



                // C.7 check if occupant load is exceeded
                string OccupancyNumberExcess = "False";
                int OccupancyNumberLimit = Int32.Parse(r.LookupParameter("OccupancyNumberLimit").AsString());
                if (OccupancyNumberSpace > OccupancyNumberLimit)
                {
                    OccupancyNumberExcess = "True";
                }

                r.LookupParameter("OccupancyNumberExcess").Set(OccupancyNumberExcess);


                // C.8 chck travel distance             

                // EgressPathTravelDistance = Double.Parse(r.LookupParameter("EgressPathTravelDistance").AsString()); // in mm ! 
                string EgressPathTravelDistanceExcess = "False";
                if (EgressPathTravelDistance > EgressPathTravelDistanceLimit)
                {
                    EgressPathTravelDistanceExcess = "True";
                }
                r.LookupParameter("EgressPathTravelDistanceExcess").Set(EgressPathTravelDistanceExcess);


            }

        }
        public void ibcCheckBuilding()
        {
            // check if required sprinklers were provided
            string SprinklerProtectionLacking = "False"; // default
            string SprinklerProtectionRequirement = "False";


            if (EgressPathTravelDistanceLimit == -1)
            {
                SprinklerProtectionRequirement = "True";
                if (SprinklerProtection == 0)
                {
                    SprinklerProtectionLacking = "True";
                }

            }

            projInfo.LookupParameter("SprinklerProtectionLacking").Set(SprinklerProtectionLacking);
            projInfo.LookupParameter("SprinklerProtectionRequirement").Set(SprinklerProtectionRequirement);


        }
        public void ibcCheckStoreys(List<Element> storeys, List<Element> rooms, List<Element> dischargeExitList)
        {
            // Loop through storeys
            foreach (Element s in storeys)
            {

                ElementId levelID = s.Id;
                int OccupancyNumberStorey = 0;

                // find rooms belonging to each storey 
                List<Element> roomsInStorey = rooms.Where(room => room.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID).AsElementId() == levelID).ToList();
                // Querry doors from that level
                List<Element> dischargeDoorsInStorey = dischargeExitList.Where(door => door.LevelId == levelID).ToList();
                //TaskDialog.Show("Debug", dischargeDoorsInStorey.Count.ToString());

                // Loop through rooms to collect number of occupants
                foreach (Element r in roomsInStorey)
                {
                    // Increment number of occupant
                    OccupancyNumberStorey += Int32.Parse(r.LookupParameter("OccupancyNumberSpace").AsString());

                }
                // Update occupant load of storey 
                s.LookupParameter("OccupancyNumberStorey").Set(OccupancyNumberStorey.ToString());



                // C.2 Compute the required exit capacity 
                double EgressCapacityRequirementStorey = EgressCapacityPerOccupant * OccupancyNumberStorey;
                if (EgressCapacityRequirementStorey < MIN_EXIT_DOOR_WIDTH)
                {
                    EgressCapacityRequirementStorey = MIN_EXIT_DOOR_WIDTH;
                }
                s.LookupParameter("EgressCapacityRequirementStorey").Set(EgressCapacityRequirementStorey.ToString());

                // C.3 Compute the requried number of exits 
                int ExitCountRequirementStorey = 2;
                if (OccupancyNumberStorey > 1000) { ExitCountRequirementStorey = 4; }
                else if (OccupancyNumberStorey <= 1000 && OccupancyNumberStorey > 500) { ExitCountRequirementStorey = 3; }

                // else, it stays == 2
                s.LookupParameter("ExitCountRequirementStorey").Set(ExitCountRequirementStorey.ToString());

                // C.4 Loop through exit doors of current level
                int ExitCountStorey = 0;
                double EgressCapacityStorey = 0;
                List<double> widths = new List<double>();
                foreach (Element d in dischargeDoorsInStorey)
                {
                    FamilyInstance d_f = d as FamilyInstance;

                    // C.4.1 : Check if door has required minimum size 
                    // Door min size was already checked in ibcCheckRooms

                    // C.4.2 : Append the number of exits belonging to this storey 
                    ExitCountStorey++;


                    // C.4.3 : Append the available egress capacity for this storey


                    double width = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Millimeters);
                    double height = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble(), UnitTypeId.Millimeters);

                    if (width == 0)
                    {
                        // could be a curtain door 
                        width = UnitUtils.ConvertFromInternalUnits(d.LookupParameter("Width").AsDouble(), UnitTypeId.Millimeters);
                    }
                    if (height == 0)
                    {
                        height = UnitUtils.ConvertFromInternalUnits(d.LookupParameter("Height").AsDouble(), UnitTypeId.Millimeters);
                    }


                    EgressCapacityStorey += width;

                    //TaskDialog.Show("Debug", "Storey : " + s.Name +" - Width : " + width.ToString());

                    // C.5 Check if egress capacity is well balanced 
                    widths.Add(width);
                }

                // C.4.2 : Check the number of exits belonging to this storey
                s.LookupParameter("ExitCountStorey").Set(ExitCountStorey.ToString());
                string ExitCountAdequateStorey = "True";
                if (ExitCountStorey < ExitCountRequirementStorey)
                {
                    ExitCountAdequateStorey = "False";
                }
                s.LookupParameter("ExitCountAdequateStorey").Set(ExitCountAdequateStorey);

                // C.4.3 : Check the available egress capacity for this storey
                s.LookupParameter("EgressCapacityStorey").Set(EgressCapacityStorey.ToString());
                string EgressCapacityAdequateStorey = "True";
                if (EgressCapacityStorey < EgressCapacityRequirementStorey)
                {
                    EgressCapacityAdequateStorey = "False";
                }
                s.LookupParameter("EgressCapacityAdequateStorey").Set(EgressCapacityAdequateStorey);


                // C.5 Check if egress capacity is well balanced 
                // If, by substracting one of the doors, the residual capacity is still 50% or more of the initial capacity 
                // Total capacity 
                double total_w = widths.Sum();
                string EgressCapacityBalanceStorey = "True";

                if (ExitCountStorey > 1)
                {
                    // only applicable if more than 1 exit
                    foreach (double w in widths)
                    {
                        double residual_w = total_w - w;
                        if (residual_w < 0.5 * total_w)
                        {
                            EgressCapacityBalanceStorey = "False";
                            break;
                        }
                    }
                }

                s.LookupParameter("EgressCapacityBalanceStorey").Set(EgressCapacityBalanceStorey);


                // C.6 Check if distances between exits are okay (if multiple exits)
                // { ... }

            }
        }
        public void ibcCheckBuildingEgressCapacity(List<Element> storeys, List<Element> dischargeExitList)
        {
            int OccupancyNumberBuilding = 0;
            // Loop through storeys to find total occupant load of building
            foreach (Element s in storeys)
            {
                OccupancyNumberBuilding += Int32.Parse(s.LookupParameter("OccupancyNumberStorey").AsString());
            }



            // Update total occupant load of building
            projInfo.LookupParameter("OccupancyNumberBuilding").Set(OccupancyNumberBuilding.ToString());


            // Querry the discharge level 
            List<Element> dischargeStoreys = storeys.Where(s => s.LookupParameter("EntranceLevel").AsInteger() == 1).ToList();

            if (dischargeStoreys.Count > 0)
            {

                Element dischargeStorey = dischargeStoreys.First();   // Only one per building is allowed ! 

                ElementId levelID = dischargeStorey.Id;

                // Querry doors from that level
                List<Element> dischargeDoorsInStorey = dischargeExitList.Where(door => door.LevelId == levelID).ToList();

                // C.2 Compute the required exit capacity 
                double EgressCapacityRequirementStorey = EgressCapacityPerOccupant * OccupancyNumberBuilding;
                if (EgressCapacityRequirementStorey < MIN_EXIT_DOOR_WIDTH)
                {
                    EgressCapacityRequirementStorey = MIN_EXIT_DOOR_WIDTH;
                }
                dischargeStorey.LookupParameter("EgressCapacityRequirementStorey").Set(EgressCapacityRequirementStorey.ToString());

                // update OccupancyNumber of storey to include all occupants evacuating through discharge floor
                dischargeStorey.LookupParameter("OccupancyNumberStorey").Set(OccupancyNumberBuilding.ToString());

                // TaskDialog.Show("Debug", EgressCapacityPerOccupant.ToString() + " * " + OccupancyNumberBuilding.ToString() + " = " + EgressCapacityRequirementStorey.ToString());

                // C.3 Compute the requried number of exits 
                int ExitCountRequirementStorey = 2;
                if (OccupancyNumberBuilding > 1000) { ExitCountRequirementStorey = 4; }
                else if (OccupancyNumberBuilding <= 1000 && OccupancyNumberBuilding > 500) { ExitCountRequirementStorey = 3; }

                // else, it stays == 2
                dischargeStorey.LookupParameter("ExitCountRequirementStorey").Set(ExitCountRequirementStorey.ToString());

                // C.4 Loop through exit doors of current level
                int ExitCountStorey = 0;
                double EgressCapacityStorey = 0;
                List<double> widths = new List<double>();
                foreach (Element d in dischargeDoorsInStorey)
                {
                    FamilyInstance d_f = d as FamilyInstance;

                    // C.4.1 : Check if door has required minimum size 
                    // Door min size was already checked in ibcCheckRooms

                    // C.4.2 : Append the number of exits belonging to this level 
                    ExitCountStorey++;


                    // C.4.3 : Append the available egress capacity for this level

                    double width = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_WIDTH).AsDouble(), UnitTypeId.Millimeters);
                    double height = UnitUtils.ConvertFromInternalUnits(d_f.Symbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT).AsDouble(), UnitTypeId.Millimeters);

                    if (width == 0)
                    {
                        // could be a curtain door 
                        width = UnitUtils.ConvertFromInternalUnits(d.LookupParameter("Width").AsDouble(), UnitTypeId.Millimeters);
                    }
                    if (height == 0)
                    {
                        height = UnitUtils.ConvertFromInternalUnits(d.LookupParameter("Height").AsDouble(), UnitTypeId.Millimeters);
                    }

                    EgressCapacityStorey += width;

                    // C.5 Check if egress capacity is well balanced 
                    widths.Add(width);
                }

                // C.4.2 : Check the number of exits belonging to this room
                dischargeStorey.LookupParameter("ExitCountStorey").Set(ExitCountStorey.ToString());
                string ExitCountAdequateStorey = "True";
                if (ExitCountStorey < ExitCountRequirementStorey)
                {
                    ExitCountAdequateStorey = "False";
                }

                dischargeStorey.LookupParameter("ExitCountAdequateStorey").Set(ExitCountAdequateStorey);

                // C.4.3 : Check the available egress capacity for this level
                dischargeStorey.LookupParameter("EgressCapacityStorey").Set(EgressCapacityStorey.ToString());
                string EgressCapacityAdequateStorey = "True";
                if (EgressCapacityStorey < EgressCapacityRequirementStorey)
                {
                    EgressCapacityAdequateStorey = "False";
                }
                dischargeStorey.LookupParameter("EgressCapacityAdequateStorey").Set(EgressCapacityAdequateStorey);


                // C.5 Check if egress capacity is well balanced 
                // If, by substracting one of the doors, the residual capacity is still 50% or more of the initial capacity 
                // Total capacity 
                double total_w = widths.Sum();
                string EgressCapacityBalanceStorey = "True";

                if (ExitCountStorey > 1)
                {
                    // only applicable if more than 1 exit
                    foreach (double w in widths)
                    {
                        double residual_w = total_w - w;
                        if (residual_w < 0.5 * total_w)
                        {
                            EgressCapacityBalanceStorey = "False";
                            break;
                        }
                    }
                }

                dischargeStorey.LookupParameter("EgressCapacityBalanceStorey").Set(EgressCapacityBalanceStorey);


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

                storey.LookupParameter("StairCount").Set("n.a");
                storey.LookupParameter("StairCapacity").Set("n.a");
                storey.LookupParameter("StairCountRequirement").Set("n.a");
                storey.LookupParameter("StairCapacityRequirement").Set("n.a");
                storey.LookupParameter("StairCountAdequate").Set("n.a");
                storey.LookupParameter("StairCapacityAdequate").Set("n.a");
                storey.LookupParameter("StairCapacityBalance").Set("n.a");


                int OccupancyNumberStorey = Int32.Parse(storey.LookupParameter("OccupancyNumberStorey").AsString());

                if (storey.LookupParameter("EntranceLevel").AsInteger() != 1 && OccupancyNumberStorey > 0)
                {
                    /// 2.1 for each storey => Compute StairCountRequirement and StairCapacityRequirement - occupant number and min width  !
                    /* Required capacity :
                   - 7.6 mm per occupant
                   - 5.1 mm if building class is not H / I-2 and there is sprinkler + voice alarm
                   - §1011.2 : If Level.occupantLoad< 50 ⇒ minimum width 36 in (914 mm)                
                   - If Level.occupantLoad > 50 ⇒ minimum width 44 in (1118 mm)*/



                    /// 2.1.1 Check StairCapacityRequirement
                    double MinRequiredStairCapacity = 0;
                    // Compute MinRequiredStairCapacity
                    if (OccupancyNumberStorey < 50)
                    {
                        MinRequiredStairCapacity = 914; //mm
                    }
                    else
                    {
                        MinRequiredStairCapacity = 1118;
                    }


                    double StairCapacityRequirement = StairCapacityPerOccupant * OccupancyNumberStorey;
                    if (StairCapacityRequirement < MinRequiredStairCapacity)
                    {
                        StairCapacityRequirement = MinRequiredStairCapacity; // avoid too smal values 
                    }
                    storey.LookupParameter("StairCapacityRequirement").Set(StairCapacityRequirement.ToString());

                    /// 2.1.2 Check StairCountRequirement
                    int StairCountRequirement = 2;
                    if (OccupancyNumberStorey > 1000) { StairCountRequirement = 4; }
                    else if (OccupancyNumberStorey <= 1000 && OccupancyNumberStorey > 500) { StairCountRequirement = 3; }

                    // else, it stays == 2
                    storey.LookupParameter("StairCountRequirement").Set(StairCountRequirement.ToString());


                }



            }


            // TaskDialog.Show("Debug", stairs.Count.ToString());
            /// 1. Loop stairs 
            foreach (Stairs s in stairs)
            {

                StairsType s_FI = doc.GetElement(s.GetTypeId()) as StairsType;

                /// 1.1 Get stair width 
                double s_width = Math.Round(UnitUtils.ConvertFromInternalUnits(s_FI.MinRunWidth, UnitTypeId.Millimeters), 1); // mm
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
                    s.LookupParameter("RiserHeightAdequate").Set("True");
                }
                else
                {
                    s.LookupParameter("RiserHeightAdequate").Set("False");
                }
                if (MIN_TREAD_DEPTH <= s_treadDepth)
                {
                    s.LookupParameter("TreadLengthAdequate").Set("True");
                }
                else
                {
                    s.LookupParameter("TreadLengthAdequate").Set("False");
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


                // 1.4.1 For each serviced storey => append StairCount and StairCapacity
                // skip DISCHARGE LEVEL
                if (l.LookupParameter("EntranceLevel").AsInteger() != 1)
                {

                    int StairCount = 0;
                    int.TryParse(l.LookupParameter("StairCount").AsString(), out StairCount);
                    StairCount += 1;
                    l.LookupParameter("StairCount").Set(StairCount.ToString());

                    double StairCapacity = 0;
                    double.TryParse(l.LookupParameter("StairCapacity").AsString(), out StairCapacity);
                    StairCapacity += s_width;
                    l.LookupParameter("StairCapacity").Set(StairCapacity.ToString());

                    /// 2.3 Check StairCapacity is well balanced 
                    // ie No serving stair should have a width less than 50% of the required capacity
                    double StairCapacityRequirement = 0;
                    double.TryParse(l.LookupParameter("StairCapacityRequirement").AsString(), out StairCapacityRequirement);
                    string StairCapacityBalance = "True";
                    double reducedStairCapacity = StairCapacity - s_width; // if this particular exit was lost ! 
                    if (reducedStairCapacity < 0.5 * StairCapacityRequirement)
                    {
                        StairCapacityBalance = "False";

                    }
                    l.LookupParameter("StairCapacityBalance").Set(StairCapacityBalance);



                }

            }

            /// 2. Loop all storeys in the model
            /// (for later use) 2.3 find StairCountRequirementOverall and StairCapacityRequirementOverall (max values)
            int StairCountRequirementOverall = 0;
            double StairCapacityRequirementOverall = 0.0;

            foreach (Element storey in storeys)
            {
                int OccupancyNumberStorey = Int32.Parse(storey.LookupParameter("OccupancyNumberStorey").AsString());

                if (storey.LookupParameter("EntranceLevel").AsInteger() != 1 && OccupancyNumberStorey > 0)
                {

                    /// 2.2 Compare with StairCount and StairCapacity => StairCountAdequate and StairCapacityAdequate

                    int StairCountRequirement = 0;
                    int.TryParse(storey.LookupParameter("StairCountRequirement").AsString(), out StairCountRequirement);

                    double StairCapacityRequirement = 0;
                    double.TryParse(storey.LookupParameter("StairCapacityRequirement").AsString(), out StairCapacityRequirement);

                    int StairCount = 0;
                    int.TryParse(storey.LookupParameter("StairCount").AsString(), out StairCount);

                    double StairCapacity = 0;
                    double.TryParse(storey.LookupParameter("StairCapacity").AsString(), out StairCapacity);

                    string StairCapacityAdequate = "True";
                    if (StairCapacity < StairCapacityRequirement)
                    {
                        StairCapacityAdequate = "False";
                    }
                    storey.LookupParameter("StairCapacityAdequate").Set(StairCapacityAdequate);

                    string StairCountAdequate = "True";
                    if (StairCount < StairCountRequirement)
                    {
                        StairCountAdequate = "False";
                    }
                    storey.LookupParameter("StairCountAdequate").Set(StairCountAdequate);





                    //2.3 find StairCountRequirementOverall and StairCapacityRequirementOverall (max values)
                    if (StairCount > StairCountRequirementOverall)
                    {
                        StairCountRequirementOverall = StairCount;
                    }
                    if (StairCapacity > StairCapacityRequirementOverall)
                    {
                        StairCapacityRequirementOverall = StairCapacity;
                    }


                }

            }


            /// 3. Ensure capacity is maintained : 
            string StairCapacityContinuity = "True";
            string StairCountContinuity = "True";
            /// 3.1 Loop storeys again ! 
            foreach (Element storey in storeys)
            {
                int OccupancyNumberStorey = Int32.Parse(storey.LookupParameter("OccupancyNumberStorey").AsString());

                if (storey.LookupParameter("EntranceLevel").AsInteger() != 1 && OccupancyNumberStorey > 0)
                {
                    /// 3.1.1 For each storey => StairCount and StairCapacity ==  StairCountRequirementOverall and StairCapacityRequirementOverall
                    int StairCount = 0;
                    int.TryParse(storey.LookupParameter("StairCount").AsString(), out StairCount);

                    double StairCapacity = 0;
                    double.TryParse(storey.LookupParameter("StairCapacity").AsString(), out StairCapacity);

                    if (StairCapacity != StairCapacityRequirementOverall)
                    {
                        StairCapacityContinuity = "False";
                    }
                    if (StairCount != StairCountRequirementOverall)
                    {
                        StairCountContinuity = "False";
                    }



                }

            }


            // update proj info 
            doc.ProjectInformation.LookupParameter("StairCapacityContinuity").Set(StairCapacityContinuity);
            doc.ProjectInformation.LookupParameter("StairCountContinuity").Set(StairCountContinuity);
            doc.ProjectInformation.LookupParameter("StairCountRequirementOverall").Set(StairCountRequirementOverall.ToString());
            doc.ProjectInformation.LookupParameter("StairCapacityRequirementOverall").Set(StairCapacityRequirementOverall.ToString());


        }

        public void makeTextNotes(List<Element> storeys , List<Element>rooms)
        {
             foreach (Element s in storeys)
            {
                // get storey 
                Level l = s as Level;
                
                // make text 
                string msg = "";
                string temp = "n.a";
                if (1 == 1)
                {
                    msg += "< Summary of Prescriptions Check For Storey \""+ l.Name + "\" >";
                    msg += "\n\n\n";                    
                    ///
                    msg += "Occupants Served : ";
                    msg += l.LookupParameter("OccupancyNumberStorey").AsString();
                    msg += "\n\n";
                    ///
                    msg += "Discharge Level : ";
                    if (l.LookupParameter("EntranceLevel").AsInteger() == 1)
                    {
                        msg += "Yes";
                    }
                    else
                    {
                        msg += "No";
                    }
                    temp = "n.a";
                    msg += "\n\n";
                    ///
                    msg += "Egress Capacity : ";
                    temp = l.LookupParameter("EgressCapacityAdequateStorey").AsString();
                    if (temp == "True")
                    {
                        msg += "Pass \u2714";
                    }
                    else if (temp == "False")
                    {
                        msg += "Fail \u274C";
                    }
                    else
                    {
                        msg += "n.a";
                    }
                    temp = "n.a";
                    msg += "\n\n";
                    ///
                    msg += "Exit Count : ";
                    temp = l.LookupParameter("ExitCountAdequateStorey").AsString();
                    if (temp == "True")
                    {
                        msg += "Pass \u2714";
                    }
                    else if (temp == "False")
                    {
                        msg += "Fail \u274C";
                    }
                    else
                    {
                        msg += "n.a";
                    }
                    temp = "n.a";
                    msg += "\n\n";
                    ///
                    msg += "Egress Capacity Balance : ";
                    temp = l.LookupParameter("EgressCapacityBalanceStorey").AsString();
                    if (temp == "True")
                    {
                        msg += "Pass \u2714";
                    }
                    else if (temp == "False")
                    {
                        msg += "Fail \u274C";
                    }
                    else
                    {
                        msg += "n.a";
                    }
                    temp = "n.a";
                    msg += "\n\n";
                    ///
                    msg += "Stair Capacity : ";
                    temp = l.LookupParameter("StairCapacityAdequate").AsString();
                    if (temp == "True")
                    {
                        msg += "Pass \u2714";
                    }
                    else if (temp == "False")
                    {
                        msg += "Fail \u274C";
                    }
                    else
                    {
                        msg += "n.a";
                    }
                    temp = "n.a";
                    msg += "\n\n";
                    ///
                    msg += "Stair Count : ";
                    temp = l.LookupParameter("StairCountAdequate").AsString();
                    if (temp == "True")
                    {
                        msg += "Pass \u2714";
                    }
                    else if (temp == "False")
                    {
                        msg += "Fail \u274C";
                    }
                    else
                    {
                        msg += "n.a";
                    }
                    temp = "n.a";
                    msg += "\n\n";
                    msg += "Stair Capacity Balance : ";
                    temp = l.LookupParameter("StairCapacityBalance").AsString();
                    if (temp == "True")
                    {
                        msg += "Pass \u2714";
                    }
                    else if (temp == "False")
                    {
                        msg += "Fail \u274C";
                    }
                    else
                    {
                        msg += "n.a";
                    }
                    temp = "n.a";
                }


                // check if a text note was not defined previously 
                string textNoteId = l.LookupParameter("TextNoteStoreyID").AsString();
                TextNote note = null;
                int makeNew = 1; // by defautlt - make a new note


                if (textNoteId != "" && textNoteId != null)
                {
                    //a note was previously defined
                    // try to retrieve it 
                    note = doc.GetElement(new ElementId(int.Parse(textNoteId))) as TextNote;
                    // check if element exists 
                    if (note != null)
                    {
                        // update its text 
                        note.Text = msg;
                        makeNew = 0; // no need for a new one
                    }
                    else
                    {
                        // it must have been removed from UI 
                        makeNew = 1;

                    }                    
                }
                // else create a new one
                if (makeNew == 1)
                {
                   
                    // get the view associated with the floor s an id
                    ElementId viewId = l.FindAssociatedPlanViewId();
                    if (viewId == ElementId.InvalidElementId)
                    {
                        // do something
                        // skip
                        continue;
                    }
                    View v = doc.GetElement(viewId) as View;

                    // define origin 
                    XYZ origin = null;

                    // cannot access location/coordinates of floor or floor planes or view so,
                    // find a random room and place the note box on it - hoping the user will notice it :) 
                    Element r = rooms.Where(room => room.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID).AsElementId() == l.Id).ToList().First();
                    if (r != null)
                    {
                        origin = (r.Location as LocationPoint).Point;
                    }
                    else
                    {
                        origin = new XYZ(167, 76, l.Elevation); // throw a random location
                    }


                    // Edit options 
                    TextNoteOptions options = new TextNoteOptions();
                    options.HorizontalAlignment = HorizontalTextAlignment.Left;
                    options.TypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
                   
                    // Create the note 
                    note = TextNote.Create(doc, viewId, origin, msg, options);

                    // store element id for later processing 
                    l.LookupParameter("TextNoteStoreyID").Set(note.Id.IntegerValue.ToString());

                }



            }




        }

        public void makeTextNotesBuildingSummary(List<Element> storeys, List<Element> rooms)
        {
            // Querry the discharge level 
            List<Element> dischargeStoreys = storeys.Where(s => s.LookupParameter("EntranceLevel").AsInteger() == 1).ToList();

            if (dischargeStoreys.Count > 0)
            {

                Element dischargeStorey = dischargeStoreys.First();

                // get storey 
                Level l = dischargeStorey as Level;

                // make text 
                string msg = "";
                string temp = "n.a";
                if (1 == 1)
                {
                    msg += "< Summary of Prescriptions Check For the Building >";
                    msg += "\n\n\n";
                    ///
                    msg += "Name : ";
                    msg += projInfo.get_Parameter(BuiltInParameter.PROJECT_BUILDING_NAME).AsString();
                    msg += "\n\n";
                    ///
                    msg += "Occupancy Type : ";
                    msg += projInfo.LookupParameter("OccupancyType").AsString();
                    msg += "\n\n";
                    ///
                    msg += "Total Occupants : ";
                    msg += projInfo.LookupParameter("OccupancyNumberBuilding").AsString();
                    msg += "\n\n";
                    ///
                    msg += "Sprinkler System : ";
                    if (projInfo.LookupParameter("SprinklerProtection").AsInteger() == 1)
                    {
                        msg += "Yes";
                    }
                    else if (projInfo.LookupParameter("SprinklerProtection").AsInteger() == 0)
                    {
                        msg += "No";
                    }
                    else
                    {
                        msg += "n.a";
                    }
                    temp = "n.a";
                    msg += "\n\n";
                    ///
                    msg += "Alarm System : ";
                    if (projInfo.LookupParameter("EmergencyCommunication").AsInteger() == 1)
                    {
                        msg += "Yes";
                    }
                    else if (projInfo.LookupParameter("EmergencyCommunication").AsInteger() == 0)
                    {
                        msg += "No";
                    }
                    else
                    {
                        msg += "n.a";
                    }
                    temp = "n.a";
                    msg += "\n\n";
                    ///
                    msg += "Sprinklers Required : ";
                    temp = projInfo.LookupParameter("SprinklerProtectionRequirement").AsString();
                    if (temp == "True")
                    {
                        msg += "Yes";
                    }
                    else if (temp == "False")
                    {
                        msg += "No";
                    }
                    else
                    {
                        msg += "n.a";
                    }
                    temp = "n.a";
                    msg += "\n\n";
                }


                // check if a text note was not defined previously 
                string textNoteId = projInfo.LookupParameter("TextNoteBuildingID").AsString();
                TextNote note = null;
                int makeNew = 1; // by defautlt - make a new note


                if (textNoteId != "" && textNoteId != null)
                {
                    //a note was previously defined
                    // try to retrieve it 
                    note = doc.GetElement(new ElementId(int.Parse(textNoteId))) as TextNote;
                    // check if element exists 
                    if (note != null)
                    {
                        // update its text 
                        note.Text = msg;
                        makeNew = 0; // no need for a new one
                    }
                    else
                    {
                        // it must have been removed from UI 
                        makeNew = 1;

                    }
                }
                // else create a new one
                if (makeNew == 1)
                {

                    // get the view associated with the floor s an id
                    ElementId viewId = l.FindAssociatedPlanViewId();
                    if (viewId != ElementId.InvalidElementId)
                    {
                        
                        View v = doc.GetElement(viewId) as View;

                        // define origin 
                        XYZ origin = null;

                        // cannot access location/coordinates of floor or floor planes or view so,
                        // find a random room and place the note box on it - hoping the user will notice it :) 
                        Element r = rooms.Where(room => room.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID).AsElementId() == l.Id).ToList().First();
                        if (r != null)
                        {
                            origin = (r.Location as LocationPoint).Point;
                        }
                        else
                        {
                            origin = new XYZ(167, 76, l.Elevation); // throw a random location
                        }


                        // Edit options 
                        TextNoteOptions options = new TextNoteOptions();
                        options.HorizontalAlignment = HorizontalTextAlignment.Left;
                        options.TypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);

                        // Create the note 
                        note = TextNote.Create(doc, viewId, origin, msg, options);

                        // store element id for later processing 
                        projInfo.LookupParameter("TextNoteBuildingID").Set(note.Id.IntegerValue.ToString());
                    }
                    else
                    {
                        // do something
                        // skip
                    }

                }



            }


        }
        public void highlightDoors (List<Element> storeys,List<Element> doorsList)
        {
            Color green = new Color((byte)0, (byte)255, (byte)0);
            Color red = new Color((byte)255, (byte)0, (byte)0);

            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            ogs.SetProjectionLineColor(green);
            ogs.SetProjectionLineWeight(7);

            OverrideGraphicSettings default_ogs = new OverrideGraphicSettings();

            OverrideGraphicSettings ogs_error = new OverrideGraphicSettings();
            ogs_error.SetProjectionLineColor(red);
            ogs_error.SetProjectionLineWeight(7);

            foreach (Element s in storeys)
            {
                Level l = s as Level;
                ElementId levelID = s.Id;
 
                 
                // Querry doors from that level
                List<Element> doorsInStorey = doorsList.Where(door => door.LevelId == levelID).ToList();


                foreach (Element d in doorsInStorey)
                {
                    // get view 
                    // get the view associated with the floor s an id
                    ElementId viewId = l.FindAssociatedPlanViewId();
                    if (viewId == ElementId.InvalidElementId)
                    {
                        // do something
                        // skip
                        continue;
                    }
                    View v = doc.GetElement(viewId) as View;
                    // check if something is wrong with that door ! 
                    if (d.LookupParameter("DimensionAdequate").AsString() == "False")
                    {
                        v.SetElementOverrides(d.Id, ogs_error);
                    }
                    // if its fine AND it is a dischargeExit => highlight it
                    else if (d.LookupParameter("DischargeExit").AsInteger() == 1)
                    {
                        v.SetElementOverrides(d.Id, ogs);
                    }
                    // if its fine and NOT discharge exit => apply default style
                    else
                    {
                        v.SetElementOverrides(d.Id, default_ogs);
                    }


                }
            }
        }
    
        public void highlightTravelPaths (List<Element> travelPaths)
        {
            Color green = new Color((byte)0, (byte)200, (byte)0);
            Color red = new Color((byte)200, (byte)0, (byte)0);

            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            ogs.SetProjectionLineColor(green);
            ogs.SetProjectionLineWeight(10);

            OverrideGraphicSettings ogs_error = new OverrideGraphicSettings();
            ogs_error.SetProjectionLineColor(red);
            ogs_error.SetProjectionLineWeight(10);

            foreach (Element t in travelPaths)
            {
                PathOfTravel p = t as PathOfTravel;
                
                double length = UnitUtils.ConvertFromInternalUnits(p.LookupParameter("Length").AsDouble(), UnitTypeId.Millimeters);

                 

                ElementId viewId = p.OwnerViewId;
                View v = doc.GetElement(viewId) as View;

                if (length <= this.EgressPathTravelDistanceLimit)
                {
                    v.SetElementOverrides(p.Id, ogs);
                }
                else
                {
                    v.SetElementOverrides(p.Id, ogs_error);
                }

                


            }
        }

        public void highlightStairs(List<Element> stairs)
        {
            Color green = new Color((byte)0, (byte)200, (byte)0);
            Color red = new Color((byte)200, (byte)0, (byte)0);

            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            ogs.SetProjectionLineColor(green);
            ogs.SetProjectionLineWeight(7);

            OverrideGraphicSettings ogs_error = new OverrideGraphicSettings();
            ogs_error.SetProjectionLineColor(red);
            ogs_error.SetProjectionLineWeight(7);

            OverrideGraphicSettings default_ogs = new OverrideGraphicSettings();


            foreach (Element stair in stairs)
            {

                // find storey (top and base level ! )
                ElementId s_toplevel = stair.LookupParameter("Top Level").AsElementId();
                ElementId s_baselevel = stair.LookupParameter("Base Level").AsElementId();

                Level lTop = doc.GetElement(s_toplevel) as Level;
                Level lBase = doc.GetElement(s_baselevel) as Level;

                ElementId viewIdTop = lTop.FindAssociatedPlanViewId();
                ElementId viewIdBase = lBase.FindAssociatedPlanViewId();


                if (viewIdTop != ElementId.InvalidElementId && viewIdBase != ElementId.InvalidElementId)
                {
                    if (stair.LookupParameter("FireEgressStair").AsInteger() != 1)
                    {
                        View v1 = doc.GetElement(viewIdTop) as View;
                        v1.SetElementOverrides(stair.Id, default_ogs);

                        View v2 = doc.GetElement(viewIdBase) as View;
                        v2.SetElementOverrides(stair.Id, default_ogs);
                    }

                    else if (stair.LookupParameter("RiserHeightAdequate").AsString() == "True" && stair.LookupParameter("TreadLengthAdequate").AsString() == "True")
                    {
                        View v1 = doc.GetElement(viewIdTop) as View;
                        v1.SetElementOverrides(stair.Id, ogs);

                        View v2 = doc.GetElement(viewIdBase) as View;
                        v2.SetElementOverrides(stair.Id, ogs);
                    }
                    else
                    {
                        View v1 = doc.GetElement(viewIdTop) as View;
                        v1.SetElementOverrides(stair.Id, ogs_error);

                        View v2 = doc.GetElement(viewIdBase) as View;
                        v2.SetElementOverrides(stair.Id, ogs_error);
                    }
                    


                }

            }


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
                        List<ElementId> m_stairsIds = m.GetAllStairsIds().Where(sId => doc.GetElement(sId).LookupParameter("FireEgressStair").AsInteger() == 1).ToList();
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

