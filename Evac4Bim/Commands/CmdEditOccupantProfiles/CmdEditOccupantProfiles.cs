using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CmdEditOccupantProfiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.Manual)]

    public class CmdEditOccupantProfiles : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            Element projInfo = doc.ProjectInformation as Element;

            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // Querry occupant profiles 
            // Check if parameter exists ( != null )
         
            int j = 5;
            string name = "occupantProfile";
            List<OccupantProfile> profilesList = new List<OccupantProfile>();
            for (int i=1; i<=j;i++)
            {
                string paramName = name + i.ToString();
                Parameter param = projInfo.LookupParameter(paramName);
                string occProfileStr = "";
                if (param.AsString() != null && param.AsString() !="")
                {
                    occProfileStr = param.AsString();
                }
                else
                {

                }
                profilesList.Add(new OccupantProfile(occProfileStr, paramName));

            }

            
            

            // Initiate figure and get return object 
            Form1 f = new Form1(profilesList);
            
            //f.profilesList = profilesList;

            f.ShowDialog();

            List<OccupantProfile> updatedprofilesList = f.profilesList;

            // parse the updated profile list
            OccupantProfile.parseOccupantProfileList(updatedprofilesList);
            // Write ino Revit model 

            


            foreach (OccupantProfile p in updatedprofilesList)
            {
                
                    projInfo.LookupParameter(p.profileId).Set(p.sequence);
                
                
            }

            tx.Commit();

            return Result.Succeeded;



        }



    }

    public class OccupantProfile
    {
        public string profileId { get; set; }
        public string name { get; set; }
        public string speed { get; set; }
        public string speedProfile { get; set; }
        public string diameter { get; set; }
        public string isMobilityImpaired { get; set; }
        public string sequence { get; set; }


        public  OccupantProfile(string occProfileStr, string pID)
        {
            // Ensure there is no space or exra commas ! name=default;speed=1.19;speedProfile=Constant;diameter=45.58;isMobilityImpaired=False
            if (occProfileStr != "")
            {
                this.sequence = occProfileStr;

                string[] substr = occProfileStr.Split(';');

                // Parse object
                string subsName = substr[0];
                string subsSpeed = substr[1];
                string subsSpeedProfile = substr[2];
                string subsDiameter = substr[3];
                string subsIsMobilityImpaired = substr[4];

                //clean
                
                this.name = subsName.Substring(subsName.IndexOf('=') + 1);
                this.speed = subsSpeed.Substring(subsSpeed.IndexOf('=') + 1);
                this.speedProfile = subsSpeedProfile.Substring(subsSpeedProfile.IndexOf('=') + 1);
                this.diameter = subsDiameter.Substring(subsDiameter.IndexOf('=') + 1);
                this.isMobilityImpaired = subsIsMobilityImpaired.Substring(subsIsMobilityImpaired.IndexOf('=') + 1);

                //TaskDialog.Show("Debug", this.name + " " + this.speed + " " + this.speedProfile + " " + this.diameter + " " + this.isMobilityImpaired);
            }
            else
            {
                this.sequence = "void";
            }
            this.profileId = pID;



        }
    
        public static void parseOccupantProfileList (List<OccupantProfile> updatedprofilesList)
        {

            foreach (OccupantProfile p in updatedprofilesList)
            {
                
                string seq = "";
                if (p.name != null && p.name != "" ) 
                    // if it has a name assigned => assume it was filled
                {

                    // name=default;speed=1.19;speedProfile=Constant;diameter=45.58;isMobilityImpaired=False
                    seq = "name="+p.name+";speed="+p.speed+";speedProfile="+p.speedProfile+"; diameter="+p.diameter+";isMobilityImpaired="+p.isMobilityImpaired;


                    

                }
                else
                {
                    //TaskDialog.Show(p.profileId, p.name + "is void" );
                    seq = "";
                }



                p.sequence = seq;
            }


 
        }
    
    }


}
