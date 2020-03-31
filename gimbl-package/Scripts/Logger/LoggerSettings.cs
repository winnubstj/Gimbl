using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gimbl
{
    [System.Serializable]
    public class LoggerSettings : ScriptableObject
    {
        public string outputFile = "Log";
        public int sessionId = 1;
    }
}