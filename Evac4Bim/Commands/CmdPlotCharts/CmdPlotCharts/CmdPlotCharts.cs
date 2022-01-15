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
    public class CmdPlotCharts  : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            Figure f = new Figure();

                                


            // select smth 
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            try
            {
                // Select some elements in Revit before invoking this command
                  

                // Get the element selection of current document.
                Selection selection = uidoc.Selection;
               

                if (0 == selectedIds.Count)
                {
                    // If no elements selected.
                    
                    TaskDialog.Show("Error", "Please select a room");
                    return Result.Failed;

                }
                 
            }
            catch (Exception e)
            {
                message = e.Message;                
                return Result.Failed;
                
            }

            // check if room was selected
            Element elem = doc.GetElement(selectedIds.First());

            if (elem.GetType().Name != "Room")
            {
                TaskDialog.Show("Error", "Please select a room");
                return Result.Failed;
            }
            
            string selectedRoomName = elem.LookupParameter("IfcName").AsString();

            // end of select smth

            // get the  csv file

            Element projInfo = doc.ProjectInformation as Element;
            string pth = projInfo.LookupParameter("ResultsFolderPath").AsString();
            string[] contents = null;        
            string[] files = Directory.GetFiles(pth, "*_rooms.csv"); // search for the file
            string path = files.First();
            

            try
            {
                contents = File.ReadAllText(path).Split('\n');

            }
            catch
            {
                TaskDialog.Show("Error", "The results file could not be opened");
                return Result.Failed;
            }


            var csv = from line in contents
                      where !String.IsNullOrEmpty(line)
                      select line.Split(',').ToArray();


            // get header
            List<string> header = csv.FirstOrDefault().ToList();

            // get time 
            List<string> time = CmdPlotChartsUtils.getColumn(0, csv.Skip(1));

            // get some room  "Room 3_217637"
            int idx = header.IndexOf("\""+ selectedRoomName+"\"");
            List<string> roomUsage = CmdPlotChartsUtils.getColumn(idx, csv.Skip(1));

            double[] dataX = CmdPlotChartsUtils.convertToDoubleArray(time);
            double[] dataY = CmdPlotChartsUtils.convertToDoubleArray(roomUsage);
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



    }


    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdPlotChartsTotals : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            Figure f = new Figure();
            Figure f2 = new Figure();

            
            // get the  csv file

            Element projInfo = doc.ProjectInformation as Element;
            string pth = projInfo.LookupParameter("ResultsFolderPath").AsString();
            string[] contents = null;
            string[] files = Directory.GetFiles(pth, "*_rooms.csv"); // search for the file
            string path = files.First();

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

                
            // get header
            List<string> header = csv.FirstOrDefault().ToList();

            // get time 
            List<string> time = CmdPlotChartsUtils.getColumn(0, csv.Skip(1));

            // get some room  "Room 3_217637"
            int idx = header.IndexOf("\"" + "Remaining (Total)" + "\"");
            List<string> roomUsage = CmdPlotChartsUtils.getColumn(idx, csv.Skip(1));

            double[] dataX = CmdPlotChartsUtils.convertToDoubleArray(time);
            double[] dataY = CmdPlotChartsUtils.convertToDoubleArray(roomUsage);
            string label = "Remaining (Total)";

            string title = "Number of Occupants in Total";
            string XLabel = "Time in seconds";
            string YLabel = "Number of Occupants";
            f.initPlot(dataX, dataY, label, title, XLabel, YLabel);

            f.Show();


            // get some room  "Room 3_217637"
            int idx2 = header.IndexOf("\"" + "Exited (Total)" + "\"");
            List<string> roomUsage2 = CmdPlotChartsUtils.getColumn(idx2, csv.Skip(1)); 
            
            double[] dataY2 = CmdPlotChartsUtils.convertToDoubleArray(roomUsage2);
            string label2 = "Excited (Total)";

            string title2 = "Number of Occupants Who Excited";
            string XLabel2 = "Time in seconds";
            string YLabel2 = "Number of Occupants";
            f2.initPlot(dataX, dataY2, label2, title2, XLabel2, YLabel2);

            f2.Show();

            var tx = new Transaction(doc);
            tx.Start("Export IFC");


            tx.Commit();
            return Result.Succeeded;
        }



    }



    public class CmdPlotChartsUtils
    {

        public static List<string> getColumn(int index, IEnumerable<string[]> csv)
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


        public static double[] convertToDoubleArray(List<string> list)
        {


            double[] array = new double[] { };



            foreach (string el in list)
            {

                array = array.Concat(new double[] { double.Parse(el) }).ToArray();


            }

            return array;
        }

    }


    


}
