#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{

    /// <summary>
    /// Sequencer command Timeline(mode, timeline, [nowait], [nostop], [#:binding]...)
    /// 
    /// Controls a Timeline (PlayableDirector).
    /// 
    /// - mode: Can be play, pause, resume, or stop.
    /// - timeline: Name of a GameObject with a PlayableDirector, or a Timeline asset in a Resources folder or asset bundle. Default: speaker.
    /// - nowait: If specified, doesn't wait for the director to finish.
    /// - nostop: If specified, doesn't force the director to stop at the end of the sequencer command.
    /// - #:binding: If specified, the number indicates the track number. The binding is the name of a GameObject to bind to that track.
    /// </summary>
    public class SequencerCommandTimeline : SequencerCommand
    {

        private PlayableDirector playableDirector = null;
        private TimelineAsset timelineAsset = null;
        private bool nostop = false;
        private bool mustDestroyPlayableDirector = false;

        public IEnumerator Start()
        {
            if (parameters == null || parameters.Length == 0)
            {
                Stop();
                yield break;
            }
            var mode = GetParameter(0).ToLower();
            var subject = GetSubject(1, Sequencer.Speaker);
            var nowait = string.Equals(GetParameter(2), "nowait", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetParameter(3), "nowait", System.StringComparison.OrdinalIgnoreCase);
            nostop = string.Equals(GetParameter(2), "nostop", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetParameter(3), "nostop", System.StringComparison.OrdinalIgnoreCase);
            playableDirector = (subject != null) ? subject.GetComponent<PlayableDirector>() : null;

            // If no suitable PlayableDirector was found, look for a Timeline asset in Resources:
            timelineAsset = DialogueManager.LoadAsset(GetParameter(1), typeof(TimelineAsset)) as TimelineAsset;
            if (timelineAsset != null)
            {
                playableDirector = new GameObject(GetParameter(1), typeof(PlayableDirector)).GetComponent<PlayableDirector>();
                playableDirector.playableAsset = timelineAsset;
                mustDestroyPlayableDirector = true;
            }

            if (playableDirector == null)
            {
                if (DialogueDebug.LogWarnings) Debug.LogWarning("Dialogue System: Sequencer: Timeline(" + GetParameters() +
                    "): Can't find playable director '" + GetParameter(0) + "'.");
            }
            else if (playableDirector.playableAsset == null)
            {
                if (DialogueDebug.LogWarnings) Debug.LogWarning("Dialogue System: Sequencer: Timeline(" + GetParameters() +
                    "): No timeline asset is assigned to the Playable Director.", playableDirector);
            }
            else
            {
                var isModeValid = (mode == "play" || mode == "pause" || mode == "resume" || mode == "stop");
                if (!isModeValid)
                {
                    if (DialogueDebug.LogWarnings) Debug.LogWarning("Dialogue System: Sequencer: Timeline(" + GetParameters() +
                        "): Invalid mode '" + mode + "'. Expected 'play', 'pause', 'resume', or 'stop'.");
                }
                else
                {
                    if (DialogueDebug.LogInfo) Debug.Log("Dialogue System: Sequencer: Timeline(" + GetParameters() + ")");

                    // Check for rebindings:
                    timelineAsset = playableDirector.playableAsset as TimelineAsset;
                    if (timelineAsset != null)
                    {
                        for (int i = 2; i < Parameters.Length; i++)
                        {
                            var s = GetParameter(i);
                            if (s.Contains(":"))
                            {
                                var colonPos = s.IndexOf(":");
                                var trackIndex = Tools.StringToInt(s.Substring(0, colonPos));
                                var bindName = s.Substring(colonPos + 1);
                                var track = timelineAsset.GetOutputTrack(trackIndex);
                                if (track != null)
                                {
                                    playableDirector.SetGenericBinding(track, Tools.GameObjectHardFind(bindName));
                                }
                            }
                        }
                    }

                    switch (mode)
                    {
                        case "play":
                            playableDirector.Play();
                            var endTime = nowait ? 0 : DialogueTime.time + playableDirector.playableAsset.duration;
                            while (DialogueTime.time < endTime)
                            {
                                yield return null;
                            }
                            break;
                        case "pause":
                            playableDirector.Pause();
                            nostop = true;
                            break;
                        case "resume":
                            playableDirector.Resume();
                            var resumedEndTime = nowait ? 0 : DialogueTime.time + playableDirector.playableAsset.duration - playableDirector.time;
                            while (DialogueTime.time < resumedEndTime)
                            {
                                yield return null;
                            }
                            break;
                        case "stop":
                            playableDirector.Stop();
                            nostop = false;
                            break;
                        default:
                            isModeValid = false;
                            break;
                    }
                }
            }
            Stop();
        }

        public void OnDestroy()
        {
            if (playableDirector != null && !nostop)
            {
                playableDirector.Stop();
                if (mustDestroyPlayableDirector) Destroy(playableDirector.gameObject);
            }
        }

    }

}
#endif
