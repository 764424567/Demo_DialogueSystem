// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

namespace PixelCrushers
{

    public enum InputDevice { Joystick, Keyboard, Mouse, Touch }

    /// <summary>
    /// This script checks for joystick and keyboard input. If the player uses a joystick,
    /// it enables autofocus. If the player uses the mouse or keyboard, it disables autofocus.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class InputDeviceManager : MonoBehaviour
    {

        [Tooltip("Current input mode.")]
        public InputDevice inputDevice = InputDevice.Joystick;

        [Tooltip("If any of these keycodes are pressed, current device is joystick.")]
        public KeyCode[] joystickKeyCodesToCheck = new KeyCode[] { KeyCode.JoystickButton0, KeyCode.JoystickButton1, KeyCode.JoystickButton2, KeyCode.JoystickButton7 };

        [Tooltip("If any of these buttons are pressed, current device is joystick. Must be defined in Input Manager.")]
        public string[] joystickButtonsToCheck = new string[0];

        [Tooltip("If any of these axes are greater than Joystick Axis Threshold, current device is joystick. Must be defined in Input Manager.")]
        public string[] joystickAxesToCheck = new string[0];
        //--- Changed to prevent errors in new projects if user hasn't clicked "Add Input Definitions" yet.
        //--- Added "Add Default Joystick Axes Check" button instead.
        //public string[] joystickAxesToCheck = new string[] { "JoystickAxis1", "JoystickAxis2", "JoystickAxis3", "JoystickAxis4", "JoystickAxis6", "JoystickAxis7" };

        [Tooltip("Joystick axis values must be above this threshold to switch to joystick mode.")]
        public float joystickAxisThreshold = 0.5f;

        [Tooltip("If any of these buttons are pressed, current device is keyboard (unless device is currently mouse).")]
        public string[] keyButtonsToCheck = new string[0];

        [Tooltip("If any of these keys are pressed, current device is keyboard (unless device is currently mouse).")]
        public KeyCode[] keyCodesToCheck = new KeyCode[] { KeyCode.Escape };

        [Tooltip("Always enable joystick/keyboard navigation even in Mouse mode.")]
        public bool alwaysAutoFocus = false;

        [Tooltip("Switch to mouse control if player clicks mouse buttons or moves mouse.")]
        public bool detectMouseControl = true;

        [Tooltip("If mouse moves more than this, current device is mouse.")]
        public float mouseMoveThreshold = 0.1f;

        [Tooltip("Hide cursor in joystick/key mode, show in mouse mode.")]
        public bool controlCursorState = true;

        [Tooltip("When paused and device is mouse, make sure cursor is visible.")]
        public bool enforceCursorOnPause = false;

        [Tooltip("Enable GraphicRaycasters (which detect cursor clicks on UI elements) only when device is mouse.")]
        public bool controlGraphicRaycasters = false;

        [Tooltip("If any of these keycodes are pressed, go back to the previous menu.")]
        public KeyCode[] backKeyCodes = new KeyCode[] { KeyCode.JoystickButton1 };

        [Tooltip("If any of these buttons are pressed, go back to the previous menu.")]
        public string[] backButtons = new string[] { "Cancel" };

        [Tooltip("Survive scene changes and only allow one instance.")]
        public bool singleton = true;

        public UnityEvent onUseKeyboard = new UnityEvent();
        public UnityEvent onUseJoystick = new UnityEvent();
        public UnityEvent onUseMouse = new UnityEvent();
        public UnityEvent onUseTouch = new UnityEvent();

        public delegate bool GetButtonDownDelegate(string buttonName);
        public delegate float GetAxisDelegate(string axisName);

        public GetButtonDownDelegate GetButtonDown = null;
        public GetAxisDelegate GetInputAxis = null;

        private Vector3 m_lastMousePosition;
        private bool m_ignoreMouse = false;

        private static InputDeviceManager m_instance = null;
        public static InputDeviceManager instance
        {
            get { return m_instance; }
            set { m_instance = value; }
        }

        public static InputDevice currentInputDevice
        {
            get
            {
                return (m_instance != null) ? m_instance.inputDevice : InputDevice.Joystick;
            }
        }

        public static bool deviceUsesCursor
        {
            get { return currentInputDevice == InputDevice.Mouse; }
        }

        public static bool autoFocus
        {
            get { return (instance != null && instance.alwaysAutoFocus) || currentInputDevice == InputDevice.Joystick || currentInputDevice == InputDevice.Keyboard; }
        }

        public static bool isBackButtonDown
        {
            get { return (m_instance != null) ? m_instance.IsBackButtonDown() : false; }
        }

        public static bool IsButtonDown(string buttonName)
        {
            return (m_instance != null && m_instance.GetButtonDown != null) ? m_instance.GetButtonDown(buttonName) : false;

        }

        public static float GetAxis(string axisName)
        {
            return (m_instance != null && m_instance.GetInputAxis != null) ? m_instance.GetInputAxis(axisName) : 0;
        }

        public void Awake()
        {
            if (m_instance != null && singleton)
            {
                Destroy(gameObject);
            }
            else
            { 
                m_instance = this;
                GetButtonDown = DefaultGetButtonDown;
                GetInputAxis = DefaultGetAxis;
                if (singleton)
                {
                    transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }
            }
#if !UNITY_5_3
            SceneManager.sceneLoaded += OnSceneLoaded;
#endif
        }

        public void OnDestroy()
        {
#if !UNITY_5_3
            SceneManager.sceneLoaded -= OnSceneLoaded;
#endif
        }

        public void Start()
        {
            m_lastMousePosition = Input.mousePosition;
            SetInputDevice(inputDevice);
            BrieflyIgnoreMouseMovement();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BrieflyIgnoreMouseMovement();
        }

        public void SetInputDevice(InputDevice newDevice)
        {
            inputDevice = newDevice;
            SetCursor(deviceUsesCursor);
            SetGraphicRaycasters(deviceUsesCursor);
            switch (inputDevice)
            {
                case InputDevice.Joystick:
                    onUseJoystick.Invoke();
                    break;
                case InputDevice.Keyboard:
                    onUseKeyboard.Invoke();
                    break;
                case InputDevice.Mouse:
                    var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                    var currentSelectable = (eventSystem != null && eventSystem.currentSelectedGameObject != null) ? eventSystem.currentSelectedGameObject.GetComponent<UnityEngine.UI.Selectable>() : null;
                    if (currentSelectable != null) currentSelectable.OnDeselect(null);
                    onUseMouse.Invoke();
                    break;
                case InputDevice.Touch:
                    onUseTouch.Invoke();
                    break;
            }
        }

        private void SetGraphicRaycasters(bool deviceUsesCursor)
        {
            if (!controlGraphicRaycasters) return;
            var raycasters = FindObjectsOfType<UnityEngine.UI.GraphicRaycaster>();
            for (int i = 0; i < raycasters.Length; i++)
            {
                raycasters[i].enabled = deviceUsesCursor;
            }
        }

        public void Update()
        {
            switch (inputDevice)
            {
                case InputDevice.Joystick:
                    if (IsUsingMouse()) SetInputDevice(InputDevice.Mouse);
                    else if (IsUsingKeyboard()) SetInputDevice(InputDevice.Mouse);
                    break;
                case InputDevice.Keyboard:
                    if (IsUsingMouse()) SetInputDevice(InputDevice.Mouse);
                    else if (IsUsingJoystick()) SetInputDevice(InputDevice.Joystick);
                    break;
                case InputDevice.Mouse:
                    if (IsUsingJoystick()) SetInputDevice(InputDevice.Joystick);
                    break;
                case InputDevice.Touch:
                    if (IsUsingMouse()) SetInputDevice(InputDevice.Mouse);
                    else if (IsUsingKeyboard()) SetInputDevice(InputDevice.Mouse);
                    break;
            }
        }

        public bool IsUsingJoystick()
        {
            try
            {
                for (int i = 0; i < joystickKeyCodesToCheck.Length; i++)
                {
                    if (Input.GetKeyDown(joystickKeyCodesToCheck[i]))
                    {
                        return true;
                    }
                }
                for (int i = 0; i < joystickButtonsToCheck.Length; i++)
                {
                    if (GetButtonDown(joystickButtonsToCheck[i]))
                    {
                        return true;
                    }
                }
                for (int i = 0; i < joystickAxesToCheck.Length; i++)
                {
                    if (Mathf.Abs(Input.GetAxisRaw(joystickAxesToCheck[i])) > joystickAxisThreshold)
                    {
                        return true;
                    }
                }
            }
            catch (System.ArgumentException e)
            {
                Debug.LogError("Some input settings listed on the Input Device Manager component are missing from Unity's Input Manager. To automatically add them, inspect the Input Device Manager component on the GameObject '" + name + "' and click the 'Add Input Definitions' button at the bottom.\n" + e.Message, this);
            }
            return false;
        }

        public bool IsUsingMouse()
        {
            if (!detectMouseControl) return false;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(1)) return true;
            var didMouseMove = !m_ignoreMouse && (Mathf.Abs(Input.mousePosition.x - m_lastMousePosition.x) > mouseMoveThreshold || Mathf.Abs(Input.mousePosition.y - m_lastMousePosition.y) > mouseMoveThreshold);
            m_lastMousePosition = Input.mousePosition;
            return didMouseMove;
        }

