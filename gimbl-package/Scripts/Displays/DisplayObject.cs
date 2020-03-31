using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Gimbl
{
    public class DisplayObject : MonoBehaviour
    {
        public DisplaySettings settings;
        public float currentBrightness = 100f;

        public void ParentToActor(ActorObject actor)
        {
            gameObject.transform.SetParent(actor.transform, false);
            gameObject.transform.localPosition = new Vector3(0, settings.heightInVR, 0);
            // Set Culling mask.
            foreach (Camera cam in gameObject.GetComponentsInChildren<Camera>())
            {
                cam.cullingMask = -1; // show everything
                cam.cullingMask &= ~(1 << LayerMask.NameToLayer(actor.name));
            }
        }

        public void Unparent()
        {
            gameObject.transform.SetParent(null);
            foreach (Camera cam in gameObject.GetComponentsInChildren<Camera>())
            {
                cam.cullingMask = -1; // show everything
            }
        }

        public void Blank()
        {
            currentBrightness = 0;
        }

        public void Show()
        {
            currentBrightness = settings.brightness;
        }

    }
}
