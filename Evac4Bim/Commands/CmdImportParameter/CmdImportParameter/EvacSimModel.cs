using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// Standard interface for storing egress simulation data 
/// Also responsible for writiing data into corresponding project parameters
/// </summary>


namespace Evac4Bim

{
    public class Room
    {
        /// <summary>
        /// Class representing a single room 
        /// <parameter>RSET : time to last occupant leaving (seconds)</parameter>
        /// </summary>
        public string initial_occupants_number { get; set; }
        public string RSET { get; set; }
        public List<Double> RSET_array = new List<Double>();
        public string name { get; set; }
        public string id { get; set; }

        public string occupantCountHistory { get; set; } //tuple with <time , remaining  > ;

    }

    public class Stair
    {


        public string first_in { get; set; }
        public List<Double> first_in_array = new List<Double>();

        public string last_out { get; set; }
        public List<Double> last_out_array = new List<Double>();

        public string flow_avg { get; set; }
        public List<Double> flow_avg_array = new List<Double>();

        public string name { get; set; }
        public string id { get; set; }
        public string occupantCountHistory { get; set; } //tuple with <time , remaining  > ;

    }
    public class Door
    {
        /// <summary>
        /// Class representing a single door component 
        /// <parameter>total_use : number of occupant crossing door</parameter>
        /// <parameter>first_in : time of first occupant crossing door (seconds)</parameter>
        /// <parameter>first_in : time of last occupant crossing door (seconds)</parameter>
        /// </summary> 
        public string total_use { get; set; }
        public List<Double> total_use_array = new List<Double>();
        public string first_in { get; set; }
        public List<Double> first_in_array = new List<Double>();
        public string last_out { get; set; }
        public List<Double> last_out_array = new List<Double>();
        public string flow_avg { get; set; }
        public List<Double> flow_avg_array = new List<Double>();
        public string name { get; set; }
        public string id { get; set; }
        public string doorFlowHistory { get; set; }

    }

    public class Building
    {
        /// <summary>
        /// Class representing a single room 
        /// <parameter>RSET : time to last occupant leaving (seconds)</parameter>
        /// <parameter>min_walk_dist : walking distance (meters)</parameter>
        /// <parameter>max_walk_dist : walking distance (meters)</parameter>
        /// <parameter>avg_walk_dist : walking distance (meters)</parameter>
        /// </summary>
        public string RSET { get; set; }
        public List<Double> RSET_array = new List<Double>();
        public string min_walk_dist { get; set; }
        public List<Double> min_walk_dist_array = new List<Double>();
        public string max_walk_dist { get; set; }
        public List<Double> max_walk_dist_array = new List<Double>();
        public string avg_walk_dist { get; set; }
        public List<Double> avg_walk_dist_array = new List<Double>();
        public string min_exit_time { get; set; }
        public List<Double> min_exit_time_array = new List<Double>();
        public string avg_exit_time { get; set; }
        public List<Double> avg_exit_time_array = new List<Double>();
        public string TotalOccupantCountHistory { get; set; } //triplet with <time , remaining , exited > ;

    }

    public class EvacSimModel
    {
        public List<Room> rooms { get; set; }
        public List<Door> doors { get; set; }
        public List<Stair> stairs { get; set; }
        public Building build { get; set; }
        public string SoftwareName { get; set; }
        public string SoftwareVersion { get; set; }
        public string SimulationSummary { get; set; }
        public string SoftwareVendor { get; set; }

