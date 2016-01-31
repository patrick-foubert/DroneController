﻿using System;
using DataAccessLibrary.Models;
using DroneConnection;
using NLog;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using System.Text;
using RabbitMQ.Client;
using System.Collections.Generic;
using DroneManager.Models.MessageContainers;

namespace DroneManager.Models
{
    public class Drone
    {

        static Logger logger = LogManager.GetCurrentClassLogger();

        // database record
        public DroneEntity data = new DroneEntity();

        // live connection
        public MavLinkConnection connection { get; set; }

        // events connection with MavLinkConnection
        MavLinkEvents events;
        // RabbitMQ consumer identifier ID
        string consumerTag;

        Dictionary<MAVLink.MAVLINK_MSG_ID, MavLinkMessage> currentState = new Dictionary<MAVLink.MAVLINK_MSG_ID, MavLinkMessage>();

        public Drone(DroneEntity entity)
        {
            data.copy(entity);
        }

        public Boolean isConnected()
        {
            if (null != connection)
            {
                if (connection.port.IsOpen)
                {
                    return true;
                }
            }
            return false;
        }


        // commands
        public void arm()
        {
            connection.sendArmMessage();
        }

        public void disarm()
        {
            connection.sendArmMessage(false);
        }

        public void returnToLand()
        {

        }

        public void land()
        {

        }

        //state
        public Heartbeat getHearbeat()
        {
            if (!this.currentState.ContainsKey(MAVLink.MAVLINK_MSG_ID.HEARTBEAT))
            {
                return null;
            }
            MavLinkMessage message = this.currentState[MAVLink.MAVLINK_MSG_ID.HEARTBEAT];
            return new Heartbeat(message);
        }

        // events
        // attempts to open listen feed
        public Boolean openMessageFeed()
        {
            logger.Debug("Opening Listening/Processing Feed for {0}", connection.port.PortName);
           
            events = new MavLinkEvents(connection.systemId, connection.componentId);
            if ((null == events) || (null == events.channel))
            {
                logger.Error("Failed to open event message queue for {0}", connection.port.PortName);
                return false;
            }

            logger.Debug("Creating Basic Consumer");
            EventingBasicConsumer consumer = new EventingBasicConsumer(events.channel);

            logger.Debug("Adding callback function");
            consumer.Received += (model, ea) =>
            {
                eventsCallback(ea);
            };

            logger.Debug("Inserting consumer into the channel");
            this.consumerTag = events.channel.BasicConsume(queue: events.getMessageQueueName(),
                                    noAck: true,
                                    consumer: consumer);

            logger.Debug("Consumer created successfully");
            return true;
        }

        void eventsCallback(BasicDeliverEventArgs eventArguments)
        {
            try
            {
                if (null == eventArguments)
                {
                    logger.Debug("events callback with no eventArguments");
                    return;
                }
                var body = eventArguments.Body;
                if (null == body)
                {
                    logger.Debug("events callback with null body");
                    return;
                }
                String jsonBody = Encoding.UTF8.GetString(body);
                if (null == jsonBody)
                {
                    logger.Debug("failed to parse JSON in events callback");
                    return;
                }
                MavLinkMessage message = JsonConvert.DeserializeObject<MavLinkMessage>(jsonBody);
                if (null == message)
                {
                    logger.Debug("Failed to parse MavLinkMessage from JSON in events callback");
                    return;
                }

                // store message in currentState Dictionary
                this.currentState[(MAVLink.MAVLINK_MSG_ID)message.messid] = message;

                if (message.messid.Equals(MAVLink.MAVLINK_MSG_ID.HEARTBEAT))
                {
                    logger.Debug("Heartbeat received on port {0} {1}", connection.port.PortName, jsonBody);
                }
            }
            catch (Exception e)
            {
                logger.Error("Failure in parsing message, exception with message {0}", e.Message);
            }
        }
    }
}
