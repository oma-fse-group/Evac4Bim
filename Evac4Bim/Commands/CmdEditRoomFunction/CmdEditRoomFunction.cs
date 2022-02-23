using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CmdEditRoomFunction;
using System.Windows.Forms;
using System.Reflection;
using Autodesk.Revit.UI.Selection;
/// <summary>
/// This class allows the user to define the function of a room 
/// Then, it changes related properties wich depend on the room function 
/// such as AreaPerOccupantSpace, load factor, ... 
/// Room functions are stored in a csv file 
/// </summary>
/// 
namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]

    public class CmdEditRoomFunction : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;


            // Retrieve selected rooms in the UI (if any) or throw an error 
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            try
            {
                // Select some elements in Revit before invoking this command


                // Get the element selection of current document.
                Selection selection = uidoc.Selection;

                // If no elements selected.
                if (0 == selectedIds.Count)
                {

                    TaskDialog.Show("Error", "Please select a room");
                    return Result.Failed;

                }

            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;

            }

            // check selection (only rooms accepted) 
            foreach (ElementId id in selectedIds)
            {
                Element elem = doc.GetElement(id);
                // check if a room was selected and nothing else !
                if (elem.GetType().Name != "Room")
                {
                    TaskDialog.Show("Error", "Please select a room");
                    return Result.Failed;
                }

            }

                // Load csv file 
                const string FUNCTION_LIST_FILE_NAME = @"\room-functions.csv";
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + FUNCTION_LIST_FILE_NAME;
            string[] contents = null; 
            try
            {
                contents = File.ReadAllText(path).Split('\n');

            }
            catch
            {
                TaskDialog.Show("Error", "The file <room-functions.csv> could not be opened");
                return Result.Failed;
            }

            // Parse CSV file 
            // Parse csv file
            var csv = from line in contents
                      where !String.IsNullOrEmpty(line)
                      select line.Split(',').ToArray();

            // get functions (first column)
            string[] items = getColumn(0, csv.Skip(1));
            string[] factors = getColumn(1, csv.Skip(1));

            // List of items for the combo box
            Figure f = new Figure(items,32,"Select the function of the room","Room function");

            DialogResult result =  f.ShowDialog();

            if (result == DialogResult.OK)
            {
                
                var tx = new Transaction(doc);
                tx.Start("Export IFC");


                // Edit room properties 

                foreach (ElementId id in selectedIds)
                {
                    Element elem = doc.GetElement(id);
                  
                    if (elem.LookupParameter("AreaPerOccupantSpace")== null || elem.LookupParameter("AreaPerOccupantSpace") == null || elem.LookupParameter("Category") == null )
                    {
                        TaskDialog.Show("Error", "Some project parameters appear to be missing. Try initialising the project first !");

                        tx.RollBack();
                        return Result.Failed;
                    }
                    
                    elem.LookupParameter("AreaPerOccupantSpace").Set(factors[f.selectedFunctionIndex].ToString());
                    
                    //TaskDialog.Show("Debug", items[f.selectedFunctionIndex]);
                    elem.LookupParameter("Category").Set(items[f.selectedFunctionIndex].ToString());

           

                    double area = UnitUtils.ConvertFromInternalUnits(elem.LookupParameter("Area").AsDouble(), UnitTypeId.SquareMeters);

                    // round up 
                    double load = Math.Ceiling(area / Double.Parse(factors[f.selectedFunctionIndex]));
                   
                    elem.LookupParameter("OccupancyNumberLimit").Set(load.ToString());


                }

                tx.Commit();    

            }

            if (result == DialogResult.Cancel)
            {
                //TaskDialog.Show("Debug", "Operation aborted");
                return Result.Cancelled;
            }
            
            return Result.Succeeded;


        }


        public static string[] getColumn(int index, IEnumerable<string[]> csv)
        {
            string[] res = null;

            if (index >= 0)
            {
                var columnQuery =
                from line in csv
                where !String.IsNullOrEmpty(line[index].ToString())
                select Convert.ToString(line[index]);

                
                res = columnQuery.ToArray();
            }

            return res;
        }


    }




}
