using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;

/// <summary>
/// This class is the main entry point to the program 
/// It includes calls for different commands 
/// and it defines the UI in Revit
/// </summary>

namespace Evac4Bim
{
    public class MainApp : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            // Create ribbon
            application.CreateRibbonTab("Evac4Bim");
            string path2 = "";

            // Create ribbon panel
            RibbonPanel panel = application.CreateRibbonPanel("Evac4Bim", "Import/Export");
            // Create ribbon panel
            RibbonPanel panel2 = application.CreateRibbonPanel("Evac4Bim", "Tools");
            // Create ribbon panel
            RibbonPanel panel3 = application.CreateRibbonPanel("Evac4Bim", "Results");


            // Add a command to the ribbon 
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdExportIfc.dll";
            PushButtonData cmdExportButton = new PushButtonData("cmdExportbutton", "Export to\nIFC2x3", path2, "Evac4Bim.CmdExportIfc");
            // Create img icon 
            Uri cmdExportPushButtonImgPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdExportIfc.png");
            BitmapImage cmdExportPushButtonImg = new BitmapImage(cmdExportPushButtonImgPath);    
            PushButton cmdExportPushButton =  panel.AddItem(cmdExportButton) as PushButton;
            cmdExportPushButton.LargeImage = cmdExportPushButtonImg;



            // Add a command to the ribbon 
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdImportParameter.dll";
            PushButtonData CmdImportParametersButton = new PushButtonData("CmdImportParametersButton", "Pathfinder\u2122\nResults", path2, "Evac4Bim.ImportParameters");
            // Create img icon 
            Uri CmdImportParametersButtonImgPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\ImportParameters.png");
            BitmapImage CmdImportParametersButtonImg = new BitmapImage(CmdImportParametersButtonImgPath);
            PushButton CmdImportParametersPushButton = panel3.AddItem(CmdImportParametersButton) as PushButton;
            CmdImportParametersPushButton.LargeImage = CmdImportParametersButtonImg;


            // Add a command to the ribbon 
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdLoadParameters.dll";
            PushButtonData CmdLoadParametersButton = new PushButtonData("CmdLoadParametersButton", "Generate\nParameters", path2, "Evac4Bim.CmdLoadParameters");
            // Create img icon 
            Uri CmdLoadParametersButtonImgPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdLoadParameters.png");
            BitmapImage CmdLoadParametersButtonImg = new BitmapImage(CmdLoadParametersButtonImgPath);
            PushButton CmdLoadParametersPushButton = panel2.AddItem(CmdLoadParametersButton) as PushButton;
            CmdLoadParametersPushButton.LargeImage = CmdLoadParametersButtonImg;

            // Add a command to the ribbon 
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdRenameItems.dll";
            PushButtonData CmdRenameItemsButton = new PushButtonData("CmdRenameItemsButton", "Rename\nElements", path2, "Evac4Bim.CmdRenameItems");
            // Create img icon 
            Uri CmdRenameItemsButtonImgPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdRenameItems.png");
            BitmapImage CmdRenameItemsButtonImg = new BitmapImage(CmdRenameItemsButtonImgPath);
            PushButton CmdRenameItemsPushButton = panel2.AddItem(CmdRenameItemsButton) as PushButton;
            CmdRenameItemsPushButton.LargeImage = CmdRenameItemsButtonImg;


            // Add a command to the Split button
            string assembly = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdPlotCharts.dll"; 

            // create push buttons for split button drop down
            PushButtonData bOne = new PushButtonData("ButtonNameA", "Room\nUsage", assembly, "Evac4Bim.CmdPlotCharts");
            bOne.LargeImage = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdPlotCharts.png"));

            PushButtonData bTwo = new PushButtonData("ButtonNameB", "Total\nUsage", assembly, "Evac4Bim.CmdPlotChartsTotals");
            bTwo.LargeImage =  new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdPlotCharts.png"));
                        
            SplitButtonData sb1 = new SplitButtonData("splitButton1", "Results");
            SplitButton sb = panel3.AddItem(sb1) as SplitButton;
            sb.AddPushButton(bOne);
            sb.AddPushButton(bTwo);




            // Add a command to the ribbon 
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdCreateSchedules.dll";
            PushButtonData CmdCreateSchedulesButton = new PushButtonData("CmdCreateSchedules", "Create\nSchedules", path2, "Evac4Bim.CmdCreateSchedules");
            // Create img icon 
           
            Uri CmdCreateSchedulesButtonImgPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+ @"\icons\CmdCreateSchedules.png");
            BitmapImage CmdCreateSchedulesButtonImg = new BitmapImage(CmdCreateSchedulesButtonImgPath);
            PushButton CmdCreateSchedulesPushButton = panel3.AddItem(CmdCreateSchedulesButton) as PushButton;
            CmdCreateSchedulesPushButton.LargeImage = CmdCreateSchedulesButtonImg;

            return Result.Succeeded;




        }
    }
    
}
