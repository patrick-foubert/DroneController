﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DroneManager.Models;
using DroneManager;
using NLog;
using DataTransferObjects;
using DroneManager.Models.MessageContainers;
using DataTransferObjects.Commands;
using DataTransferObjects.Messages;
using DroneController.DTOFactory;
using DroneController.Responses;

namespace DroneController.Controllers
{
    public class DroneController : ApiController
    {
        static DroneManagementStation droneMgr = new DroneManagementStation();
        static Logger logger = LogManager.GetLogger("applog");

        [HttpGet]
        [Route("drones")]
        public IHttpActionResult get()
        {
            logger.Debug("Entering /drones GET");

            return Ok(this.getActiveRecords());
        }

        private List<DroneDTO> getActiveRecords()
        {
            // return list of active connection data records (trying to serialize the pure connection object will fail due to the data stream)
            List<DroneDTO> result = new List<DroneDTO>();
            List<Drone> drones = droneMgr.Connections;
            foreach (Drone drone in drones)
            {
                // make sure returned results are valid
                if (drone.isConnected())
                {
                    result.Add(DTOFactory.DTOFactory.createDroneDTO(drone));
                }
                else
                {
                    // remove stale entries
                    droneMgr.disconnect(drone.id);
                }
            }
            return result;
        }

        [HttpGet]
        [Route("drones/discover")]
        public IHttpActionResult discover()
        {
            logger.Debug("Entering /discover");
            droneMgr.discover();
            return Ok();
        }

        [HttpPut]
        [Route("drones/{id}/disconnect")]
        public IHttpActionResult disconnect(string id)
        {
            logger.Debug("Disconnecting /drones/{0}", id);
            Boolean success = droneMgr.disconnect(new Guid(id));
            if (success)
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("drones/{id}")]
        public IHttpActionResult getById(string id)
        {
            logger.Debug("Fetching /drones/{0}", id);
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                DroneDTO targetDTO = DTOFactory.DTOFactory.createDroneDTO(target);
                return Ok(targetDTO);
            }
            else
            {
                return NotFound();
            }
        }

