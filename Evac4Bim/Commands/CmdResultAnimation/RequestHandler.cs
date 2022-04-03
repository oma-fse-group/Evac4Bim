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
        

        // The value of the latest request made by the modeless form 
        private Request m_request = new Request();

        //value of slider 
        public double m_sliderVal = 0;

        // List of frame containers 
        List<Frames> m_frames = new List<Frames>();

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
                            //TaskDialog.Show("Hello", this.m_sliderVal.ToString());

                            UpdateFrame(uiapp,this.m_sliderVal);

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


         
      
         
        private void UpdateFrame (UIApplication uiapp, double sliderValue)
        {
            Document doc = uiapp.ActiveUIDocument.Document;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            Transaction trans = new Transaction(uidoc.Document);
            trans.Start("new Transaction");
            
            //this.m_doc.GetElement(new ElementId())
            int id = int.Parse(doc.ProjectInformation.LookupParameter("AnnotationID").AsString());
            ElementId eId = new ElementId(id);

            TextNote note = doc.GetElement(eId) as TextNote;
            note.Text = sliderValue.ToString();

            trans.Commit();

           // TaskDialog.Show("Hello", this.m_sliderVal.ToString());
        }

    }  // class

}  // namespace
