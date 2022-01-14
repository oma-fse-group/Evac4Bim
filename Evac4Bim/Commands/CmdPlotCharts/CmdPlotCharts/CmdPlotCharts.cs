using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CmdPlotCharts;

namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdPlotCharts : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            Figure f = new Figure();




            // get the  csv file
            //string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + SHARED_PARAMETER_FILE_NAME;
            string path = @"C:\Users\Nazim\OneDrive - UGent\studies\Thesis\rvt samples\test_pathfinder\test2_rooms.csv";
            //TaskDialog.Show("Debug", path);
            string[] contents = null;

            try
            {
                contents = File.ReadAllText(path).Split('\n');

            }
            catch
            {
                TaskDialog.Show("Error", "The shared file could not be opened");
                return Result.Failed;
            }


            var csv = from line in contents
                      where !String.IsNullOrEmpty(line)
                      select line.Split(',').ToArray();


            // select smth 
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            try
            {
                // Select some elements in Revit before invoking this command

                // Get the handle of current document.
                

                // Get the element selection of current document.
                Selection selection = uidoc.Selection;
               

                if (0 == selectedIds.Count)
                {
                    // If no elements selected.
                    
                    TaskDialog.Show("Error", "Please select a room");
                    return Result.Failed;

                }
                else
                {
                    String info = "Ids of selected elements in the document are: ";
                    foreach (ElementId id in selectedIds)
                    {
                        info += "\n\t" + id.IntegerValue;
                    }

                    TaskDialog.Show("Revit", info);
                }
            }
            catch (Exception e)
            {
                message = e.Message;                
                return Result.Failed;
                
            }

            string selectedRoomName = doc.GetElement(selectedIds.First()).LookupParameter("IfcName").AsString();
           
        // end of select smth


        // get header
        List<string> header = csv.FirstOrDefault().ToList();

            // get time 
            List<string> time = getColumn(0, csv.Skip(1));

            // get some room  "Room 3_217637"
            int idx = header.IndexOf("\""+ selectedRoomName+"\"");
            List<string> roomUsage = getColumn(idx, csv.Skip(1));

            double[] dataX = convertToDoubleArray(time);
            double[] dataY = convertToDoubleArray(roomUsage);
            string label = "Room 4_217640";
                      
            string title = "Number of Occupants in Selected Rooms";
            string XLabel = "Time in seconds";
            string YLabel = "Number of Occupants";
            f.initPlot(dataX, dataY, label, title, XLabel, YLabel); 

            f.Show();

            var tx = new Transaction(doc);
            tx.Start("Export IFC");


            tx.Commit();
            return Result.Succeeded;
        }





        public List<string> getColumn(int index, IEnumerable<string[]> csv)
        {
            List<string> res = new List<string>(); 

            if (index >= 0)
            {
                var columnQuery =
                from line in csv
                where !String.IsNullOrEmpty(line[index].ToString())
                select Convert.ToString(line[index]);

                res = columnQuery.ToList();
            }



            return res;
        }


        public double[] convertToDoubleArray (List<string> list)
        {
            

            double[] array = new double[] {};

            

            foreach (string el in list)
            {
                
                array = array.Concat(new double[] { double.Parse(el) }).ToArray();

               
            }

            return array;
        }


    }
}
