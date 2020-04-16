// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;

namespace PixelCrushers.DialogueSystem
{

    [Serializable]
    public class ConversationOverrideDisplaySettings
    {
        public bool useOverrides = false;

        public bool overrideSubtitleSettings = false;
        public bool showNPCSubtitlesDuringLine = true;
        public bool showNPCSubtitlesWithResponses = true;
        public bool showPCSubtitlesDuringLine = false;
        public bool skipPCSubtitleAfterResponseMenu = false;
        public float subtitleCharsPerSecond = 30;
        public float minSubtitleSeconds = 2;
        public DisplaySettings.SubtitleSettings.ContinueButtonMode continueButton;

        public bool overrideSequenceSettings = false;
        public string defaultSequence;
        public string defaultPlayerSequence;
        public string defaultResponseMenuSequence;

        public bool overrideInputSettings = false;
        public bool alwaysForceResponseMenu = true;
        public bool includeInvalidEntries = false;
        public float responseTimeout = 0;
    }


}
