using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
/// <summary>
/// This class allows user to export an extended pathfinder input file 
/// The original input file (contatining the geometry) is selected by user 
/// Additional properties -extracted from the model - are added to the input file 
/// </summary>

namespace Evac4Bim
{
    [TransactionAttribute(TransactionMode.ReadOnly)]
    public class CmdGenerateInputFile : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Retrieve UIDocument object 
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var uiapp = commandData.Application;
            var doc = uidoc.Document;
            var app = commandData.Application.Application;

            Element projInfo = doc.ProjectInformation as Element;

            // Store definitions in a dictionnary 
            // These definitions can be retrieved 
            // Parameters are included (<..>) and can be edited by editing the string (string.Replace(<>,value)
            IDictionary<string, string> DefinitionsDict = new Dictionary<string, string>();
            DefinitionsDict.Add("Uniform", "<id>: {\"min\":\"<1> <unit>\", \"max\":\"<unit> s\", \"type\":\"unif\"}");
            DefinitionsDict.Add("Normal", "<id>: {\"min\":\"<1> <unit>\", \"max\":\"<2> <unit>\", \"mean\":\"<3> <unit>\", \"type\":\"stdNorm\", \"stDev\":\"<4> <unit>\"}");
            DefinitionsDict.Add("LogNormal", "<id>: {\"min\":\"<1> <unit>\", \"max\":\"<2> <unit>\", \"mean\":\"<3> <unit>\", \"type\":\"logNorm\", \"stDev\":\"<4> <unit>\"}");
            DefinitionsDict.Add("Constant", "<id>: {\"val\":\"<1> <unit>\", \"type\":\"cc\"}");
            DefinitionsDict.Add("Profile", "0: {\"OccProfile.SHAPE\":0,\"OccProfile.COLOR\":[0.0,1.0,0.0,1.0],\"OccProfile.NAME\":\"<ProfileName>\", \"OccProfile.MAXVEL\":<CurveID>}");
            DefinitionsDict.Add("CloseDoor", "0.0 close_door <doorId> -1.0");

            // Node dictionnary to store name and id of each node 
            // key = id 
            // value = [name - element id - type]
            IDictionary<string, string[]> NodesList = new Dictionary<string, string[]>();

            // List of doors to be closed 
            List<string> closedDoorsIds = new List<string>();


            //1. read pathfinder input file (user selection)

            string path = "";
            FileOpenDialog d = new FileOpenDialog("Text file |*.txt");
            ItemSelectionDialogResult resDial = d.Show();
            if (resDial == ItemSelectionDialogResult.Confirmed)
            {
                ModelPath pth = d.GetSelectedModelPath();
                path = ModelPathUtils.ConvertModelPathToUserVisiblePath(pth);
            }
            else
            {
                return Result.Cancelled;
            }

            //2. Parse the input file 
            List<string> lines = new List<string>();

            // set the output file
            string savePath = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + "-combined.txt";
            try
            {
                lines = System.IO.File.ReadAllLines(path).ToList();

            }
            catch
            {
                TaskDialog.Show("Error", "The file could not be opened");
                return Result.Failed;
            }


            //3. Parse model parameters
            int curveIndex = -1;

            // get pre evac time
            Parameter PreEvacuationTime = projInfo.LookupParameter("PreEvacuationTime");
            string preEvacTimeStr = "";
            
            if (PreEvacuationTime != null && PreEvacuationTime.AsString() != "")
            {

                string str = PreEvacuationTime.AsString();
                //TaskDialog.Show("Debug", PreEvacuationTime.AsString());
                if (str.Contains("(")) // ==> implies its a distribution and not a discrete value
                {
                    curveIndex +=1;
                    //preEvacTimeStr = WaitCurveDict["unif"];
                    string[] res = parseDistribution(str);
                    preEvacTimeStr = DefinitionsDict[res[0]];

                    for (int j = 1; j < res.Count(); j++)
                    {
                        preEvacTimeStr = preEvacTimeStr.Replace("<" + j.ToString() + ">", res[j]);
                        preEvacTimeStr = preEvacTimeStr.Replace("<unit>", "s");
                        preEvacTimeStr = preEvacTimeStr.Replace("<id>", curveIndex.ToString());

                    }


                }
                else
                {
                    curveIndex += 1;
                    preEvacTimeStr = DefinitionsDict["Constant"];
                    preEvacTimeStr = preEvacTimeStr.Replace("<1>", str);
                    preEvacTimeStr = preEvacTimeStr.Replace("<unit>", "s");
                    preEvacTimeStr = preEvacTimeStr.Replace("<id>", curveIndex.ToString());

                }
                //TaskDialog.Show("Debug", preEvacTimeStr);
            }

