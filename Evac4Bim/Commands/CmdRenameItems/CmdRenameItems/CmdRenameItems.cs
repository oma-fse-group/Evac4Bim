using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

/// <summary>
/// This class sets the parameter "IfcName" for elements such as doors and rooms 
/// The name includes the id of the element in the Revit model
/// The name is stored in the ifc model and used to querry the elements when importing results 
/// </summary>

namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdRenameItems : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            // Open transaction
            var tx = new Transaction(doc);
            tx.Start("Export IFC");


            //Querry doors and rooms 
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            // collect elements
            IList<Element> doors = collector.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().ToElements();
            List<Element> rooms = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).WhereElementIsNotElementType().Where(room => room.GetType() == typeof(Room) ).ToList();


            //Loop and rename 

            // 1. Doors : Door_<Mark>_<elemID>
            int doorCounter = 0;
            foreach (Element d in doors)
            {
                Parameter ifcName = d.LookupParameter("IfcName");
                

                if (!ifcName.HasValue)
                {
                    ifcName.Set("Door_" + d.LookupParameter("Mark").AsString() + "_" + d.Id.ToString());
                    doorCounter++;
                }
                
            }


            // 2. Rooms : <RoomName>_<elemdID>
            int roomCounter = 0;
            foreach (Element r in rooms)
            {
                Parameter ifcName = r.LookupParameter("IfcName");

               // TaskDialog.Show("Debug", "Rooms found : " + ifcName.AsString());

                if (!ifcName.HasValue)
                {
                    ifcName.Set(r.LookupParameter("Name").AsString()+"_" + r.Id.ToString());
                    roomCounter++;
                }

            }


            TaskDialog.Show("Success", doorCounter.ToString()+" door(s) and "+ roomCounter +" room(s) " + "have been renamed");


            tx.Commit();
            return Result.Succeeded;
        }
    }
}
