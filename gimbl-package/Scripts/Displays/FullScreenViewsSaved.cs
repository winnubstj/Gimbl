﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gimbl
{
    // Needed only when PERSIST_AS_RESOURCE is defined in FullScreenViews.cs

    [Serializable]
    public class FullScreenViewsSaved : ScriptableObject
    {
        public List<string> cameraNames;
        public void OnEnable()
        {
            if (cameraNames == null)
            {
                cameraNames = new List<string>();
            }
        }
    }
}