            // get occupant profiles 
            Parameter OccupantProfile = projInfo.LookupParameter("OccupantProfilesList");
            string OccupantProfileDistStr = "";
            string name = "";
            string speed = "";
            string speedProfile = "";
            if (OccupantProfile.AsString() != null && OccupantProfile.AsString() != "")
            {
                string[] profilesList = OccupantProfile.AsString().Split('}');
                string profile = profilesList.First();
                profile = profile.Replace("{", string.Empty);
                //TaskDialog.Show("Debug", temp);
                string[] substr = profile.Split(';');
                // Parse object
                // Parse object
                string subsName = substr[0];
                string subsSpeed = substr[1];
                string subsSpeedProfile = substr[2];
                //clean
                name = subsName.Substring(subsName.IndexOf('=') + 1);
                speed = subsSpeed.Substring(subsSpeed.IndexOf('=') + 1);
                speedProfile = subsSpeedProfile.Substring(subsSpeedProfile.IndexOf('=') + 1);


                if (speedProfile.Contains("(")) // ==> implies its a distribution and not a discrete value
                {
                    curveIndex += 1;
                    //preEvacTimeStr = WaitCurveDict["unif"];
                    string[] res = parseDistribution(speedProfile);
                    OccupantProfileDistStr = DefinitionsDict[res[0]];

                    for (int j = 1; j < res.Count(); j++)
                    {
                        OccupantProfileDistStr = OccupantProfileDistStr.Replace("<" + j.ToString() + ">", res[j]);
                        OccupantProfileDistStr = OccupantProfileDistStr.Replace("<unit>", "m\\/s");
                        OccupantProfileDistStr = OccupantProfileDistStr.Replace("<id>", curveIndex.ToString());


                    }


                }

                else if (speedProfile == "Constant")
                {
                    curveIndex += 1;
                    OccupantProfileDistStr = DefinitionsDict["Constant"];
                    OccupantProfileDistStr = OccupantProfileDistStr.Replace("<1>", speed);
                    OccupantProfileDistStr = OccupantProfileDistStr.Replace("<unit>", "m\\/s");
                    OccupantProfileDistStr = OccupantProfileDistStr.Replace("<id>", curveIndex.ToString());

                }
                //TaskDialog.Show("Debug", OccupantProfileDistStr);

            }

