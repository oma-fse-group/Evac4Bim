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
            // Create ribbon panel
            RibbonPanel panel4 = application.CreateRibbonPanel("Evac4Bim", "IBC Check");

            


            // Add a command to the ribbon 
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdExportIfc.dll";
            PushButtonData cmdExportButton = new PushButtonData("cmdExportbutton", "Export to\nIFC2x3", path2, "Evac4Bim.CmdExportIfc");
            // Create img icon 
            Uri cmdExportPushButtonImgPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdExportIfc.png");
            BitmapImage cmdExportPushButtonImg = new BitmapImage(cmdExportPushButtonImgPath);    
            PushButton cmdExportPushButton =  panel.AddItem(cmdExportButton) as PushButton;
            cmdExportPushButton.LargeImage = cmdExportPushButtonImg;

            // Add a command to the ribbon 
 
            // create push buttons for split button drop down
            string assembly = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdImportParameter.dll";

            PushButtonData bResultsOne = new PushButtonData("ImportParameters", "Import Pathfinder\u2122\nResults", assembly, "Evac4Bim.ImportParameters");
            bResultsOne.LargeImage = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\ImportParameters.png"));

            PushButtonData bResultsTwo = new PushButtonData("ImportParametersMultipleRuns", "Import Pathfinder\u2122\nResults (Multi)", assembly, "Evac4Bim.ImportParametersMultipleRuns");
            bResultsTwo.LargeImage = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\ImportParameters.png"));
                        

            SplitButtonData splt = new SplitButtonData("splt", "Results");
            SplitButton splt_b = panel3.AddItem(splt) as SplitButton;
            splt_b.AddPushButton(bResultsOne);
            splt_b.AddPushButton(bResultsTwo);


            // Add a command to the ribbon 
            assembly = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdLaunchResults.dll";
            PushButtonData CmdLaunchResultsButton = new PushButtonData("CmdLaunchResults", "Launch\nResutls\u2122", assembly, "Evac4Bim.CmdLaunchResults");
            // Create img icon 
            Uri CmdLaunchResultsButtonImgPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdLaunchResults.png");
            BitmapImage CmdLaunchResultsButtonImg = new BitmapImage(CmdLaunchResultsButtonImgPath);
            PushButton CmdLaunchResultsPushButton = panel3.AddItem(CmdLaunchResultsButton) as PushButton;
            CmdLaunchResultsPushButton.LargeImage = CmdLaunchResultsButtonImg;


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

            // Add a command to the ribbon 
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdEditOccupantProfiles.dll";
            PushButtonData CmdEditOccupantProfilesButton = new PushButtonData("CmdEditOccupantProfiles", "Edit Occupant\nProfiles", path2, "Evac4Bim.CmdEditOccupantProfiles");
            // Create img icon 
            Uri CmdEditOccupantProfilesPushButtonImgPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdEditOccupantProfiles.png");
            BitmapImage CmdEditOccupantProfilesPushButtonImg = new BitmapImage(CmdEditOccupantProfilesPushButtonImgPath);
            PushButton CmdEditOccupantProfilesPushButton = panel2.AddItem(CmdEditOccupantProfilesButton) as PushButton;
            CmdEditOccupantProfilesPushButton.LargeImage = CmdEditOccupantProfilesPushButtonImg;


            // Add a command to the Split button
            assembly = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdPlotCharts.dll"; 

            // create push buttons for split button drop down
            PushButtonData bOne = new PushButtonData("CmdPlotCharts", "Room\nUsage", assembly, "Evac4Bim.CmdPlotCharts");
            bOne.LargeImage = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdPlotCharts.png"));

            PushButtonData bTwo = new PushButtonData("CmdPlotChartsTotals", "Total\nUsage", assembly, "Evac4Bim.CmdPlotChartsTotals");
            bTwo.LargeImage =  new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdPlotCharts.png"));

            PushButtonData bThree = new PushButtonData("CmdPlotChartStairs", "Stair\nUsage", assembly, "Evac4Bim.CmdPlotChartStairs");
            bThree.LargeImage = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdPlotCharts.png"));

            PushButtonData bFour = new PushButtonData("CmdPlotChartsDoorFlowRate", "Door\nFlowrate", assembly, "Evac4Bim.CmdPlotChartsDoorFlowRate");
            bFour.LargeImage = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdPlotCharts.png"));

            SplitButtonData sb1 = new SplitButtonData("splitButton1", "Results");
            SplitButton sb = panel3.AddItem(sb1) as SplitButton;
            sb.AddPushButton(bOne);
            sb.AddPushButton(bTwo);
            sb.AddPushButton(bThree);
            sb.AddPushButton(bFour);




            // Add a command to the ribbon 
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdCreateSchedules.dll";
            PushButtonData CmdCreateSchedulesButton = new PushButtonData("CmdCreateSchedules", "Create\nSchedules", path2, "Evac4Bim.CmdCreateSchedules");
            // Create img icon 
           
            Uri CmdCreateSchedulesButtonImgPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+ @"\icons\CmdCreateSchedules.png");
            BitmapImage CmdCreateSchedulesButtonImg = new BitmapImage(CmdCreateSchedulesButtonImgPath);
            PushButton CmdCreateSchedulesPushButton = panel3.AddItem(CmdCreateSchedulesButton) as PushButton;
            CmdCreateSchedulesPushButton.LargeImage = CmdCreateSchedulesButtonImg;

            
            // Building group 
            // Add a command to the ribbon 
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdBuildingGroup.dll";
            PushButtonData CmdBuildingGroupButton = new PushButtonData("CmdBuildingGroup", "Building\nGroup", path2, "Evac4Bim.CmdBuildingGroup");
            // Create img icon 
            BitmapImage CmdBuildingGroupPushButtonImg = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdBuildingGroup.png"));
            PushButton CmdBuildingGroupPushButton = panel4.AddItem(CmdBuildingGroupButton) as PushButton;
            CmdBuildingGroupPushButton.LargeImage = CmdBuildingGroupPushButtonImg;

             // Room function 
             path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdEditRoomFunction.dll";
             PushButtonData CmdEditRoomFunctionButton = new PushButtonData("CmdEditRoomFunction", "Room\nFunction", path2, "Evac4Bim.CmdEditRoomFunction");
             // Create img icon 
             BitmapImage CmdEditRoomFunctionPushButtonImg = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdEditRoomFunction.png"));
             PushButton CmdEditRoomFunctionPushButton = panel4.AddItem(CmdEditRoomFunctionButton) as PushButton;
             CmdEditRoomFunctionPushButton.LargeImage = CmdEditRoomFunctionPushButtonImg;

            
             // Edit parameters 
             path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdEditParameters.dll";
             PushButtonData CmdEditParametersButton = new PushButtonData("CmdEditParameters", "Edit\nParameters", path2, "Evac4Bim.CmdEditParameters");
             BitmapImage CmdEditParametersPushButtonImg = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdEditParameters.png"));
             PushButton CmdEditParametersPushButton = panel4.AddItem(CmdEditParametersButton) as PushButton;
             CmdEditParametersPushButton.LargeImage = CmdEditParametersPushButtonImg;


            // Load travel distances
            // Add a command to the ribbon 

            // create push buttons for split button drop down
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdMakePaths.dll";

            PushButtonData bPathOne = new PushButtonData("CmdSelectPreferredExit", "Assign\nExits", path2, "Evac4Bim.CmdSelectPreferredExit");
            bPathOne.LargeImage = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdSelectPreferredExit.png"));

            PushButtonData bPathTwo = new PushButtonData("CmdMakePaths", "Draw Travel\npaths", path2, "Evac4Bim.CmdMakePaths");
            bPathTwo.LargeImage = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdMakePaths.png"));


            SplitButtonData spltPath = new SplitButtonData("spltPath", "Paths");
            SplitButton spltPathB = panel4.AddItem(spltPath) as SplitButton;
            spltPathB.AddPushButton(bPathTwo);
            spltPathB.AddPushButton(bPathOne);



            // IBC Check
            path2 = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\CmdIBCCheck.dll";
            PushButtonData CmdIBCCheckButton = new PushButtonData("CmdIBCCheck", "Check IBC\nPrescriptions", path2, "Evac4Bim.CmdIBCCheck");
            // Create img icon 
            BitmapImage CmdIBCCheckPushButtonImg = new BitmapImage(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\CmdIBCCheckButton.png"));
            PushButton CmdIBCCheckPushButton = panel4.AddItem(CmdIBCCheckButton) as PushButton;
            CmdIBCCheckPushButton.LargeImage = CmdIBCCheckPushButtonImg;

            


            return Result.Succeeded;


        }
    }
    
}
