using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

/// <summary>
/// This class imports simulation results and stores in project parameters previously defined 
/// First, Pathfinder result json file is de-serialized in the class Pathfinder.PathfinderResultDeserializer
/// Then results are stored in class EvacSimModel.EvacSimModel which is the standard interface for storing simulation data
/// In case of multiple run simulation, the multiple files are retrieved and parsed => one instance of EvacSimModel is created per file then consolidated into a single instance by EvacSimModel.MergeMultipleRuns
/// In case of multiple run simulation, the multiple files are retrieved and parsed => one instance of EvacSimModel is created per file then consolidated into a single instance by EvacSimModel.MergeMultipleRuns
/// </summary>
namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class ImportParameters : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            // Import JSON file 
            string localPath = "";
            FileOpenDialog d = new FileOpenDialog("JSON file |*.json");
            ItemSelectionDialogResult res = d.Show();
            if (res == ItemSelectionDialogResult.Confirmed)
            {                               
                ModelPath pth = d.GetSelectedModelPath();
                localPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(pth);         
            }
            else
            {
                return Result.Cancelled;
            }

            // Parse and deserialize JSON file

            // read file content
            StreamReader r = new StreamReader(localPath);
            string json = r.ReadToEnd();

           
            // Deserialize pathfinder result JSON object and import into the main  EvClass
            PathfinderResultDeserializer deserializedClass = JsonConvert.DeserializeObject<PathfinderResultDeserializer>(json);


            string roomCsvFilePath = ImportUtils.returnCsvFileName(localPath, "_rooms.csv");
            string doorCsvPath = ImportUtils.returnCsvFileName(localPath, "_doors.csv");

             

            // start transaction 
            var tx = new Transaction(doc);
            tx.Start("Export IFC");
                     


            // Initialize main evac result class 
            EvacSimModel EvClass = deserializedClass.ImportPathfinderResults(roomCsvFilePath, doorCsvPath);

            // Parse results and write into Revit document
            bool result = EvClass.WriteIntoRevitModel(doc);

            if (result)
            {
                TaskDialog.Show("Importing results", "Results were imported succesfuly");
            }
            else
            {
                TaskDialog.Show("Importing results", "The applicaiton encountered an error. Results could not be imported");
            }


            tx.Commit();
            return Result.Succeeded;
        }

        
    }

    [TransactionAttribute(TransactionMode.Manual)]
    public class ImportParametersMultipleRuns : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            // Select folder

            OpenFileDialog folderBrowser = new OpenFileDialog();
            // Set validate names and check file exists to false otherwise windows will
            // not let you select "Folder Selection."
            folderBrowser.ValidateNames = false;
            folderBrowser.CheckFileExists = false;
            folderBrowser.CheckPathExists = true;
            // Always default to Folder Selection.
            folderBrowser.FileName = "Folder Selection";
            string folderPath = "";
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                folderPath = Path.GetDirectoryName(folderBrowser.FileName);
                // ...
            }
            else
            {
                return Result.Cancelled;
            }

            // Parse for summary file (*_summary.json)
            string[] files = Directory.GetFiles(folderPath, "*_summary.json");


            // Loop through files and generate a list of EvacSimModels (one per run)
            List<EvacSimModel> EvClassM = new List<EvacSimModel>();
            foreach (string file in files)
            {
                //TaskDialog.Show("Debug", file);

                // Deserialize JSON file
                // read file content
                StreamReader r = new StreamReader(file);
                string json = r.ReadToEnd();


                // Deserialize pathfinder result JSON object and import into the main  EvClass
                PathfinderResultDeserializer deserializedClass = JsonConvert.DeserializeObject<PathfinderResultDeserializer>(json);

                string roomCsvFilePath = ImportUtils.returnCsvFileName(file, "_rooms.csv");
                string doorCsvPath = ImportUtils.returnCsvFileName(file, "_doors.csv");

                // Initialize main evac result class 
                EvClassM.Add(deserializedClass.ImportPathfinderResults(roomCsvFilePath, doorCsvPath));


            }

            // Combine all EvacSimModels into one
            EvacSimModel mergedModel = EvacSimModel.MergeMultipleRuns(doc, EvClassM);

           
            // start transaction 
            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // write into Revit document 
            bool result = mergedModel.WriteIntoRevitModel(doc);

            if (result)
             {
                 TaskDialog.Show("Importing results (m)", "Results were imported succesfuly");
             }
             else
             {
                 TaskDialog.Show("Importing results", "The applicaiton encountered an error. Results could not be imported");
             }

            tx.Commit();


            return Result.Succeeded;
        }


    }

    public class ImportUtils
    {
        /***
         * Return the name of csv files based on the path to summary json file 
         * Replace _summary.json by the desired subst (e.g _rooms.csv)
         * */
        public static string returnCsvFileName (string localJsonPath, string csvFileSubst)
        {
            string jsonFileName = Path.GetFileName(localJsonPath);
            jsonFileName = jsonFileName.Remove(jsonFileName.LastIndexOf("_summary.json"));
            string folderName = Path.GetDirectoryName(localJsonPath);


            return folderName + "\\" + jsonFileName + csvFileSubst;
 
        }
    }




}





