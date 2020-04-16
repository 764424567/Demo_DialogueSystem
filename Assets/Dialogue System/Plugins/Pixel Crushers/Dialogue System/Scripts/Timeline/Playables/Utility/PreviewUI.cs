#if UNITY_2017_1_OR_NEWER
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This MonoBehaviour is used internally by the Dialogue System's
    /// playables to show an editor representation of activity that can
    /// only be accurately viewed at runtime.
    /// </summary>
    [AddComponentMenu("")] // No menu item. Only used internally.
    [ExecuteInEditMode]
    public class PreviewUI : MonoBehaviour
    {

        private string message;
        private float endTime;
        private int lineOffset;
        private bool computedRect;
        private Rect rect;

        public static void ShowMessage(string message, float duration, int lineOffset)
        {
            var go = new GameObject("Editor Preview UI: " + message);
            go.tag = "EditorOnly";
            go.hideFlags = HideFlags.DontSave;
            var previewUI = go.AddComponent<PreviewUI>();
            previewUI.Show(message, duration, lineOffset);
        }

        protected void Show(string message, float duration, int lineOffset)
        {
            this.message = message;
            this.lineOffset = lineOffset;
            endTime = Time.realtimeSinceStartup + (Mathf.Approximately(0, duration) ? 2 : duration);
            computedRect = false;
        }

        private void OnGUI()
        {
            if (!computedRect)
            {
                computedRect = true;
                var size = GUI.skin.label.CalcSize(new GUIContent(message));
                rect = new Rect((Screen.width - size.x) / 2, (Screen.height - size.y) / 2 + lineOffset * size.y, size.x, size.y);
            }
            GUI.Label(rect, message);
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else if (Time.realtimeSinceStartup >= endTime)
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
#endif
