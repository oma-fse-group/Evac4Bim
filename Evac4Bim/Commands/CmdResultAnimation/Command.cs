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
        public virtual Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) {

            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            
            List<Frames> f = new List<Frames>();
            // this list will include objects (such as tags, rooms...) + an árray of possible values + a reference to the variable to be changed (e.g for a tag, edit the text value


            int numberOfValues; // max number of positions (number of rows ! )
            double timestep;

            /// init model 
            initModel(doc, out f, out numberOfValues, out timestep);


            // ==> assign the list of frames into the request handler
            Application.thisApp.ShowForm(commandData.Application, numberOfValues,timestep);
            // ==> assign the list of frames into the request handler


            return Result.Succeeded;
           
            
        }

        private int initModel (Document doc, out List<Frames> f, out int numberOfValues, out double timestep)
        {
            f = new List<Frames>();
            /// Step 0 : Find number of values and timestep of occupant/usage history
            numberOfValues = 0; // max number of positions (number of rows ! )
            timestep = 0;

            try
            {
                timestep = double.Parse(doc.ProjectInformation.LookupParameter("CsvTimeStep").AsString());

                numberOfValues = int.Parse(doc.ProjectInformation.LookupParameter("NumberOfValues").AsString());

                if (numberOfValues>1)
                {
                    numberOfValues--; // substract the 0th value
                }
                

                 
            }
            catch
            {
                return -1; // error : either variables do not exist or parsing error
            }
           

           

            /// Step 1 : Find the current view 
            /// Step 2 : Querry all rooms and doors included in this view 
            /// Step 3 : Find occupant history / usage history of each element and convert into array
            /// Step 4 : Create tags for each room and door
            /// Step 5 : Create a frame object with reference to tag/room and array
            /// Step 6 : Append frame list
            /// Step 7 : Pass frame list into the handler object
            /// Step 8 : Destroy all tags at exit


            return 0;
        }


         
    }
}

