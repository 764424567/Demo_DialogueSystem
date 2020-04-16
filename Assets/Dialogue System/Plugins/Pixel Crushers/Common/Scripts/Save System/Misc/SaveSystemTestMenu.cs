// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Events;

namespace PixelCrushers
{

    /// <summary>
    /// Simple menu for testing the Save System.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class SaveSystemTestMenu : MonoBehaviour
    {

        [Tooltip("Unity input button that toggles menu open/closed.")]
        public string menuInputButton = "Cancel";

        [Tooltip("Slot that menu saves game in.")]
        public int saveSlot = 1;

        [Tooltip("Optional instructions to show when script starts.")]
        public string instructions = "Press Escape for menu.";

        [Tooltip("How long to show instructions.")]
        public float instructionsDuration = 5;

        [Tooltip("Pause the game while the menu is open.")]
        public bool pauseWhileOpen = false;

        public UnityEvent onShow = new UnityEvent();
        public UnityEvent onHide = new UnityEvent();

        private bool m_isVisible = false;
        private float instructionsDoneTime;

        private void Awake()
        {
            instructionsDoneTime = string.IsNullOrEmpty(instructions) ? 0 : Time.time + instructionsDuration;
        }

        private void Update()
        {
            if (Input.GetButtonDown(menuInputButton)) ToggleMenu();
        }

        public void ToggleMenu()
        {
            m_isVisible = !m_isVisible;
            if (pauseWhileOpen) Time.timeScale = m_isVisible ? 0 : 1;
            if (m_isVisible) onShow.Invoke(); else onHide.Invoke();
        }

        void OnGUI()
        {
            // Draw instructions if within the timeframe to do so:
            if (Time.time < instructionsDoneTime)
            {
                GUILayout.Label(instructions);
            }

            // Draw menu if visible:
            if (!m_isVisible) return;
            const int ButtonWidth = 200;
            const int ButtonHeight = 30;
            GUILayout.BeginArea(new Rect((Screen.width - ButtonWidth) / 2, (Screen.height - 4 * ButtonHeight) / 2, ButtonWidth, 4 * (ButtonHeight + 10)));
            if (GUILayout.Button("Resume", GUILayout.Height(ButtonHeight)))
            {
                ToggleMenu();
            }
            if (GUILayout.Button("Save", GUILayout.Height(ButtonHeight)))
            {
                ToggleMenu();
                Debug.Log("Saving game to slot " + saveSlot);
                SaveSystem.SaveToSlot(saveSlot);
            }
            if (GUILayout.Button("Load", GUILayout.Height(ButtonHeight)))
            {
                ToggleMenu();
                Debug.Log("Loading game from slot " + saveSlot);
                SaveSystem.LoadFromSlot(saveSlot);
            }
            if (GUILayout.Button("Quit", GUILayout.Height(ButtonHeight)))
            {
                ToggleMenu();
                Debug.Log("Quitting");
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
            GUILayout.EndArea();
        }
    }
}
