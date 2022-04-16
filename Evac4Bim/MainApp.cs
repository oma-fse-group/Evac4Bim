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




            // Add CmdExportIfc 
            PushButtonData pbCmdExportIfc = createPushButton("CmdExportIfc.dll",
                "cmdExportbutton",
                "Evac4Bim.CmdExportIfc",
                "Export to\nIFC2x3",
                "CmdExportIfc.png");
            panel.AddItem(pbCmdExportIfc);


            // Add CmdImportParameter via split button            
            PushButtonData bResultsOne = createPushButton("CmdImportParameter.dll",
                "ImportParameters",
                "Evac4Bim.ImportParameters",
                "Import\nResults",
                "ImportParameters.png");

            PushButtonData bResultsTwo = createPushButton("CmdImportParameter.dll", "ImportParametersMultipleRuns",
              "Evac4Bim.ImportParametersMultipleRuns",
              "Import\nResults (Multi)",
              "ImportParameters.png");

            SplitButton splt_CmdImportParameter = panel3.AddItem(new SplitButtonData("spltCmdImportParameter", "Import Results")) as SplitButton;
            splt_CmdImportParameter.AddPushButton(bResultsOne);
            splt_CmdImportParameter.AddPushButton(bResultsTwo);


            // Add LaucnResults to the ribbon 
            PushButtonData pbCmdLaunchResults = createPushButton("CmdLaunchResults.dll",
                "CmdLaunchResults",
                "Evac4Bim.CmdLaunchResults",
                "Launch\nResutls\u2122",
                "CmdLaunchResults.png");
            panel3.AddItem(pbCmdLaunchResults);


            // Add CmdLoadParameters 
            PushButtonData pbCmdLoadParameters = createPushButton("CmdLoadParameters.dll",
                "CmdLoadParameters",
                "Evac4Bim.CmdLoadParameters",
                "Initialize\nProject",
                "CmdLoadParameters.png");
            panel2.AddItem(pbCmdLoadParameters);

            // Add CmdRenameItems 
            PushButtonData pbCmdRenameItems = createPushButton("CmdRenameItems.dll",
                "CmdRenameItems",
                "Evac4Bim.CmdRenameItems",
                "Initialize\nElements",
                "CmdRenameItems.png");
            panel2.AddItem(pbCmdRenameItems);

            // Add CmdEditOccupantProfiles 
            PushButtonData pbCmdEditOccupantProfiles = createPushButton("CmdEditOccupantProfiles.dll",
                "CmdEditOccupantProfiles",
                "Evac4Bim.CmdEditOccupantProfiles",
                "Edit Occupant\nProfiles",
                "CmdEditOccupantProfiles.png");
            panel2.AddItem(pbCmdEditOccupantProfiles);

            // Add CmdPlotCharts via split button            
            PushButtonData bOne = createPushButton("CmdPlotCharts.dll",
                "CmdPlotCharts",
                "Evac4Bim.CmdPlotCharts",
                "Room\nUsage",
                "CmdPlotCharts.png");

            PushButtonData bTwo = createPushButton("CmdPlotCharts.dll",
                "CmdPlotChartsTotals",
                "Evac4Bim.CmdPlotChartsTotals",
                "Total\nUsage",
                "CmdPlotCharts.png");

            PushButtonData bThree = createPushButton("CmdPlotCharts.dll",
                "CmdPlotChartStairs",
                "Evac4Bim.CmdPlotChartStairs",
                "Stair\nUsage",
                "CmdPlotCharts.png");

            PushButtonData bFour = createPushButton("CmdPlotCharts.dll",
                "CmdPlotChartsDoorFlowRate",
                "Evac4Bim.CmdPlotChartsDoorFlowRate",
                "Door\nFlowrate",
                "CmdPlotCharts.png");

            SplitButton splt_CmdPlotCharts = panel3.AddItem(new SplitButtonData("spltCmdPlotCharts", "Plot Results")) as SplitButton;
            splt_CmdPlotCharts.AddPushButton(bOne);
            splt_CmdPlotCharts.AddPushButton(bTwo);
            splt_CmdPlotCharts.AddPushButton(bThree);
            splt_CmdPlotCharts.AddPushButton(bFour);

            // Add CmdCreateSchedules 
            PushButtonData pbCmdCreateSchedules = createPushButton("CmdCreateSchedules.dll",
                "CmdCreateSchedules",
                "Evac4Bim.CmdCreateSchedules",
                "Schedule\nResults",
                "CmdCreateSchedules.png");
            panel3.AddItem(pbCmdCreateSchedules);

            // Add CmdResultAnimation 
            PushButtonData pbCmdResultAnimation = createPushButton("CmdResultAnimation.dll",
                "CmdResultAnimation",
                "Revit.SDK.Samples.ModelessForm_ExternalEvent.CS.Command",
                "Play\nResults",
                "CmdResultAnimation.png");
            panel3.AddItem(pbCmdResultAnimation);


            // Add CmdRenameItems 
            PushButtonData pbCmdBuildingGroup = createPushButton("CmdBuildingGroup.dll",
                "CmdBuildingGroup",
                "Evac4Bim.CmdBuildingGroup",
                "Building\nGroup",
                "CmdBuildingGroup.png");
            panel4.AddItem(pbCmdBuildingGroup);

            // Add CmdEditRoomFunction 
            PushButtonData pbCmdEditRoomFunction = createPushButton("CmdEditRoomFunction.dll",
                "CmdEditRoomFunction",
                "Evac4Bim.CmdEditRoomFunction",
                "Room\nFunction",
                "CmdEditRoomFunction.png");
            panel4.AddItem(pbCmdEditRoomFunction);

            // Add CmdEditParameters 
            PushButtonData pbCmdEditParameters = createPushButton("CmdEditParameters.dll",
                "CmdEditParameters",
                "Evac4Bim.CmdEditParameters",
                "Edit\nParameters",
                "CmdEditParameters.png");
            panel4.AddItem(pbCmdEditParameters);


            // Add CmdMakePaths via split button            
            PushButtonData bPathOne = createPushButton("CmdMakePaths.dll",
                "CmdSelectPreferredExit",
                "Evac4Bim.CmdSelectPreferredExit",
                "Assign\nExits",
                "CmdSelectPreferredExit.png");

            PushButtonData bPathTwo = createPushButton("CmdMakePaths.dll",
                "CmdMakePaths",
                "Evac4Bim.CmdMakePaths",
                "Draw Travel\npaths",
                "CmdMakePaths.png");

            PushButtonData bPathThree = createPushButton("CmdMakePaths.dll",
                "CmdSelectPreferredStair",
                "Evac4Bim.CmdSelectPreferredStair",
                "Assign\nStairway",
                "CmdSelectPreferredStair.png");

            PushButtonData bPathFour = createPushButton("CmdMakePaths.dll",
                "CmdAssignLinkedComponent",
                "Evac4Bim.CmdAssignLinkedComponent",
                "Link\nStairways",
                "CmdAssignLinkedComponent.png");

            SplitButton splt_CmdMakePaths = panel4.AddItem(new SplitButtonData("spltCmdMakePaths", "Travel Paths")) as SplitButton;
            splt_CmdMakePaths.AddPushButton(bPathOne);
            splt_CmdMakePaths.AddPushButton(bPathTwo);
            splt_CmdMakePaths.AddPushButton(bPathThree);
            splt_CmdMakePaths.AddPushButton(bPathFour);

            // Add CmdIBCCheck 
            PushButtonData pbCmdIBCCheck = createPushButton("CmdIBCCheck.dll",
                "CmdIBCCheck",
                "Evac4Bim.CmdIBCCheck",
                "Check IBC\nPrescriptions",
                "CmdIBCCheckButton.png");
            panel4.AddItem(pbCmdIBCCheck);


            return Result.Succeeded;


        }

        public PushButtonData createPushButton(string assemblyName, string name, string className, string text, string iconName)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + assemblyName;

            Uri pushButtonImgPath = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\icons\" + iconName);
            PushButtonData pushButtonData = new PushButtonData(name, text, path, className);
            pushButtonData.LargeImage = new BitmapImage(pushButtonImgPath);

            return pushButtonData;
        }

    }

}
