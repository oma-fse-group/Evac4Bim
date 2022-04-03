using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <Parameter map>
/// <Doors>
/// first_in => "FirstIn"
/// last_out =>  "LastOut"
/// flow_avg => "FlowAvg"
/// total_use => "TotalUse"
/// </Doors>
/// <Rooms>
/// first_in => "FirstIn"
/// last_out =>  "RSET"
/// </Rooms>
/// </Parameter map>

/// <note>
/// Generated using 
/// https://json2csharp.com/ 
/// </note>
/// 




namespace Evac4Bim
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Min
    {
        public string distance { get; set; }
        public string name { get; set; }
        public string time { get; set; }
    }

    public class Max
    {
        public string distance { get; set; }
        public string name { get; set; }
        public string time { get; set; }
    }

    public class MovementDistanceProfile
    {
        public Min min { get; set; }
        public string avg { get; set; }
        public Max max { get; set; }
        public string profile { get; set; }
        public int count { get; set; }
        public string stdDev { get; set; }
    }

    public class CompletionTimesAll
    {
        public string average { get; set; }
        public Min min { get; set; }
        public string stdDev { get; set; }
        public Max max { get; set; }
    }

    public class CompletionTimesProfile
    {
        public Min min { get; set; }
        public string avg { get; set; }
        public Max max { get; set; }
        public string profile { get; set; }
        public int count { get; set; }
        public string stdDev { get; set; }
    }

    public class MovementDistancesAll
    {
        public string average { get; set; }
        public Min min { get; set; }
        public string stdDev { get; set; }
        public Max max { get; set; }
    }

    public class RoomUsage
    {
        public string total_use { get; set; }
        public string first_in { get; set; }
        public string last_out { get; set; }
        public string room { get; set; }
        public string last_out_name { get; set; }

        


    }

    public class DoorFlowRate
    {
        public string total_use { get; set; }
        public string door { get; set; }
        public string first_in { get; set; }
        public string last_out { get; set; }
        public string flow_avg { get; set; }
        public string last_out_name { get; set; }
    }

    public class MovementDistancesBehavior
    {
        public Min min { get; set; }
        public string avg { get; set; }
        public Max max { get; set; }
        public int count { get; set; }
        public string behavior { get; set; }
        public string stdDev { get; set; }
    }

    public class CompletionTimesBehavior
    {
        public Min min { get; set; }
        public string avg { get; set; }
        public Max max { get; set; }
        public int count { get; set; }
        public string behavior { get; set; }
        public string stdDev { get; set; }
    }



    public class PathfinderResultDeserializer
    {
        public string cpu_time { get; set; }
        public string startup_time { get; set; }
        public int components_all { get; set; }
        public string simulation { get; set; }
        public List<MovementDistanceProfile> movement_distance_profile { get; set; }
        public string version { get; set; }
        public CompletionTimesAll completion_times_all { get; set; }
        public List<CompletionTimesProfile> completion_times_profile { get; set; }
        public MovementDistancesAll movement_distances_all { get; set; }
        public string mode { get; set; }
        public int triangles { get; set; }
        public List<RoomUsage> room_usage { get; set; }
        public int total_occupants { get; set; }
        public List<DoorFlowRate> door_flow_rates { get; set; }
        public List<MovementDistancesBehavior> movement_distances_behavior { get; set; }
        public int components_doors { get; set; }
        public List<CompletionTimesBehavior> completion_times_behavior { get; set; }


        public EvacSimModel ImportPathfinderResults(string roomCsvFilePath, string doorCsvPath)
        {
            EvacSimModel EvClass = new EvacSimModel();
            EvClass.rooms = new List<Room>();
            EvClass.doors = new List<Door>();
            EvClass.stairs = new List<Stair>();

            // Init simulation summary 
            EvClass.EvacuationSimulationBrief = "n.a";
            EvClass.EvacuationModelName = "Pathfinder";
            EvClass.EvacuationModelVersion = "n.a";
            EvClass.EvacuationModelVendor = "https://www.thunderheadeng.com/pathfinder/";


            // Get csv files 

            string[] contents = null;
            string path = roomCsvFilePath;


            try
            {
                contents = File.ReadAllText(path).Split('\n');

            }
            catch
            {
                TaskDialog.Show("Error", "The results file could not be opened");
                //return Result.Failed;
            }

            // Parse csv file
            var csv = from line in contents
                      where !String.IsNullOrEmpty(line)
                      select line.Split(',').ToArray();

            // get header (first line)
            List<string> header = csv.FirstOrDefault().ToList();

            // get time (first column)
            List<string> time = getColumn(0, csv.Skip(1));

            // Register number of values and timestep
            EvClass.numberOfValues = time.Count.ToString();
            double timestep = 0;
             
                if (time.Count>1)
                {

                try
                {
                    double t0 = double.Parse(time.ElementAt(0));
                    double t1 = double.Parse(time.ElementAt(1));

                    timestep = t1 - t0;

                }
                catch
                {

                }
                
                }
            EvClass.csvTimeStep = timestep.ToString();
            

            /***
             * 
             * 
             * 
             * */
            // Csv file for doors 
            string[] contentsDoor = null;
            string pathDoorCsv = doorCsvPath;

            try
            {
                contentsDoor = File.ReadAllText(pathDoorCsv).Split('\n');

            }
            catch
            {
                TaskDialog.Show("Error", "The results file could not be opened");
                //return Result.Failed;
            }

            // Parse csv file
            var csvDoor = from line in contentsDoor
                          where !String.IsNullOrEmpty(line)
                          select line.Split(',').ToArray();

            // get header (first line)
            List<string> headerDoor = csvDoor.FirstOrDefault().ToList();          

            // get time (first column)
            List<string> timeDoor = getColumn(0, csvDoor.Skip(1));
        


            // Loop through rooms
            foreach (RoomUsage r in this.room_usage)
            {
                if (r.room.Contains("Stair"))
                {
                    
                    Stair stair = new Stair();
                    stair.name = r.room;                    
                    stair.id = stair.name.Split('_').Last();
                    stair.FirstOccupantInTime = r.first_in;
                    stair.LastOccupantOutTime = r.last_out;

                    stair.AverageOccupantFlowrate = "NaN"; //  Does not exist in Pathfinder


                    
                    // Write stair history 
                    stair.OccupancyHistory = "";
                    // get column index number of a room  (by its name)
                    string selectedRoomName = stair.name;
                    int idx = header.IndexOf("\"" + selectedRoomName + "\"");
                    List<string> roomUsage = getColumn(idx, csv.Skip(1)); // skip first line- header
                                                                          // loop through time and usage columns 
                    for (int i = 0; i < time.Count(); i++)
                    {
                        stair.OccupancyHistory += Double.Parse(time.ElementAt(i)).ToString() + "," + Double.Parse(roomUsage.ElementAt(i)).ToString() + ";";

                    }
                    stair.OccupancyHistory = stair.OccupancyHistory.Remove(stair.OccupancyHistory.Length - 1);


                    // Append stair list
                    EvClass.stairs.Add(stair);

                }
                else
                {
                    Room room = new Room();
                    room.name = r.room;
                    room.InitialOccupancyNumber = r.total_use; // replace with actual initial number - before movmeent starts
                    room.id = room.name.Split('_').Last();
                    // Exceptions 
                    if (r.last_out == "") { r.last_out = "0"; }
                    room.EvacuationTime = r.last_out;


                    // Write room history 
                    room.OccupancyHistory = "";
                    // get column index number of a room  (by its name)
                    string selectedRoomName = room.name;
                    int idx = header.IndexOf("\"" + selectedRoomName + "\"");
                    List<string> roomUsage = getColumn(idx, csv.Skip(1)); // skip first line- header
                                                                          // loop through time and usage columns 
                    for (int i = 0; i < time.Count(); i++)
                    {
                        room.OccupancyHistory += Double.Parse(time.ElementAt(i)).ToString() + "," + Double.Parse(roomUsage.ElementAt(i)).ToString() + ";";

                    }
                    room.OccupancyHistory = room.OccupancyHistory.Remove(room.OccupancyHistory.Length - 1);




                    EvClass.rooms.Add(room);
                }
                
            }

            foreach (DoorFlowRate d in this.door_flow_rates)
            {
                Door door = new Door();
                door.name = d.door;
                door.id = door.name.Split('_').Last();

                door.FirstOccupantInTime = d.first_in;
                door.TotalUse = d.total_use;
                door.LastOccupantOutTime = d.last_out;
                door.AverageOccupantFlowrate = d.flow_avg;

                // Write door history 
                door.DoorFlowrateHistory = "";
                // get column index number of a room  (by its name)
                string selectedName = door.name;                
                int idx = headerDoor.IndexOf("\"" + selectedName + "\"");
                List<string> doorUsage = getColumn(idx, csvDoor.Skip(1)); // skip first line- header
                
                
                // loop through time and usage columns 


                for (int i = 0; i < timeDoor.Count(); i++)
                {


                    double t = Double.Parse(timeDoor.ElementAt(i));
                    double usg = Double.Parse(doorUsage.ElementAt(i));


                    double step = t;
                    if (i > 0)
                    {
                        step = t - Double.Parse(timeDoor.ElementAt(i - 1));
                    }

                    double flw = usg;
                    if (step != 0)
                    {
                        flw = usg / step;

                    }

                    door.DoorFlowrateHistory +=  t.ToString()+ "," + flw.ToString() + ";";

                }
                door.DoorFlowrateHistory = door.DoorFlowrateHistory.Remove(door.DoorFlowrateHistory.Length - 1);


                EvClass.doors.Add(door);
            }


            // Edit building 

            // get column index where data is stored  
            int idx2 = header.IndexOf("\"" + "Remaining (Total)" + "\"");
            List<string> totalRemaining = getColumn(idx2, csv.Skip(1));
            int idx3 = header.IndexOf("\"" + "Exited (Total)" + "\"");
            List<string> totalExited = getColumn(idx3, csv.Skip(1));

            Building b = new Building();
            b.EvacuationTimeOverall = this.completion_times_all.max.time;
            b.MaxTravelDistance = this.movement_distances_all.max.distance;
            b.MinTravelDistance = this.movement_distances_all.min.distance;
            b.AverageTravelDistance = this.movement_distances_all.average;
            b.AverageEvacuationTime = this.completion_times_all.average;
            b.MinEvacuationTime = this.completion_times_all.min.time;

            for (int i = 0; i < time.Count(); i++)
            {
                b.OccupancyHistoryOverall += Double.Parse(time.ElementAt(i)).ToString() + "," + Double.Parse(totalRemaining.ElementAt(i)).ToString() + "," + Double.Parse(totalExited.ElementAt(i)).ToString() + ";";

            }

            // remove last ; 
            b.OccupancyHistoryOverall = b.OccupancyHistoryOverall.Remove(b.OccupancyHistoryOverall.Length-1);

            EvClass.build = b;

            return EvClass;

        }




        /// <summary>
        /// Retrieve content of a specific column 
        /// </summary>
        /// <param name="index">index of a column</param>
        /// <param name="csv">csv file content</param>
        /// <returns></returns>
        public static List<string> getColumn(int index, IEnumerable<string[]> csv)
        {
            List<string> res = new List<string>();

            if (index >= 0)
            {
                var columnQuery =
                from line in csv
                where !String.IsNullOrEmpty(line[index].ToString())
                select Convert.ToString(line[index]);

                res = columnQuery.ToList();
            }



            return res;
        }

     


    }

}
