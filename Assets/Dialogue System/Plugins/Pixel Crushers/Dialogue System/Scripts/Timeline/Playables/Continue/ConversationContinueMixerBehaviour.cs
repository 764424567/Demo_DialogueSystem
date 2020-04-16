#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    public class ContinueConversationMixerBehaviour : PlayableBehaviour
    {

        private HashSet<int> played = new HashSet<int>();

        // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            int inputCount = playable.GetInputCount();

            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight > 0.001f && !played.Contains(i))
                {
                    played.Add(i);
                    if (Application.isPlaying)
                    {
                        DialogueManager.instance.BroadcastMessage("OnConversationContinueAll", SendMessageOptions.DontRequireReceiver);
                    }
                    else
                    {
                        PreviewUI.ShowMessage("Continue", 3, -1);
                    }
                }
                else if (inputWeight <= 0.001f && played.Contains(i))
                {
                    played.Remove(i);
                }
            }
        }

        public override void OnGraphStart(Playable playable)
        {
            base.OnGraphStart(playable);
            played.Clear();
        }

        public override void OnGraphStop(Playable playable)
        {
            base.OnGraphStop(playable);
            played.Clear();
        }

    }
}
#endif
