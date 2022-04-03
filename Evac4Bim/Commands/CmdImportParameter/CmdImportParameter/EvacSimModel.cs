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
/// Also responsible for writing data into corresponding project parameters
/// In case of single run simulation : one innstance is created, initialized by Pathfinder.cs then its data written to the model 
/// In case of multiple runs, multiple instances are created and stored in a list List<EvacSimModel> 
/// Then, the list is parsed and data is consolidated in a single and final instance
/// Mupltiple data is translated into distribution with mean,min,max,and stddev 
/// </summary>


namespace Evac4Bim

{
    public class Room
    {
        /// <summary>
        /// Class representing a single room 
        /// <parameter>RSET : time to last occupant leaving (seconds)</parameter>
        /// </summary>
        public string InitialOccupancyNumber { get; set; }
        public string EvacuationTime { get; set; }
        public List<Double> EvacuationTime_array = new List<Double>();
        public string name { get; set; }
        public string id { get; set; }

        public string OccupancyHistory { get; set; } //tuple with <time , remaining  > ;

    }

    public class Stair
    {

        public string FirstOccupantInTime { get; set; }
        public List<Double> FirstOccupantInTime_array = new List<Double>();

        public string LastOccupantOutTime { get; set; }
        public List<Double> LastOccupantOutTime_array = new List<Double>();

        public string AverageOccupantFlowrate { get; set; }
        public List<Double> AverageOccupantFlowrate_array = new List<Double>();

        public string name { get; set; }
        public string id { get; set; }
        public string OccupancyHistory { get; set; } //tuple with <time , remaining  > ;

    }
    public class Door
    {
        /// <summary>
        /// Class representing a single door component 
        /// <parameter>total_use : number of occupant crossing door</parameter>
        /// <parameter>first_in : time of first occupant crossing door (seconds)</parameter>
        /// <parameter>first_in : time of last occupant crossing door (seconds)</parameter>
        /// </summary> 
        public string TotalUse { get; set; }
        public List<Double> TotalUse_array = new List<Double>();
        public string FirstOccupantInTime { get; set; }
        public List<Double> FirstOccupantInTime_array = new List<Double>();
        public string LastOccupantOutTime { get; set; }
        public List<Double> LastOccupantOutTime_array = new List<Double>();
        public string AverageOccupantFlowrate { get; set; }
        public List<Double> AverageOccupantFlowrate_array = new List<Double>();
        public string name { get; set; }
        public string id { get; set; }
        public string DoorFlowrateHistory { get; set; }

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
        public string EvacuationTimeOverall { get; set; }
        public List<Double> EvacuationTimeOverall_array = new List<Double>();
        public string MinTravelDistance { get; set; }
        public List<Double> MinTravelDistance_array = new List<Double>();
        public string MaxTravelDistance { get; set; }
        public List<Double> MaxTravelDistance_array = new List<Double>();
        public string AverageTravelDistance { get; set; }
        public List<Double> AverageTravelDistance_array = new List<Double>();
        public string MinEvacuationTime { get; set; }
        public List<Double> MinEvacuationTime_array = new List<Double>();
        public string AverageEvacuationTime { get; set; }
        public List<Double> AverageEvacuationTime_array = new List<Double>();
        public string OccupancyHistoryOverall { get; set; } //triplet with <time , remaining , exited > ;

    }

    public class EvacSimModel
    {
        public List<Room> rooms { get; set; }
        public List<Door> doors { get; set; }
        public List<Stair> stairs { get; set; }
        public Building build { get; set; }
        public string EvacuationModelName { get; set; }
        public string EvacuationModelVersion { get; set; }
        public string EvacuationSimulationBrief { get; set; }
        public string EvacuationModelVendor { get; set; }
        public string numberOfValues { get; set; }
        public string csvTimeStep { get; set; }

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
                    ele.LookupParameter("EvacuationTime").Set(room.EvacuationTime.ToString());
                    ele.LookupParameter("InitialOccupancyNumber").Set(room.InitialOccupancyNumber.ToString());
                    ele.LookupParameter("OccupancyHistory").Set(room.OccupancyHistory.ToString());


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
                    ele.LookupParameter("FirstOccupantInTime").Set(door.FirstOccupantInTime.ToString());
                    ele.LookupParameter("LastOccupantOutTime").Set(door.LastOccupantOutTime.ToString());
                    ele.LookupParameter("AverageOccupantFlowrate").Set(door.AverageOccupantFlowrate.ToString());
                    ele.LookupParameter("TotalUse").Set(door.TotalUse.ToString());
                    ele.LookupParameter("DoorFlowrateHistory").Set(door.DoorFlowrateHistory.ToString());

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
                    
