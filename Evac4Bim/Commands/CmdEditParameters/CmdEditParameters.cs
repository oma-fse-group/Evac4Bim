using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
/// <summary>
/// This class creates a schedule in the revit UI
/// This allows user to edit fields that are requried for the check
/// </summary>


namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdEditParameters : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            // transaction
            Transaction t = new Transaction(doc, "Create Schedule");
            t.Start();

            // check if schedule exists 
            // if so, increment name to prevent conflicts

            IList<Element> scheduleList = new FilteredElementCollector(doc)
                                    .OfClass(typeof(ViewSchedule))
                                    .ToElements();
            string roomScheduleName = "Edit Rooms Schedule (Evac4Bim)";
            string levelScheduleName = "Edit Levels Schedule (Evac4Bim)";
            string doorScheduleName = "Edit Doors Schedule (Evac4Bim)";
            string projInfoScheduleName = "Edit Building Schedule (Evac4Bim)";
            string stairScheduleName = "Stair Schedule (Evac4Bim)";

            int roomScheduleNameIndex = 0;
            int doorScheduleNameIndex = 0;
            int levelScheduleNameindex = 0;
            int projInfoScheduleNameIndex = 0;
            int stairScheduleNameIndex = 0;


            foreach (Element e in scheduleList)
            {
                string name = e.Name;

                if (name.Contains(roomScheduleName))
                {
                    roomScheduleNameIndex++;
                }
                if (name.Contains(doorScheduleName))
                {
                    doorScheduleNameIndex++;
                }
                if (name.Contains(levelScheduleName))
                {
                    levelScheduleNameindex++;
                }
                if (name.Contains(projInfoScheduleName))
                {
                    projInfoScheduleNameIndex++;
                }
                if (name.Contains(stairScheduleName))
                {
                    stairScheduleNameIndex++;
                }




            }
            if (roomScheduleNameIndex > 0)
            {
                roomScheduleName = roomScheduleName + " - " + roomScheduleNameIndex.ToString();
            }
            if (doorScheduleNameIndex > 0)
            {
                doorScheduleName = doorScheduleName + " - " + doorScheduleNameIndex.ToString();
            }
            if (levelScheduleNameindex > 0)
            {
                levelScheduleName = levelScheduleName + " - " + levelScheduleNameindex.ToString();
            }
            if (projInfoScheduleNameIndex > 0)
            {
                projInfoScheduleName = projInfoScheduleName + " - " + projInfoScheduleNameIndex.ToString();
            }
            if (stairScheduleNameIndex > 0)
            {
                stairScheduleName = stairScheduleName + " - " + stairScheduleNameIndex.ToString();
            }


            List<string> roomParamList = new List<string> { "Level", "Name", "Area", "Category", "isCorridor","ExcludeComponent", "AssignedExit", "OccupancyType", "AreaPerOccupantSpace", "OccupancyNumberLimit", "OccupancyNumberSpace", "EgressPathTravelDistance", "EgressCapacityRequirement","EgressCapacity","ExitCountRequirement","ExitCount","EgressCapacityAdequate", "ExitCountAdequate", "EgressCapacityBalance","OccupancyNumberExcess","EgressPathTravelDistanceExcess","DiagonalLength","EgressComponentsPlacement"  };
            ViewSchedule roomSchedule = createSchedule(doc, roomParamList, roomScheduleName, BuiltInCategory.OST_Rooms);
            // Add filters 
            ScheduleField f = FindField(roomSchedule, "isCorridor" );
            ScheduleFilter filter = new ScheduleFilter(f.FieldId, ScheduleFilterType.NotEqual,1);
            roomSchedule.Definition.AddFilter(filter);
            // Sorting 
            ScheduleSortGroupField sf1 = new ScheduleSortGroupField(FindField(roomSchedule, "Level").FieldId,ScheduleSortOrder.Ascending);
            ScheduleSortGroupField sf2 = new ScheduleSortGroupField(FindField(roomSchedule, "Name").FieldId, ScheduleSortOrder.Ascending);
            roomSchedule.Definition.AddSortGroupField(sf1);
            roomSchedule.Definition.AddSortGroupField(sf2);



            List<string> doorParamList = new List<string> { "Level", "IfcName", "From Room: Name", "Width", "Height", "DischargeExit", "FireExit","DimensionAdequate" };
            ViewSchedule doorSchedule = createSchedule(doc, doorParamList, doorScheduleName, BuiltInCategory.OST_Doors);
            // Sorting 
            ScheduleSortGroupField sf3 = new ScheduleSortGroupField(FindField(doorSchedule, "Level").FieldId, ScheduleSortOrder.Ascending);
            ScheduleSortGroupField sf4 = new ScheduleSortGroupField(FindField(doorSchedule, "From Room: Name").FieldId, ScheduleSortOrder.Ascending);
            ScheduleSortGroupField sf5 = new ScheduleSortGroupField(FindField(doorSchedule, "IfcName").FieldId, ScheduleSortOrder.Ascending);
            doorSchedule.Definition.AddSortGroupField(sf3);
            doorSchedule.Definition.AddSortGroupField(sf4);
            doorSchedule.Definition.AddSortGroupField(sf5);
            // Add filters 
            ScheduleField f2 = FindField(doorSchedule, "FireExit");
            ScheduleFilter filter2 = new ScheduleFilter(f2.FieldId, ScheduleFilterType.NotEqual,0);
            doorSchedule.Definition.AddFilter(filter2);




            List<string> levelParamList = new List<string> { "Name", "EntranceLevel", "OccupancyNumberBuilding", "OccupancyNumberStorey", "EgressCapacityRequirementStorey", "EgressCapacityStorey", "EgressCapacityAdequateStorey", "ExitCountRequirementStorey", "ExitCountStorey",   "ExitCountAdequateStorey", "EgressCapacityBalanceStorey", "StairCapacityRequirement", "StairCapacity", "StairCountRequirement", "StairCount", "StairCapacityAdequate", "StairCountAdequate", "StairCapacityBalance", "StairCapacityPerOccupant" };
            ViewSchedule levelSchedule = createSchedule(doc, levelParamList, levelScheduleName, BuiltInCategory.OST_Levels);
            // Sorting 
            ScheduleSortGroupField sf6 = new ScheduleSortGroupField(FindField(levelSchedule, "Name").FieldId, ScheduleSortOrder.Ascending);
            roomSchedule.Definition.AddSortGroupField(sf6);



            List<string> projInfoParamList = new List<string> { "Building Name", "OccupancyType", "EmergencyCommunication" , "SprinklerProtection" , "EgressCapacityPerOccupant" , "OccupancyNumberLimitSingleExitSpace" , "EgressPathTravelDistanceLimitLowOccupancy","EgressPathTravelDistanceLimitHighOccupancy","EgressPathTravelDistanceLimit","StairCapacityPerOccupant","OccupancyNumberBuilding","SprinklerProtectionRequirement","SprinklerProtectionLacking", "StairCountRequirementOverall", "StairCountContinuity", "StairCapacityRequirementOverall", "StairCapacityContinuity" };
            ViewSchedule projInfoSchedule = createSchedule(doc, projInfoParamList, projInfoScheduleName, BuiltInCategory.INVALID);

            List<string> stairParamList = new List<string> { "IfcName", "FireEgressStair", "Top Level", "Base Level", "Width","Actual Riser Height","Actual Tread Depth","RiserHeightAdequate","TreadLengthAdequate" };
            ViewSchedule stairSchedule = createSchedule(doc, stairParamList, stairScheduleName, BuiltInCategory.OST_Stairs);


            // close transaction
            t.Commit();

            // set active view 
            uidoc.ActiveView = doorSchedule;
            uidoc.ActiveView = levelSchedule;
            uidoc.ActiveView = projInfoSchedule;
            uidoc.ActiveView = roomSchedule;
            uidoc.ActiveView = stairSchedule;

            //result
            return Result.Succeeded;
        }



        public ViewSchedule createSchedule(Document doc, List<string> paramList, string name, BuiltInCategory cat)
        {
            // Fields are ordered according to index in the paramList 

            ElementId categoryId = new ElementId(cat);
            ViewSchedule schedule = ViewSchedule.CreateSchedule(doc, categoryId);
            schedule.Name = name;

            SortedDictionary<int, SchedulableField> fieldsDict = new SortedDictionary<int, SchedulableField>();

            foreach (SchedulableField sf in schedule.Definition.GetSchedulableFields())
            {
                string sfName = sf.GetName(schedule.Document);

                if (paramList.Contains(sfName))
                {
                    int index = paramList.IndexOf(sfName);
                    try
                    {
                        fieldsDict.Add(index, sf);
                    }
                    catch
                    {
                        // field already exists 
                        // do something 

                        // keep newest field
                        fieldsDict.Remove(index);
                        fieldsDict.Add(index, sf);

                    }

                }

            }

            // parse dictionnary to create fields 
            foreach (SchedulableField sf in fieldsDict.Values)
            {
                ScheduleField scheduleField = schedule.Definition.AddField(sf);
            }



            return schedule;
        }

        public static ScheduleField FindField(ViewSchedule schedule, string fieldName)
        {
            ScheduleDefinition definition = schedule.Definition;
            ScheduleField foundField = null;
            

            foreach (ScheduleFieldId fieldId in definition.GetFieldOrder())
            {
                foundField = definition.GetField(fieldId);
                if (foundField.GetName() == fieldName)
                {
                    return foundField;
                }
            }

            return null;
        }


    }
}



