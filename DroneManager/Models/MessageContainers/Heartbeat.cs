﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DroneConnection;
using NLog;
using Utilities;

namespace DroneManager.Models.MessageContainers
{
    /*

        The heartbeat message shows that a system is present and responding. 
        The type of the MAV and Autopilot hardware allow the receiving system to treat further messages 
        from this system appropriate (e.g. by laying out the user interface based on the autopilot).
        
        type	uint8_t	Type of the MAV (quadrotor, helicopter, etc., up to 15 types, defined in MAV_TYPE ENUM)
        autopilot	uint8_t	Autopilot type / class. defined in MAV_AUTOPILOT ENUM
        base_mode	uint8_t	System mode bitfield, see MAV_MODE_FLAG ENUM in mavlink/include/mavlink_types.h
        custom_mode	uint32_t	A bitfield for use for autopilot-specific flags.
        system_status	uint8_t	System status flag, see MAV_STATE ENUM
        mavlink_version	uint8_t_mavlink_version	MAVLink version, not writable by user, gets added by protocol because of magic data type: uint8_t_mavlink_version
    */
    public class Heartbeat : MessageContainerBase
    {
        static Logger logger = LogManager.GetLogger("applog");

        // message parameters
        public MAVLink.MAV_TYPE type { get; set; }
        public MAVLink.MAV_AUTOPILOT autopilot { get; set; }
        public UInt32 custom_mode { get; set; }
        public List<MAVLink.MAV_MODE_FLAG> base_mode { get; set; }
        public MAVLink.MAV_STATE system_status { get; set; }
        public int mavlink_version { get; set; }

        public override MAVLink.MAVLINK_MSG_ID MessageID { get; } = MAVLink.MAVLINK_MSG_ID.HEARTBEAT;

        public override string DesctiptionUrl
        {
            get
            {
                return "https://pixhawk.ethz.ch/mavlink/#HEARTBEAT";
            }
        }

        public Heartbeat(MavLinkMessage message) : base(null)
        {
            if (message.messid == this.MessageID)
            {
                // this code is faster... run it and pass null to base message for speed increase
                // base message uses reflection
                MAVLink.mavlink_heartbeat_t raw_data = (MAVLink.mavlink_heartbeat_t)message.data_struct;
                type = (MAVLink.MAV_TYPE)raw_data.type;
                autopilot = (MAVLink.MAV_AUTOPILOT)raw_data.autopilot;
                custom_mode = raw_data.custom_mode;
                base_mode = Utilities.BitwiseOperations.parseBitValues<MAVLink.MAV_MODE_FLAG>(raw_data.base_mode);
                system_status = (MAVLink.MAV_STATE)raw_data.system_status;
                mavlink_version = (int)raw_data.mavlink_version;
            }

        }

        public override Type getStructType()
        {
            return typeof(MAVLink.mavlink_heartbeat_t);
        }
    }
}
