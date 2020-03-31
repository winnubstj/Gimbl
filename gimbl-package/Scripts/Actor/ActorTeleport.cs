using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
namespace Gimbl
{
    public partial class ActorObject : MonoBehaviour
    {
        /// <summary>
        /// Teleports Actor to specified position with given heading (y-rotation) in degrees.
        /// </summary>
        public void Teleport(Vector3 position, float heading, bool freeze=true, bool blink=true)
        {
            MoveActor(position, new Vector3(0, heading, 0),freeze,blink);
        }

        /// <summary>
        /// Teleports Actor to specified position.
        /// </summary>
        public void Teleport(Vector3 position, bool freeze = true, bool blink = true)
        {
            MoveActor(position, this.gameObject.transform.eulerAngles, freeze, blink);
        }

        /// <summary>
        /// Teleports Actor to specified object and adopts heading.
        /// </summary>
        public void TeleportTo(GameObject targetObj, bool freeze = true, bool blink = true)
        {
            MoveActor(targetObj.transform.position, targetObj.transform.eulerAngles, freeze, blink);
        }

        /// <summary>
        /// Teleports Actor to specified object and adopts heading.
        /// </summary>
        public void TeleportTo(GameObject targetObj, Vector3 offset, bool freeze = true, bool blink = true)
        {
            MoveActor(targetObj.transform.position + offset, targetObj.transform.eulerAngles, freeze, blink);
        }

        private async void MoveActor(Vector3 position, Vector3 rotation, bool freeze, bool blink)
        {
            int duration = PlayerPrefs.GetInt("Gimbl_BlinkDuration", 2000);
            int fadeTime = PlayerPrefs.GetInt("Gimbl_BlinkFadeTime", 3000);

            if (freeze) { isActive = false; }
            DisplayObject disp = this.GetComponentInChildren<DisplayObject>();
            if (blink && disp!=null)
            {
                await FadeOut(disp, fadeTime);
                await Task.Delay(duration);
            }

            transform.position = position;
            this.gameObject.transform.eulerAngles = rotation;


            if (blink && disp != null) { await FadeIn(disp, fadeTime); }
            if (freeze) { isActive = true; }

        }
    }
}