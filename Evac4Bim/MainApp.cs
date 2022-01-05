using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Media.Imaging;

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

            string path = Assembly.GetExecutingAssembly().Location;
             

            // Add a command to the ribbon 
            PushButtonData cmdExportButton = new PushButtonData("cmdExportbutton", "Export to IFC2x3", path, "Evac4Bim.CmdExportIfc");

            // Create img icon 
            Uri cmdExportPushButtonImgPath = new Uri(@"D:\revit_api\Evac4Bim\CmdExportIfc.png");
            BitmapImage cmdExportPushButtonImg = new BitmapImage(cmdExportPushButtonImgPath);

            // Create ribbon panel
            RibbonPanel panel = application.CreateRibbonPanel("Evac4Bim","Commands");
            
            PushButton cmdExportPushButton =  panel.AddItem(cmdExportButton) as PushButton;
            cmdExportPushButton.LargeImage = cmdExportPushButtonImg;


            return Result.Succeeded;




        }
    }
    
}
