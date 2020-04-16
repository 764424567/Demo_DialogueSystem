// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This is the Dialogue System welcome window. It provides easy shortcuts to
    /// tools, documentation, and support.
    /// </summary>
    [InitializeOnLoad]
    public class WelcomeWindow : EditorWindow
    {

        private const string ShowOnStartEditorPrefsKey = "PixelCrushers.DialogueSystem.WelcomeWindow.ShowOnStart";

        private static bool showOnStartPrefs
        {
            get { return EditorPrefs.GetBool(ShowOnStartEditorPrefsKey, true); }
            set { EditorPrefs.SetBool(ShowOnStartEditorPrefsKey, value); }
        }

        [MenuItem("Tools/Pixel Crushers/Dialogue System/Welcome Window", false, -2)]
        public static WelcomeWindow ShowWindow()
        {
            var window = GetWindow<WelcomeWindow>(false, "Welcome");
            window.minSize = new Vector2(370, 280);
            window.showOnStart = true; // Can't check EditorPrefs when constructing window: showOnStartPrefs;
            return window;
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            RegisterWindowCheck();
        }

        private static void RegisterWindowCheck()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.update += CheckShowWelcomeWindow;
            }
        }

        private static void CheckShowWelcomeWindow()
        {
            EditorApplication.update -= CheckShowWelcomeWindow;
            if (showOnStartPrefs)
            {
                ShowWindow();
            }
        }

        public bool showOnStart = true;

        private static Texture2D icon = null;
        private static GUIStyle iconButtonStyle = null;

        private void OnGUI()
        {
            DrawBanner();
            DrawButtons();
            DrawFooter();
        }

        private void DrawBanner()
        {
            if (icon == null) icon = DialogueSystemControllerEditor.FindIcon();
            if (icon == null) return;
            if (iconButtonStyle == null)
            {
                iconButtonStyle = new GUIStyle(EditorStyles.label);
                iconButtonStyle.normal.background = icon;
                iconButtonStyle.active.background = icon;
            }
            GUI.DrawTexture(new Rect(5, 5, icon.width, icon.height), icon);
            var version = DialogueSystemMenuItems.GetVersion();
            if (!string.IsNullOrEmpty(version))
            {
                var versionSize = EditorStyles.label.CalcSize(new GUIContent(version));
                GUI.Label(new Rect(position.width - (versionSize.x + 5), 5, versionSize.x, versionSize.y), version);
            }
        }

        private const float ButtonWidth = 68;

        private void DrawButtons()
        {
            GUILayout.BeginArea(new Rect(5, 40, position.width - 10, position.height - 40));
            try
            {
                EditorWindowTools.DrawHorizontalLine();
                EditorGUILayout.HelpBox("Welcome to the Dialogue System for Unity!\n\nThe buttons below are shortcuts to commonly-used functions. You can find even more in Tools > Pixel Crushers > Dialogue System.", MessageType.None);
                EditorWindowTools.DrawHorizontalLine();
                GUILayout.Label("Help", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                try
                {
                    if (GUILayout.Button(new GUIContent("Quick\nStart\n", "Open Quick Start tutorial"), GUILayout.Width(ButtonWidth)))
                    {
                        Application.OpenURL("http://www.pixelcrushers.com/dialogue_system/manual2x/html/quick_start.html");
                    }
                    if (GUILayout.Button(new GUIContent("\nManual\n", "Open online manual"), GUILayout.Width(ButtonWidth)))
                    {
                        Application.OpenURL("http://www.pixelcrushers.com/dialogue_system/manual2x/html/");
                    }
                    if (GUILayout.Button(new GUIContent("\nVideos\n", "Open video tutorial list"), GUILayout.Width(ButtonWidth)))
                    {
                        Application.OpenURL("http://www.pixelcrushers.com/dialogue-system-tutorials/");
                    }
                    if (GUILayout.Button(new GUIContent("Scripting\nReference\n", "Open scripting & API reference"), GUILayout.Width(ButtonWidth)))
                    {
                        Application.OpenURL("http://www.pixelcrushers.com/dialogue_system/manual2x/html/scripting.html");
                    }
                    if (GUILayout.Button(new GUIContent("\nForum\n", "Go to the Pixel Crushers forum"), GUILayout.Width(ButtonWidth)))
                    {
                        Application.OpenURL("http://www.pixelcrushers.com/phpbb");
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }
                EditorWindowTools.DrawHorizontalLine();
                GUILayout.Label("Wizards & Resources", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                try
                {
                    if (GUILayout.Button(new GUIContent("Dialogue\nEditor\n", "Open the Dialogue Editor window"), GUILayout.Width(ButtonWidth)))
                    {
                        PixelCrushers.DialogueSystem.DialogueEditor.DialogueEditorWindow.OpenDialogueEditorWindow();
                    }
                    if (GUILayout.Button(new GUIContent("Dialogue\nManager\nWizard", "Configure a Dialogue Manager, the component that coordinates all Dialogue System activity"), GUILayout.Width(ButtonWidth)))
                    {
                        DialogueManagerWizard.Init();
                    }
                    if (GUILayout.Button(new GUIContent("Player\nSetup\nWizard", "Configure a player GameObject to work with the Dialogue System"), GUILayout.Width(ButtonWidth)))
                    {
                        PlayerSetupWizard.Init();
                    }
                    if (GUILayout.Button(new GUIContent("NPC\nSetup\nWizard", "Configure a non-player character or other interactive GameObject to work with the Dialogue System"), GUILayout.Width(ButtonWidth)))
                    {
                        NPCSetupWizard.Init();
                    }
                    if (GUILayout.Button(new GUIContent("Free\nExtras\n", "Go to the Dialogue System free extras website"), GUILayout.Width(ButtonWidth)))
                    {
                        Application.OpenURL("http://www.pixelcrushers.com/dialogue-system-extras/");
                    }
                }
                finally
                {
                    GUILayout.EndHorizontal();
                }
                EditorWindowTools.DrawHorizontalLine();
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private void DrawFooter()
        {
            var newShowOnStart = EditorGUI.ToggleLeft(new Rect(5, position.height - 5 - EditorGUIUtility.singleLineHeight, position.width - 90, EditorGUIUtility.singleLineHeight), "Show at start", showOnStart);
            if (newShowOnStart != showOnStart)
            {
                showOnStart = newShowOnStart;
                showOnStartPrefs = newShowOnStart;
            }
            if (GUI.Button(new Rect(position.width - 80, position.height - 5 - EditorGUIUtility.singleLineHeight, 70, EditorGUIUtility.singleLineHeight), new GUIContent("Support", "Contact the developer for support")))
            {
                Application.OpenURL("http://www.pixelcrushers.com/support-form/");
            }
        }

    }

}
