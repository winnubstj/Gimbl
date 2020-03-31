using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpDX;
using SharpDX.DirectInput;
using System;

namespace Gimbl
{
    public class Gamepad
    {
        private DirectInput directInput;
        public Joystick joystick;
        public MQTTChannel[] buttonChannels;
        private bool[] prevButtonState; // prevents button spamming.
        private bool[] testChange;
        public static string[] GetDeviceNames()
        {

            // Get Devices.
            IList<DeviceInstance> deviceList = new DirectInput().GetDevices(SharpDX.DirectInput.DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices);
            string[] deviceNames = new string[deviceList.Count+1];
            deviceNames[0] = "Mouse and Keyboard";
            for (int i = 0; i < deviceList.Count; i++)
            {
                deviceNames[i+1] = deviceList[i].ProductName;
            }
            return deviceNames;
        }

        public void Acquire(int id)
        {
            // Initialize DirectInput
            directInput = new DirectInput();

            // Get Devices.
            IList<DeviceInstance> deviceList = directInput.GetDevices(SharpDX.DirectInput.DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices);
            if (id + 1 > deviceList.Count) { Debug.LogError("Could not find Game Controller. Please Rescan Devices"); return; }
            Guid joystickGuid = deviceList[id].InstanceGuid;

            // Instantiate the joystick
            joystick = new Joystick(directInput, joystickGuid);

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();

        }

        // Set MQTT Button Triggered channels.
        public void SetupChannels(string[] topics)
        {
            buttonChannels = new MQTTChannel[topics.Length];
            for (int i = 0; i < buttonChannels.Length; i++)
                if (topics[i] != "")
                    buttonChannels[i] = new MQTTChannel(topics[i],false);
        }
        // Send MQTT Triggers.
        public void SendChannels(bool[] buttons)
        {
            for (int i = 0; i < buttonChannels.Length; i++)
            {
                if (buttons[i] & buttonChannels[i] != null)
                {
                    buttonChannels[i].Send();
                }
            }
        }


        public float normRange(int x)
        {
            return ((x / Mathf.Pow(2, 16)) * 2) - 1; 
        }

        public bool[] checkButtonChange(JoystickState state)
        {
            if (prevButtonState == null) { prevButtonState = new bool[state.Buttons.Length]; testChange = new bool[prevButtonState.Length]; }
            for (int i = 0; i < testChange.Length; i++)
                testChange[i] = state.Buttons[i] & !prevButtonState[i];
            prevButtonState = state.Buttons;
            return testChange;
        }
    }
    [System.Serializable]
    public class GamepadSettings
    {
        public int selectedGamepad;
    }
}