        public bool WriteIntoRevitModel(Document doc)
        {


            // Parse rooms 
            foreach (Room room in this.rooms)
            {

                //TaskDialog.Show(room.room, room_id);

                Element ele = null;
                ElementId eleID = null;
                try
                {
                    eleID = new ElementId(int.Parse(room.id));
                }
                catch
                {
                    continue; // id not correct or does not exist
                }


                try
                {
                    ele = doc.GetElement(eleID);
                }
                catch
                {

                    continue;
                }

                // set parameters
                try
                {
                    ele.LookupParameter("RSET").Set(room.RSET.ToString());
                    ele.LookupParameter("InitialOccupantNumber").Set(room.initial_occupants_number.ToString());
                    ele.LookupParameter("occupantCountHistory").Set(room.occupantCountHistory.ToString());


                }
                catch
                {
                    continue;
                }

            }

            // Parse doors
            foreach (Door door in this.doors)
            {

                Element ele = null;
                ElementId eleID = null;
                try
                {
                    eleID = new ElementId(int.Parse(door.id));
                }
                catch
                {
                    continue; // id not correct or does not exist
                }

                try
                {
                    ele = doc.GetElement(eleID);
                }
                catch
                {
                    continue;
                }

                // set parameters
                try
                {
                    ele.LookupParameter("FirstIn").Set(door.first_in.ToString());
                    ele.LookupParameter("LastOut").Set(door.last_out.ToString());
                    ele.LookupParameter("FlowAvg").Set(door.flow_avg.ToString());
                    ele.LookupParameter("TotalUse").Set(door.total_use.ToString());
                    ele.LookupParameter("doorFlowHistory").Set(door.doorFlowHistory.ToString());

                }
                catch
                {
                    continue;
                }

            }

            // Stairs 
            // TaskDialog.Show("Debug", this.stairs.Count().ToString());
            foreach (Stair stair in this.stairs)
            {

                Element ele = null;
                ElementId eleID = null;
                try
                {
                    eleID = new ElementId(int.Parse(stair.id));
                }
                catch
                {
                    continue; // id not correct or does not exist
                }

                try
                {
                    ele = doc.GetElement(eleID);
                }
                catch
                {
                    continue;
                }

                // set parameters
                try
                {
                    ele.LookupParameter("FirstIn").Set(stair.first_in.ToString());
                    ele.LookupParameter("LastOut").Set(stair.last_out.ToString());
                    ele.LookupParameter("FlowAvg").Set(stair.flow_avg.ToString());
                    ele.LookupParameter("occupantCountHistory").Set(stair.occupantCountHistory.ToString());
                }
                catch
                {
                    continue;
                }

            }

            // building - proj info
            Element projInfo = doc.ProjectInformation as Element;
            projInfo.LookupParameter("TotalRSET").Set(this.build.RSET.ToString());
            projInfo.LookupParameter("MaxWalkDistance").Set(this.build.max_walk_dist.ToString());
            projInfo.LookupParameter("AvgWalkDistance").Set(this.build.avg_walk_dist.ToString());
            projInfo.LookupParameter("MinWalkDistance").Set(this.build.min_walk_dist.ToString());

            projInfo.LookupParameter("MinExitTime").Set(this.build.min_exit_time.ToString());
            projInfo.LookupParameter("AvgExitTime").Set(this.build.avg_exit_time.ToString());

            projInfo.LookupParameter("TotalOccupantCountHistory").Set(this.build.TotalOccupantCountHistory.ToString());

            // Software summary 
            projInfo.LookupParameter("SimulationSummary").Set(this.SimulationSummary.ToString());
            projInfo.LookupParameter("SoftwareName").Set(this.SoftwareName.ToString());
            projInfo.LookupParameter("SoftwareVersion").Set(this.SoftwareVersion.ToString());
            projInfo.LookupParameter("SoftwareVendor").Set(this.SoftwareVendor.ToString());


            return true;
        }

