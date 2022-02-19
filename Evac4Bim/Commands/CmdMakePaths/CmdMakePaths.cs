using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CmdMakePaths;

namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdMakePaths: IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            // Open transaction
            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // offer to remove previous paths of travel 
                TaskDialog dialog = new TaskDialog("Decision");
                dialog.MainContent = "Do you want to clear previous paths of travel ?";
                dialog.AllowCancellation = true;
                dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

                TaskDialogResult result = dialog.Show();
                if (result == TaskDialogResult.Yes)
                {
                // Yes
                List<Element> travelPaths = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PathOfTravelLines).WhereElementIsNotElementType().ToList();
               // TaskDialog.Show("Debug", travelPaths.Count().ToString());
                    foreach (Element tp in travelPaths)
                    {
                     
                        doc.Delete(tp.Id);

                    }
 
                }
                else
                    {
                        // No
                        // Do nothing
                    }

            //Querry rooms which are not corridors
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<Element> rooms = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).WhereElementIsNotElementType().Where(room => room.GetType() == typeof(Room)).Where(room => room.LookupParameter("isCorridor").AsInteger() == 0).ToList();


            // List of storeys 
            List<Element> storeys = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToList();



            // Clear previous paths if any !! 


            /// 0. Draw travel paths 
            generateTravelPaths(storeys, rooms, doc);


            tx.Commit();
            return Result.Succeeded;
        }


        /// <summary>
        /// Limitation : there must be a floor plan view ! 
        /// Limitation : preferredExit name must include the id of element separated by _ 
        /// </summary>
        /// <param name="storeys"></param>
        public static void generateTravelPaths(List<Element> storeys, List<Element> rooms, Document doc)
        {
            foreach (Element s in storeys)
            {
                Level l = s as Level;

                // get the view associated with the floor s an id
                ElementId viewId = l.FindAssociatedPlanViewId();

                if (viewId != ElementId.InvalidElementId)
                {
                    View v = doc.GetElement(viewId) as View;

                    // find rooms belonging to each storey 
                    ElementId levelID = s.Id;

                    List<Element> roomsInStorey = rooms.Where(room => room.get_Parameter(BuiltInParameter.ROOM_LEVEL_ID).AsElementId() == levelID).ToList();

                    // Parse all the rooms in that floor 
                    // get the list of vertices 
                    // query the prefered exit for that room
                    // find the most remote corner from the exit
                    // plot path of travel
                    // update travelDistance
                    foreach (Element r in roomsInStorey)
                    {

                        // get vertices 
                        List<XYZ> vertices = IBCCheckUtils.getRoomVertices(r as Room);

                        // get the prefered exit and its location
                        string preferredExitName = r.LookupParameter("PreferredExit").AsString();
                        // ensure it was defined
                        if (preferredExitName!="" && preferredExitName!=null)
                        {
                            XYZ exit = null;
                            ElementId eid = new ElementId(int.Parse(preferredExitName.Split('_').Last()));
                            //ElementId eid = new ElementId(356162);
                            Element exitEle = doc.GetElement(eid);
                            if (exitEle.Location != null)
                            {
                                exit = (exitEle.Location as LocationPoint).Point;
                            }
                            else
                            {
                                // can be a curtain glass 
                                // fetch origin from family instnce
                                FamilyInstance exitEleFamIns = exitEle as FamilyInstance;
                                exit = exitEleFamIns.GetTransform().Origin;
                                
                            }
                           
                            if (exit != null)
                            {
                                 // find furthest point from exit door
                                double maxDist = 0;
                                XYZ selectedPoint = new XYZ();
                                foreach (XYZ point in vertices)
                                {
                                    double d = point.DistanceTo(exit);
                                    if (d > maxDist)
                                    {
                                        maxDist = d;
                                        selectedPoint = point;
                                    }

                                }
 

                                try
                                {
                                   PathOfTravel pth =  PathOfTravel.Create(v, selectedPoint, exit);
                                    if (pth != null)
                                    {
                                        double distance = Math.Round(UnitUtils.ConvertFromInternalUnits(pth.LookupParameter("Length").AsDouble(), UnitTypeId.Millimeters), 0);
                                        r.LookupParameter("TravelDistance").Set(distance.ToString());
                                    }
                                   

                                }
                                catch
                                {
                                    // handle exceptions
                                }



                            }
                         
                        }
                        

                    }


 
                }
                else
                {
                    // cannot draw path of travel without a floor plan
                    // do something 
                }


            }

        }



    }



    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdSelectPreferredExit : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            
            // Retrieve selected rooms in the UI (if any) or throw an error 
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            try
            {
                // Select some elements in Revit before invoking this command


                // Get the element selection of current document.
                Selection selection = uidoc.Selection;

                // If no elements selected.
                if (0 == selectedIds.Count)
                {

                    TaskDialog.Show("Error", "Please select a room");
                    return Result.Failed;

                }

            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;

            }

            // check selection (only rooms accepted) 
            foreach (ElementId id in selectedIds)
            {
                Element elem = doc.GetElement(id);
                // check if a room was selected and nothing else !
                if (elem.GetType().Name != "Room")
                {
                    TaskDialog.Show("Error", "Please select a room");
                    return Result.Failed;
                }

            }



            // Querry potential exits 
            // Only keep exits at same level as rooms 

            ElementId roomLevel = doc.GetElement(selectedIds.First()).get_Parameter(BuiltInParameter.ROOM_LEVEL_ID).AsElementId();
            string roomLevelName = doc.GetElement(roomLevel).Name;

            //Querry all doors in the model which are exits (can be a room exit or a discharge exit)
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IEnumerable<Element> doorsList = collector.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().Where(room => room.LookupParameter("isExitDoor").AsInteger() == 1);
            //List<Element> roomExitList = doorsList.ToList();
            List<Element> dischargeExitList = doorsList.Where(door => door.LookupParameter("isDischargeExit").AsInteger() == 1).Where(door => door.LevelId == roomLevel).ToList();

            if (dischargeExitList.Count() == 0)
            {

                TaskDialog.Show("Error", "There are no valid discharge exits in the model");
                return Result.Failed;

            }
            // Send to form ui as a list of string (names) 
            // get doors names 
            List<string> exitNames = new List<string>();

            foreach (Element ele in dischargeExitList)
            {
                exitNames.Add(ele.LookupParameter("IfcName").AsString());
                 
  
            }


            //TaskDialog.Show("Error", exitNames.First().name + " - "+exitNames.First().levelName);

            // make figure
            Form1 f = new Form1(exitNames, roomLevelName);
            f.ShowDialog();

            int selectedExitIndex = f.selectedExitIndex;

            if (selectedExitIndex == -1)
            {
                return Result.Failed;
            }

            string selectedExitName = exitNames.ElementAt(selectedExitIndex);
           // TaskDialog.Show("Debug", selectedExitName);

            // Open transaction
            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // Apply selection to all rooms 
            foreach (ElementId id in selectedIds)
            {
                Element elem = doc.GetElement(id);
                // check if a room was selected and nothing else !
                elem.LookupParameter("PreferredExit").Set(selectedExitName);

            }


            tx.Commit();
            return Result.Succeeded;
        }

    }

  
    public class IBCCheckUtils
    {
       
        // output the point's three coordinates
        public static string XYZToString(XYZ point)
        {
            return point.X + ";" + point.Y + ";" + point.Z;
        }
        public static XYZ StringtoXYZ(string point)
        {
            return new XYZ(Double.Parse(point.Split(';')[0]), Double.Parse(point.Split(';')[1]), Double.Parse(point.Split(';')[2]));
        }

        public static List<XYZ> getRoomVertices(Room room)
        {

            // List of boundary points 
            List<string> vertices = new List<string>();
            // Store coordiantes as a string list to ensure points are unique
            // then convert the unique ones into <XYZ
            List<XYZ> verts = new List<XYZ>(); // then convert into XYZ

            // Get list of vertices
            IList<IList<Autodesk.Revit.DB.BoundarySegment>> segments = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
            if (null != segments)  //the room may not be bound
            {
                foreach (IList<Autodesk.Revit.DB.BoundarySegment> segmentList in segments)
                {

                    foreach (Autodesk.Revit.DB.BoundarySegment boundarySegment in segmentList)
                    {

                        // Get curve start point
                        string start = XYZToString(boundarySegment.GetCurve().GetEndPoint(0));
                        if (!vertices.Contains(start))
                        {
                            vertices.Add(start);
                        }
                        // Get curve end point
                        string end = XYZToString(boundarySegment.GetCurve().GetEndPoint(1));
                        if (!vertices.Contains(end))
                        {
                            vertices.Add(end);
                        }

                    }
                }
            }


            // Convert to XYZ 
            foreach (string v in vertices)
            {
                verts.Add(StringtoXYZ(v));
            }
            return verts;



        }
    }







}
