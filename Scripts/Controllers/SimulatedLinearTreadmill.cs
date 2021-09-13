using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SharpDX.DirectInput;

namespace Gimbl
{
    public class SimulatedLinearTreadmill : LinearTreadmill
    {
        public JoystickState state = new JoystickState();
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private float moveControl;
        private float passedTime;
        private bool[] buttonPresses = new bool[4];
        public void Start()
        {
            // Acquire gamepad if selected.
            if (settings.gamepadSettings.selectedGamepad > 0) gamepad.Acquire(settings.gamepadSettings.selectedGamepad - 1);
            // Setup MQTT Channels for button presses.
            gamepad.SetupChannels(settings.buttonTopics);
            // Create smooth buffer
            smoothBuffer = new ValueBuffer(GetBufferSize(settings.inputSmooth), true);
            // Setup tracking of settings changes.
            logSettings = new KeyLinearSettings();
            LogLinearSettings();
        }
        public new void Update()
        {
            GetSimulatedInput();
            ProcessMovement();
            CheckLinearSettings();
        }
        public void GetSimulatedInput()
        {
            passedTime = (float)stopwatch.Elapsed.TotalMilliseconds;
            // Keyboard mouse.
            if (settings.gamepadSettings.selectedGamepad == 0)
            {
                moveControl = Input.GetAxis("Vertical") * passedTime * 0.008f;
                // Check button presses.
                buttonPresses[0] = Input.GetButtonDown("Fire1");
                buttonPresses[1] = Input.GetButtonDown("Fire2");
                buttonPresses[2] = Input.GetButtonDown("Fire3");
                buttonPresses[3] = Input.GetButtonDown("Jump");
                if (this.Actor !=null) { gamepad.SendChannels(buttonPresses);  }
            }
            //Gamepad.
            else
            {
                gamepad.joystick.GetCurrentState(ref state);
                moveControl = -gamepad.normRange(state.Y) * passedTime * 0.15f;
                if (Mathf.Abs(moveControl) < 0.075) moveControl = 0;
                moveControl *= 0.05f;
                // Check button that have changed to On.
                if (this.Actor != null) { gamepad.SendChannels(gamepad.checkButtonChange(state)); }
            }
            stopwatch.Restart();
            movement.Add(moveControl, 0, 0);
        }

    }
}
