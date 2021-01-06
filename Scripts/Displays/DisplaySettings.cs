using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gimbl
{
    [System.Serializable]
    public class DisplaySettings : ScriptableObject
    {
        public bool isActive = true;
        public float brightness = 100f;
        public float heightInVR = 0.2f;
    }
}