            //4. Loop through lines from the input file 
            int i = 0;
            int distTagIndx = 0;
            int eventTagIndx = 0;
            for (int index = 0; index < lines.Count; index++)
            {
                string line = lines[index];

                // edit behaviour 
                if (line == "[behaviors]")
                {
                    if (preEvacTimeStr != "")
                    {
                        string temp = lines.ElementAt(i + 1);
                        int idx = temp.IndexOf("tag"); // get back by 3 characters
                        temp = temp.Insert(idx, "wait curve " + preEvacTimeStr.Split(':').First() + ";");
                        lines[i + 1] = temp;
                        
                    }

                }

                // edit profiles 
                if (line == "[profiles]")
                {
                    if (OccupantProfileDistStr != "")
                    {
                        lines[i + 1] = DefinitionsDict["Profile"].Replace("<ProfileName>", name).Replace("<CurveID>", "" + OccupantProfileDistStr.Split(':').First());
                        
                    }


                }

                // edit distributions - find its location if it exists to append the lines below it 
                if (line == "[distributions]")
                {
                    distTagIndx = i;
                }

                // find the location of events tag - used to close doors - if it exists
                if (line == "[events]")
                {
                    eventTagIndx = i;
                }


                // parse the nodes
                if (line == "[nodes]")
                {
                    int k = i + 1;
                    while (lines[k] != "")
                    {
                        string updatedNodeDefition = "";
                        string node = lines[k];
                        string key = node.Split(':').First();
                        // Extract all the string between "..."
                        string nodeName = node.Substring(node.IndexOf('"') + 1, node.LastIndexOf('"') - 1 - node.IndexOf('"'));

                        string type = "unknown";
                        string nodeId = nodeName.Split('_').Last();
                        if (nodeName.Contains("Room"))
                        {
                            type = "Room";
                        }
                        else if (nodeName.Contains("Door"))
                        {
                            type = "Door";

                            // check if door is closed 
                            int dooeEleId = -1;
                            if (int.TryParse(nodeId, out dooeEleId))
                            {
                                ElementId doorId = new ElementId(dooeEleId);

                                Element ele = doc.GetElement(doorId);
                                string isDoorClosed = ele.LookupParameter("isAccessible").AsString();

                                if (isDoorClosed == "False" || isDoorClosed == "false")
                                {
                                    // add to the list of events to be generated
                                    closedDoorsIds.Add(key);

                                    // edit the node definition
                                    // e.g 1: "Door02_217336" -1, -1 => 1: "Door02_217336" -1, 0
                                    string[] updatedNode = node.Split(' ');
                                    updatedNode[updatedNode.Count() - 1] = " 0";
                                    
                                    foreach (string s in updatedNode)
                                    {
                                        updatedNodeDefition += s + " ";
                                    }
                                    // remove last comma
                                    updatedNodeDefition=updatedNodeDefition.Remove(updatedNodeDefition.Count()-1, 1);
                                    //TaskDialog.Show("Debug", updatedNodeDefition);

                                }


                            }

                            }

                            string[] value = { nodeName, nodeId, type };
                            NodesList.Add(key, value);

                        if (updatedNodeDefition != "")
                        {
                            lines[k] = updatedNodeDefition;
                        }
                        k++;
                    }

                }

                // Parse doors to set flowrate
                if (line == "[doors]")
                {
                    int k = i + 1;
                    while (lines[k] != "")
                    {
                        string doorLine = lines[k];
                        string[] param = doorLine.Split(' ');
                        string key = param.ElementAt(1);
                        if (NodesList[key].ElementAt(2) == "Door")
                        {
                            string doorID = NodesList[key].ElementAt(1);
                            //TaskDialog.Show(k.ToString(), NodesList[key].ElementAt(0));
                            int doorEleId = -1;


                            if (int.TryParse(NodesList[key].ElementAt(1), out doorEleId))
                            {
                                ElementId doorId = new ElementId(doorEleId);

                                Element ele = doc.GetElement(doorId);
                                string doorflowrate = ele.LookupParameter("RequiredDoorFlowrate").AsString();


                                //TaskDialog.Show("Debug", lines[k]);
                                // edit the line ! 
                                if (doorflowrate != "" && doorflowrate != null)
                                {
                                    param[5] = doorflowrate;
                                    string resultingLine = "";
                                    foreach (string s in param)
                                    {
                                        resultingLine += " " + s;
                                    }
                                    resultingLine = resultingLine.Remove(0, 1); // remove first space
                                    doorLine = resultingLine;
                                    //TaskDialog.Show("Debug", resultingLine);

                                }

                            }


                        }

                        lines[k] = doorLine;
                        k++;

                    }

                }

                i++;

            }

            // edit distribution 
            if (distTagIndx == 0)
            {
                // distirbution does not exist 
                // add it at the end 
                lines.Add("[distributions]");
                

            }

            distTagIndx = lines.IndexOf("[distributions]");
        
            if (preEvacTimeStr != "")
            {
                distTagIndx++;
                lines.Insert(distTagIndx, preEvacTimeStr);
            }
            if (OccupantProfileDistStr != "")
            {
                distTagIndx++;
                lines.Insert(distTagIndx, OccupantProfileDistStr);
            }


            // add the events 
            if (eventTagIndx == 0 && closedDoorsIds.Count() > 0)
            {
                // events does not exist 
                // add it at the end 
                lines.Add("[events]");

            }

            eventTagIndx = lines.IndexOf("[events]");
            
            if (closedDoorsIds.Count()>0)
            {
                
                foreach (string cd in closedDoorsIds)
                {
                    eventTagIndx+=1;
                    // 0.0 close_door <id> -1.0
                    lines.Insert(eventTagIndx, DefinitionsDict["CloseDoor"].Replace("<doorId>", cd));

                }

            }


            // Write lines into file 
            File.WriteAllLines(savePath, lines);

            TaskDialog.Show("Success", "The resulting input file was saved to the same folder");

            

            return Result.Succeeded;
        }


        public static string[] parseDistribution(string dist)
        {
            List<string> result = new List<string>();
            string[] tmp = dist.Replace(")", string.Empty).Split('(');
            string[] tmp2 = tmp[1].Split(',');

            result.Add(tmp[0]);

            foreach (string s in tmp2)
            {
                result.Add(s);
            }


            return result.ToArray();

        }

    }
}
