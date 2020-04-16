// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace PixelCrushers.DialogueSystem
{

    [AddComponentMenu("")] // Use wrapper.
    public class StandardUISubtitlePanel : UIPanel
    {

        #region Serialized Fields

        [Tooltip("(Optional) Main panel for subtitle.")]
        public RectTransform panel;

        [Tooltip("(Optional) Image for actor's portrait.")]
        public UnityEngine.UI.Image portraitImage;

        [Tooltip("(Optional) Text element for actor's name.")]
        public UITextField portraitName;

        [Tooltip("Subtitle text.")]
        public UITextField subtitleText;

        [Tooltip("Add speaker's name to subtitle text.")]
        public bool addSpeakerName = false;

        [Tooltip("Format to add speaker name, where {0} is name and {1} is subtitle text.")]
        public string addSpeakerNameFormat = "{0}: {1}";

        [Tooltip("Each subtitle adds to Subtitle Text instead of replacing it.")]
        public bool accumulateText = false;

        [Tooltip("(Optional) Continue button. Only shown if Dialogue Manager's Continue Button mode uses continue button.")]
        public UnityEngine.UI.Button continueButton;

        [Tooltip("When the subtitle UI elements should be visible.")]
        public UIVisibility visibility = UIVisibility.OnlyDuringContent;

        [Tooltip("When focusing panel, set this animator trigger.")]
        public string focusAnimationTrigger = string.Empty;

        [Tooltip("When unfocusing panel, set this animator trigger.")]
        public string unfocusAnimationTrigger = string.Empty;

        [Tooltip("Check Dialogue Actors for portrait animator controllers. Portrait image must have an Animator.")]
        public bool useAnimatedPortraits = false;

        [Tooltip("If a player actor uses this panel, don't show player portrait name & image; keep previous NPC portrait visible instead.")]
        public bool onlyShowNPCPortraits = false;

        /// <summary>
        /// Invoked when the subtitle panel gains focus.
        /// </summary>
        public UnityEvent onFocus = new UnityEvent();

        /// <summary>
        /// Invoked when the subtitle panel loses focus.
        /// </summary>
        public UnityEvent onUnfocus = new UnityEvent();

        #endregion

        #region Public Properties

        private bool m_hasFocus = true;
        public virtual bool hasFocus
        {
            get { return m_hasFocus; }
            protected set { m_hasFocus = value; }
        }

        private Subtitle m_currentSubtitle = null;
        public virtual Subtitle currentSubtitle
        {
            get { return m_currentSubtitle; }
            protected set { m_currentSubtitle = value; }
        }

        #endregion

        #region Internal Properties

        private bool m_haveSavedOriginalColor = false;
        protected bool haveSavedOriginalColor { get { return m_haveSavedOriginalColor; } set { m_haveSavedOriginalColor = value; } }
        protected Color originalColor { get; set; }
        private string m_accumulatedText = string.Empty;
        private Animator m_animator = null;
        private Animator animator { get { if (m_animator == null && portraitImage != null) m_animator = portraitImage.GetComponent<Animator>(); return m_animator; } }
        private bool m_isDefaultNPCPanel = false;
        public bool isDefaultNPCPanel { get { return m_isDefaultNPCPanel; } set { m_isDefaultNPCPanel = value; } }
        private bool m_isDefaultPCPanel = false;
        public bool isDefaultPCPanel { get { return m_isDefaultPCPanel; } set { m_isDefaultPCPanel = value; } }
        private int m_panelNumber = -1;
        public int panelNumber { get { return m_panelNumber; } set { m_panelNumber = value; } }

        #endregion

        #region Typewriter Control

        /// <summary>
        /// Returns the typewriter effect on the subtitle text element, or null if there is none.
        /// </summary>
        public AbstractTypewriterEffect GetTypewriter()
        {
            return TypewriterUtility.GetTypewriter(subtitleText);
        }

        /// <summary>
        /// Checks if the subtitle text element has a typewriter effect.
        /// </summary>
        public bool HasTypewriter()
        {
            return GetTypewriter() != null;
        }

        /// <summary>
        /// Returns the speed of the typewriter effect on the subtitle element if it has one.
        /// </summary>
        public float GetTypewriterSpeed()
        {
            return TypewriterUtility.GetTypewriterSpeed(subtitleText);
        }

        /// <summary>
        /// Sets the speed of the typewriter effect on the subtitle element if it has one.
        /// </summary>
        public void SetTypewriterSpeed(float charactersPerSecond)
        {
            TypewriterUtility.SetTypewriterSpeed(subtitleText, charactersPerSecond);
        }

        #endregion

        #region Show & Hide

        /// <summary>
        /// Shows the panel at the start of the conversation; called if it's configured to be visible at the start.
        /// </summary>
        /// <param name="portraitImage">The image of the first actor who will use this panel.</param>
        /// <param name="portraitName">The name of the first actor who will use this panel.</param>
        /// <param name="dialogueActor">The actor's DialogueActor component, or null if none.</param>
        public virtual void OpenOnStartConversation(Texture2D portraitImage, string portraitName, DialogueActor dialogueActor)
        {
            Open();
            SetUIElementsActive(true);
            if (this.portraitImage != null) this.portraitImage.sprite = UITools.CreateSprite(portraitImage);
            if (this.portraitName != null) this.portraitName.text = portraitName;
            if (subtitleText.text != null) subtitleText.text = string.Empty;
            CheckDialogueActorAnimator(dialogueActor);
        }

        public void OnConversationStart(Transform actor)
        {
            m_accumulatedText = string.Empty;
        }

        /// <summary>
        /// Shows a subtitle, playing the open and focus animations.
        /// </summary>
        public virtual void ShowSubtitle(Subtitle subtitle)
        {
            SetUIElementsActive(true);
            Open();
            Focus();
            SetContent(subtitle);
            CheckSubtitleAnimator(subtitle);
        }

        /// <summary>
        /// Hides a subtitle, playing the unfocus and close animations.
        /// </summary>
        public virtual void HideSubtitle(Subtitle subtitle)
        {
            if (panelState != PanelState.Closed) Unfocus();
            Close();
        }

        /// <summary>
        /// Immediately hides the panel without playing any animations.
        /// </summary>
        public virtual void HideImmediate()
        {
            DeactivateUIElements();
        }

        /// <summary>
        /// Opens the panel.
        /// </summary>
        public override void Open()
        {
            base.Open();
        }

        /// <summary>
        /// Closes the panel.
        /// </summary>
        public override void Close()
        {
            if (isOpen) base.Close();
            m_accumulatedText = string.Empty;
            hasFocus = true;
        }

        /// <summary>
        /// Focuses the panel.
        /// </summary>
        public virtual void Focus()
        {
            if (hasFocus) return;
            hasFocus = true;
            animatorMonitor.SetTrigger(focusAnimationTrigger, null, false);
            onFocus.Invoke();
        }

        /// <summary>
        /// Unfocuses the panel.
        /// </summary>
        public virtual void Unfocus()
        {
            if (!hasFocus) return;
            hasFocus = false;
            animatorMonitor.SetTrigger(unfocusAnimationTrigger, null, false);
            onUnfocus.Invoke();
        }

        protected void ActivateUIElements()
        {
            SetUIElementsActive(true);
        }

        protected void DeactivateUIElements()
        {
            SetUIElementsActive(false);
            ClearText();
        }

        protected virtual void SetUIElementsActive(bool value)
        {
            Tools.SetGameObjectActive(panel, value);
            Tools.SetGameObjectActive(portraitImage, value);
            portraitName.SetActive(value);
            subtitleText.SetActive(value);
            Tools.SetGameObjectActive(continueButton, false); // Let ConversationView determine if continueButton should be shown.
        }

        public virtual void ClearText()
        {
            subtitleText.text = string.Empty;
        }

        public virtual void ShowContinueButton()
        {
            Tools.SetGameObjectActive(continueButton, true);
            if (continueButton != null && continueButton.onClick.GetPersistentEventCount() == 0)
            {
                var fastForward = continueButton.GetComponent<StandardUIContinueButtonFastForward>();
                if (fastForward != null)
                {
                    continueButton.onClick.AddListener(fastForward.OnFastForward);
                }
                else
                {
                    continueButton.onClick.AddListener(OnContinue);
                }
            }
        }

        public virtual void HideContinueButton()
        {
            Tools.SetGameObjectActive(continueButton, false);
        }

        /// <summary>
        /// Finishes the subtitle without hiding the panel. Called if the panel is configured to stay open.
        /// Hides the continue button and stops the typewriter effect.
        /// </summary>
        public virtual void FinishSubtitle()
        {
            HideContinueButton();
            var typewriter = GetTypewriter();
            if (typewriter != null && typewriter.isPlaying) typewriter.Stop();
        }

        /// <summary>
        /// Selects the panel's continue button (i.e., navigates to it).
        /// </summary>
        /// <param name="allowStealFocus">Select continue button even if another element is already selected.</param>
        public virtual void Select(bool allowStealFocus = true)
        {
            UITools.Select(continueButton, allowStealFocus);
        }

        /// <summary>
        /// The continue button should call this method to continue.
        /// </summary>
        public virtual void OnContinue()
        {
            var dialogueUI = GetComponentInParent<AbstractDialogueUI>();
            if (dialogueUI == null) dialogueUI = DialogueManager.dialogueUI as AbstractDialogueUI;
            if (dialogueUI != null) dialogueUI.OnContinueConversation();
        }

        /// <summary>
        /// Sets the content of the panel. Assumes the panel is already open.
        /// </summary>
        public virtual void SetContent(Subtitle subtitle)
        {
            if (subtitle == null) return;
            currentSubtitle = subtitle;
            if (!onlyShowNPCPortraits || subtitle.speakerInfo.isNPC)
            {
                if (portraitImage != null) portraitImage.sprite = UITools.CreateSprite(subtitle.GetSpeakerPortrait());
                portraitName.text = subtitle.speakerInfo.Name;
                UITools.SendTextChangeMessage(portraitName);
            }
            TypewriterUtility.StopTyping(subtitleText);
            var previousText = accumulateText ? m_accumulatedText : string.Empty;
            SetFormattedText(subtitleText, previousText, subtitle.formattedText);
            if (accumulateText) m_accumulatedText = subtitleText.text + "\n";
            TypewriterUtility.StartTyping(subtitleText, subtitleText.text, previousText.Length);
        }

        protected virtual void SetFormattedText(UITextField textField, string previousText, FormattedText formattedText)
        {
            textField.text = previousText + UITools.GetUIFormattedText(formattedText);
            UITools.SendTextChangeMessage(textField);
            if (!haveSavedOriginalColor)
            {
                originalColor = textField.color;
                haveSavedOriginalColor = true;
            }
            textField.color = (formattedText.emphases != null && formattedText.emphases.Length > 0) ? formattedText.emphases[0].color : originalColor;
        }

        public virtual void SetActorPortraitTexture(string actorName, Texture2D portraitTexture)
        {
            if (portraitImage == null) return;
            portraitImage.sprite = UITools.CreateSprite(AbstractDialogueUI.GetValidPortraitTexture(actorName, portraitTexture));
        }

        public void CheckSubtitleAnimator(Subtitle subtitle)
        {
            if (subtitle != null && useAnimatedPortraits && animator != null)
            {
                var dialogueActor = DialogueActor.GetDialogueActorComponent(subtitle.speakerInfo.transform);
                if (dialogueActor != null && dialogueActor.standardDialogueUISettings.portraitAnimatorController != null)
                {
                    var speakerPanelNumber = dialogueActor.GetSubtitlePanelNumber();
                    var isMyPanel = (PanelNumberUtility.GetSubtitlePanelIndex(speakerPanelNumber) == this.panelNumber) ||
                        (speakerPanelNumber == SubtitlePanelNumber.Default && subtitle.speakerInfo.isNPC && isDefaultNPCPanel) ||
                        (speakerPanelNumber == SubtitlePanelNumber.Default && subtitle.speakerInfo.isPlayer && isDefaultPCPanel) ||
                        (speakerPanelNumber == SubtitlePanelNumber.Custom && dialogueActor.standardDialogueUISettings.customSubtitlePanel == this);
                    if (isMyPanel)
                    {
                        StartCoroutine(SetAnimatorAtEndOfFrame(dialogueActor.standardDialogueUISettings.portraitAnimatorController));
                    }
                }
            }
        }

        protected void CheckDialogueActorAnimator(DialogueActor dialogueActor)
        {
            if (dialogueActor != null && useAnimatedPortraits && animator != null &&
                dialogueActor.standardDialogueUISettings.portraitAnimatorController != null)
            {
                StartCoroutine(SetAnimatorAtEndOfFrame(dialogueActor.standardDialogueUISettings.portraitAnimatorController));
            }
        }

        private IEnumerator SetAnimatorAtEndOfFrame(RuntimeAnimatorController animatorController)
        {
            if (animator.runtimeAnimatorController != animatorController)
            {
                animator.runtimeAnimatorController = animatorController;
            }
            yield return new WaitForEndOfFrame();
            if (animator.runtimeAnimatorController != animatorController)
            {
                animator.runtimeAnimatorController = animatorController;
            }
        }

        #endregion

    }
}
