// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Manages subtitle panels for StandardDialogueUI.
    /// </summary>
    [System.Serializable]
    public class StandardUISubtitleControls : AbstractUISubtitleControls
    {

        #region Private Fields

        // The built-in subtitle panels assigned to the StandardDialogueUI:
        private List<StandardUISubtitlePanel> m_builtinPanels = new List<StandardUISubtitlePanel>();
        private StandardUISubtitlePanel m_defaultNPCPanel = null;
        private StandardUISubtitlePanel m_defaultPCPanel = null;

        // The panel that's currently focused:
        private StandardUISubtitlePanel m_focusedPanel = null;

        // After we look up which panel an actor uses, we cache the value so we don't need to look it up again:
        private Dictionary<Transform, StandardUISubtitlePanel> m_actorPanelCache = new Dictionary<Transform, StandardUISubtitlePanel>();

        // If a entry overrides the actor's panel using the [panel=#] tag, we record it here so we know to hide the panel when we switch back:
        private Dictionary<Transform, StandardUISubtitlePanel> m_actorOverridePanel = new Dictionary<Transform, StandardUISubtitlePanel>();

        // If the speaker has no DialogueActor, we can also override by actor ID:
        private Dictionary<int, StandardUISubtitlePanel> m_actorIdOverridePanel = new Dictionary<int, StandardUISubtitlePanel>();

        // Actor ID of last actor to use each panel:
        private Dictionary<int, StandardUISubtitlePanel> m_lastPanelUsedByActor = new Dictionary<int, StandardUISubtitlePanel>();
        private Dictionary<StandardUISubtitlePanel, int> m_lastActorToUsePanel = new Dictionary<StandardUISubtitlePanel, int>();

        // Cache DialogueActor components so we don't need to look them up again:
        private Dictionary<Transform, DialogueActor> m_dialogueActorCache = new Dictionary<Transform, DialogueActor>();

        // Cache of actors that want to use bark UIs:
        private List<Transform> m_useBarkUIs = new List<Transform>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Indicates whether the focused subtitle contains text.
        /// </summary>
        public override bool hasText { get { return m_focusedPanel != null && !string.IsNullOrEmpty(m_focusedPanel.subtitleText.text); } }

        #endregion

        #region Initialization & Lookup

        public void Initialize(StandardUISubtitlePanel[] subtitlePanels, StandardUISubtitlePanel defaultNPCSubtitlePanel, StandardUISubtitlePanel defaultPCSubtitlePanel)
        {
            m_builtinPanels.Clear();
            m_builtinPanels.AddRange(subtitlePanels);
            m_defaultNPCPanel = (defaultNPCSubtitlePanel != null) ? defaultNPCSubtitlePanel : (m_builtinPanels.Count > 0) ? m_builtinPanels[0] : null;
            m_defaultPCPanel = (defaultPCSubtitlePanel != null) ? defaultPCSubtitlePanel : (m_builtinPanels.Count > 0) ? m_builtinPanels[0] : null;
            if (m_defaultNPCPanel != null) m_defaultNPCPanel.isDefaultNPCPanel = true;
            if (m_defaultPCPanel != null) m_defaultPCPanel.isDefaultPCPanel = true;
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                if (m_builtinPanels[i] != null) m_builtinPanels[i].panelNumber = i;
            }
            ClearCache();
        }

        private void ClearCache()
        {
            m_actorPanelCache.Clear();
            m_actorOverridePanel.Clear();
            m_actorIdOverridePanel.Clear();
            m_lastPanelUsedByActor.Clear();
            m_lastActorToUsePanel.Clear();
            m_dialogueActorCache.Clear();
            m_useBarkUIs.Clear();
        }

        /// <summary>
        /// For speakers who do not have DialogueActor components, this method overrides the
        /// actor's default panel.
        /// </summary>
        public void OverrideActorPanel(Actor actor, SubtitlePanelNumber subtitlePanelNumber)
        {
            if (actor == null) return;
            var customPanel = actor.IsPlayer ? m_defaultPCPanel : m_defaultNPCPanel;
            m_actorIdOverridePanel[actor.id] = GetPanelFromNumber(subtitlePanelNumber, customPanel);
        }

        private StandardUISubtitlePanel GetPanel(Subtitle subtitle, out DialogueActor dialogueActor)
        {
            dialogueActor = null;
            if (subtitle == null) return m_defaultNPCPanel;

            // Check [panel=#] tag:
            var overrideIndex = subtitle.formattedText.subtitlePanelNumber;
            if (0 <= overrideIndex && overrideIndex < m_builtinPanels.Count)
            {
                var overridePanel = m_builtinPanels[overrideIndex];
                return overridePanel;
            }

            // Check actor ID override:
            if (m_actorIdOverridePanel.ContainsKey(subtitle.speakerInfo.id))
            {
                return m_actorIdOverridePanel[subtitle.speakerInfo.id];
            }

            // Get actor's panel:
            var speakerTransform = subtitle.speakerInfo.transform;
            var panel = GetActorTransformPanel(speakerTransform, subtitle.speakerInfo.isNPC ? m_defaultNPCPanel : m_defaultPCPanel, out dialogueActor);
            return panel;
        }

        private StandardUISubtitlePanel GetActorTransformPanel(Transform speakerTransform, StandardUISubtitlePanel defaultPanel, out DialogueActor dialogueActor)
        {
            dialogueActor = null;
            if (speakerTransform == null) return defaultPanel;
            if (m_dialogueActorCache.ContainsKey(speakerTransform))
            {
                dialogueActor = m_dialogueActorCache[speakerTransform];
            }
            else
            {
                dialogueActor = DialogueActor.GetDialogueActorComponent(speakerTransform);
                m_dialogueActorCache.Add(speakerTransform, dialogueActor);
            }
            if (m_actorPanelCache.ContainsKey(speakerTransform)) return m_actorPanelCache[speakerTransform];
            if (m_useBarkUIs.Contains(speakerTransform)) return null;
            if (DialogueActorUsesBarkUI(dialogueActor))
            {
                m_useBarkUIs.Add(speakerTransform);
                return null;
            }
            else
            {
                var panel = GetDialogueActorPanel(dialogueActor);
                if (panel == null) panel = defaultPanel;
                m_actorPanelCache.Add(speakerTransform, panel);
                return panel;
            }
        }


        private bool DialogueActorUsesBarkUI(DialogueActor dialogueActor)
        {
            return dialogueActor != null && dialogueActor.GetSubtitlePanelNumber() == SubtitlePanelNumber.UseBarkUI;
        }

        private StandardUISubtitlePanel GetDialogueActorPanel(DialogueActor dialogueActor)
        {
            if (dialogueActor == null) return null;
            return GetPanelFromNumber(dialogueActor.standardDialogueUISettings.subtitlePanelNumber, dialogueActor.standardDialogueUISettings.customSubtitlePanel);
        }

        private StandardUISubtitlePanel GetPanelFromNumber(SubtitlePanelNumber subtitlePanelNumber, StandardUISubtitlePanel customPanel)
        {
            switch (subtitlePanelNumber)
            {
                case SubtitlePanelNumber.Default:
                    return null;
                case SubtitlePanelNumber.Custom:
                    return customPanel;
                case SubtitlePanelNumber.UseBarkUI:
                    return null;
                default:
                    var index = PanelNumberUtility.GetSubtitlePanelIndex(subtitlePanelNumber);
                    return (0 <= index && index < m_builtinPanels.Count) ? m_builtinPanels[index] : null;
            }
        }


        private bool SubtitleUsesBarkUI(Subtitle subtitle)
        {
            if (subtitle == null) return false;
            return m_useBarkUIs.Contains(subtitle.speakerInfo.transform);
        }

        private string GetSubtitleTextSummary(Subtitle subtitle)
        {
            return (subtitle == null) ? "(empty subtitle)" : "[" + subtitle.speakerInfo.Name + "] '" + subtitle.formattedText.text + "'";
        }

        /// <summary>
        /// Changes a dialogue actor's panel for the current conversation. Can still be overridden by [panel=#] tags.
        /// </summary>
        public virtual void SetActorSubtitlePanelNumber(DialogueActor dialogueActor, SubtitlePanelNumber subtitlePanelNumber)
        {
            if (dialogueActor == null) return;
            if (m_actorPanelCache.ContainsKey(dialogueActor.transform))
            {
                m_actorPanelCache.Remove(dialogueActor.transform);
            }
            if (!m_dialogueActorCache.ContainsKey(dialogueActor.transform))
            {
                m_dialogueActorCache.Add(dialogueActor.transform, dialogueActor);
            }
            if (m_useBarkUIs.Contains(dialogueActor.transform) && subtitlePanelNumber != SubtitlePanelNumber.UseBarkUI)
            {
                m_useBarkUIs.Remove(dialogueActor.transform);
            }
        }

        #endregion

        #region Show & Hide

        /// <summary>
        /// Shows a subtitle. Opens a subtitle panel and sets the content. If the speaker
        /// has a DialogueActor component, this may dictate which panel opens.
        /// </summary>
        /// <param name="subtitle">Subtitle to show.</param>
        public override void ShowSubtitle(Subtitle subtitle)
        {
            if (subtitle == null) return;
            DialogueActor dialogueActor;
            var panel = GetPanel(subtitle, out dialogueActor);
            if (SubtitleUsesBarkUI(subtitle))
            {
                DialogueManager.BarkString(subtitle.formattedText.text, subtitle.speakerInfo.transform, subtitle.listenerInfo.transform, subtitle.sequence);
            }
            else if (panel == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: Can't find subtitle panel for " + GetSubtitleTextSummary(subtitle) + ".");
            }
            else if (string.IsNullOrEmpty(subtitle.formattedText.text))
            {
                HideSubtitle(subtitle);
            }
            else
            {
                // If actor is currently displaying on another panel, close that panel:
                var actorID = subtitle.speakerInfo.id;
                if (m_lastPanelUsedByActor.ContainsKey(actorID) && m_lastPanelUsedByActor[actorID] != panel)
                {
                    var previousPanel = m_lastPanelUsedByActor[actorID];
                    if (m_lastActorToUsePanel.ContainsKey(previousPanel) && m_lastActorToUsePanel[previousPanel] == actorID)
                    {
                        if (previousPanel.hasFocus) previousPanel.Unfocus();
                        if (previousPanel.isOpen) previousPanel.Close();
                    }
                }
                m_lastActorToUsePanel[panel] = actorID;
                m_lastPanelUsedByActor[actorID] = panel;

                // Focus the panel and show the subtitle:
                m_focusedPanel = panel;
                if (panel.addSpeakerName)
                {
                    subtitle.formattedText.text = string.Format(panel.addSpeakerNameFormat, new object[] { subtitle.speakerInfo.Name, subtitle.formattedText.text });
                }
                if (dialogueActor != null && dialogueActor.standardDialogueUISettings.setSubtitleColor)
                {
                    subtitle.formattedText.text = dialogueActor.AdjustSubtitleColor(subtitle);
                }
                panel.ShowSubtitle(subtitle);
                SupercedeOtherPanels(panel);
            }
        }

        /// <summary>
        /// Hides a subtitle.
        /// </summary>
        /// <param name="subtitle">Subtitle associated with panel to hide.</param>
        public void HideSubtitle(Subtitle subtitle)
        {
            if (subtitle == null) return;
            DialogueActor dialogueActor;
            var panel = GetPanel(subtitle, out dialogueActor);
            if (SubtitleUsesBarkUI(subtitle)) return;
            if (panel == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: Can't find subtitle panel for " + GetSubtitleTextSummary(subtitle) + ".");
            }
            else if (panel.visibility == UIVisibility.OnlyDuringContent)
            {
                panel.HideSubtitle(subtitle);
            }
            else
            {
                panel.FinishSubtitle();
            }
        }

        /// <summary>
        /// Close all panels.
        /// </summary>
        public void Close()
        {
            if (m_defaultNPCPanel != null) m_defaultNPCPanel.Close();
            if (m_defaultPCPanel != null) m_defaultPCPanel.Close();
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                if (m_builtinPanels[i] != null) m_builtinPanels[i].Close();
            }
            foreach (var kvp in m_actorPanelCache)
            {
                if (kvp.Value != null) kvp.Value.Close();
            }
            ClearCache();
        }

        protected virtual void SupercedeOtherPanels(StandardUISubtitlePanel newPanel)
        {
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                var panel = m_builtinPanels[i];
                if (panel == null || panel == newPanel) continue;
                if (panel.isOpen)
                {
                    if (panel.visibility == UIVisibility.UntilSuperceded)
                    {
                        panel.Close();
                    }
                    else
                    {
                        panel.Unfocus();
                    }
                }
            }
        }

        public virtual void UnfocusAll()
        {
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                var panel = m_builtinPanels[i];
                if (panel != null && panel.isOpen && panel.hasFocus) panel.Unfocus();
            }
        }

        public override void ShowContinueButton()
        {
            if (m_focusedPanel != null) m_focusedPanel.ShowContinueButton();
        }

        public override void HideContinueButton()
        {
            if (m_focusedPanel != null) m_focusedPanel.HideContinueButton();
        }

        public override void SetActive(bool value) { } // Unused. Work is done by StandardUISubtitlePanel.
        public override void SetSubtitle(Subtitle subtitle) { } // Unused. Work is done by StandardUISubtitlePanel.
        public override void ClearSubtitle() { } // Unused. Work is done by StandardUISubtitlePanel.

        /// <summary>
        /// Sets the portrait texture to use in the subtitle if the named actor is the speaker.
        /// This is used to immediately update the GUI control if the SetPortrait() sequencer 
        /// command changes the portrait texture.
        /// </summary>
        /// <param name="actorName">Actor name in database.</param>
        /// <param name="portraitTexture">Portrait texture.</param>
        public override void SetActorPortraitTexture(string actorName, Texture2D portraitTexture)
        {
            if (string.IsNullOrEmpty(actorName)) return;
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                var panel = m_builtinPanels[i];
                if (panel != null && panel.currentSubtitle != null && string.Equals(panel.currentSubtitle.speakerInfo.nameInDatabase, actorName))
                {
                    panel.SetActorPortraitTexture(actorName, portraitTexture);
                    return;
                }
            }
            foreach (var panel in m_actorPanelCache.Values)
            {
                if (panel != null && panel.currentSubtitle != null && string.Equals(panel.currentSubtitle.speakerInfo.nameInDatabase, actorName))
                {
                    panel.SetActorPortraitTexture(actorName, portraitTexture);
                    return;
                }
            }
        }

        /// <summary>
        /// Searches the current conversation for any DialogueActors who use subtitle
        /// panels that are configured to appear when the conversation starts.
        /// </summary>
        public void OpenSubtitlePanelsOnStartConversation()
        {
            var conversation = DialogueManager.MasterDatabase.GetConversation(DialogueManager.lastConversationStarted);
            if (conversation == null) return;
            HashSet<StandardUISubtitlePanel> checkedPanels = new HashSet<StandardUISubtitlePanel>();
            HashSet<int> checkedActorIDs = new HashSet<int>();

            // Check main Actor & Conversant:
            var mainActorID = conversation.ActorID;
            var mainActor = DialogueManager.masterDatabase.GetActor(DialogueActor.GetActorName(DialogueManager.currentActor));
            if (mainActor != null) mainActorID = mainActor.id;
            CheckActorIDOnStartConversation(mainActorID, checkedActorIDs, checkedPanels);
            CheckActorIDOnStartConversation(conversation.ConversantID, checkedActorIDs, checkedPanels);

            // Check other actors:
            for (int i = 0; i < conversation.dialogueEntries.Count; i++)
            {
                var actorID = conversation.dialogueEntries[i].ActorID;
                CheckActorIDOnStartConversation(actorID, checkedActorIDs, checkedPanels);
            }
        }

        private void CheckActorIDOnStartConversation(int actorID, HashSet<int> checkedActorIDs, HashSet<StandardUISubtitlePanel> checkedPanels)
        {
            if (checkedActorIDs.Contains(actorID)) return;
            checkedActorIDs.Add(actorID);
            var actor = DialogueManager.MasterDatabase.GetActor(actorID);
            if (actor == null) return;
            var actorTransform = CharacterInfo.GetRegisteredActorTransform(actor.Name);
            if (actorTransform == null)
            {
                var go = GameObject.Find(actor.Name);
                if (go != null) actorTransform = go.transform;
            }
            DialogueActor dialogueActor;
            var panel = GetActorTransformPanel(actorTransform, actor.IsPlayer ? m_defaultPCPanel : m_defaultNPCPanel, out dialogueActor);
            if (m_actorIdOverridePanel.ContainsKey(actor.id))
            {
                panel = m_actorIdOverridePanel[actor.id];
            }
            if (checkedPanels.Contains(panel)) return;
            checkedPanels.Add(panel);
            if (panel.visibility == UIVisibility.AlwaysFromStart)
            {
                var actorPortrait = (dialogueActor != null && dialogueActor.portrait != null) ? dialogueActor.portrait : actor.portrait;
                var actorName = CharacterInfo.GetLocalizedDisplayNameInDatabase(actor.Name);
                panel.OpenOnStartConversation(actorPortrait, actorName, dialogueActor);

                m_lastActorToUsePanel[panel] = actorID;
                m_lastPanelUsedByActor[actorID] = panel;
            }
        }

        #endregion

        #region Typewriter Speed

        public virtual float GetTypewriterSpeed()
        {
            AbstractTypewriterEffect typewriter;
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                typewriter = GetTypewriter(m_builtinPanels[i]);
                if (typewriter != null) return TypewriterUtility.GetTypewriterSpeed(typewriter);
            }
            typewriter = GetTypewriter(m_defaultNPCPanel);
            if (typewriter != null) return TypewriterUtility.GetTypewriterSpeed(typewriter);
            typewriter = GetTypewriter(m_defaultNPCPanel);
            return TypewriterUtility.GetTypewriterSpeed(typewriter);
        }

        public virtual void SetTypewriterSpeed(float charactersPerSecond)
        {
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                if (m_builtinPanels[i] != null) TypewriterUtility.GetTypewriterSpeed(m_builtinPanels[i].subtitleText);
            }
            if (m_defaultNPCPanel != null && !m_builtinPanels.Contains(m_defaultNPCPanel)) TypewriterUtility.GetTypewriterSpeed(m_defaultNPCPanel.subtitleText);
            if (m_defaultPCPanel != null && !m_builtinPanels.Contains(m_defaultPCPanel)) TypewriterUtility.GetTypewriterSpeed(m_defaultPCPanel.subtitleText);
        }

        private AbstractTypewriterEffect GetTypewriter(StandardUISubtitlePanel panel)
        {
            return (panel != null) ? TypewriterUtility.GetTypewriter(panel.subtitleText) : null;
        }

        #endregion

    }

}
