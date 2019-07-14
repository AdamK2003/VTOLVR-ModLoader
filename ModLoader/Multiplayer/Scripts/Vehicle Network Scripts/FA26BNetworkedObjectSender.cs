﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using System.Collections;

namespace NetworkedObjects.Vehicles
{
    public class FA26BNetworkedObjectSender : MonoBehaviour
    {
        public UnityClient client;
        public Transform worldCenter;

        //Classes we use to find the information out
        private FlightInfo flightInfo;
        private WheelsController wheelsController;
        private AeroController aeroController;
        private VRThrottle vRThrottle;

        //Information which gets sent over the network 
        //(These variables are also the last sent ones over the network which is compaired in CheckVariabes() )
        private float positionX, positionY, positionZ;
        private float rotationX, rotationY, rotationZ;
        private float speed = 0;
        private bool landingGear = true;
        private float flaps; //0 = 0, 0.5 = 1, 1 = 1
        private float pitch, roll, yaw;
        private float breaks, throttle, wheels;

        private float minDistance = 0.1f;
        private float minRotation = 0.1f;
        private float minSpeed = 0.1f;
        private void Start()
        {
            flightInfo = GetComponent<FlightInfo>();
            wheelsController = GetComponent<WheelsController>();
            aeroController = GetComponent<AeroController>();
            vRThrottle = gameObject.GetComponentInChildren<VRThrottle>();
        }

        private void Update()
        {
            CheckVariables();
        }

        private void CheckVariables()
        {
            if (Vector3.Distance(new Vector3(positionX, positionY, positionZ), transform.position) >= minDistance ||
                Vector3.Distance(new Vector3(rotationX, rotationY, rotationZ), transform.rotation.eulerAngles) >= minRotation ||
                Mathf.Abs(speed - flightInfo.airspeed) >= minSpeed ||
                landingGear != LandingGearState() ||
                flaps != aeroController.flaps)
            {
                UpdateVariables(true);
            }
        }

        private bool LandingGearState()
        {
            return wheelsController.gearAnimator.GetCurrentState() == GearAnimator.GearStates.Extended;
        }

        private void UpdateVariables(bool sendInfo = false)
        {
            Vector3 position = worldCenter.position - transform.position;
            positionX = position.x;
            positionY = position.y;
            positionZ = position.z;

            Vector3 rotation = transform.rotation.eulerAngles;
            rotationX = rotation.x;
            rotationY = rotation.y;
            rotationZ = rotation.z;

            speed = flightInfo.airspeed;

            landingGear = LandingGearState();

            flaps = aeroController.flaps;

            pitch = aeroController.input.x;
            yaw = aeroController.input.y;
            roll = aeroController.input.z;
            breaks = aeroController.brake;

            if (sendInfo)
                SendVariables();
        }

        private void SendVariables()
        {
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(positionX);
                writer.Write(positionY);
                writer.Write(positionZ);
                writer.Write(rotationX);
                writer.Write(rotationY);
                writer.Write(rotationZ);
                writer.Write(speed);
                writer.Write(landingGear);
                writer.Write(flaps);
                writer.Write(pitch);
                writer.Write(yaw);
                writer.Write(roll);
                writer.Write(breaks);
                writer.Write(throttle);
                writer.Write(wheels);

                using (Message message = Message.Create((ushort)Tags.FA26B_General, writer))
                    client.SendMessage(message, SendMode.Unreliable);
            }
        }
    }
}
