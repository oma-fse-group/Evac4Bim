using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit.SDK.Samples.ModelessForm_ExternalEvent.CS
{
    public class Frames
    {
        public ElementId targetId { get; set; } // can be the id of a text box or a room
        public string[] values { get; set; }  // values 
        public string parameterName { get; set; }
        public TargetType type { get; set; }

    }

    public enum TargetType : int
    {

        Room = 0,
        TextBox = 1,
        Door = 2
    }

     
}