        IHttpActionResult commandNoParams(string id, string action)
        {
            logger.Debug("running command {1} on /drones/{0}", id, action);
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                CommandAck result = runCommand(target, action);
                if (null == result)
                {
                    return NotFound();
                }
                return Ok(DTOFactory.DTOFactory.createCommandAckDTO(result));
            }
            else
            {
                return NotFound();
            }
        }

        CommandAck runCommand(Drone target, string action)
        {
            if (action.Equals("arm"))
            {
                return target.Command.arm();
            }
            if (action.Equals("disarm"))
            {
                return target.Command.disarm();
            }
            if (action.Equals("returnToLaunch"))
            {
                return target.Command.returnToLaunch();
            }
            return null;
        }

        [HttpGet]
        [Route("drones/{id}/parameters")]
        public IHttpActionResult getParameters(string id)
        {
            logger.Debug("getting parameters for /drones/{0}", id);
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                Dictionary<String, ParamValue> parameters = target.Parameters;
                if (null != parameters)
                {
                    return Ok(DTOFactory.DTOFactory.createParametersDTO(parameters));
                }
                else
                {
                    return Ok();
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPut]
        [Route("drones/{id}/parameters")]
        public IHttpActionResult putParameters(string id, [FromBody] ParametersDTO values)
        {
            logger.Debug("setting parameters for /drones/{0}", id);
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                Boolean result = true;
                foreach (String key in values.parameters.Keys)
                {
                    ParamValueDTO parameter = values.parameters[key];
                    bool parameterSetResult = target.setParameter(parameter.param_id, parameter.param_value, (MAVLink.MAV_PARAM_TYPE)Enum.Parse(typeof(MAVLink.MAV_PARAM_TYPE), parameter.param_type));
                    result = result && parameterSetResult;
                }
                Dictionary<String, ParamValue> parameters = target.Parameters;
                if ((null != parameters)&&(result))
                {
                    return Ok(DTOFactory.DTOFactory.createParametersDTO(parameters));
                }
                else
                {
                    return ResponseMessage(new HttpResponseMessage(HttpStatusCode.RequestTimeout));
                }
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("drones/{id}/arm")]
        public IHttpActionResult armCommand(string id)
        {
            return commandNoParams(id, "arm");
        }

        [HttpPost]
        [Route("drones/{id}/disarm")]
        public IHttpActionResult disarmCommand(string id)
        {
            return commandNoParams(id, "disarm");
        }

        [HttpPost]
        [Route("drones/{id}/returnToLaunch")]
        public IHttpActionResult rtlCommand(string id)
        {
            return commandNoParams(id, "returnToLaunch");
        }


        [HttpPost]
        [Route("drones/{id}/landAtLocation")]
        public IHttpActionResult landAtLocation(string id, [FromBody] LandAtLocationAction parameters)
        {
            String action = "landAtLocation";
            logger.Debug("running command {1} on /drones/{0}", id, action);
            if (null == parameters)
            {
                return BadRequest("Missing Required Parameters");
            }
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                CommandAck result = target.Command.landAtLocation(parameters.latitude, parameters.longitude, parameters.altitude);
                if (null == result)
                {
                    return NotFound();
                }
                return Ok(DTOFactory.DTOFactory.createCommandAckDTO(result));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("drones/{id}/loiterTime")]
        public IHttpActionResult loiterTime(string id, [FromBody] LoiterTimeAction parameters)
        {
            String action = "loiterTime";
            logger.Debug("running command {1} on /drones/{0}", id, action);
            if (null == parameters)
            {
                return BadRequest("Missing Required Parameters");
            }
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                CommandAck result = target.Command.loiterTime(parameters.loiterTimeSeconds, parameters.radiusMeters, parameters.latitude, parameters.longitude, parameters.altitude);
                if (null == result)
                {
                    return NotFound();
                }
                return Ok(DTOFactory.DTOFactory.createCommandAckDTO(result));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("drones/{id}/loiterTurns")]
        public IHttpActionResult loiterTurns(string id, [FromBody] LoiterTurnsAction parameters)
        {
            String action = "loiterTurns";
            logger.Debug("running command {1} on /drones/{0}", id, action);
            if (null == parameters)
            {
                return BadRequest("Missing Required Parameters");
            }
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                CommandAck result = target.Command.loiterTurns(parameters.numTurns, parameters.radiusMeters, parameters.latitude, parameters.longitude, parameters.altitude);
                if (null == result)
                {
                    return NotFound();
                }
                return Ok(DTOFactory.DTOFactory.createCommandAckDTO(result));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("drones/{id}/loiterUnlimited")]
        public IHttpActionResult loiterUnlimited(string id, [FromBody] LoiterUnlimitedAction parameters)
        {
            String action = "loiterUnlimited";
            logger.Debug("running command {1} on /drones/{0}", id, action);
            if (null == parameters)
            {
                return BadRequest("Missing Required Parameters");
            }
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                CommandAck result = target.Command.loiterUnlimited(parameters.radiusMeters, parameters.latitude, parameters.longitude, parameters.altitude);
                if (null == result)
                {
                    return NotFound();
                }
                return Ok(DTOFactory.DTOFactory.createCommandAckDTO(result));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("drones/{id}/navigateWaypoint")]
        public IHttpActionResult navigateWaypoint(string id, [FromBody] NavigateWaypointAction parameters)
        {
            String action = "navigateWaypoint";
            logger.Debug("running command {1} on /drones/{0}", id, action);
            if (null == parameters)
            {
                return BadRequest("Missing Required Parameters");
            }
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                CommandAck result = target.Command.navigateWaypoint(parameters.latitude, parameters.longitude, parameters.altitude);
                if (null == result)
                {
                    return NotFound();
                }
                return Ok(DTOFactory.DTOFactory.createCommandAckDTO(result));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("drones/{id}/takeoff")]
        public IHttpActionResult takeoff(string id, [FromBody] TakeoffAction parameters)
        {
            String action = "takeoff";
            logger.Debug("running command {1} on /drones/{0}", id, action);
            if (null == parameters)
            {
                return BadRequest("Missing Required Parameters");
            }
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                CommandAck result = target.Command.takeoff(parameters.heightMeters);
                if (null == result)
                {
                    return NotFound();
                }
                return Ok(DTOFactory.DTOFactory.createCommandAckDTO(result));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("drones/{id}/setMode")]
        public IHttpActionResult setMode(string id, [FromBody] SetModeAction parameters)
        {
            String action = "setMode";
            logger.Debug("running command {1} on /drones/{0}", id, action);
            if (null == parameters)
            {
                return BadRequest("Missing Required Parameters");
            }
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                MAVLink.MAV_MODE mode = 0;
                try
                {
                    mode = (MAVLink.MAV_MODE)Enum.Parse(typeof(MAVLink.MAV_MODE), parameters.mode);
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }
                CommandAck result = target.Command.setMode(mode);
                if (null == result)
                {
                    return new CustomResponse("Null command result.", HttpStatusCode.RequestTimeout);
                }
                return Ok(DTOFactory.DTOFactory.createCommandAckDTO(result));
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("drones/{id}/rcOverrideEnable")]
        public IHttpActionResult rcOverrideEnable(string id)
        {
            String action = "rcOverrideEnable";
            logger.Debug("running command {1} on /drones/{0}", id, action);
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                target.EnableRcOverride();
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }
        [HttpPost]
        [Route("drones/{id}/rcOverrideDisable")]
        public IHttpActionResult rcOverrideDisable(string id)
        {
            String action = "rcOverrideDisable";
            logger.Debug("running command {1} on /drones/{0}", id, action);
            Drone target = droneMgr.getById(new Guid(id));
            if (null != target)
            {
                if (!target.isConnected())
                {
                    return BadRequest("Target system is not connected, refusing request");
                }
                target.DisableRcOverride();
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

    }
}
