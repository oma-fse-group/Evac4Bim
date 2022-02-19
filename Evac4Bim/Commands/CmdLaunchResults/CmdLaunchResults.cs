using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
/// <summary>
/// This class enables the user to launch Pathfinder results from the Revit UI 
/// </summary>
namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.ReadOnly)]
    public class CmdLaunchResults : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            // Query the binary file (stored as project parameter)
            
            string fullPath = doc.ProjectInformation.LookupParameter("PathfinderResultsBinary").AsString();
            if (fullPath =="" || fullPath==null)
            {
                TaskDialog.Show("Error", "The path to PathfinderResults.exe was not specified in the project information");
                return Result.Failed;
            }

            // check if file exists 
            if (!File.Exists(fullPath))
            {
                TaskDialog.Show("Error", "PathfinderResults.exe was not found at the specified location. Try updating the file path in the project information ");
                return Result.Failed;
            }
            string PathfinderResultsFolder = Path.GetDirectoryName(fullPath);
            string PathfinderResultsBinary = Path.GetFileName(fullPath);
            
           

            // Query result file (prompt user)
            string pfrFilePath = "\"";
            FileOpenDialog d = new FileOpenDialog("PFR file |*.pfr");
            ItemSelectionDialogResult res = d.Show();
            if (res == ItemSelectionDialogResult.Confirmed)
            {
                ModelPath pth = d.GetSelectedModelPath();
                pfrFilePath += ModelPathUtils.ConvertModelPathToUserVisiblePath(pth)+ "\"";
            }
            else
            {
                return Result.Cancelled;
            }


            var proc1 = new ProcessStartInfo();     
            proc1.UseShellExecute = true;
            proc1.WorkingDirectory = PathfinderResultsFolder;
            proc1.FileName = PathfinderResultsBinary;
            proc1.Verb = "runas";
            proc1.Arguments = pfrFilePath;
            proc1.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(proc1);

 



            return Result.Succeeded;
        }
    }
}





