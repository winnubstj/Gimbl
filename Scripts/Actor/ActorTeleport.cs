using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
namespace Gimbl
{
    public enum ActorTeleportStatus
    {
        INACTIVE,
        FADE_IN,
        DARK,
        MOVED,
        FADE_OUT,
        ACTIVE,
        START,
        END
    }
    public partial class ActorObject : MonoBehaviour
    {
        public event EventHandler<ActorTeleportStatus> TeleportStatusChange; // event
        /// <summary>
        /// Teleports Actor to specified position with given heading (y-rotation) in degrees.
        /// </summary>
        public void Teleport(Vector3 position, float heading, bool freeze = true, bool blink = true, int darkDurationMs = -1, int FadeDurationMs = -1)
        {
            MoveActor(position, new Vector3(0, heading, 0), freeze, blink, darkDurationMs, FadeDurationMs);
        }

        /// <summary>
        /// Teleports Actor to specified position.
        /// </summary>
        public void Teleport(Vector3 position, bool freeze = true, bool blink = true, int darkDurationMs = -1, int FadeDurationMs = -1)
        {
            MoveActor(position, this.gameObject.transform.eulerAngles, freeze, blink, darkDurationMs, FadeDurationMs);
        }

        /// <summary>
        /// Teleports Actor to specified object and adopts heading.
        /// </summary>
        public void TeleportTo(GameObject targetObj, bool freeze = true, bool blink = true, int darkDurationMs = -1, int FadeDurationMs = -1)
        {
            MoveActor(targetObj.transform.position, targetObj.transform.eulerAngles, freeze, blink, darkDurationMs, FadeDurationMs);
        }

        /// <summary>
        /// Teleports Actor to specified object and adopts heading.
        /// </summary>
        public void TeleportTo(GameObject targetObj, Vector3 offset, bool freeze = true, bool blink = true, int darkDurationMs = -1, int FadeDurationMs = -1)
        {
            MoveActor(targetObj.transform.position + offset, targetObj.transform.eulerAngles, freeze, blink, darkDurationMs, FadeDurationMs);
        }

        private async void MoveActor(Vector3 position, Vector3 rotation, bool freeze, bool blink, int darkDurationMs, int FadeDurationMs)
        {
            // Check if to use default values.
            if (darkDurationMs == -1) { darkDurationMs = PlayerPrefs.GetInt("Gimbl_BlinkDuration", 2000); }
            if (FadeDurationMs == -1) { FadeDurationMs = PlayerPrefs.GetInt("Gimbl_BlinkFadeTime", 3000); }
            TeleportStatusChange?.Invoke(this, ActorTeleportStatus.START);
            if (freeze) { isActive = false; TeleportStatusChange?.Invoke(this, ActorTeleportStatus.INACTIVE); }
            DisplayObject disp = this.GetComponentInChildren<DisplayObject>();
            if (blink && disp != null)
            {
                TeleportStatusChange?.Invoke(this, ActorTeleportStatus.FADE_OUT);
                await FadeOut(disp, FadeDurationMs);
                TeleportStatusChange?.Invoke(this, ActorTeleportStatus.DARK);
                await Task.Delay(darkDurationMs);
            }

            // Check if editor hasnt quit in mean time.
            if (this != null)
            {
                transform.position = position;
                this.gameObject.transform.eulerAngles = rotation;
                TeleportStatusChange?.Invoke(this, ActorTeleportStatus.MOVED);
                if (blink && disp != null) { TeleportStatusChange?.Invoke(this, ActorTeleportStatus.FADE_IN); ; await FadeIn(disp, FadeDurationMs); }
                if (freeze) { isActive = true; TeleportStatusChange?.Invoke(this, ActorTeleportStatus.ACTIVE); }
                TeleportStatusChange?.Invoke(this, ActorTeleportStatus.END);
            }


        }
    }
}