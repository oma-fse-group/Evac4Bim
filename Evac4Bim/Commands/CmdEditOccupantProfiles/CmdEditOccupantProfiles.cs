using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CmdEditOccupantProfiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// This class allows user to edit/store occupant profiles in the model with a UI
/// </summary>
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



            // Querry occupant profiles 
            // Check if parameter exists ( != null )

            // Template 
            // {name=Fruin2;speed=;speedProfile=Normal(0.6,1.8,1.2,0.2); diameter=45.58;isMobilityImpaired=False}{name=default;speed=1.19;speedProfile=Constant; diameter=45.58;isMobilityImpaired=False} ...
            string paramName = "OccupantProfilesList";
            List<OccupantProfile> profilesList = new List<OccupantProfile>();
            Parameter param = projInfo.LookupParameter(paramName);
            if (param.AsString() != null && param.AsString() != "")
            {
                string[] profilesListString = param.AsString().Split('}');
                foreach (string s in profilesListString)
                {
                    if ( s!= null && s!= "")
                    {
                        string temp = s.Replace("{", string.Empty);
                        //TaskDialog.Show("Debug", temp);
                        profilesList.Add(new OccupantProfile(temp));
                    }

                }
                
            }
            else
            {
                
            }

           

            // Initiate figure and get return object 
            Form1 f = new Form1(profilesList);
            
            //f.profilesList = profilesList;

            f.ShowDialog();

            List<OccupantProfile> updatedprofilesList = f.profilesList;

             

            var tx = new Transaction(doc);
            tx.Start("Export IFC");

            // parse the updated profile list
            OccupantProfile.parseOccupantProfileList(updatedprofilesList);
            // Write ino Revit model 



            string completeSequence = "";
            foreach (OccupantProfile p in updatedprofilesList)
            {


                completeSequence += "{" + p.sequence + "}";
                
                
                
            }

            projInfo.LookupParameter(paramName).Set(completeSequence);

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


        public  OccupantProfile(string occProfileStr)
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
                this.profileId = this.name;

              //  TaskDialog.Show("Debug", this.name + " " + this.speed + " " + this.speedProfile + " " + this.diameter + " " + this.isMobilityImpaired);
            }
            else
            {
                this.sequence = "void";
                this.profileId = "Empty profile";
            }
            



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
