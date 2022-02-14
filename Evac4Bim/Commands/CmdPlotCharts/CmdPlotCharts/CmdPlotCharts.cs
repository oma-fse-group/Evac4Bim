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
/// <summary>
/// This class plots various charts 
/// Data is retrieved from internal properties  
/// Data is parsed from the internal variables and passed to the external class "Figure" for plotting
/// 4 classes are defined : one for rooms - one for global data - one for door flowrates - one for stair usage 
/// </summary>
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

            // Init figure object
            Figure f = new Figure();
                     

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



            // check if a room was selected and nothing else !
            Element elem = doc.GetElement(selectedIds.First());

            if (elem.GetType().Name != "Room")
            {
                TaskDialog.Show("Error", "Please select a room");
                return Result.Failed;
            }
            
            // get the name of selected room
            string selectedRoomName = elem.LookupParameter("IfcName").AsString();
           
            
           

            //get room history data 
            double[] dataX =  new double[] { }; 
            double[] dataY =  new double[] { }; 
            string occupantCountHistory = elem.LookupParameter("OccupancyHistory").AsString();
            if (occupantCountHistory == "n.a" || occupantCountHistory == "")
            {
                TaskDialog.Show("Error", "No data available");
                return Result.Failed;
            }
            // Init transaction 
            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // Parse occupantCountHistory
            string[] rows = occupantCountHistory.Split(';');
            foreach (string r in rows)
            {
                string[] cols = r.Split(',');
                double t = Double.Parse(cols[0]);
                double rem = Double.Parse(cols[1]);
              
                dataX = dataX.Concat(new double[] { t }).ToArray();
                dataY = dataY.Concat(new double[] { rem }).ToArray();
            }

           
            string label = selectedRoomName;
                      
            string title = "Number of Occupants in room : "+ selectedRoomName;
            string XLabel = "Time in seconds";
            string YLabel = "Number of Occupants";
            // Init plotting from object and display
            f.initPlot(dataX, dataY, label, title, XLabel, YLabel); 

            f.Show();



            tx.Commit();
            return Result.Succeeded;
        }



    }

    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdPlotChartsDoorFlowRate : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            // Init figure object
            Figure f = new Figure();


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

                    TaskDialog.Show("Error", "Please select a door");
                    return Result.Failed;

                }

            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;

            }



            // check if a room was selected and nothing else !
            Element elem = doc.GetElement(selectedIds.First());
            if (elem.Category.Name != "Doors")
            {
                TaskDialog.Show("Error", "Please select a door");
                return Result.Failed;
            }

            // get the name of selected room
            string selectedName = elem.LookupParameter("IfcName").AsString();


            

            //get room history data 
            double[] dataX = new double[] { };
            double[] dataY = new double[] { };
            string occupantCountHistory = elem.LookupParameter("DoorFlowrateHistory").AsString();

            if (occupantCountHistory == "n.a" || occupantCountHistory == "")
            {
                TaskDialog.Show("Error", "No data available");
                return Result.Failed;
            }

            // Init transaction 
            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // Parse occupantCountHistory
            string[] rows = occupantCountHistory.Split(';');
            foreach (string r in rows)
            {
                string[] cols = r.Split(',');
                double t = Double.Parse(cols[0]);
                double rem = Double.Parse(cols[1]);

                dataX = dataX.Concat(new double[] { t }).ToArray();
                dataY = dataY.Concat(new double[] { rem }).ToArray();
            }


            string label = selectedName;

            string title = "Door Flowrate (Raw)";
            string XLabel = "Time in seconds";
            string YLabel = "Flow (pers/sec)";
            // Init plotting from object and display
            f.initPlot(dataX, dataY, label, title, XLabel, YLabel);

            f.Show();



            tx.Commit();
            return Result.Succeeded;
        }



    }

    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdPlotChartStairs : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            // Init figure object
            Figure f = new Figure();


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

                    TaskDialog.Show("Error", "Please select a stair");
                    return Result.Failed;

                }

            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;

            }



            // check if a room was selected and nothing else !
            Element elem = doc.GetElement(selectedIds.First());

            //TaskDialog.Show("Debug", elem.GetType().Name);

            if (elem.GetType().Name != "Stairs")
            {
                TaskDialog.Show("Error", "Please select a stair");
                return Result.Failed;
            }

            // get the name of selected room
            string selectedRoomName = elem.LookupParameter("IfcName").AsString();


            

            //get room history data 
            double[] dataX = new double[] { };
            double[] dataY = new double[] { };
            string occupantCountHistory = elem.LookupParameter("OccupancyHistory").AsString();

            if (occupantCountHistory == "n.a" || occupantCountHistory == "")
            {
                TaskDialog.Show("Error", "No data available");
                return Result.Failed;
            }

            // Init transaction 
            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // Parse occupantCountHistory
            string[] rows = occupantCountHistory.Split(';');
            foreach (string r in rows)
            {
                string[] cols = r.Split(',');
                double t = Double.Parse(cols[0]);
                double rem = Double.Parse(cols[1]);

                dataX = dataX.Concat(new double[] { t }).ToArray();
                dataY = dataY.Concat(new double[] { rem }).ToArray();
            }


            string label = selectedRoomName;

            string title = "Number of Occupants in stair : " + selectedRoomName;
            string XLabel = "Time in seconds";
            string YLabel = "Number of Occupants";
            // Init plotting from object and display
            f.initPlot(dataX, dataY, label, title, XLabel, YLabel);

            f.Show();



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
            Element projInfo = doc.ProjectInformation as Element;


           

            Figure f = new Figure();
            Figure f2 = new Figure();

            //get room history data 
            double[] dataX = new double[] { }; 
            double[] remaining = new double[] { };
            double[] exited = new double[] { };
            string occupantCountHistory = projInfo.LookupParameter("OccupancyHistoryOverall").AsString();

            if (occupantCountHistory == "n.a" || occupantCountHistory == "")
            {
                TaskDialog.Show("Error", "No data available");
                return Result.Failed;
            }

            // Init transaction 
            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // Parse occupantCountHistory
            string[] rows = occupantCountHistory.Split(';');
            foreach (string r in rows)
            {
                string[] cols = r.Split(',');
                double t = Double.Parse(cols[0]);
                double rem = Double.Parse(cols[1]);
                double ext = Double.Parse(cols[2]);
               
                dataX = dataX.Concat(new double[] { t }).ToArray();
                remaining = remaining.Concat(new double[] { rem }).ToArray();
                exited = exited.Concat(new double[] { ext }).ToArray();
            }



            
            string label = "Remaining (Total)";
            string title = "Number of Occupants in Total";
            string XLabel = "Time in seconds";
            string YLabel = "Number of Occupants";
            f.initPlot(dataX, remaining, label, title, XLabel, YLabel);

            f.Show();


            
            string label2 = "Exited (Total)";
            string title2 = "Number of Occupants Who Exited";
            string XLabel2 = "Time in seconds";
            string YLabel2 = "Number of Occupants";
            f2.initPlot(dataX, exited, label2, title2, XLabel2, YLabel2);

            f2.Show();




            tx.Commit();
            return Result.Succeeded;
        }



    }


    
        
    


    


}
