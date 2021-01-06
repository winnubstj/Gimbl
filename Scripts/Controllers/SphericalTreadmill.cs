﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gimbl;
using System.Linq;
using UnityEditor;

namespace Gimbl
{
    public class SphericalTreadmill : Gimbl.ControllerObject
    {
        // Movement Variables.
        public SphericalTreadmillSettings settings;
        // for parsing.
        private Vector3 moveArcLengths = new Vector3();
        private Vector3 moveHeading = new Vector3();
        private ActorObject prevActor;
        private CharacterController ActorCharController;

        // Messaging Variables.
        public LoggerObject logger;

        public class MSG
        {
            public float pitch;
            public float roll;
            public float yaw;
        }
        public class StatusMsg
        {
            public bool status;
        }
        public MQTTChannel<StatusMsg> statusChannel;
        public class SphericalControllerMsg
        {
            public string name;
            public int roll;
            public int yaw;
            public int pitch;
        }
        SphericalControllerMsg sphericalControllerMsg = new SphericalControllerMsg();

        public void Update()
        {
            ProcessMovement();
        }

        public void ProcessMovement()
        {
            if (!settings.isActive)
            {
                movement.Clear();
                smoothBuffer.Clear();
            }
            if (settings.isActive)
            {
                // Read inputs.
                #region Read Inputs.
                if (movement.counter > 0)
                {
                    lock (movement)
                    {
                        moveArcLengths = movement.Sum(); // sum only what has been added.
                        movement.Clear();
                    }
                }
                // log.
                if (settings.enableLogging)
                {
                    sphericalControllerMsg.name = name;
                    sphericalControllerMsg.roll = (int)(moveArcLengths.x *1000);
                    sphericalControllerMsg.yaw = (int)(moveArcLengths.y * 1000);
                    sphericalControllerMsg.pitch = (int)(moveArcLengths.z * 1000);
                    logger.logFile.Log("Spherical Controller", sphericalControllerMsg);
                }
                #endregion
                if (Actor != null)
                {
                    // Check if actor changed.
                    if (prevActor != Actor)
                    {
                        prevActor = Actor;
                        ActorCharController = Actor.GetComponent<CharacterController>();
                    }
                    // Smooth input buffer.
                    #region Smooth input.
                    if(GetBufferSize(settings.inputSmooth) != smoothBuffer.bufferSize)
                        { smoothBuffer = new ValueBuffer(GetBufferSize(settings.inputSmooth),true);}
                    smoothBuffer.Add(moveArcLengths.x, moveArcLengths.y, moveArcLengths.z);
                    moveArcLengths = smoothBuffer.Average(); 
                    #endregion

                    // Input gain and calculate speed.
                    if (moveArcLengths.x > 0) { moveArcLengths.x *= settings.gain.strafeLeft; }
                    else { moveArcLengths.x *= settings.gain.strafeRight; }

                    if (moveArcLengths.y > 0) { moveArcLengths.y *= settings.gain.turnLeft; }
                    else { moveArcLengths.y *= settings.gain.turnRight; }

                    if (moveArcLengths.z > 0) { moveArcLengths.z *= settings.gain.backward; }
                    else { moveArcLengths.z *= settings.gain.forward; }

                    float speed = Mathf.Sqrt(Mathf.Pow(moveArcLengths.x, 2) + Mathf.Pow(moveArcLengths.z, 2)) / Time.fixedDeltaTime;

                    // Yaw and Trajectory based heading.
                    #region Yaw and Trajectory based heading.

                    // Trajectory heading.
                    moveHeading.y = moveArcLengths.y; // default.
                    if (settings.trajectoryHeading.contribution > 0)
                    {
                        float trajectoryHeading = 0;
                        float xFraction = moveArcLengths.z / moveArcLengths.x; // pitch / roll
                        if (xFraction > 20) { xFraction = 20; }
                        if (xFraction < -20) { xFraction = -20; }
                        if (!float.IsNaN(xFraction) & speed > settings.trajectoryHeading.minSpeed)
                        {
                            trajectoryHeading = settings.trajectoryHeading.trajectoryCurve.Evaluate(Mathf.Abs(xFraction));
                            trajectoryHeading *= Time.fixedDeltaTime;
                            if (moveArcLengths.x > 0) trajectoryHeading *= -1; // larger then zero because it is from ball perspective.
                        }

                        // Combine according to proportion.
                        float trajProp = (float)settings.trajectoryHeading.contribution / 100f;
                        moveHeading.y = -((trajectoryHeading * trajProp) +
                                (moveArcLengths.y * (1f - trajProp)));
                    }
                    #endregion
                    // Apply translation in opposite direction according to heading.
                    if (Actor.isActive)
                    {
                        //Heading.
                        if (!float.IsNaN(moveHeading.y))
                        {
                            Actor.transform.Rotate(moveHeading, Space.Self);
                            // fix rotation of camera.
                            if (settings.lockCameraRotation && Actor.display!=null){ Actor.display.transform.Rotate(-moveHeading, Space.Self); }
                        }
                        //Translation.
                        moveArcLengths.x *= -1; moveArcLengths.y = 0; moveArcLengths.z *= -1;
                        ActorCharController.Move(Actor.transform.TransformVector(
                            moveArcLengths));
                    }
                }
            }
        }
        void Start()
        {
            // Get instance of logger.
            logger = FindObjectOfType<LoggerObject>();
            // Setup Listener.
            MQTTChannel<MSG> channel = new MQTTChannel<MSG>(string.Format("{0}/Data", settings.deviceName));
            channel.Event.AddListener(OnMessage);
            // Start treadmill.
            statusChannel = new MQTTChannel<StatusMsg>(string.Format("{0}/Status", settings.deviceName),false);
            statusChannel.Send(new StatusMsg() { status = true });
            // Create smooth buffer
            smoothBuffer = new ValueBuffer(GetBufferSize(settings.inputSmooth),true);
        }

