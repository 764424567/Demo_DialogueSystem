// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Tools for working with Unity UI.
    /// </summary>
    public static class UITools
    {

        /// <summary>
        /// Ensures that the scene has an EventSystem.
        /// </summary>
        public static void RequireEventSystem()
        {
            var eventSystem = GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning(DialogueDebug.Prefix + ": The scene is missing an EventSystem. Adding one.");
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                               typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }
        }

        public static int GetAnimatorNameHash(AnimatorStateInfo animatorStateInfo)
        {
			return animatorStateInfo.fullPathHash;
        }

        /// <summary>
        /// Dialogue databases use Texture2D for actor portraits. Unity UI uses sprites.
        /// UnityUIDialogueUI converts textures to sprites. This dictionary contains
        /// converted sprites so we don't need to reconvert them every single time we
        /// want to show an actor's portrait.
        /// </summary>
        public static Dictionary<Texture2D, Sprite> spriteCache = new Dictionary<Texture2D, Sprite>();

        public static void ClearSpriteCache()
        {
            spriteCache.Clear();
        }

        /// <summary>
        /// Gets the Sprite version of a Texture2D. Uses a cache so a texture will only be 
        /// converted to a sprite once.
        /// </summary>
        /// <param name="texture">Original Texture2D.</param>
        /// <returns>Sprite version.</returns>
        public static Sprite CreateSprite(Texture2D texture)
        {
            if (texture == null) return null;
            if (spriteCache.ContainsKey(texture)) return spriteCache[texture];
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            spriteCache.Add(texture, sprite);
            return sprite;
        }

        public static string GetUIFormattedText(FormattedText formattedText)
        {
            if (formattedText == null)
            {
                return string.Empty;
            }
            else if (formattedText.italic)
            {
                return "<i>" + formattedText.text + "</i>";
            }
            else
            {
                return formattedText.text;
            }
        }

        private static AbstractDialogueUI dialogueUI = null;

        /// <summary>
        /// Sends "OnTextChange(text)" to the dialogue UI GameObject.
        /// </summary>
        public static void SendTextChangeMessage(UnityEngine.UI.Text text)
        {
            if (text == null) return;
            if (dialogueUI == null) dialogueUI = text.GetComponentInParent<AbstractDialogueUI>();
            if (dialogueUI == null) return;
            dialogueUI.SendMessage(DialogueSystemMessages.OnTextChange, text, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Sends "OnTextChange(textField)" to the dialogue UI GameObject.
        /// </summary>
        public static void SendTextChangeMessage(UITextField textField)
        {
            if (textField.gameObject == null) return;
            textField.gameObject.SendMessage(DialogueSystemMessages.OnTextChange, textField, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Selects a Selectable UI element and visually shows it as selected.
        /// </summary>
        /// <param name="selectable"></param>
        /// <param name="allowStealFocus"></param>
        public static void Select(UnityEngine.UI.Selectable selectable, bool allowStealFocus = true)
        {
            var currentEventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (currentEventSystem == null || selectable == null) return;
            if (currentEventSystem.alreadySelecting) return;
            if (currentEventSystem.currentSelectedGameObject == null || allowStealFocus)
            {
                currentEventSystem.SetSelectedGameObject(selectable.gameObject);
                selectable.Select();
                selectable.OnSelect(null);
            }
        }

        public const string RPGMakerCodeQuarterPause = @"\,";
        public const string RPGMakerCodeFullPause = @"\.";
        public const string RPGMakerCodeSkipToEnd = @"\^";
        public const string RPGMakerCodeInstantOpen = @"\>";
        public const string RPGMakerCodeInstantClose = @"\<";

        /// <summary>
        /// Returns a string without any embedded RPG Maker codes.
        /// </summary>
        public static string StripRPGMakerCodes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Contains(@"\") ? s.Replace(RPGMakerCodeQuarterPause, string.Empty).
                Replace(RPGMakerCodeFullPause, string.Empty).
                Replace(RPGMakerCodeSkipToEnd, string.Empty).
                Replace(RPGMakerCodeInstantOpen, string.Empty).
                Replace(RPGMakerCodeInstantClose, string.Empty)
                : s;
        }

        /// <summary>
        /// Wraps a string in rich text color codes. Properly handles nested 
        /// rich text codes.
        /// </summary>
        /// <param name="text">Original text to be wrapped.</param>
        /// <param name="color">Color to wrap around text.</param>
        public static string WrapTextInColor(string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var colorCode = "<color=" + Tools.ToWebColor(color) + ">";

            // If text definitely has no other rich text codes, it's easy; just put a color code around the whole thing:
            if (!text.Contains("<")) return colorCode + text + "</color>";

            // Otherwise put color codes only around substrings that aren't already in existing rich text codes:
            var result = string.Empty;
            int index = 0;

            foreach (Match match in Regex.Matches(text, @"<i><color=^(?!.*</color>)</color></i>|<b><color=^(?!.*</color>)</color></b>|<i><b><color=^(?!.*</color>)</color></b></i>|<color=^(?!.*</color>)</color>"))
            {
                result += colorCode + text.Substring(index, match.Index) + "</color>" + match.Value;
                index = match.Index + match.Value.Length;
            }
            if (index < text.Length)
            {
                result += colorCode + text.Substring(index) + "</color>";
            }

            return result;
        }

        public static void EnableInteractivity(GameObject go)
        {
            var canvas = go.GetComponentInChildren<Canvas>() ?? go.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                if (canvas.worldCamera == null) canvas.worldCamera = Camera.main;
            }
            var graphicRaycaster = go.GetComponentInChildren<UnityEngine.UI.GraphicRaycaster>() ?? go.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>();
            if (graphicRaycaster != null) graphicRaycaster.enabled = true;
        }

    }

}
