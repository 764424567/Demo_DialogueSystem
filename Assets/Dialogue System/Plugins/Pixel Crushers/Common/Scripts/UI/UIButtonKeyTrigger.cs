// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.EventSystems;

namespace PixelCrushers
{

    /// <summary>
    /// This script adds a key or button trigger to a Unity UI Selectable.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    [RequireComponent(typeof(UnityEngine.UI.Selectable))]
    public class UIButtonKeyTrigger : MonoBehaviour
    {

        [Tooltip("Trigger the selectable when this key is pressed.")]
        public KeyCode key = KeyCode.None;

        [Tooltip("Trigger the selectable when this input button is pressed.")]
        public string buttonName = string.Empty;

        private UnityEngine.UI.Selectable m_selectable;

        void Awake()
        {
            m_selectable = GetComponent<UnityEngine.UI.Selectable>();
            if (m_selectable == null) enabled = false;
        }

        void Update()
        {
            if (Input.GetKeyDown(key) || (!string.IsNullOrEmpty(buttonName) && InputDeviceManager.IsButtonDown(buttonName)))
            {
                ExecuteEvents.Execute(m_selectable.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.submitHandler);
            }
        }

    }

}
