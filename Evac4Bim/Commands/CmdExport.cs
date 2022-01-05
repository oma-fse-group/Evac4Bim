using System;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

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
            if (res == ItemSelectionDialogResult.Confirmed)
            {

                ModelPath path = d.GetSelectedModelPath();
                string localPath = null;

                localPath = ModelPathUtils.ConvertModelPathToUserVisiblePath(path);

                localPath = Path.GetDirectoryName(localPath);

                //TaskDialog.Show("f", localPath);

                ExportToIfc(doc, localPath);
            }
            else
            {
                return Result.Failed;
            }
                                 
            return Result.Succeeded;
        }

        /// <summary>
        ///     Export current view to IFC for
        ///     https://forums.autodesk.com/t5/revit-api-forum/ifc-export-using-document-export-not-working/m-p/8118082
        /// </summary>
        private static Result ExportToIfc(Document doc, string localPath)
        {
            var r = Result.Failed;
                     
            var save_path = localPath;

            var tx = new Transaction(doc);
            tx.Start("Export IFC");

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
            opt.AddOption("ExportRoomsInView", "false");


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

            tx.RollBack();

            

            return r;
        }
    }
}