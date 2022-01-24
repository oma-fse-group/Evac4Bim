using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

/// <summary>
/// This class imports simulation results and stores in project parameters previously defined 
/// First, Pathfinder result json file is de-serialized in the class Pathfinder.PathfinderResultDeserializer
/// Then results are stored in class EvacSimModel.EvacSimModel which is the standard interface for storing simulation data
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
                return Result.Failed;
            }

            // Parse and deserialize JSON file

            // read file content
            StreamReader r = new StreamReader(localPath);
            string json = r.ReadToEnd();

           

            // Deserialize pathfinder result JSON object and import into the main  EvClass
            PathfinderResultDeserializer deserializedClass = JsonConvert.DeserializeObject<PathfinderResultDeserializer>(json);

            // Initialize main evac result class 
            EvacSimModel EvClass = deserializedClass.ImportPathfinderResults();


            // start transaction 
            var tx = new Transaction(doc);
            tx.Start("Export IFC");


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


            // building - proj info
            // save csv files at same location as revit project ! 

            // 0. Path to simulation files
            string path = Path.GetDirectoryName(localPath);

            // 1. create a sub folder @ project location 
            string projectDir = Path.GetDirectoryName(doc.PathName);
            string pathString = System.IO.Path.Combine(projectDir, "Evac.bak");
            System.IO.Directory.CreateDirectory(pathString);

            // 2. copy _rooms.csv file to that folder 
            string[] files = Directory.GetFiles(path, "*_rooms.csv"); // search for the file
            string p = files.First(); // only keep first occurence

            try
            {
                //contents = File.ReadAllText(p).Split('\n');          

                //TaskDialog.Show("Debug",p );
                File.Copy(p, Path.Combine(pathString, Path.GetFileName(p)) , true); 
            }
            catch
            {
                TaskDialog.Show("Error", "The results could not be copied");
                return Result.Failed;
            }



            // 3. save new path in the revit project
            Element projInfo = doc.ProjectInformation as Element;       
            projInfo.LookupParameter("ResultsFolderPath").Set(pathString);



            



            tx.Commit();
            return Result.Succeeded;
        }

        
    }




    

}





