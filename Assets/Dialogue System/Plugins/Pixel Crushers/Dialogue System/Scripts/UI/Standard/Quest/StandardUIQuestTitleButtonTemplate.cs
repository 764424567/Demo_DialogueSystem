// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Unity UI template for a quest name button with a toggle for progress tracking.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class StandardUIQuestTitleButtonTemplate : StandardUIContentTemplate
    {

        [Header("Quest Title Button")]

        [Tooltip("Button UI element.")]
        public UnityEngine.UI.Button button;

        [Tooltip("Label text to set on button.")]
        public UITextField label;

        [Header("Tracking Toggle")]

        public StandardUIToggleTemplate trackToggleTemplate;

        public virtual void Awake()
        {
            if (button == null && DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: UI Button is unassigned.", this);
            if (trackToggleTemplate == null && DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: UI Track Toggle Template is unassigned.", this);
        }

        public virtual void Assign(string quest, ToggleChangedDelegate trackToggleDelegate)
        {
            if (UITextField.IsNull(label)) label.uiText = button.GetComponentInChildren<UnityEngine.UI.Text>();
            name = quest;
            label.text = quest;
            var canTrack = QuestLog.IsQuestActive(quest) && QuestLog.IsQuestTrackingAvailable(quest);
            trackToggleTemplate.Assign(canTrack, QuestLog.IsQuestTrackingEnabled(quest), quest, trackToggleDelegate);
        }

    }
}
