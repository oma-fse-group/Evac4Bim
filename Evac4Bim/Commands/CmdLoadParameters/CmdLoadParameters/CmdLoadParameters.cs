﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Reflection;
using System.IO;
using Autodesk.Revit.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// This class loads shared parameters in the Revit project 
/// Shared parameters are stored in a CSV file which contains name, type and applicable category
/// Shared parameter file is stored at he location of the dll assembly
/// </summary>


namespace Evac4Bim
{

    [TransactionAttribute(TransactionMode.Manual)]

    public class CmdLoadParameters : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;
            // Get the BingdingMap of current document.
            BindingMap bindingMap = doc.ParameterBindings;

                        

             
            const string SHARED_PARAMETER_FILE_NAME = @"\shared-pramas-list.csv";

            // Parameter group in the Revit UI
            
            IDictionary<string, BuiltInParameterGroup> ParameterGroupDict = new Dictionary<string, BuiltInParameterGroup>();
            ParameterGroupDict.Add("PG_IFC", BuiltInParameterGroup.PG_IFC); 
            ParameterGroupDict.Add("PG_FIRE_PROTECTION", BuiltInParameterGroup.PG_FIRE_PROTECTION); 
        

            // create a dictionnary to convert category names (retrieved from csv file) into enumerations
            IDictionary<string, BuiltInCategory> BuiltInCategoryDict = new Dictionary<string, BuiltInCategory>();
            BuiltInCategoryDict.Add("OST_Doors", BuiltInCategory.OST_Doors);
            BuiltInCategoryDict.Add("OST_Rooms", BuiltInCategory.OST_Rooms);
            BuiltInCategoryDict.Add("OST_ProjectInformation", BuiltInCategory.OST_ProjectInformation);
            BuiltInCategoryDict.Add("OST_Levels", BuiltInCategory.OST_Levels);
            BuiltInCategoryDict.Add("OST_Stairs", BuiltInCategory.OST_Stairs);
             
  

            // create a dictionnary to convert param types (retrieved from csv file) into enumerations
            IDictionary<string, ForgeTypeId> ParameterTypeDict = new Dictionary<string, ForgeTypeId>();
            ParameterTypeDict.Add("YesNo", SpecTypeId.Boolean.YesNo);
            ParameterTypeDict.Add("TEXT", SpecTypeId.String.Text);
            ParameterTypeDict.Add("NUMBER", SpecTypeId.Number);
            ParameterTypeDict.Add("AREA", SpecTypeId.Area);
            ParameterTypeDict.Add("INTEGER", SpecTypeId.Int.Integer);
            ParameterTypeDict.Add("LENGTH", SpecTypeId.Length);
            ParameterTypeDict.Add("PERIOD", SpecTypeId.Period);
 
            string paramList = "";
            int paramCount = 0;

            // First check if a shared param txt file is already defined in Revit => overwrite it
            // Else, create one (ask user)
            DefinitionFile file = null;
            if (!File.Exists(app.SharedParametersFilename))
            {
                // if it does not exist 
                TaskDialog dialog = new TaskDialog("Decision");
                dialog.MainContent = "No shared parameter file is defined in the project.\nCreate one to proceed ?";
                dialog.AllowCancellation = true;
                dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

                TaskDialogResult result = dialog.Show();
                // Ask user to create a new shared param file in Revit and save its path in the project (app.SharedParametersFilename)
                if (result == TaskDialogResult.Yes)
                {
                    // Yes
                    FileSaveDialog d = new FileSaveDialog("Text files|*.txt");
                    ItemSelectionDialogResult res = d.Show();
                    if (res == ItemSelectionDialogResult.Confirmed)
                    {

                        ModelPath pth = d.GetSelectedModelPath();
                        string localPath = null;

                        localPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(pth);

                        // crate the blank file and save it 
                        System.IO.File.WriteAllLines(localPath, new string[0]);
                        app.SharedParametersFilename = localPath;

                    }
                    else
                    {
                        return Result.Failed;
                    }


                }
                else
                {
                    // No                    
                    return Result.Failed;
                }

            }


