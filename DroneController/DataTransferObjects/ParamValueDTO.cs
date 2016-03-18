﻿using DroneManager.Models.MessageContainers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DroneController.DataTransferObjects
{
    public class ParamValueDTO
    {
        /// <summary> Onboard parameter value </summary>
        public Single param_value { get; set; }
        /// <summary> Total number of onboard parameters </summary>
        public UInt16 param_count { get; set; }
        /// <summary> Index of this onboard parameter </summary>
        public UInt16 param_index { get; set; }
        /// <summary> Onboard parameter id, terminated by NULL if the length is less than 16 human-readable chars and WITHOUT null termination (NULL) byte if the length is exactly 16 chars - applications have to provide 16+1 bytes storage if the ID is stored as string </summary>
        public string param_id { get; set; }
        /// <summary> Onboard parameter type: see the MAV_PARAM_TYPE enum for supported data types. </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public MAVLink.MAV_PARAM_TYPE param_type { get; set; }

        public ParamValueDTO(ParamValue source)
        {
            Utilities.CopySimilar.CopyAll(source, this);
        }
    }
}