                    ele.LookupParameter("FirstOccupantInTime").Set(stair.FirstOccupantInTime.ToString());
                    ele.LookupParameter("LastOccupantOutTime").Set(stair.LastOccupantOutTime.ToString());
                    ele.LookupParameter("AverageOccupantFlowrate").Set(stair.AverageOccupantFlowrate.ToString());
                    ele.LookupParameter("OccupancyHistory").Set(stair.OccupancyHistory.ToString());

                    
                }
                catch
                {
                    continue;
                }

            }

            // building - proj info
            Element projInfo = doc.ProjectInformation as Element;
            projInfo.LookupParameter("EvacuationTimeOverall").Set(this.build.EvacuationTimeOverall.ToString());
            projInfo.LookupParameter("MaxTravelDistance").Set(this.build.MaxTravelDistance.ToString());
            projInfo.LookupParameter("AverageTravelDistance").Set(this.build.AverageTravelDistance.ToString());
            projInfo.LookupParameter("MinTravelDistance").Set(this.build.MinTravelDistance.ToString());

            projInfo.LookupParameter("MinEvacuationTime").Set(this.build.MinEvacuationTime.ToString());
            projInfo.LookupParameter("AverageEvacuationTime").Set(this.build.AverageEvacuationTime.ToString());

            projInfo.LookupParameter("OccupancyHistoryOverall").Set(this.build.OccupancyHistoryOverall.ToString());

            // Software summary 
            projInfo.LookupParameter("EvacuationSimulationBrief").Set(this.EvacuationSimulationBrief.ToString());
            projInfo.LookupParameter("EvacuationModelName").Set(this.EvacuationModelName.ToString());
            projInfo.LookupParameter("EvacuationModelVersion").Set(this.EvacuationModelVersion.ToString());
            projInfo.LookupParameter("EvacuationModelVendor").Set(this.EvacuationModelVendor.ToString());

            projInfo.LookupParameter("CsvTimeStep").Set(this.csvTimeStep.ToString());
            projInfo.LookupParameter("NumberOfValues").Set(this.numberOfValues.ToString());

            




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
                mergedModel.EvacuationSimulationBrief = evc.EvacuationSimulationBrief;
                mergedModel.EvacuationModelName = evc.EvacuationModelName;
                mergedModel.EvacuationModelVersion = evc.EvacuationModelVersion;
                mergedModel.EvacuationModelVendor = evc.EvacuationModelVendor;
                mergedModel.numberOfValues = evc.numberOfValues;
                mergedModel.csvTimeStep = evc.csvTimeStep;


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
                    mergedRoom.EvacuationTime_array.Add(Double.Parse(r.EvacuationTime));




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
                    mergedStair.FirstOccupantInTime_array.Add(Double.Parse(r.FirstOccupantInTime));
                    mergedStair.LastOccupantOutTime_array.Add(Double.Parse(r.LastOccupantOutTime));
                    mergedStair.AverageOccupantFlowrate_array.Add(Double.Parse(r.AverageOccupantFlowrate));



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
                    
                    try
                    {
                        mergedDoor.FirstOccupantInTime_array.Add(Double.Parse(r.FirstOccupantInTime));
                        mergedDoor.LastOccupantOutTime_array.Add(Double.Parse(r.LastOccupantOutTime));
                        mergedDoor.AverageOccupantFlowrate_array.Add(Double.Parse(r.AverageOccupantFlowrate));
                        mergedDoor.TotalUse_array.Add(Double.Parse(r.TotalUse));
                    }
                    catch
                    {
                        mergedDoor.FirstOccupantInTime_array.Add(Double.Parse("0"));
                        mergedDoor.LastOccupantOutTime_array.Add(Double.Parse("0"));
                        mergedDoor.AverageOccupantFlowrate_array.Add(Double.Parse("0"));
                        mergedDoor.TotalUse_array.Add(Double.Parse("0"));
                    }
                     

                }


                // Building info
                mergedBuild.EvacuationTimeOverall_array.Add(Double.Parse(evc.build.EvacuationTimeOverall));
                mergedBuild.MinTravelDistance_array.Add(Double.Parse(evc.build.MinTravelDistance));
                mergedBuild.MaxTravelDistance_array.Add(Double.Parse(evc.build.MaxTravelDistance));
                mergedBuild.AverageTravelDistance_array.Add(Double.Parse(evc.build.AverageTravelDistance));
                mergedBuild.MinEvacuationTime_array.Add(Double.Parse(evc.build.MinEvacuationTime));
                mergedBuild.AverageEvacuationTime_array.Add(Double.Parse(evc.build.AverageEvacuationTime));

            }

            // Now process the data stored in arrays ! 
            
 
            // Loop through rooms 
            foreach (Room r in roomList.Values)
            {
                Room final = new Room();
                final.name = r.name;
                final.id = r.id;
                final.InitialOccupancyNumber = "n.a";// cannot be processed yet
                final.OccupancyHistory = "n.a"; // cannot be processed yet
                // process the array 
                final.EvacuationTime = procArray(r.EvacuationTime_array);

                //TaskDialog.Show(final.name, final.RSET);

                mergedModel.rooms.Add(final);



            }

            foreach (Stair r in stairList.Values)
            {
                Stair final = new Stair();
                final.name = r.name;
                final.id = r.id;
                final.OccupancyHistory = "n.a"; // cannot be processed yet
                // process the array 
                final.FirstOccupantInTime = procArray(r.FirstOccupantInTime_array);
                final.LastOccupantOutTime = procArray(r.LastOccupantOutTime_array);
                final.AverageOccupantFlowrate = procArray(r.AverageOccupantFlowrate_array);

                //TaskDialog.Show(final.name, final.RSET);

                mergedModel.stairs.Add(final);

            }

            foreach (Door r in doorList.Values)
            {
                Door final = new Door();
                final.name = r.name;
                final.id = r.id;
                // process the array 
                final.FirstOccupantInTime = procArray(r.FirstOccupantInTime_array);
                final.LastOccupantOutTime = procArray(r.LastOccupantOutTime_array);
                final.AverageOccupantFlowrate = procArray(r.AverageOccupantFlowrate_array);
                final.TotalUse = procArray(r.TotalUse_array);
                final.DoorFlowrateHistory = "n.a"; // cannot be processed yet

                //TaskDialog.Show(final.name, final.RSET);

                mergedModel.doors.Add(final);

            }


            // Building info
             
            mergedBuild.EvacuationTimeOverall = procArray(mergedBuild.EvacuationTimeOverall_array);
            mergedBuild.MinTravelDistance = procArray(mergedBuild.MinTravelDistance_array);
            mergedBuild.MaxTravelDistance = procArray(mergedBuild.MaxTravelDistance_array);
            mergedBuild.AverageTravelDistance = procArray(mergedBuild.AverageTravelDistance_array);
            mergedBuild.MinEvacuationTime = procArray(mergedBuild.MinEvacuationTime_array);
            mergedBuild.AverageEvacuationTime = procArray(mergedBuild.AverageEvacuationTime_array);
            mergedBuild.OccupancyHistoryOverall = "n.a";

            mergedModel.build = mergedBuild;


            return mergedModel;
        }


        // Process an array and return a string contaning min,max,avg and std dev
        public static string procArray (List<double> nums)
        {
            double min = 0 ;
            double max = 0;
            double avg =0;
            double std = 0;
            string result = "";
            string str = "avg=<avg>,min=<min>,max=<max>,std=<std>";
            try
            {
                min = Math.Round(nums.Min(), 2);
                max = Math.Round(nums.Max(), 2);
                avg = Math.Round(nums.Average(), 2);
                std = Math.Round(stdDev(nums), 2);
                result = str;
            }
            catch
            {
                //TaskDialog.Show("Debg", nums.Count.ToString());
            }
            
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

