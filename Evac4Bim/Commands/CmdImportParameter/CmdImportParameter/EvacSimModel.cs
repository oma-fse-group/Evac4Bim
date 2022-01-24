﻿using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// Standard interface for storing egress simulation data 
/// Also responsible for writiing data into corresponding project parameters
/// </summary>


namespace Evac4Bim
    
{    public class Room
    {
        /// <summary>
        /// Class representing a single room 
        /// <parameter>RSET : time to last occupant leaving (seconds)</parameter>
        /// </summary>
        public string initial_occupants_number { get; set; }
        public string RSET { get; set; }
        public string name { get; set; }
        public string id { get; set; }

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
        public string first_in { get; set; }
        public string last_out { get; set; }
        public string flow_avg { get; set; }
        public string name { get; set; }
        public string id { get; set; }

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
        public string min_walk_dist { get; set; }
        public string max_walk_dist { get; set; }
        public string avg_walk_dist { get; set; }


    }

    public class EvacSimModel
    {
        public List<Room> rooms { get; set; }
        public List<Door> doors { get; set; }
        public Building build { get; set; }

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
    
            return true;
        }

    }

}

