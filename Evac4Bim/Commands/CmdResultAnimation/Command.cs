//
// (C) Copyright 2003-2019 by Autodesk, Inc.
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE. AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
//
// Use, duplication, or disclosure by the U.S. Government is subject to
// restrictions set forth in FAR 52.227-19 (Commercial Computer
// Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii)
// (Rights in Technical Data and Computer Software), as applicable.
//

using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;

using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using Autodesk.Revit.DB.Architecture;

namespace Revit.SDK.Samples.ModelessForm_ExternalEvent.CS
{
    /// <summary>
    /// Implements the Revit add-in interface IExternalCommand
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        /// <summary>
        /// Implement this method as an external command for Revit.
        /// </summary>
        /// <param name="commandData">An object that is passed to the external application 
        /// which contains data related to the command, 
        /// such as the application object and active view.</param>
        /// <param name="message">A message that can be set by the external application 
        /// which will be displayed if a failure or cancellation is returned by 
        /// the external command.</param>
        /// <param name="elements">A set of elements to which the external application 
        /// can add elements that are to be highlighted in case of failure or cancellation.</param>
        /// <returns>Return the status of the external command. 
        /// A result of Succeeded means that the API external method functioned as expected. 
        /// Cancelled can be used to signify that the user cancelled the external operation 
        /// at some point. Failure should be returned if the application is unable to proceed with 
        /// the operation.</returns>
        public virtual Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;


            List<Frames> f = new List<Frames>();
            // this list will include objects (such as tags, rooms...) + an árray of possible values + a reference to the variable to be changed (e.g for a tag, edit the text value


            int numberOfValues; // max number of positions (number of rows ! )
            double timestep;


            var tx = new Transaction(doc);
            tx.Start("init model");

            /// init model 
            initModel(doc, out f, out numberOfValues, out timestep);


            Application.thisApp.ShowForm(commandData.Application, numberOfValues, timestep, f);

            tx.Commit();

