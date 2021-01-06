using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gimbl
{
    [System.Serializable]
    public class SphericalTreadmillSettings : ScriptableObject
    {
        public string deviceName = "sphericalTreadmill";
        public bool isActive = true;
        public bool enableLogging = false;
        public int inputSmooth = 100;
        public bool lockCameraRotation = false;
        public SphericalGain gain = new SphericalGain();
        public Vector3 inputGain = new Vector3() { x=1, y=1, z=1 };
        public TracjectorySettings trajectoryHeading;
        public string[] buttonTopics;
        public GamepadSettings gamepadSettings;
    }
    [System.Serializable]
    public class SphericalGain
    {
        public float forward = 1;
        public float backward = 1;
        public float strafeLeft = 1;
        public float strafeRight = 1;
        public float turnLeft = 1;
        public float turnRight = 1;
    }


    [System.Serializable]
    public class TracjectorySettings
    {
        public int contribution = 0; // in percentage.
        public float minSpeed = 0.1f; //units per second.
        public AnimationCurve trajectoryCurve = new AnimationCurve(new Keyframe(0, -60), new Keyframe(2.5f, -30,30,30), new Keyframe(5, 0));
    }

}

