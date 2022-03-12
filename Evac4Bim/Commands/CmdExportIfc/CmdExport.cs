using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
/// <summary>
/// This class provides a shortcut for calling the IFC exporter 
/// The IFC exporter must be loaded into Revit - at startup - through a separate addin !
/// This class calls the exporter with pre-defiend parameters 
/// It will intercept the export and fill empty fields with default values (retrieved from the shared params file)
/// </summary>
namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdExportIfc : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;


            FileSaveDialog d = new FileSaveDialog("IFC files|*.ifc");
            ItemSelectionDialogResult res = d.Show();

            var tx = new Transaction(doc);
            tx.Start("Export IFC");


            if (res == ItemSelectionDialogResult.Confirmed)
            {

                ModelPath path = d.GetSelectedModelPath();
                string localPath = null;

                localPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(path);

                localPath = Path.GetDirectoryName(localPath);

                //TaskDialog.Show("f", localPath);


                // before exporting - fill empty fields
                // ask the user 
                TaskDialog dialog = new TaskDialog("Decision");
                dialog.MainContent = "Do you want to save empty fields with default value ?\nNote : The parameters in the Revit environment will not be affected.";
                dialog.AllowCancellation = false;
                dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

                TaskDialogResult result = dialog.Show();

                

                // Ask user to create a new shared param file in Revit and save its path in the project (app.SharedParametersFilename)
                if (result == TaskDialogResult.Yes)
                {
                    if (res == ItemSelectionDialogResult.Confirmed)
                    {
                        initEmptyFields(doc);

                    }
                    else
                    {
                        // do nothing
                    }

                }

                // Now call the exporter 
               ExportToIfc(doc, localPath);
            }
            else
            {
                tx.RollBack();
                return Result.Failed;
            }

            tx.RollBack();
            // rollback to preserve the environment and not alter it with default values ! 
            // rollback to preserve the environment and not alter it with default values ! 
            return Result.Succeeded;
        }

        /// <summary>
        ///     Export current view to IFC for
        /// </summary>
        private static Result ExportToIfc(Document doc, string localPath)
        {
            var r = Result.Failed;
                     
            var save_path = localPath;

           

            IFCExportOptions opt = new IFCExportOptions();
            opt.FileVersion = IFCVersion.IFC2x3;
            opt.WallAndColumnSplitting = true;
            opt.ExportBaseQuantities = false;
            //opt.FilterViewId = v.Id;
            opt.AddOption("IFCVersion", "IFC 2x3 Coordination View 2.0");
            opt.AddOption("ExportInternalRevitPropertySets", "false");
            opt.AddOption("ExportAnnotations ", "false");
            opt.AddOption("SpaceBoundaries ", "0");
            opt.AddOption("VisibleElementsOfCurrentView ", "false");
            opt.AddOption("Use2DRoomBoundaryForVolume ", "false");
            opt.AddOption("UseFamilyAndTypeNameForReference ", "false");            
            opt.AddOption("ExportIFCCommonPropertySets", "true");
            opt.AddOption("Exportuserdefinedpropertysets", "false");
            opt.AddOption("Export2DElements", "false");
            opt.AddOption("ExportPartsAsBuildingElements", "false");
            opt.AddOption("ExportBoundingBox", "false");
            opt.AddOption("ExportSolidModelRep", "false");
            opt.AddOption("ExportSchedulesAsPsets", "false");
            opt.AddOption("ExportLinkedFiles", "false");
            opt.AddOption("IncludeSiteElevation", "false");
            opt.AddOption("UseActiveViewGeometry", "false");
            opt.AddOption("ExportSpecificSchedules", "false");
            opt.AddOption("TessellationLevelOfDetail", "0.5");
            //opt.AddOption("StoreIFCGUID", "true");


            try
            {
                doc.Export(save_path, doc.Title, opt);
                r = Result.Succeeded;
            }
            catch
            {
                TaskDialog.Show("Error ","File could not be saved");
                r = Result.Failed;
            }

          

            

            return r;
        }
    
    /// <summary>
    /// Retrieve the definitions of parameters 
    /// Store parameters in specific list for each Revit element (rooms, doors..etc) 
    /// Loop all building element instances 
    /// For each instance, loop every definition in the related list 
    /// Check if the definition is initialized (lookupparameter) 
    /// if not, set the parameter
    /// </summary>
    /// <param name="doc"></param>
    /// <returns></returns>
        private static Result initEmptyFields(Document doc)
        {
            const string SHARED_PARAMETER_FILE_NAME = @"\shared-pramas-list.csv";

            // Store definitions 
            List<ParamDefinition> roomDefs = new List<ParamDefinition>();
            List<ParamDefinition> doorDefs = new List<ParamDefinition>();
            List<ParamDefinition> stairDefs = new List<ParamDefinition>();
            List<ParamDefinition> storeyDefs = new List<ParamDefinition>();
            List<ParamDefinition> projInfoDefs = new List<ParamDefinition>();


            // get the definitions csv file
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + SHARED_PARAMETER_FILE_NAME;
            string[] contents = null;
            try
            {
                contents = File.ReadAllText(path).Split('\n');

            }
            catch
            {
                TaskDialog.Show("Error", "The shared file could not be opened. Empty fields could not be initialized.");                
                return Result.Failed;
            }

            // Parse csv file
            var csv = from line in contents
                      select line.Split(',').ToArray();

            // loop definitions 
            foreach (var row in csv.Skip(1)
                .TakeWhile(r => r.Length > 1 && r.Last().Trim().Length > 0))
            {

                string val = row[6].ToString();
                
               
                if (!val.Contains("null"))
                {
                    string target = row[2];


                    ParamDefinition newDef = new ParamDefinition();
                    newDef.name = row[0];
                    newDef.default_val = val;
                    newDef.type = row[1];

                    string[] Targets = target.Split(';');
                    foreach (string t in Targets)
                    {

                        switch (t)
                        {
                            case "OST_Doors":
                                doorDefs.Add(newDef);
                                break;
                            case "OST_Stairs":
                                stairDefs.Add(newDef);
                                break;
                            case "OST_Rooms":
                                roomDefs.Add(newDef);
                                break;
                            case "OST_Levels":
                                storeyDefs.Add(newDef);
                                break;
                            case "OST_ProjectInformation":
                                projInfoDefs.Add(newDef);
                                break;
                            default:
                                break;
                        }


                    }

                }




            }


            // Retrierve element instances and fill them

            // doors 
            List<Element> doorsList = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().ToList();
            // rooms 
            List<Element> rooms = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).WhereElementIsNotElementType().ToList();
            // levels 
            List<Element> storeys = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToList();
            // proj info 
            List<Element> buildings = new List<Element>();
            buildings.Add(doc.ProjectInformation as Element);
            // stairs
            List<Element> allStairs = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Stairs).ToList();


           

            // rooms 
            writeEmptyFields(rooms, roomDefs);

            // doors 
            writeEmptyFields(doorsList, doorDefs);

            // levels 
            writeEmptyFields(storeys, storeyDefs);

            // proj info 
            writeEmptyFields(buildings, projInfoDefs);

            // stairs 
            writeEmptyFields(allStairs, stairDefs);

            

            return Result.Succeeded;


        }


        private static Result writeEmptyFields (List<Element>instances, List<ParamDefinition> definitions)
        {
            foreach (Element e in instances)
            {
                foreach (ParamDefinition d in definitions)
                {
                    Parameter p = e.LookupParameter(d.name);

                    bool forceInit = false;

                    if (p != null)
                    {
                         
                        if (p.StorageType.ToString() == "String" && p.AsString() == string.Empty)
                        {
                            forceInit = true;
                        }
                    }
                    

                    // the parameter must exist (ie non null) and have no value to be defined
                    if ((p != null && p.HasValue == false ) || forceInit == true)
                    {
                        //TaskDialog.Show("Debug", d.name + " : " + p.HasValue.ToString());
                        //return Result.Failed;

                        // if it has no value, initialize it - pay attention to the type
                        switch (d.type)
                        {
                            case "YesNo":
                                p.Set(int.Parse(d.default_val));
                                break;
                            case "TEXT":
                                p.Set(d.default_val);
                                break;
                            case "NUMBER":
                                p.Set(double.Parse(d.default_val));
                                break;
                            case "AREA":
                                double val2 = double.Parse(d.default_val);
                                p.Set(UnitUtils.Convert(val2, UnitTypeId.SquareMeters, UnitTypeId.SquareFeet));
                                //p.Set(double.Parse(d.default_val));
                                break;
                            case "INTEGER":
                                //   p.Set(int.Parse(d.default_val));                                 
                                p.Set(int.Parse(d.default_val));                              
                                break;
                            case "LENGTH":
                                double val = double.Parse(d.default_val);
                                p.Set(UnitUtils.Convert(val, UnitTypeId.Millimeters, UnitTypeId.Feet));
                                //p.Set(val);
                                break;
                            case "PERIOD":
                                p.Set(double.Parse(d.default_val));
                                break;
                            default:
                                break;
                        }

                    }
                }
            }



            return Result.Succeeded;
        }


    }



    public class ParamDefinition
    {
        public string name { get; set; }
        public string type { get; set; }
        public string default_val { get; set; }

    }
}