using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
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
        public int total_use { get; set; }
        public string first_in { get; set; }
        public string last_out { get; set; }
        public string room { get; set; }
        public string last_out_name { get; set; }
    }

    public class DoorFlowRate
    {
        public int total_use { get; set; }
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


        public EvacSimModel ImportPathfinderResults()
        {
            EvacSimModel EvClass = new EvacSimModel();
            EvClass.rooms = new List<Room>();
            EvClass.doors = new List<Door>();


            foreach (RoomUsage r in this.room_usage)
            {
                Room room = new Room();
                room.name = r.room;
                room.initial_occupants_number = r.total_use; // replace with actual initial number - before movmeent starts
                room.id = room.name.Split('_').Last();
                room.RSET = Convert.ToDouble(r.last_out);

                EvClass.rooms.Add(room);
            }

            foreach (DoorFlowRate d in this.door_flow_rates)
            {
                Door door = new Door();
                door.name = d.door;
                door.id = door.name.Split('_').Last();

                door.first_in = Convert.ToDouble(d.first_in);
                door.total_use = Convert.ToDouble(d.total_use);
                door.last_out = Convert.ToDouble(d.last_out);
                door.flow_avg = Convert.ToDouble(d.flow_avg);

                EvClass.doors.Add(door);
            }

            Building b = new Building();
            b.RSET = Convert.ToDouble(this.completion_times_all.max.time);
            b.max_walk_dist = Convert.ToDouble(this.movement_distances_all.max.distance);
            b.min_walk_dist = Convert.ToDouble(this.movement_distances_all.min.distance);
            b.avg_walk_dist = Convert.ToDouble(this.movement_distances_all.average);
            EvClass.build = b;

            return EvClass;

        }
    }

}