            return Result.Succeeded;


        }

        private int initModel(Document doc, out List<Frames> f, out int numberOfValues, out double timestep)
        {
            f = new List<Frames>();
            /// Step 0 : Find number of values and timestep of occupant/usage history
            numberOfValues = 0; // max number of positions (number of rows ! )
            timestep = 0;

            try
            {
                timestep = double.Parse(doc.ProjectInformation.LookupParameter("CsvTimeStep").AsString());

                numberOfValues = int.Parse(doc.ProjectInformation.LookupParameter("NumberOfValues").AsString());

                if (numberOfValues > 1)
                {
                    numberOfValues--; // substract the 0th value
                }



            }
            catch
            {
                return -1; // error : either variables do not exist or parsing error
            }

            /// Step 1 : Find the current view 
            Autodesk.Revit.DB.View v = doc.ActiveView;
            ElementId v_id = v.Id;


            /// Step 2 : Querry all rooms and doors included in this view 

            List<Element> rooms = new FilteredElementCollector(doc, v_id).OfClass(typeof(SpatialElement)).WhereElementIsNotElementType().Where(room => room.GetType() == typeof(Room)).Where(room => room.LookupParameter("OccupancyHistory").AsString() != string.Empty && room.LookupParameter("OccupancyHistory").AsString() != "n.a" && room.LookupParameter("OccupancyHistory").AsString() != "n.s.").ToList();

            List<Element> doors = new FilteredElementCollector(doc, v_id).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType().Where(door => door.LookupParameter("DoorFlowrateHistory").AsString() != string.Empty && door.LookupParameter("DoorFlowrateHistory").AsString() != "n.a" && door.LookupParameter("DoorFlowrateHistory").AsString() != "n.s.").ToList();

            // Txt box options 
            TextNoteOptions options = new TextNoteOptions();
            options.HorizontalAlignment = HorizontalTextAlignment.Left;
            options.TypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);

            /// Step 3 : For each room : create a Frame object
            /// Also compute densities

            foreach (Element r in rooms)
            {
                /// Parse occupantCountHistory
                string[] dataY = new string[] { };
                string occupantCountHistory = r.LookupParameter("OccupancyHistory").AsString();

                string[] densities = new string[] { };
                double area = UnitUtils.ConvertFromInternalUnits(r.LookupParameter("Area").AsDouble(), UnitTypeId.SquareMeters);

                string[] rows = occupantCountHistory.Split(';');
                foreach (string row in rows)
                {
                    string[] cols = row.Split(',');
                    string rem = (cols[1]);

                    string dens = "0";
                    // try computing density
                    try
                    {
                        double occ = double.Parse(rem);
                        if (occ == 0)
                        {
                            dens = "0";
                        }
                        else
                        {
                            dens = Math.Round(area / occ, 2).ToString();
                        }

                    }
                    catch
                    {
                        // fails because number of occupants isNaN ?
                    }

                    dataY = dataY.Concat(new string[] { rem }).ToArray();
                    densities = densities.Concat(new string[] { dens }).ToArray();
                }

                //create a text box

                XYZ origin30 = GetRoomCenter(r as Room);

                TextNote note30 = TextNote.Create(doc, v_id, origin30, "000", options);


                // create Frames instance 
                Frames frm30 = new Frames();
                frm30.values = dataY;
                frm30.targetId = note30.Id;
                frm30.parameterName = "# = ";
                frm30.type = TargetType.TextBox;

                // append list of frames
                f.Add(frm30);


                // create Frames instance for room density
                Frames frm2 = new Frames();
                frm2.values = densities;
                frm2.targetId = r.Id;
                frm2.parameterName = "AreaPerOccupant";
                frm2.type = TargetType.Room;

                // append list of frames
                f.Add(frm2);



            }

            //Create color schemes 
            highlightRooms(rooms, doc);
            placeLegend(v, "Area Per Occupant", doc);



            /// Repeat for overall occupant number
            /// Parse occupantCountHistory

            string occupantCountHistoryOverall = doc.ProjectInformation.LookupParameter("OccupancyHistoryOverall").AsString();
            string[] values = new string[] { };
            if (occupantCountHistoryOverall != "n.a" || occupantCountHistoryOverall != "")
            {

                // Parse occupantCountHistory
                string[] rows = occupantCountHistoryOverall.Split(';');
                foreach (string r in rows)
                {
                    string[] cols = r.Split(',');
                    string val = cols[1] + "/" + cols[2]; // remaining / exited

                    values = values.Concat(new string[] { val }).ToArray();
                }

                //create a text box

                XYZ origin20 = GetElementCenter(v);

                TextNote note20 = TextNote.Create(doc, v_id, origin20, "000", options);


                // create Frames instance 
                Frames frm10 = new Frames();
                frm10.values = values;
                frm10.targetId = note20.Id;
                frm10.parameterName = "Remaining / Exited : ";
                frm10.type = TargetType.TextBox;

                // append list of frames
                f.Add(frm10);


            }


            /// Display an indication of max flowrate 
            double MaxDoorFlowrate = doc.ProjectInformation.LookupParameter("MaxDoorFlowrate").AsDouble();
            //create a text box
            string[] vals = new string[] { MaxDoorFlowrate.ToString() };

            XYZ origin = GetElementCenter(v);

            TextNote note = TextNote.Create(doc, v_id, origin, "000", options);


            // create Frames instance 
            Frames frm = new Frames();
            frm.values = vals;
            frm.targetId = note.Id;
            frm.parameterName = "MaxDoorFlowrate (pers/sec) : ";
            frm.type = TargetType.TextBox;

            // append list of frames
            f.Add(frm);

            /// Step 4 : For each door : create a Frame object
            foreach (Element d in doors)
            {
                /// Parse DoorFlowrateHistory
                string[] dataY = new string[] { };
                string DoorFlowrateHistory = d.LookupParameter("DoorFlowrateHistory").AsString();
                // Parse occupantCountHistory
                string[] rows = DoorFlowrateHistory.Split(';');
                foreach (string r in rows)
                {
                    string[] cols = r.Split(',');
                    string rem = cols[1];

                    dataY = dataY.Concat(new string[] { rem }).ToArray();
                }

                //create a text box

                origin = null;

                if (d.Location == null)
                {
                    // can be a curtain glass 
                    // fetch origin from family instnce
                    FamilyInstance exitEleFamIns = d as FamilyInstance;
                    origin = exitEleFamIns.GetTransform().Origin;

                }
                else
                {
                    origin = (d.Location as LocationPoint).Point;
                }

                note = TextNote.Create(doc, v_id, origin, "000", options);


                // create Frames instance 
                Frames frm3 = new Frames();
                frm3.values = dataY;
                frm3.targetId = note.Id;
                frm3.parameterName = "Q = ";
                frm3.type = TargetType.TextBox;



                // append list of frames
                f.Add(frm3);


                // create Frames for door coloring 
                Frames frm4 = new Frames();
                frm4.values = dataY;
                frm4.targetId = d.Id;
                frm4.parameterName = "Max Door Flowrate";
                frm4.type = TargetType.Door;
                f.Add(frm4);


            }


            


            return 0;
        }


        public XYZ GetElementCenter(Element elem)
        {
            BoundingBoxXYZ bounding = elem.get_BoundingBox(null);
            XYZ center = (bounding.Max + bounding.Min) * 0.5;
            return center;
        }

        public XYZ GetRoomCenter(Room room)
        {
            // Get the room center point.
            XYZ boundCenter = GetElementCenter(room);
            LocationPoint locPt = (LocationPoint)room.Location;
            XYZ roomCenter = new XYZ(boundCenter.X, boundCenter.Y, locPt.Point.Z);
            return roomCenter;
        }

        public void highlightRooms(List<Element> rooms, Document doc)
        {
            Color green = new Color((byte)0, (byte)200, (byte)0);
            Color red = new Color((byte)200, (byte)0, (byte)0);


            List<string> requiredSchema = new List<string>();
            requiredSchema.Add("Area Per Occupant");

            List<string> missingSchema = new List<string>();
            List<string> existingSchema = new List<string>();


            // Find color schemes in the document
            List<Element> schemaList = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ColorFillSchema).WhereElementIsNotElementType().ToList();
            int roomCatIndex = (int)BuiltInCategory.OST_Rooms;
            List<ColorFillScheme> schema = schemaList.Cast<ColorFillScheme>().Where(scheme => scheme.CategoryId.IntegerValue == roomCatIndex).ToList();

            // Check if the schemes are already defined 
            foreach (ColorFillScheme scheme in schema)
            {
                existingSchema.Add(scheme.Name);
            }

            foreach (string s in requiredSchema)
            {
                // if the scheme is required but was not found, create it 
                if (!existingSchema.Contains(s))
                {
                    missingSchema.Add(s);
                }
            }


            // Else create new ones by duplicating a pre existing scheme


            if (missingSchema.Count > 0 && schema.Count > 0 && rooms.Count > 0)
            {
                foreach (string name in missingSchema)
                {

                    // find the scheme to duplicate
                    ColorFillScheme sch = schema.First();
                    ColorFillScheme newSch = doc.GetElement(sch.Duplicate(name)) as ColorFillScheme;
                    newSch.Title = name;


                    // edit colors and category
                    switch (name)
                    {

                        case "Area Per Occupant":
                            {
                                /// Values extracted from IBC code

                                // all existing entries are removed 
                                newSch.ParameterDefinition = rooms.First().LookupParameter("AreaPerOccupant").Id; newSch.IsByRange = true;

                                IDictionary<double, string> evalDict = new Dictionary<double, string>();
                                evalDict.Add(1, "Less than 1 m²/pers");
                                evalDict.Add(2, "Between 1-2 m²/pers");
                                evalDict.Add(3, "Between 2-3 m²/pers");
                                evalDict.Add(5, "Between 3-5 m²/pers");
                                evalDict.Add(10, "Between 5-10 m²/pers");
                                evalDict.Add(15, "Between 10-15 m²/pers");
                                evalDict.Add(20, "Between 15-20 m²/pers");
                                evalDict.Add(25, "Between 20-40 m²/pers");
                                evalDict.Add(40, "More than 40 m²/pers");

                                IDictionary<double, Color> colorsDict = new Dictionary<double, Color>();
                                colorsDict.Add(1, new Color((byte)250, (byte)230, (byte)23));
                                colorsDict.Add(2, new Color((byte)250, (byte)240, (byte)23));
                                colorsDict.Add(3, new Color((byte)230, (byte)250, (byte)25));
                                colorsDict.Add(5, new Color((byte)230, (byte)250, (byte)24));
                                colorsDict.Add(10, new Color((byte)230, (byte)250, (byte)23));
                                colorsDict.Add(15, new Color((byte)241, (byte)250, (byte)23));
                                colorsDict.Add(20, new Color((byte)250, (byte)248, (byte)23));
                                colorsDict.Add(25, new Color((byte)250, (byte)230, (byte)25));
                                colorsDict.Add(40, new Color((byte)250, (byte)230, (byte)25));





                                createSchemeEntries(evalDict, colorsDict, newSch);

                                break;

                            }

                        default:
                            {
                                // some kind of a warning here should
                                // notify us about an unexpected request 
                                break;
                            }
                    }






                }


            }
            else if (schema.Count == 0 || rooms.Count == 0)
            {
                // error - do something
                TaskDialog.Show("Error", "Could not create the color fill schema.");
            }


        }

        public static void createSchemeEntries(IDictionary<double, string> evalDict, IDictionary<double, Color> colorsDict, ColorFillScheme newSch)
        {


            foreach (int key in evalDict.Keys)
            {
                ColorFillSchemeEntry entry = new ColorFillSchemeEntry(StorageType.Double);
                entry.SetDoubleValue(key);
                entry.Caption = evalDict[key];
                entry.Color = colorsDict[key];

                if (newSch.CanUpdateEntry(entry)) // means this entry exists
                {
                    newSch.UpdateEntry(entry);
                }
                else
                {
                    try
                    {
                        newSch.AddEntry(entry);
                    }
                    catch
                    {
                        //
                    }

                }


            }
        }


        public void placeLegend(Autodesk.Revit.DB.View v, string schemeName, Document doc)
        {
            // Find color schemes in the document
            List<Element> schemaList = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ColorFillSchema).WhereElementIsNotElementType().ToList();
            List<ColorFillScheme> schema = schemaList.Cast<ColorFillScheme>().Where(scheme => scheme.Title == schemeName).ToList();
            
            ElementId schId = null;

            ElementId viewId = v.Id;

            if (schema.Count > 0)
            {
                schId = schema.First().Id;
            }
            
            List<Element> legendList = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ColorFillLegends).WhereElementIsNotElementType().ToList();


            if (schId != null)
            {
                // check if a legend is already defined 

                List<Element> existingLegends = legendList.Where(leg => leg.OwnerViewId == viewId).ToList();
                if (existingLegends.Count == 0)
                {

                    XYZ origin = GetElementCenter(v);                    
                    
                    try
                    {
                        v.SetColorFillSchemeId(new ElementId(BuiltInCategory.OST_Rooms), schId);

                        ColorFillLegend legend = ColorFillLegend.Create(doc, viewId, new ElementId(BuiltInCategory.OST_Rooms), origin);
                        
                    }
                    catch
                    {
                        //
                    }

                }



            }








        }


    }
}

