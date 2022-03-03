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
/// <summary>
/// Limitations : Vertical travel not handled - Occupants are assumed to be safe as soon as they reach the emergency staircase (true is stair is protected by fire door + pressurization)
/// CmdMakePaths : this class enables the generation of travel paths 
/// It queries the list of rooms and storeys and calls for the helper method generateTravelPaths
/// CmdSelectPreferredExit : This class allows user to assign an exit (DischargeExit) to different rooms
/// </summary>
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
            List<Element> rooms = new List<Element>();
            try
            {
                rooms = new FilteredElementCollector(doc).OfClass(typeof(SpatialElement)).WhereElementIsNotElementType().Where(room => room.GetType() == typeof(Room)).Where(room => room.LookupParameter("isCorridor").AsInteger() == 0).ToList();
            }
            catch
            {
                TaskDialog.Show("Error", "Some project parameters appear to be missing. Try initialising the project first !");

                tx.RollBack();
                return Result.Failed;
            }

             


            // List of storeys 
            List<Element> storeys = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToList();



            // Clear previous paths if any !! 


            /// 0. Draw travel paths 
            generateTravelPaths(storeys, rooms, doc);


            tx.Commit();
            return Result.Succeeded;
        }


        /// <summary>
        /// This function generates the travel path from a list of rooms to corresponding - pre assigned exits 
        /// First, return the room geometry as polygon 
        /// Gather vertices composing that polygon 
        /// Find the furthest vertex from the assigned exit 
        /// Trace a path of travel from the vertex to the exit 
        /// Limitation : there must be a floor plan view ! 
        /// Limitation : AssignedExit name must include the id of element separated by _ 
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
                    // update EgressPathTravelDistanceHorizontal
                    foreach (Element r in roomsInStorey)
                    {

                        // get vertices 
                        List<XYZ> vertices = IBCCheckUtils.getRoomVertices(r as Room);

                        // get the prefered exit and its location
                        string preferredExitName = r.LookupParameter("AssignedExit").AsString();
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

                                        r.LookupParameter("EgressPathTravelDistanceHorizontal").Set(UnitUtils.Convert(distance, UnitTypeId.Millimeters, UnitTypeId.Feet));

                                        List<Curve> pthCurves = pth.GetCurves().ToList();
                                        List<string> pthCurvesStr = new List<string>();
                                        string pthPoints = "";
                                        foreach (Curve c in pthCurves)
                                        {
                                            XYZ p0 = c.GetEndPoint(0);
                                            string p0str = IBCCheckUtils.XYZToString(p0);
                                            XYZ p1 = c.GetEndPoint(1);
                                            string p1str = IBCCheckUtils.XYZToString(p1);
                                            // avoid duplicates
                                            if(!pthCurvesStr.Contains(p0str))
                                            {
                                                pthCurvesStr.Add(p0str);
                                            }
                                            if (!pthCurvesStr.Contains(p1str))
                                            {
                                                pthCurvesStr.Add(p1str);
                                            }


                                        }

                                        foreach (string st in pthCurvesStr)
                                        {
                                            pthPoints += "(" + st + ")"+",";
                                           // TaskDialog.Show("Debug", st);
                                        }
                                        pthPoints = pthPoints.Remove(pthPoints.Count() - 1);
                                       // TaskDialog.Show("Debug", pthPoints);
                                        //Write into  model

                                        r.LookupParameter("EgressPathTravelXYZ").Set(pthPoints);

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
            IEnumerable<Element> doorsList = null;
            List<Element> dischargeExitList = new List<Element>();
            try
            {
                doorsList = collector.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().Where(room => room.LookupParameter("FireExit").AsInteger() == 1);
                dischargeExitList = doorsList.Where(door => door.LookupParameter("DischargeExit").AsInteger() == 1).Where(door => door.LevelId == roomLevel).ToList();
            }
            catch
            {
                TaskDialog.Show("Error", "Some project parameters appear to be missing. Try initialising the project first !");

                 
                return Result.Failed;
            }
            

 
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
            Form1 f = new Form1(exitNames, roomLevelName, "Select Exit","Select an exit :","");
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
                elem.LookupParameter("AssignedExit").Set(selectedExitName);

            }


            tx.Commit();
            return Result.Succeeded;
        }




    }


    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdSelectPreferredStair : IExternalCommand
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
            List<Element> stairsList = new List<Element>();
            try
            {
                stairsList = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Stairs).Where(stair => stair.LookupParameter("FireEgressStair").AsInteger() == 1).Where(stair => stair.LookupParameter("Top Level").AsElementId() == roomLevel).ToList();
                                
               
            }
            catch
            {
                TaskDialog.Show("Error", "Some project parameters appear to be missing. Try initialising the project first !");


                return Result.Failed;
            }



            if (stairsList.Count() == 0)
            {

                TaskDialog.Show("Error", "There are no valid fire staircases on this floor");
                return Result.Failed;

            }
            // Send to form ui as a list of string (names) 
            // get doors names 
            List<string> exitNames = new List<string>();

            foreach (Element ele in stairsList)
            {
                exitNames.Add(ele.LookupParameter("IfcName").AsString());


            }
            exitNames.Add("None");


            //TaskDialog.Show("Error", exitNames.First().name + " - "+exitNames.First().levelName);

            // make figure
            Form1 f = new Form1(exitNames, roomLevelName, "Select Stair", "Select a stair :", "");
            f.ShowDialog();

            int selectedExitIndex = f.selectedExitIndex;

            if (selectedExitIndex == -1)
            {
                return Result.Failed;
            }

            string selectedExitName = exitNames.ElementAt(selectedExitIndex);
            if (selectedExitName == "None")
            {
                selectedExitName = "";
            }
            // TaskDialog.Show("Debug", selectedExitName);

            // Open transaction
            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // Apply selection to all rooms 
            foreach (ElementId id in selectedIds)
            {
                Element elem = doc.GetElement(id);
                // check if a room was selected and nothing else !
                elem.LookupParameter("AssignedStaircase").Set(selectedExitName);

            }


            tx.Commit();
            return Result.Succeeded;
        }




    }


    [TransactionAttribute(TransactionMode.Manual)]
    public class CmdAssignLinkedComponent : IExternalCommand
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

                    TaskDialog.Show("Error", "Please select a stair");
                    return Result.Failed;

                }

            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;

            }

            // check selection (only stairs accepted) 

            // only allow one element to be selected 

            ElementId selectedId = selectedIds.First();
             
                Element elem = doc.GetElement(selectedId);
                // check if a room was selected and nothing else !
                if (elem.GetType().Name != "Stairs")
                {
                    TaskDialog.Show("Error", "Please select a stair");
                    return Result.Failed;
                }

             



            // Querry potential exits 
            // Only keep exits at same level as rooms 
            

            ElementId level = doc.GetElement(selectedId).LookupParameter("Base Level").AsElementId();
            string levelName = doc.GetElement(level).Name;
            string stairName = doc.GetElement(selectedId).LookupParameter("IfcName").AsString();

            //Querry all doors in the model which are exits (can be a room exit or a discharge exit)
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IEnumerable<Element> doorsList = null;
            List<Element> linkedComponentsList = new List<Element>();
            try
            {
                doorsList = collector.OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().Where(room => room.LookupParameter("FireExit").AsInteger() == 1);
                linkedComponentsList = doorsList.Where(door => door.LookupParameter("DischargeExit").AsInteger() == 1).Where(door => door.LevelId == level).ToList();
            }
            catch
            {
                TaskDialog.Show("Error", "Some project parameters appear to be missing. Try initialising the project first !");


                return Result.Failed;
            }

            // also query all stairs which have a top level == the base level of current stair 
            try
            {
                List<Element> stairs = new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Stairs).Where(stair => stair.LookupParameter("FireEgressStair").AsInteger() == 1).Where(stair => stair.LookupParameter("Top Level").AsElementId() == level).ToList();

                foreach(Element e in stairs)
                {
                    linkedComponentsList.Add(e);
                }
            }
            catch
            {
                TaskDialog.Show("Error", "Some project parameters appear to be missing. Try initialising the project first !");

                
                return Result.Failed;
            }

            if (linkedComponentsList.Count() == 0)
            {

                TaskDialog.Show("Error", "There are no valid discharge exits in the model");
                return Result.Failed;

            }
            // Send to form ui as a list of string (names) 
            // get doors names 
            List<string> exitNames = new List<string>();

            foreach (Element ele in linkedComponentsList)
            {
                exitNames.Add(ele.LookupParameter("IfcName").AsString());


            }


            //TaskDialog.Show("Error", exitNames.First().name + " - "+exitNames.First().levelName);

            // make figure
            Form1 f = new Form1(exitNames, levelName,"Link component","Select a component from base floor to link to :", stairName);
            f.ShowDialog();

            int selectedExitIndex = f.selectedExitIndex;

            if (selectedExitIndex == -1)
            {
                return Result.Failed;
            }

            string selectedExitName = exitNames.ElementAt(selectedExitIndex);
            //TaskDialog.Show("Debug", selectedExitName);

            // Open transaction
            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // Apply selection to all rooms 
              
            elem.LookupParameter("LinkedComponent").Set(selectedExitName);
            

             


            tx.Commit();
            return Result.Succeeded;
        }
    }

    public class IBCCheckUtils
    {
       
        // output the point's three coordinates
        public static string XYZToString(XYZ point)
        {
            return Math.Round(point.X,2) + ";" + Math.Round(point.Y, 2) + ";" + Math.Round(point.Z, 2);
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
