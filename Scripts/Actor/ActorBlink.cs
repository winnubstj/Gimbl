﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;
namespace Gimbl
{
    public partial class ActorObject : MonoBehaviour
    {
        public bool isFading = false;
        public async void Blink(int duration, int fadeTime, bool disable = false)
        {
            Gimbl.DisplayObject disp = this.GetComponentInChildren<DisplayObject>();
            bool prev = this.isActive;
            if (disable) this.isActive = false;
            await FadeOut(disp,fadeTime);
            await Task.Delay(duration);
            await FadeIn(disp, fadeTime);
            if (disable) this.isActive = prev;
        }

        public async Task FadeOut(DisplayObject disp, float duration)
        {
            if (!isFading)
            {
                isFading = true;
                // Only run if fade out is really required.
                if (disp.currentBrightness > 0)
                {
                    int sleepTime = (int)(duration / (disp.currentBrightness));
                    await Task.Run(() =>
                    {
                        for (float brightness = disp.currentBrightness; brightness >= 0; brightness--)
                        {
                            Thread.Sleep(sleepTime);
                            disp.currentBrightness = brightness;
                        }
                    });
                }
                disp.currentBrightness = 0; // ensures expected brightness settings.
                isFading = false;
            }
        }

        public async Task FadeIn(DisplayObject disp, float duration)
        {
            if (!isFading) // Prevents two fading actions.
            {
                isFading = true;
                // Only run if fade in is really required.
                if (disp.currentBrightness < disp.settings.brightness)
                {
                    int sleepTime = (int)(duration / (disp.settings.brightness - disp.currentBrightness));
                    await Task.Run(() =>
                    {
                        for (float brightness = disp.currentBrightness; brightness <= disp.settings.brightness; brightness++)
                        {
                            Thread.Sleep(sleepTime);
                            disp.currentBrightness = brightness;
                        }
                    });
                }
                disp.currentBrightness = disp.settings.brightness; // ensures expected brightness settings.
                isFading = false;
            }
        }


    }

}
            
    