        public static EvacSimModel MergeMultipleRuns(Document doc, List<EvacSimModel> EvacEvClassM)
        {
            EvacSimModel mergedModel = new EvacSimModel();
            mergedModel.rooms = new List<Room>();
            mergedModel.doors = new List<Door>();
            mergedModel.stairs = new List<Stair>();

            Dictionary<string, Room> roomList = new Dictionary<string, Room>();
            Dictionary<string, Stair> stairList = new Dictionary<string, Stair>();
            Dictionary<string, Door> doorList = new Dictionary<string, Door>();

            Building mergedBuild = new Building();

            // Qeurry all doors, stairs and rooms
            foreach (EvacSimModel evc in EvacEvClassM)
            {
                // Init simulation summary with a default value
                mergedModel.SimulationSummary = evc.SimulationSummary;
                mergedModel.SoftwareName = evc.SoftwareName;
                mergedModel.SoftwareVersion = evc.SoftwareVersion;
                mergedModel.SoftwareVendor = evc.SoftwareVendor;


                foreach (Room r in evc.rooms)
                {
                    // Loop rooms 
                    // If rooms was not already, append the list 
                    if (!roomList.ContainsKey(r.name))
                    {
                        Room append = new Room();
                        append.name = r.name;
                        append.id = r.name.Split('_').Last();

                        roomList.Add(r.name, append);
                    }
                    // else, retrieve it 
                    Room mergedRoom = roomList[r.name];
                    // append / update its properties                     
                    mergedRoom.RSET_array.Add(Double.Parse(r.RSET));




                }
                foreach (Stair r in evc.stairs)
                {
                    // Loop stairs 
                    // If stair was not already, append the list 
                    if (!stairList.ContainsKey(r.name))
                    {
                        Stair append = new Stair();
                        append.name = r.name;
                        append.id = r.name.Split('_').Last();

                        stairList.Add(r.name, append);
                    }
                    // else, retrieve it 
                    Stair mergedStair = stairList[r.name];
                    // append / update its properties
                    mergedStair.first_in_array.Add(Double.Parse(r.first_in));
                    mergedStair.last_out_array.Add(Double.Parse(r.last_out));
                    mergedStair.flow_avg_array.Add(Double.Parse(r.flow_avg));



                }
                foreach (Door r in evc.doors)
                {
                    // Loop door 
                    // If door was not already, append the list 
                    if (!doorList.ContainsKey(r.name))
                    {
                        Door append = new Door();
                        append.name = r.name;
                        append.id = r.name.Split('_').Last();
                        doorList.Add(r.name, append);
                    }
                    // else, retrieve it 
                    Door mergedDoor = doorList[r.name];
                    // append / update its properties
                    mergedDoor.first_in_array.Add(Double.Parse(r.first_in));
                    mergedDoor.last_out_array.Add(Double.Parse(r.last_out));
                    mergedDoor.flow_avg_array.Add(Double.Parse(r.flow_avg));
                    mergedDoor.total_use_array.Add(Double.Parse(r.total_use));


                }


                // Building info
                mergedBuild.RSET_array.Add(Double.Parse(evc.build.RSET));
                mergedBuild.min_walk_dist_array.Add(Double.Parse(evc.build.min_walk_dist));
                mergedBuild.max_walk_dist_array.Add(Double.Parse(evc.build.max_walk_dist));
                mergedBuild.avg_walk_dist_array.Add(Double.Parse(evc.build.avg_walk_dist));
                mergedBuild.min_exit_time_array.Add(Double.Parse(evc.build.min_exit_time));
                mergedBuild.avg_exit_time_array.Add(Double.Parse(evc.build.avg_exit_time));

            }

            // Now process the data stored in arrays ! 
            
 
            // Loop through rooms 
            foreach (Room r in roomList.Values)
            {
                Room final = new Room();
                final.name = r.name;
                final.id = r.id;
                final.initial_occupants_number = "n.a";// cannot be processed yet
                final.occupantCountHistory = "n.a"; // cannot be processed yet
                // process the array 
                final.RSET = procArray(r.RSET_array);

                //TaskDialog.Show(final.name, final.RSET);

                mergedModel.rooms.Add(final);



            }

            foreach (Stair r in stairList.Values)
            {
                Stair final = new Stair();
                final.name = r.name;
                final.id = r.id;
                final.occupantCountHistory = "n.a"; // cannot be processed yet
                // process the array 
                final.first_in = procArray(r.first_in_array);
                final.last_out = procArray(r.last_out_array);
                final.flow_avg = procArray(r.flow_avg_array);

                //TaskDialog.Show(final.name, final.RSET);

                mergedModel.stairs.Add(final);

            }

            foreach (Door r in doorList.Values)
            {
                Door final = new Door();
                final.name = r.name;
                final.id = r.id;
                // process the array 
                final.first_in = procArray(r.first_in_array);
                final.last_out = procArray(r.last_out_array);
                final.flow_avg = procArray(r.flow_avg_array);
                final.total_use = procArray(r.total_use_array);
                final.doorFlowHistory = "n.a"; // cannot be processed yet

                //TaskDialog.Show(final.name, final.RSET);

                mergedModel.doors.Add(final);

            }


            // Building info
             
            mergedBuild.RSET = procArray(mergedBuild.RSET_array);
            mergedBuild.min_walk_dist = procArray(mergedBuild.min_walk_dist_array);
            mergedBuild.max_walk_dist = procArray(mergedBuild.max_walk_dist_array);
            mergedBuild.avg_walk_dist = procArray(mergedBuild.avg_walk_dist_array);
            mergedBuild.min_exit_time = procArray(mergedBuild.min_exit_time_array);
            mergedBuild.avg_exit_time = procArray(mergedBuild.avg_exit_time_array);
            mergedBuild.TotalOccupantCountHistory = "n.a";

            mergedModel.build = mergedBuild;


            return mergedModel;
        }


        // Process an array and return a string contaning min,max,avg and std dev
        public static string procArray (List<double> nums)
        {
            string str = "avg=<avg>,min=<min>,max=<max>,std=<std>";

            double min = Math.Round(nums.Min(), 2);
            double max = Math.Round(nums.Max(), 2);
            double avg = Math.Round(nums.Average(), 2);
            double std = Math.Round(stdDev(nums), 2);
            string result = str;
            result = result.Replace("<min>", min.ToString());
            result = result.Replace("<max>", max.ToString());
            result = result.Replace("<avg>", avg.ToString());
            result = result.Replace("<std>", std.ToString());

            return result;
        }
        public static double stdDev(List<double> nums)
        {
            if (nums.Count() > 1)
            {

                // Get the average of the values
                double avg = nums.Average();

                // Now figure out how far each point is from the mean
                // So we subtract from the number the average
                // Then raise it to the power of 2
                double sumOfSquares = 0.0;

                foreach (double num in nums)
                {
                    sumOfSquares += Math.Pow((num - avg), 2.0);
                }

                // Finally divide it by n - 1 (for standard deviation variance)
                // Or use length without subtracting one ( for population standard deviation variance)
                double variance = sumOfSquares / (double)(nums.Count() - 1);
                // Square root the variance to get the standard deviation
                return Math.Sqrt(variance);

            }
            else { return 0.0; }
        }






    }


}