        public void OnMessage(MSG msg)
        {
            // ordered by influence on movement axis in unity.(roll/x: side-to-side, yaw/y: turn, pitch/z: forwards-backwards)
            // pitch and roll: arc lengths in VR world units, yaw: rotation in degrees.
            lock (movement) { movement.Add(msg.roll,msg.yaw,msg.pitch); }
        }
        public override void LinkSettings(string assetPath = "")
        {
            SphericalTreadmillSettings asset;
            if (assetPath == "")
            {
                asset = ScriptableObject.CreateInstance<SphericalTreadmillSettings>();
                UnityEditor.AssetDatabase.CreateAsset(asset, string.Format("Assets/VRSettings/Controllers/{0}.asset", this.gameObject.name));
            }
            else
            {
                asset = (SphericalTreadmillSettings)UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(SphericalTreadmillSettings));
            }
            settings = asset;
        }

        public override void EditMenu()
        {
            SerializedObject serializedObject = new SerializedObject(settings);
            if (this.GetType() == typeof(SimulatedSphericalTreadmill))
            {
                ControllerMenuTitle(settings.isActive, "Sim. Spherical Treadmill");
                // Select controller.
                EditorGUILayout.LabelField("Device", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal(LayoutSettings.editFieldOp);
                    if (EditorApplication.isPlaying) GUI.enabled = false;
                    if (settings.gamepadSettings.selectedGamepad >= deviceNames.Length) settings.gamepadSettings.selectedGamepad = 0;
                    settings.gamepadSettings.selectedGamepad = EditorGUILayout.Popup(settings.gamepadSettings.selectedGamepad, deviceNames);
                    if (GUILayout.Button("Rescan Devices")) { deviceNames = Gamepad.GetDeviceNames(); }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonTopics"), true, LayoutSettings.editFieldOp);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("isActive"), new GUIContent("Active"), LayoutSettings.editFieldOp);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("enableLogging"), new GUIContent("Log Input"), true, LayoutSettings.editFieldOp);
                    GUI.enabled = true;
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

            }
            else
            {
                ControllerMenuTitle(settings.isActive, "Spherical Treadmill");
                EditorGUILayout.LabelField("Device", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isActive"), new GUIContent("Active"), LayoutSettings.editFieldOp);
                if (EditorApplication.isPlaying) GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("deviceName"), new GUIContent("MQTT Name"), LayoutSettings.editFieldOp);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableLogging"), new GUIContent("Log Input"), true, LayoutSettings.editFieldOp);
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.LabelField("Movement Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lockCameraRotation"), new GUIContent("Fix Camera Rotation"), true, LayoutSettings.editFieldOp);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gain"), true, LayoutSettings.editFieldOp);
            EditorGUILayout.BeginHorizontal(LayoutSettings.editFieldOp); EditorGUILayout.PropertyField(serializedObject.FindProperty("inputSmooth"), new GUIContent("Input Smoothing")); EditorGUILayout.LabelField("(ms)", GUILayout.Width(70)); EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trajectoryHeading"), true, LayoutSettings.editFieldOp);
            EditorGUI.indentLevel--;


            serializedObject.ApplyModifiedProperties();
        }
    }


}