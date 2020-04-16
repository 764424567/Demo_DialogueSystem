// Copyright (c) Pixel Crushers. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Manages response menus for StandardDialogueUI.
    /// </summary>
    [System.Serializable]
    public class StandardUIResponseMenuControls : AbstractUIResponseMenuControls
    {

        #region Public Fields

        /// <summary>
        /// Assign this delegate if you want it to replace the default timeout handler.
        /// </summary>
        public System.Action timeoutHandler = null;

        public override AbstractUISubtitleControls subtitleReminderControls { get { return null; } } // Not used.

        #endregion

        #region Private Fields

        private List<StandardUIMenuPanel> m_builtinPanels = new List<StandardUIMenuPanel>();
        private StandardUIMenuPanel m_defaultPanel = null;
        private Dictionary<Transform, StandardUIMenuPanel> m_actorPanelCache = new Dictionary<Transform, StandardUIMenuPanel>();
        private StandardUIMenuPanel m_currentPanel = null;
        private Texture2D m_pcPortraitTexture = null;
        private string m_pcPortraitName = null;

        #endregion

        #region Initialization & Lookup

        public void Initialize(StandardUIMenuPanel[] menuPanels, StandardUIMenuPanel defaultMenuPanel)
        {
            m_builtinPanels.Clear();
            m_builtinPanels.AddRange(menuPanels);
            m_defaultPanel = (defaultMenuPanel != null) ? defaultMenuPanel : (m_builtinPanels.Count > 0) ? m_builtinPanels[0] : null;
            ClearCache();
            if (timeoutHandler == null) timeoutHandler = DefaultTimeoutHandler;
        }

        private void ClearCache()
        {
            m_actorPanelCache.Clear();
        }

        /// <summary>
        /// Changes a dialogue actor's menu panel for the current conversation.
        /// </summary>
        public virtual void SetActorMenuPanelNumber(DialogueActor dialogueActor, MenuPanelNumber menuPanelNumber)
        {
            if (dialogueActor == null) return;
            OverrideActorMenuPanel(dialogueActor.transform, menuPanelNumber, dialogueActor.standardDialogueUISettings.customMenuPanel);
        }

        /// <summary>
        /// For speakers who do not have DialogueActor components, this method overrides the
        /// actor's default panel.
        /// </summary>
        public void OverrideActorMenuPanel(Transform actorTransform, MenuPanelNumber menuPanelNumber, StandardUIMenuPanel customPanel)
        {
            if (actorTransform == null) return;
            m_actorPanelCache[actorTransform] = GetPanelFromNumber(menuPanelNumber, customPanel);
        }

        private StandardUIMenuPanel GetPanel(Subtitle lastSubtitle)
        {
            // Find player's DialogueActor:
            var playerTransform = (lastSubtitle != null && lastSubtitle.speakerInfo.isPlayer) ? lastSubtitle.speakerInfo.transform : DialogueManager.currentActor;
            if (playerTransform == null) return m_defaultPanel;
            if (m_actorPanelCache.ContainsKey(playerTransform)) return m_actorPanelCache[playerTransform];
            var dialogueActor = DialogueActor.GetDialogueActorComponent(playerTransform);

            // Check NPC for non-default menu panel:
            var playerUsesDefaultMenuPanel = dialogueActor == null || dialogueActor.standardDialogueUISettings.menuPanelNumber == MenuPanelNumber.Default;
            var otherTransform = (lastSubtitle != null && lastSubtitle.speakerInfo.isNPC) ? lastSubtitle.speakerInfo.transform : DialogueManager.currentConversant;
            if (playerUsesDefaultMenuPanel && otherTransform != null && m_actorPanelCache.ContainsKey(otherTransform)) return m_actorPanelCache[otherTransform];
            var otherDialogueActor = DialogueActor.GetDialogueActorComponent(otherTransform);
            if (otherDialogueActor != null &&
                (otherDialogueActor.standardDialogueUISettings.useMenuPanelFor == DialogueActor.UseMenuPanelFor.MeAndResponsesToMe ||
                (otherDialogueActor.standardDialogueUISettings.menuPanelNumber != MenuPanelNumber.Default && playerUsesDefaultMenuPanel)))
            {
                if (otherTransform != null && m_actorPanelCache.ContainsKey(otherTransform)) return m_actorPanelCache[otherTransform];
                var otherPanel = GetDialogueActorPanel(otherDialogueActor);
                if (otherPanel != null) return otherPanel;
            }

            // Otherwise use player's menu panel:
            var panel = GetDialogueActorPanel(dialogueActor);
            if (panel == null) panel = m_defaultPanel;
            m_actorPanelCache.Add(playerTransform, panel);
            return panel;
        }

        private StandardUIMenuPanel GetDialogueActorPanel(DialogueActor dialogueActor)
        {
            if (dialogueActor == null) return null;
            return GetPanelFromNumber(dialogueActor.standardDialogueUISettings.menuPanelNumber, dialogueActor.standardDialogueUISettings.customMenuPanel);
        }

        private StandardUIMenuPanel GetPanelFromNumber(MenuPanelNumber menuPanelNumber, StandardUIMenuPanel customMenuPanel)
        { 
            switch (menuPanelNumber)
            {
                case MenuPanelNumber.Default:
                    return m_defaultPanel;
                case MenuPanelNumber.Custom:
                    return customMenuPanel;
                default:
                    var index = PanelNumberUtility.GetMenuPanelIndex(menuPanelNumber);
                    return (0 <= index && index < m_builtinPanels.Count) ? m_builtinPanels[index] : null;
            }
        }

        #endregion

        #region Portraits

        /// <summary>
        /// Sets the PC portrait name and texture to use in the response menu.
        /// </summary>
        /// <param name="portraitTexture">Portrait texture.</param>
        /// <param name="portraitName">Portrait name.</param>
        public override void SetPCPortrait(Texture2D portraitTexture, string portraitName)
        {
            m_pcPortraitTexture = portraitTexture;
            m_pcPortraitName = portraitName;
        }

        /// <summary>
        /// Sets the portrait texture to use in the response menu if the named actor is the player.
        /// This is used to immediately update the GUI control if the SetPortrait() sequencer 
        /// command changes the portrait texture.
        /// </summary>
        /// <param name="actorName">Actor name in database.</param>
        /// <param name="portraitTexture">Portrait texture.</param>
        public override void SetActorPortraitTexture(string actorName, Texture2D portraitTexture)
        {
            if (string.Equals(actorName, m_pcPortraitName))
            {
                Texture2D actorPortraitTexture = AbstractDialogueUI.GetValidPortraitTexture(actorName, portraitTexture);
                m_pcPortraitTexture = actorPortraitTexture;
                if (m_currentPanel != null && m_currentPanel.pcImage != null && DialogueManager.masterDatabase.IsPlayer(actorName))
                {
                    m_currentPanel.pcImage.sprite = UITools.CreateSprite(actorPortraitTexture);
                }
            }
        }

        #endregion

        #region Show & Hide Responses 

        protected override void ClearResponseButtons() { } // Unused. Handled by StandardUIMenuPanel.
        protected override void SetResponseButtons(Response[] responses, Transform target) { } // Unused. Handled by StandardUIMenuPanel.

        public override void SetActive(bool value)
        {
            // Only hide. Show is handled by StandardUIMenuPanel.
            if (value == false && m_currentPanel != null) m_currentPanel.HideResponses();
        }

        /// <summary>
        /// Shows a response menu.
        /// </summary>
        /// <param name="lastSubtitle">The last subtitle shown. Used to determine which menu panel to use.</param>
        /// <param name="responses">Responses to show in menu panel.</param>
        /// <param name="target">Send OnClick events to this GameObject (the dialogue UI).</param>
        public override void ShowResponses(Subtitle lastSubtitle, Response[] responses, Transform target)
        {
            var panel = GetPanel(lastSubtitle);
            if (panel == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: Can't find menu panel.");
            }
            else
            {
                m_currentPanel = panel;
                panel.SetPCPortrait(m_pcPortraitTexture, m_pcPortraitName);
                panel.ShowResponses(lastSubtitle, responses, target);
            }
        }

        /// <summary>
        /// Makes the current menu panel's buttons non-clickable.
        /// Typically called by the dialogue UI as soon as a button has been
        /// clicked to make sure the player can't click another one while the
        /// menu is playing its hide animation.
        /// </summary>
        public virtual void MakeButtonsNonclickable()
        {
            if (m_currentPanel != null)
            {
                m_currentPanel.MakeButtonsNonclickable();
            }
        }

        /// <summary>
        /// Close all panels.
        /// </summary>
        public void Close()
        {
            for (int i = 0; i < m_builtinPanels.Count; i++)
            {
                if (m_builtinPanels[i] != null) m_builtinPanels[i].Close();
            }
            if (m_defaultPanel != null && !m_builtinPanels.Contains(m_defaultPanel)) m_defaultPanel.Close();
            foreach (var kvp in m_actorPanelCache)
            {
                var panel = kvp.Value;
                if (panel != null && !m_builtinPanels.Contains(panel)) panel.Close();
            }
            ClearCache();
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <param name='timeout'>Timeout duration in seconds.</param>
        public override void StartTimer(float timeout)
        {
            if (m_currentPanel != null) m_currentPanel.StartTimer(timeout, timeoutHandler);
        }

        public void DefaultTimeoutHandler()
        {
            DialogueManager.instance.SendMessage(DialogueSystemMessages.OnConversationTimeout);
        }

        #endregion

    }

}
