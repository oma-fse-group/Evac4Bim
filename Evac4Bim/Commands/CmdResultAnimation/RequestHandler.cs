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
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit.SDK.Samples.ModelessForm_ExternalEvent.CS
{
    /// <summary>
    ///   A class with methods to execute requests made by the dialog user.
    /// </summary>
    /// 
    public class RequestHandler : IExternalEventHandler
    {
        // Door colors 
        private OverrideGraphicSettings ogs;
        private OverrideGraphicSettings default_ogs;
        private OverrideGraphicSettings ogs_error;
        private OverrideGraphicSettings ogs_warning;


        public RequestHandler(UIApplication uiapp)
        {
            // This is the constructor method.  

            Document doc = uiapp.ActiveUIDocument.Document;

            // color doors depending on max flowrate
            Color green = new Color((byte)0, (byte)255, (byte)0);
            Color red = new Color((byte)255, (byte)0, (byte)0);
            Color amber = new Color((byte)255, (byte)191, (byte)0);


            this.ogs = new OverrideGraphicSettings();
            this.ogs.SetProjectionLineColor(green);
            this.ogs.SetProjectionLineWeight(10);

            this.default_ogs = new OverrideGraphicSettings();

            this.ogs_error = new OverrideGraphicSettings();
            this.ogs_error.SetProjectionLineColor(red);
            this.ogs_error.SetProjectionLineWeight(10);

            this.ogs_warning = new OverrideGraphicSettings();
            this.ogs_warning.SetProjectionLineColor(amber);
            this.ogs_warning.SetProjectionLineWeight(10);



            this.MaxDoorFlowrate = doc.ProjectInformation.LookupParameter("MaxDoorFlowrate").AsDouble();

        }


        private double MaxDoorFlowrate;


        // The value of the latest request made by the modeless form 
        private Request m_request = new Request();

        //value of slider 
        public int m_sliderVal = 0;

        // List of frame containers 
        public List<Frames> m_frames = new List<Frames>();

        // Reference to form
        public ModelessForm m_form = null;
        /// <summary>
        /// A public property to access the current request value
        /// </summary>
        public Request Request
        {
            get { return m_request; }
        }

        /// <summary>
        ///   A method to identify this External Event Handler
        /// </summary>
        public String GetName()
        {
            return "R2014 External Event Sample";
        }


        /// <summary>
        ///   The top method of the event handler.
        /// </summary>
        /// <remarks>
        ///   This is called by Revit after the corresponding
        ///   external event was raised (by the modeless form)
        ///   and Revit reached the time at which it could call
        ///   the event's handler (i.e. this object)
        /// </remarks>
        /// 
        public void Execute(UIApplication uiapp)
        {
            try
            {
                switch (Request.Take())
                {

                    case RequestId.None:
                        {
                            return;  // no request at this time -> we can leave immediately
                        }
                    case RequestId.SliderScroll:
                        {

                            UpdateFrame(uiapp, this.m_sliderVal);

                            break;

                        }
                    case RequestId.Exit:
                        {
                            // TaskDialog.Show("Debug", "clear model");
                            Clear(uiapp);

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
            finally
            {
                Application.thisApp.WakeFormUp();
            }

            return;
        }





        private void UpdateFrame(UIApplication uiapp, double sliderValue)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            Transaction trans = new Transaction(uidoc.Document);
            trans.Start("new Transaction");

            

            int index = this.m_sliderVal;
            foreach (Frames f in this.m_frames)
            {
                // value 
                string val = null;
                if (index >= f.values.Length)
                {
                    val = f.values[0];
                }
                else
                {
                    val = f.values[index];
                }
                 
                // write value
                if (f.type == TargetType.TextBox)
                {
                    TextNote note = doc.GetElement(f.targetId) as TextNote;
                    note.Text = f.parameterName + val;
                }
                else if (f.type == TargetType.Room)
                {
                    // edit room property ! 
                    Element ele = doc.GetElement(f.targetId);
                    try
                    {
                        double value = double.Parse(val);
                        ele.LookupParameter(f.parameterName).Set(value);
                    }
                    catch
                    {
                        continue;
                    }

                }

                else if(f.type == TargetType.Door)
                {

                   
                        double flowrate = double.Parse(val);

                        if (flowrate >= MaxDoorFlowrate)
                        {
                            doc.ActiveView.SetElementOverrides(f.targetId, ogs_error);
                        }
                        else if (flowrate >= (0.5 * MaxDoorFlowrate))
                        {
                            doc.ActiveView.SetElementOverrides(f.targetId, ogs_warning);
                        }
                        else if (flowrate > 0)
                        {
                            doc.ActiveView.SetElementOverrides(f.targetId, ogs);
                        }
                        else
                        {
                            doc.ActiveView.SetElementOverrides(f.targetId, default_ogs);
                        }

                    

                   



                }


            }

            trans.Commit();

            // TaskDialog.Show("Hello", this.m_sliderVal.ToString());
        }


        // remove text boxes
        private void Clear(UIApplication uiapp)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            UIDocument uidoc = uiapp.ActiveUIDocument;

 

            Transaction trans = new Transaction(uidoc.Document);
            trans.Start("new Transaction");

            foreach (Frames f in this.m_frames)
            {
                // ensure its not a room or a critical element  !

                if (f.type == TargetType.TextBox)
                {
                    doc.Delete(f.targetId);
                }
                if (f.type == TargetType.Door)
                {
                    doc.ActiveView.SetElementOverrides(f.targetId, default_ogs);
                }

            }

            this.m_form.Close();

            trans.Commit();
        }


    }  // class

}  // namespace
