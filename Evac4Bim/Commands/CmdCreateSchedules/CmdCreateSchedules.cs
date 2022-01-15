using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdCreateSchedules : IExternalCommand
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
            IList <Element> scheduleList = new FilteredElementCollector(doc)
                                    .OfClass(typeof(ViewSchedule))
                                    .ToElements();
            string roomScheduleName = "Room Schedule (Evac4Bim)";
            string doorScheduleName = "Door Schedule (Evac4Bim)";
            string projInfoScheduleName = "Project Schedule (Evac4Bim)";
            int roomScheduleNameIndex = 0;
            int doorScheduleNameIndex = 0;
            int projInfoScheduleNameIndex = 0;

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
                if (name.Contains(projInfoScheduleName))
                {
                    projInfoScheduleNameIndex++;
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
            if (roomScheduleNameIndex > 0)
            {
                projInfoScheduleName = projInfoScheduleName + " - " + projInfoScheduleNameIndex.ToString();
            }

            //TaskDialog.Show("Debug", roomScheduleNameIndex.ToString());


            List<string> roomParamList = new List<string> { "Level", "Name", "IfcName" , "Area", "RSET", "InitialOccupantNumber"};
            ViewSchedule roomSchedule = createSchedule(doc, roomParamList, roomScheduleName, BuiltInCategory.OST_Rooms);

            List<string> doorParamList = new List<string> { "Level", "IfcName",  "Room Name", "Function", "Width", "FlowAvg","FirstIn","LastOut","TotalUse", };
            ViewSchedule doorSchedule = createSchedule(doc, doorParamList, doorScheduleName, BuiltInCategory.OST_Doors);


            List<string> projInfoParamList = new List<string> { "Building Name", "Project Name", "ResultsFolderPath", "Author", "Client Name", "TotalRSET", "AvgWalkDistance", "MaxWalkDistance", "MinWalkDistance", };
            ViewSchedule projInfoSchedule = createSchedule(doc, projInfoParamList, projInfoScheduleName, BuiltInCategory.INVALID);





            // close transaction
            t.Commit();
                
           // set active view 
           uidoc.ActiveView = projInfoSchedule;

           //result
           return Result.Succeeded;
        }



        public ViewSchedule createSchedule(Document doc, List<string> paramList, string name, BuiltInCategory cat)
        {
            // Fields are ordered according to index in the paramList 

            ElementId categoryId = new ElementId(cat);
            ViewSchedule schedule = ViewSchedule.CreateSchedule(doc, categoryId);
            schedule.Name = name;

            SortedDictionary<int ,SchedulableField > fieldsDict = new SortedDictionary<int, SchedulableField>();
            
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



    }
}



