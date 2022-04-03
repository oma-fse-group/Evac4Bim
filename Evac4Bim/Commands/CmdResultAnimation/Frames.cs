using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.SDK.Samples.ModelessForm_ExternalEvent.CS
{
    class Frames
    {
        public Element target { get; set; } // can be the id of a tag or a room
        public double[] values { get; set; }  // values 
        public string parameterName { get; set; }


    }
}
