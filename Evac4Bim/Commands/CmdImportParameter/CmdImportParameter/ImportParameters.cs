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

            //localPath = @"C:\Users\Nazim\OneDrive - UGent\studies\Thesis\rvt samples\test_pathfinder\test2_summary.json";
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






            tx.Commit();
            return Result.Succeeded;
        }

        
    }




    

}





