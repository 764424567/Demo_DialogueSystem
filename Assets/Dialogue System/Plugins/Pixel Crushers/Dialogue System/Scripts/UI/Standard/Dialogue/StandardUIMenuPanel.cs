// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    [AddComponentMenu("")] // Use wrapper.
    public class StandardUIMenuPanel : UIPanel
    {

        #region Serialized Fields

        [Tooltip("(Optional) Main response menu panel.")]
        public UnityEngine.UI.Graphic panel;

        [Tooltip("(Optional) Image to show PC portrait during response menu.")]
        public UnityEngine.UI.Image pcImage;

        [Tooltip("(Optional) Text element to show PC name during response menu.")]
        public UITextField pcName;

        [Tooltip("(Optional) Slider for timed menus.")]
        public UnityEngine.UI.Slider timerSlider;

        [Tooltip("Assign design-time positioned buttons starting with first or last button.")]
        public ResponseButtonAlignment buttonAlignment = ResponseButtonAlignment.ToFirst;

        [Tooltip("Show buttons that aren't assigned to any responses. If using a 'dialogue wheel' for example, you'll want to show unused buttons so the entire wheel structure is visible.")]
        public bool showUnusedButtons = false;

        [Tooltip("Design-time positioned response buttons. (Optional if Button Template is assigned.)")]
        public StandardUIResponseButton[] buttons;

        [Tooltip("Template from which to instantiate response buttons. (Optional if using Buttons list above.)")]
        public StandardUIResponseButton buttonTemplate;

        [Tooltip("If using Button Template, instantiate buttons under this GameObject.")]
        public UnityEngine.UI.Graphic buttonTemplateHolder;

        [Tooltip("(Optional) Scrollbar to use if instantiated button holder is in a scroll rect.")]
        public UnityEngine.UI.Scrollbar buttonTemplateScrollbar;

        [Tooltip("(Optional) Component that enables or disables scrollbar as necessary for content.")]
        public UIScrollbarEnabler scrollbarEnabler;

        [Tooltip("Reset the scroll bar to this value when preparing response menu. To skip resetting the scrollbar, specify a negative value.")]
        public float buttonTemplateScrollbarResetValue = 1;

        [Tooltip("Automatically set up explicit joystick/keyboard navigation for instantiated template buttons instead of using Automatic navigation.")]
        public bool explicitNavigationForTemplateButtons = true;

        [Tooltip("If explicit navigation is enabled, loop around when navigating past end of menu.")]
        public bool loopExplicitNavigation = false;

        public UIAutonumberSettings autonumber = new UIAutonumberSettings();

        public UnityEvent onContentChanged = new UnityEvent();

        [Tooltip("When focusing panel, set this animator trigger.")]
        public string focusAnimationTrigger = string.Empty;

        [Tooltip("When unfocusing panel, set this animator trigger.")]
        public string unfocusAnimationTrigger = string.Empty;

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

        private bool m_hasFocus = false;
        public virtual bool hasFocus
        {
            get { return m_hasFocus; }
            protected set { m_hasFocus = value; }
        }

        /// <summary>
        /// The instantiated buttons. These are only valid during a specific response menu,
        /// and only if you're using templates. Each showing of the response menu clears 
        /// this list and re-populates it with new buttons.
        /// </summary>
        public List<GameObject> instantiatedButtons { get { return m_instantiatedButtons; } }
        private List<GameObject> m_instantiatedButtons = new List<GameObject>();

        #endregion

        #region Internal Fields

        protected List<GameObject> instantiatedButtonPool { get { return m_instantiatedButtonPool; } }
        private List<GameObject> m_instantiatedButtonPool = new List<GameObject>();

        protected StandardUITimer m_timer = null;
        protected System.Action m_timeoutHandler = null;

        #endregion

        #region Initialization

        public virtual void Awake()
        {
            Tools.SetGameObjectActive(buttonTemplate, false);
        }

        #endregion

        #region Show & Hide

        public virtual void SetPCPortrait(Texture2D portraitTexture, string portraitName)
        {
            if (pcImage != null) pcImage.sprite = UITools.CreateSprite(portraitTexture);
            pcName.text = portraitName;
        }

        public virtual void ShowResponses(Subtitle subtitle, Response[] responses, Transform target)
        {
            if (responses == null || responses.Length == 0)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("Dialogue System: StandardDialogueUI ShowResponses received an empty list of responses.", this);
                return;
            }
            ClearResponseButtons();
            SetResponseButtons(responses, target);
            ActivateUIElements();
            Open();
            Focus();
        }

        public virtual void HideResponses()
        {
            StopTimer();
            Unfocus();
            Close();
        }

        public override void Close()
        {
            if (isOpen) base.Close();
        }

        public virtual void Focus()
        {
            if (hasFocus) return;
            hasFocus = true;
            animatorMonitor.SetTrigger(focusAnimationTrigger, null, false);
            UITools.EnableInteractivity(gameObject);
            onFocus.Invoke();
        }

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
        }

        protected virtual void SetUIElementsActive(bool value)
        {
            Tools.SetGameObjectActive(panel, value);
            Tools.SetGameObjectActive(pcImage, value);
            pcName.SetActive(value);
            Tools.SetGameObjectActive(timerSlider, false); // Let StartTimer activate if needed.
            if (value == false) ClearResponseButtons();
        }

        public virtual void HideImmediate()
        {
            DeactivateUIElements();
        }

        protected virtual void ClearResponseButtons()
        {
            DestroyInstantiatedButtons();
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == null) continue;
                    buttons[i].Reset();
                    buttons[i].isVisible = showUnusedButtons;
                    buttons[i].gameObject.SetActive(showUnusedButtons);
                }
            }
        }

        /// <summary>
        /// Sets the response buttons.
        /// </summary>
        /// <param name='responses'>Responses.</param>
        /// <param name='target'>Target that will receive OnClick events from the buttons.</param>
        protected virtual void SetResponseButtons(Response[] responses, Transform target)
        {
            firstSelected = null;
            DestroyInstantiatedButtons();

            if ((buttons != null) && (responses != null))
            {
                // Add explicitly-positioned buttons:
                int buttonNumber = 0;
                for (int i = 0; i < responses.Length; i++)
                {
                    if (responses[i].formattedText.position != FormattedText.NoAssignedPosition)
                    {
                        int position = responses[i].formattedText.position;
                        if (0 <= position && position < buttons.Length && buttons[position] != null)
                        {
                            SetResponseButton(buttons[position], responses[i], target, buttonNumber++);
                        }
                        else
                        {
                            Debug.LogWarning("Dialogue System: Buttons list doesn't contain a button for position " + position + ".", this);
                        }
                    }
                }

                if ((buttonTemplate != null) && (buttonTemplateHolder != null))
                {
                    // Reset scrollbar to top:
                    if (buttonTemplateScrollbar != null)
                    {
                        if (buttonTemplateScrollbarResetValue >= 0)
                        {
                            buttonTemplateScrollbar.value = buttonTemplateScrollbarResetValue;
                            if (scrollbarEnabler != null)
                            {
                                scrollbarEnabler.CheckScrollbarWithResetValue(buttonTemplateScrollbarResetValue);
                            }
                        }
                        else if (scrollbarEnabler != null)
                        {
                            scrollbarEnabler.CheckScrollbar();
                        }
                    }

                    // Instantiate buttons from template:
                    for (int i = 0; i < responses.Length; i++)
                    {
                        if (responses[i].formattedText.position != FormattedText.NoAssignedPosition) continue;
                        GameObject buttonGameObject = InstantiateButton();
                        if (buttonGameObject == null)
                        {
                            Debug.LogError("Dialogue System: Couldn't instantiate response button template.");
                        }
                        else
                        {
                            instantiatedButtons.Add(buttonGameObject);
                            buttonGameObject.transform.SetParent(buttonTemplateHolder.transform, false);
                            buttonGameObject.transform.SetAsLastSibling();
                            buttonGameObject.SetActive(true);
                            StandardUIResponseButton responseButton = buttonGameObject.GetComponent<StandardUIResponseButton>();
                            SetResponseButton(responseButton, responses[i], target, buttonNumber++);
                            if (responseButton != null) buttonGameObject.name = "Response: " + responseButton.text;
                            if (firstSelected == null) firstSelected = buttonGameObject;

                        }
                    }
                }
                else
                {
                    // Auto-position remaining buttons:
                    if (buttonAlignment == ResponseButtonAlignment.ToFirst)
                    {
                        // Align to first, so add in order to front:
                        for (int i = 0; i < Mathf.Min(buttons.Length, responses.Length); i++)
                        {
                            if (responses[i].formattedText.position == FormattedText.NoAssignedPosition)
                            {
                                int position = Mathf.Clamp(GetNextAvailableResponseButtonPosition(0, 1), 0, buttons.Length - 1);
                                SetResponseButton(buttons[position], responses[i], target, buttonNumber++);
                                if (firstSelected == null) firstSelected = buttons[position].gameObject;
                            }
                        }
                    }
                    else
                    {
                        // Align to last, so add in reverse order to back:
                        for (int i = Mathf.Min(buttons.Length, responses.Length) - 1; i >= 0; i--)
                        {
                            if (responses[i].formattedText.position == FormattedText.NoAssignedPosition)
                            {
                                int position = Mathf.Clamp(GetNextAvailableResponseButtonPosition(buttons.Length - 1, -1), 0, buttons.Length - 1);
                                SetResponseButton(buttons[position], responses[i], target, buttonNumber++);
                                firstSelected = buttons[position].gameObject;
                            }
                        }
                    }
                }
            }

            if (explicitNavigationForTemplateButtons) SetupTemplateButtonNavigation();

            NotifyContentChanged();
        }

        protected virtual void SetResponseButton(StandardUIResponseButton button, Response response, Transform target, int buttonNumber)
        {
            if (button != null)
            {
                button.gameObject.SetActive(true);
                button.isVisible = true;
                button.isClickable = response.enabled;
                button.target = target;
                if (response != null) button.SetFormattedText(response.formattedText);
                button.response = response;

                // Auto-number:
                if (autonumber.enabled)
                {
                    button.text = string.Format(autonumber.format, buttonNumber + 1, button.text);
                    var keyTrigger = button.GetComponent<UIButtonKeyTrigger>();
                    if (autonumber.regularNumberHotkeys)
                    {
                        if (keyTrigger == null) keyTrigger = button.gameObject.AddComponent<UIButtonKeyTrigger>();
                        keyTrigger.key = (KeyCode)((int)KeyCode.Alpha1 + buttonNumber);
                    }
                    if (autonumber.numpadHotkeys)
                    {
                        if (autonumber.regularNumberHotkeys || keyTrigger == null) keyTrigger = button.gameObject.AddComponent<UIButtonKeyTrigger>();
                        keyTrigger.key = (KeyCode)((int)KeyCode.Keypad1 + buttonNumber);
                    }
                }
            }
        }

        protected int GetNextAvailableResponseButtonPosition(int start, int direction)
        {
            if (buttons != null)
            {
                int position = start;
                while ((0 <= position) && (position < buttons.Length))
                {
                    if (buttons[position].isVisible && buttons[position].response != null)
                    {
                        position += direction;
                    }
                    else
                    {
                        return position;
                    }
                }
            }
            return 5;
        }

        public virtual void SetupTemplateButtonNavigation()
        {
            // Assumes buttons are active (since uses GetComponent), so call after activating panel.
            if (instantiatedButtons == null || instantiatedButtons.Count == 0) return;
            for (int i = 0; i < instantiatedButtons.Count; i++)
            {
                var button = instantiatedButtons[i].GetComponent<StandardUIResponseButton>().button;
                var above = (i == 0) ? (loopExplicitNavigation ? instantiatedButtons[instantiatedButtons.Count - 1].GetComponent<StandardUIResponseButton>().button : null)
                    : instantiatedButtons[i - 1].GetComponent<StandardUIResponseButton>().button;
                var below = (i == instantiatedButtons.Count - 1) ? (loopExplicitNavigation ? instantiatedButtons[0].GetComponent<StandardUIResponseButton>().button : null)
                    : instantiatedButtons[i + 1].GetComponent<StandardUIResponseButton>().button;
                var navigation = new UnityEngine.UI.Navigation();

                navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                navigation.selectOnUp = above;
                navigation.selectOnLeft = above;
                navigation.selectOnDown = below;
                navigation.selectOnRight = below;
                button.navigation = navigation;
            }
        }

        protected GameObject InstantiateButton()
        {
            // Try to pull from pool first:
            if (m_instantiatedButtonPool.Count > 0)
            {
                var button = m_instantiatedButtonPool[0];
                m_instantiatedButtonPool.RemoveAt(0);
                return button;
            }
            else
            {
                return GameObject.Instantiate(buttonTemplate.gameObject) as GameObject;
            }
        }

        public void DestroyInstantiatedButtons()
        {
            // Return buttons to pool:
            for (int i = 0; i < instantiatedButtons.Count; i++)
            {
                instantiatedButtons[i].SetActive(false);
            }
            m_instantiatedButtonPool.AddRange(instantiatedButtons);

            instantiatedButtons.Clear();
            NotifyContentChanged();
        }

        /// <summary>
        /// Makes the panel's buttons non-clickable.
        /// Typically called by the dialogue UI as soon as a button has been
        /// clicked to make sure the player can't click another one while the
        /// menu is playing its hide animation.
        /// </summary>
        public void MakeButtonsNonclickable()
        {
            for (int i = 0; i < instantiatedButtons.Count; i++)
            {
                var responseButton = (instantiatedButtons[i] != null) ? instantiatedButtons[i].GetComponent<StandardUIResponseButton>() : null;
                if (responseButton != null) responseButton.isClickable = false;
            }
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null) buttons[i].isClickable = false;
            }
        }

        protected void NotifyContentChanged()
        {
            onContentChanged.Invoke();
        }

        #endregion

        #region Timer

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <param name='timeout'>Timeout duration in seconds.</param>
        /// <param name="timeoutHandler">Invoke this handler on timeout.</param>
        public virtual void StartTimer(float timeout, System.Action timeoutHandler)
        {            
            if (m_timer == null)
            {
                if (timerSlider != null)
                {
                    Tools.SetGameObjectActive(timerSlider, true);
                    m_timer = timerSlider.GetComponent<StandardUITimer>();
                    if (m_timer == null) m_timer = timerSlider.gameObject.AddComponent<StandardUITimer>();
                }
                else
                {
                    m_timer = GetComponentInChildren<StandardUITimer>();
                    if (m_timer == null) m_timer = gameObject.AddComponent<StandardUITimer>();
                }
            }
            Tools.SetGameObjectActive(m_timer, true);
            m_timer.StartCountdown(timeout, timeoutHandler);
        }

        public virtual void StopTimer()
        {
            if (m_timer != null)
            {
                m_timer.StopCountdown();
                Tools.SetGameObjectActive(m_timer, false);
            }
        }

        #endregion

    }

}
