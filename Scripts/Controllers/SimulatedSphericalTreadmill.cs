using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SharpDX.DirectInput;

namespace Gimbl
{
    public class SimulatedSphericalTreadmill : SphericalTreadmill
    {
        // Simulation Movement Variables .
        Vector3 inputMovement = new Vector3();
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private float passedTime = new float();
        private bool[] buttonPresses = new bool[4];
        private JoystickState state = new JoystickState();

        // Start is called before the first frame update
        void Start()
        {
            // Get instance of logger.
            logger = FindObjectOfType<LoggerObject>();
            // Acquire gamepad if selected.
            if (settings.gamepadSettings.selectedGamepad > 0) gamepad.Acquire(settings.gamepadSettings.selectedGamepad - 1);
            // Setup MQTT Channels for button presses.
            gamepad.SetupChannels(settings.buttonTopics);
            stopwatch.Start();
            // Create smooth buffer
            smoothBuffer = new ValueBuffer(GetBufferSize(settings.inputSmooth),true);
            // Setup tracking of settings changes.
            logSettings = new KeySphericalSettings();
            LogSphericalSettings();
        }

        public new void Update()
        {
            GetSimulatedInput();
            ProcessMovement();
            CheckSphericalSettings();
        }
        // Update is called once per frame
        private void GetSimulatedInput()
        {
            passedTime = (float)stopwatch.Elapsed.TotalMilliseconds;
            // Keyboard mouse.
            if (settings.gamepadSettings.selectedGamepad == 0)
            {
                // Get movement.
                inputMovement.x = -Input.GetAxis("Horizontal") * passedTime *0.008f;
                inputMovement.y = Input.GetAxis("Mouse X")*passedTime*0.1f;
                inputMovement.z = -Input.GetAxis("Vertical") *passedTime* 0.008f;
                // Check button presses.
                buttonPresses[0] = Input.GetButtonDown("Fire1");
                buttonPresses[1] = Input.GetButtonDown("Fire2");
                buttonPresses[2] = Input.GetButtonDown("Fire3");
                buttonPresses[3] = Input.GetButtonDown("Jump");
                gamepad.SendChannels(buttonPresses);
            }
            // Directx controller.
            else
            {
                gamepad.joystick.GetCurrentState(ref state);
                inputMovement.x = -gamepad.normRange(state.X) * passedTime * 0.01f;
                inputMovement.y = gamepad.normRange(state.RotationX) * passedTime * 0.1f;
                inputMovement.z = gamepad.normRange(state.Y) * passedTime * 0.01f;

                // Remove small movements.
                for (int i = 0; i < 3; i++)
                    if (Mathf.Abs(inputMovement[i]) < 0.075) inputMovement[i] = 0;
                // Check button that have changed to On.
                gamepad.SendChannels(gamepad.checkButtonChange(state));
            }
            stopwatch.Restart();
            // Store.
            lock (movement) { movement.Add(inputMovement.x,inputMovement.y,inputMovement.z); };
        }




    }
}