            // open the shared param file file
            file = app.OpenSharedParameterFile();


            // Initiate transaction 
            var tx = new Transaction(doc);
            tx.Start("Export IFC");


            // if the group "Evac" is not there, create it
            DefinitionGroup group = file.Groups.get_Item("Evac");
            if (group == null) group = file.Groups.Create("Evac");

            // get the definitions csv file
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + SHARED_PARAMETER_FILE_NAME;
            string[] contents = null;
            try
            {
                contents = File.ReadAllText(path).Split('\n');

            }
            catch
            {
                TaskDialog.Show("Error", "The shared file could not be opened");
                tx.RollBack();
                return Result.Failed;
            }

            // Parse csv file
            var csv = from line in contents
                      select line.Split(',').ToArray();

            // loop definitions 
            foreach (var row in csv.Skip(1)
                .TakeWhile(r => r.Length > 1 && r.Last().Trim().Length > 0))
            {
                // retrieve info from csv file
                string name = row[0]; // leftmost column                
                string type = row[1];
                string target = row[2];
                string paramGroup = row[3];
                string description = row[4];
                string user_modifiable = row[5];
                
                

                //TaskDialog.Show("Debug", type);

                // create definition 
                // create an instance definition in definition group 
                ExternalDefinitionCreationOptions option = new ExternalDefinitionCreationOptions(name, ParameterTypeDict[type]);
                option.UserModifiable = bool.Parse(user_modifiable);
                option.Description = description;
                Definition def = null;
                try
                {
                    // maybe the parameter already exists - but try to create it anyway
                    def = group.Definitions.Create(option);

                }
                catch
                {
                    // Already exists, retrieve its definition from shared file
                    //TaskDialog.Show("Debug", "Not included in shared file " + name);                     
                    def = group.Definitions.get_Item(name);

                }

                // create a category set and insert category of wall to it
                CategorySet cats = app.Create.NewCategorySet();
                // use BuiltInCategory to get category enum
                string[] Targets = target.Split(';');
                foreach (var t in Targets)
                {
                    Category cat = Category.GetCategory(doc, BuiltInCategoryDict[t]);
                    cats.Insert(cat);
                }


                //Create an instance of InstanceBinding
                InstanceBinding instanceBinding = app.Create.NewInstanceBinding(cats);

                
                // Bind the definitions to the document
                // but check if param already exists ! 


                if (ContainsParameterName(bindingMap, option.Name, ParameterGroupDict[paramGroup], option.GetDataType()))
                {
                    continue;

                }
                else
                {
                    bindingMap.Insert(def, instanceBinding, ParameterGroupDict[paramGroup]);

                }

                paramList += "\n" + name;
                paramCount++;
            }

            if (paramList == "")
            {
                TaskDialog.Show("Loading parameters", "Some parameters already exist in the project");
            }
            else
            {
                // TaskDialog.Show("Loading parameters", msg + paramList);
                TaskDialog.Show("Evac4Bim", "Project initialized. "+ paramCount.ToString() +" parameters were loaded");
            }

            
            // Terminate transaction
            tx.Commit();

            return Result.Succeeded;
        }
        /// <summary>
        /// Check if the parameter is already defined in the project (even if it is not defined in the shared file
        /// Revit allows definition of multiple params having same name (but different GUID). GUID cannot be accessed in this context
        /// If a parameter having same name, type and category already exists
        /// Then high chances it is our paramter - defined in a previous attempt maybe ?
        /// </summary>
        /// <param name="bindingMap"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        private static bool ContainsParameterName(BindingMap bindingMap, string paramName, BuiltInParameterGroup paramGroup, ForgeTypeId paramType)
        {
            DefinitionBindingMapIterator it = bindingMap.ForwardIterator();
            it.Reset();
            while (it.MoveNext())
            {
                if (it.Key.Name == paramName && it.Key.ParameterGroup == paramGroup && it.Key.GetDataType() == paramType)
                {
                    return true;
                    
                }


            }
            return false;


        }


    }
}