        public void BrieflyIgnoreMouseMovement()
        {
            StartCoroutine(BrieflyIgnoreMouseMovementCoroutine());
        }

        IEnumerator BrieflyIgnoreMouseMovementCoroutine()
        {
            m_ignoreMouse = true;
            yield return new WaitForSeconds(0.5f);
            m_ignoreMouse = false;
            m_lastMousePosition = Input.mousePosition;
            if (deviceUsesCursor)
            {
                SetCursor(true);
            }
        }

        public bool IsUsingKeyboard()
        {
            try
            {
                for (int i = 0; i < keyCodesToCheck.Length; i++)
                {
                    if (Input.GetKeyDown(keyCodesToCheck[i]))
                    {
                        return true;
                    }
                }
                for (int i = 0; i < keyButtonsToCheck.Length; i++)
                {
                    if (GetButtonDown(keyButtonsToCheck[i]))
                    {
                        return true;
                    }
                }
            }
            catch (System.ArgumentException e)
            {
                Debug.LogError("Some input settings listed on the Input Device Manager component are missing from Unity's Input Manager. To automatically add them, inspect the Input Device Manager component and click the 'Add Input Definitions' button at the bottom.\n" + e.Message, this);
            }
            return false;
        }

        public bool IsBackButtonDown()
        {
            try
            {
                for (int i = 0; i < backKeyCodes.Length; i++)
                {
                    if (Input.GetKeyDown(backKeyCodes[i]))
                    {
                        return true;
                    }
                }
                for (int i = 0; i < backButtons.Length; i++)
                {
                    if (GetButtonDown(backButtons[i]))
                    {
                        return true;
                    }
                }
            }
            catch (System.ArgumentException e)
            {
                Debug.LogError("Some input settings listed on the Input Device Manager component are missing from Unity's Input Manager. To automatically add them, inspect the Input Device Manager component and click the 'Add Input Definitions' button at the bottom.\n" + e.Message, this);
            }
            return false;
        }

        public bool DefaultGetButtonDown(string buttonName)
        {
            try
            {
                return string.IsNullOrEmpty(buttonName) ? false : Input.GetButtonDown(buttonName);
            }
            catch (System.ArgumentException) // Input button not in setup.
            {
                return false;
            }
        }

        public float DefaultGetAxis(string axisName)
        {
            try
            {
                return string.IsNullOrEmpty(axisName) ? 0 : Input.GetAxis(axisName);
            }
            catch (System.ArgumentException) // Input axis not in setup.
            {
                return 0;
            }
        }

        public void SetCursor(bool visible)
        {
            if (!controlCursorState) return;
            ForceCursor(visible);
        }

        public void ForceCursor(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            StartCoroutine(ForceCursorAfterOneFrameCoroutine(visible));
        }

        private IEnumerator ForceCursorAfterOneFrameCoroutine(bool visible)
        {
            yield return new WaitForEndOfFrame();
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

    }
}