﻿using System.Collections;
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
        public SphericalGain gain = new SphericalGain();
        public Vector3 inputGain = new Vector3() { x=1, y=1, z=1 };
        public TracjectorySettings trajectoryHeading;
        public string[] buttonTopics;
        public GamepadSettings gamepadSettings;
        public bool loopPath;
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
        public bool Equals(SphericalGain other)
        {
            return this.forward == other.forward & this.backward == other.backward & this.strafeLeft == other.strafeLeft &
                this.strafeRight == other.strafeRight & this.turnLeft == other.turnLeft & this.turnRight == other.turnRight;
        }
    }


    [System.Serializable]
    public class TracjectorySettings
    {
        public float maxRotPerSec = 90;
        public float angleOffsetBias = 0f;
        public float minSpeed = 0.1f; //units per second.
        public bool Equals(TracjectorySettings other)
        {
            return this.maxRotPerSec == other.maxRotPerSec & this.angleOffsetBias == other.angleOffsetBias & this.minSpeed == other.minSpeed;
        }
    }

}

