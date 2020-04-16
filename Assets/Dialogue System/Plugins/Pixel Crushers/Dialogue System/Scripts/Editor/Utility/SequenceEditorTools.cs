// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This class provides a custom drawer for Sequence fields.
    /// </summary>
    public static class SequenceEditorTools
    {

        private enum MenuResult
        {
            Unselected, DefaultSequence, Delay, DefaultCameraAngle, UpdateTracker, RandomizeNextEntry, None, Continue, ContinueTrue, ContinueFalse, OtherCommand
        }

        private static MenuResult menuResult = MenuResult.Unselected;

        private enum AudioDragDropCommand { AudioWait, Audio, SALSA, LipSync }

        private static AudioDragDropCommand audioDragDropCommand = AudioDragDropCommand.AudioWait;

        private enum GameObjectDragDropCommand { Camera, DOF, SetActiveTrue, SetActiveFalse }

        private static GameObjectDragDropCommand gameObjectDragDropCommand = GameObjectDragDropCommand.Camera;

        private static GameObjectDragDropCommand alternateGameObjectDragDropCommand = GameObjectDragDropCommand.SetActiveTrue;

        private static string otherCommandName = string.Empty;

        [Serializable]
        private class DragDropCommands
        {
            public AudioDragDropCommand audioDragDropCommand;
            public GameObjectDragDropCommand gameObjectDragDropCommand;
            public GameObjectDragDropCommand alternateGameObjectDragDropCommand;
        }

        public static string SaveDragDropCommands()
        {
            var commands = new DragDropCommands();
            commands.audioDragDropCommand = audioDragDropCommand;
            commands.gameObjectDragDropCommand = gameObjectDragDropCommand;
            commands.alternateGameObjectDragDropCommand = alternateGameObjectDragDropCommand;
            return JsonUtility.ToJson(commands);
        }

        public static void RestoreDragDropCommands(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            var commands = JsonUtility.FromJson<DragDropCommands>(s);
            audioDragDropCommand = commands.audioDragDropCommand;
            gameObjectDragDropCommand = commands.gameObjectDragDropCommand;
            alternateGameObjectDragDropCommand = commands.alternateGameObjectDragDropCommand;
        }

        public static string DrawLayout(GUIContent guiContent, string sequence, ref Rect rect)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(guiContent);
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(26)))
            {
                DrawContextMenu(sequence);
            }
            EditorGUILayout.EndHorizontal();
            if (menuResult != MenuResult.Unselected)
            {
                sequence = ApplyMenuResult(menuResult, sequence);
                menuResult = MenuResult.Unselected;
            }

            EditorWindowTools.StartIndentedSection();
            var newSequence = EditorGUILayout.TextArea(sequence);
            if (!string.Equals(newSequence, sequence))
            {
                sequence = newSequence;
                GUI.changed = true;
            }

            switch (Event.current.type)
            {
                case EventType.Repaint:
                    rect = GUILayoutUtility.GetLastRect();
                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (Event.current.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            foreach (var obj in DragAndDrop.objectReferences)
                            {
                                if (obj is AudioClip)
                                {
                                    // Drop audio clip according to selected audio command:
                                    var clip = obj as AudioClip;
                                    var path = AssetDatabase.GetAssetPath(clip);
                                    if (path.Contains("Resources"))
                                    {
                                        sequence = AddCommandToSequence(sequence, GetCurrentAudioCommand() + "(" + GetResourceName(path) + ")");
                                        GUI.changed = true;
                                    }
                                    else
                                    {
                                        EditorUtility.DisplayDialog("Not in Resources Folder", "Audio clips must be located in the hierarchy of a Resources folder or an AssetBundle.", "OK");
                                    }
                                }
                                else if (obj is GameObject)
                                {
                                    // Drop GameObject.
                                    var go = obj as GameObject;
                                    if (sequence.EndsWith("("))
                                    {
                                        // If sequence ends in open paren, add GameObject and close:
                                        sequence += go.name + ")";
                                    }
                                    else
                                    {
                                        // Drop GameObject according to selected GameObject command:
                                        var command = Event.current.alt ? alternateGameObjectDragDropCommand : gameObjectDragDropCommand;
                                        sequence = AddCommandToSequence(sequence, GetCurrentGameObjectCommand(command, go.name));
                                    }
                                    GUI.changed = true;
                                }
                            }
                        }
                    }
                    break;
            }

            EditorWindowTools.EndIndentedSection();

            return sequence;
        }

        private static void DrawContextMenu(string sequence)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Help/Overview..."), false, OpenURL, "https://www.pixelcrushers.com/dialogue_system/manual2x/html/cutscene_sequences.html");
            menu.AddItem(new GUIContent("Help/Command Reference..."), false, OpenURL, "https://www.pixelcrushers.com/dialogue_system/manual2x/html/sequencer_command_reference.html");
            menu.AddSeparator("");
            menu.AddDisabledItem(new GUIContent("Shortcuts:"));
            menu.AddItem(new GUIContent("Include Dialogue Manager's Default Sequence"), false, SetMenuResult, MenuResult.DefaultSequence);
            menu.AddItem(new GUIContent("Delay for subtitle length"), false, SetMenuResult, MenuResult.Delay);
            menu.AddItem(new GUIContent("Cut to speaker's default camera angle"), false, SetMenuResult, MenuResult.DefaultCameraAngle);
            menu.AddItem(new GUIContent("Update quest tracker"), false, SetMenuResult, MenuResult.UpdateTracker);
            menu.AddItem(new GUIContent("Randomize next entry"), false, SetMenuResult, MenuResult.RandomizeNextEntry);
            menu.AddItem(new GUIContent("None (null command with zero duration)"), false, SetMenuResult, MenuResult.None);
            menu.AddItem(new GUIContent("Continue/Simulate continue button click"), false, SetMenuResult, MenuResult.Continue);
            menu.AddItem(new GUIContent("Continue/Enable continue button"), false, SetMenuResult, MenuResult.ContinueTrue);
            menu.AddItem(new GUIContent("Continue/Disable continue button"), false, SetMenuResult, MenuResult.ContinueFalse);
            menu.AddItem(new GUIContent("Audio Drag-n-Drop/Help..."), false, ShowSequenceEditorAudioHelp, null);
            menu.AddItem(new GUIContent("Audio Drag-n-Drop/Use AudioWait()"), audioDragDropCommand == AudioDragDropCommand.AudioWait, SetAudioDragDropCommand, AudioDragDropCommand.AudioWait);
            menu.AddItem(new GUIContent("Audio Drag-n-Drop/Use Audio()"), audioDragDropCommand == AudioDragDropCommand.Audio, SetAudioDragDropCommand, AudioDragDropCommand.Audio);
            menu.AddItem(new GUIContent("Audio Drag-n-Drop/Use SALSA() (3rd party)"), audioDragDropCommand == AudioDragDropCommand.SALSA, SetAudioDragDropCommand, AudioDragDropCommand.SALSA);
            menu.AddItem(new GUIContent("Audio Drag-n-Drop/Use LipSync() (3rd party)"), audioDragDropCommand == AudioDragDropCommand.LipSync, SetAudioDragDropCommand, AudioDragDropCommand.LipSync);
            menu.AddItem(new GUIContent("GameObject Drag-n-Drop/Help..."), false, ShowSequenceEditorGameObjectHelp, null);
            menu.AddItem(new GUIContent("GameObject Drag-n-Drop/Default/Use Camera()"), gameObjectDragDropCommand == GameObjectDragDropCommand.Camera, SetGameObjectDragDropCommand, GameObjectDragDropCommand.Camera);
            menu.AddItem(new GUIContent("GameObject Drag-n-Drop/Default/Use DOF()"), gameObjectDragDropCommand == GameObjectDragDropCommand.DOF, SetGameObjectDragDropCommand, GameObjectDragDropCommand.DOF);
            menu.AddItem(new GUIContent("GameObject Drag-n-Drop/Default/SetActive(GameObject,true)"), gameObjectDragDropCommand == GameObjectDragDropCommand.SetActiveTrue, SetGameObjectDragDropCommand, GameObjectDragDropCommand.SetActiveTrue);
            menu.AddItem(new GUIContent("GameObject Drag-n-Drop/Default/SetActive(GameObject,false)"), gameObjectDragDropCommand == GameObjectDragDropCommand.SetActiveFalse, SetGameObjectDragDropCommand, GameObjectDragDropCommand.SetActiveFalse);
            menu.AddItem(new GUIContent("GameObject Drag-n-Drop/Alt-Key/Use Camera()"), gameObjectDragDropCommand == GameObjectDragDropCommand.Camera, SetAlternateGameObjectDragDropCommand, GameObjectDragDropCommand.Camera);
            menu.AddItem(new GUIContent("GameObject Drag-n-Drop/Alt-Key/Use DOF()"), gameObjectDragDropCommand == GameObjectDragDropCommand.DOF, SetAlternateGameObjectDragDropCommand, GameObjectDragDropCommand.DOF);
            menu.AddItem(new GUIContent("GameObject Drag-n-Drop/Alt-Key/SetActive(GameObject,true)"), gameObjectDragDropCommand == GameObjectDragDropCommand.SetActiveTrue, SetAlternateGameObjectDragDropCommand, GameObjectDragDropCommand.SetActiveTrue);
            menu.AddItem(new GUIContent("GameObject Drag-n-Drop/Alt-Key/SetActive(GameObject,false)"), gameObjectDragDropCommand == GameObjectDragDropCommand.SetActiveFalse, SetAlternateGameObjectDragDropCommand, GameObjectDragDropCommand.SetActiveFalse);
            AddAllSequencerCommands(menu);
            menu.ShowAsContext();
        }

        private static void OpenURL(object url)
        {
            Application.OpenURL(url as string);
        }

        private static void ShowSequenceEditorAudioHelp(object data)
        {
            EditorUtility.DisplayDialog("Audio Drag & Drop Help", "Select an item in this Audio submenu to specify which command to add when dragging an audio clip onto the Sequence field. Audio clips must be in a Resources folder. Audio commands can use AssetBundles, but not with this drag-n-drop feature.", "OK");
        }

        private static void SetAudioDragDropCommand(object data)
        {
            audioDragDropCommand = (AudioDragDropCommand)data;
        }

        private static string GetCurrentAudioCommand()
        {
            switch (audioDragDropCommand)
            {
                case AudioDragDropCommand.Audio:
                    return "Audio";
                case AudioDragDropCommand.SALSA:
                    return "SALSA";
                case AudioDragDropCommand.LipSync:
                    return "LipSync";
                default:
                    return "AudioWait";
            }
        }

        private static void ShowSequenceEditorGameObjectHelp(object data)
        {
            EditorUtility.DisplayDialog("GameObject Drag & Drop Help", "Select an item in this GameObject submenu to specify which command to add when dragging a GameObject onto the Sequence field.", "OK");
        }

        private static void SetGameObjectDragDropCommand(object data)
        {
            gameObjectDragDropCommand = (GameObjectDragDropCommand)data;
        }

        private static void SetAlternateGameObjectDragDropCommand(object data)
        {
            alternateGameObjectDragDropCommand = (GameObjectDragDropCommand)data;
        }

        private static string GetCurrentGameObjectCommand(GameObjectDragDropCommand command, string goName)
        {
            if (string.IsNullOrEmpty(goName)) return string.Empty;
            switch (command)
            {
                default:
                case GameObjectDragDropCommand.Camera:
                    return "Camera(default," + goName + ")";
                case GameObjectDragDropCommand.DOF:
                    return "DOF(" + goName + ")";
                case GameObjectDragDropCommand.SetActiveTrue:
                    return "SetActive(" + goName + ",true)";
                case GameObjectDragDropCommand.SetActiveFalse:
                    return "SetActive(" + goName + ",false)";
            }
        }

        private static void SetMenuResult(object data)
        {
            menuResult = (MenuResult)data;
        }

        private static string ApplyMenuResult(MenuResult menuResult, string sequence)
        {
            GUI.changed = true;
            var newCommand = GetMenuResultCommand(menuResult);
            if (string.IsNullOrEmpty(newCommand))
            {
                return sequence;
            }
            else
            {
                return AddCommandToSequence(sequence, newCommand);
            }
        }

        private static string GetMenuResultCommand(MenuResult menuResult)
        {
            switch (menuResult)
            {
                case MenuResult.DefaultSequence:
                    return "{{default}}";
                case MenuResult.Delay:
                    return "Delay({{end}})";
                case MenuResult.DefaultCameraAngle:
                    return "Camera(default)";
                case MenuResult.UpdateTracker:
                    return "UpdateTracker()";
                case MenuResult.RandomizeNextEntry:
                    return "RandomizeNextEntry()";
                case MenuResult.None:
                    return "None()";
                case MenuResult.Continue:
                    return "Continue()";
                case MenuResult.ContinueTrue:
                    return "SetContinueMode(true)";
                case MenuResult.ContinueFalse:
                    return "SetContinueMode(false)";
                case MenuResult.OtherCommand:
                    return otherCommandName;
                default:
                    return string.Empty;
            }
        }

        private static string AddCommandToSequence(string sequence, string newCommand)
        {
            return sequence + (string.IsNullOrEmpty(sequence) ? string.Empty : ";\n") + newCommand;
        }

        private static string GetResourceName(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            var index = path.IndexOf("Resources/");
            if (index == -1) return string.Empty;
            var s = path.Substring(index + "Resources/".Length);
            index = s.LastIndexOf(".");
            if (index != -1) s = s.Substring(0, index);
            return s;
        }

        private static string[] InternalSequencerCommands =
        {
"None",
"AnimatorController",
"AnimatorBool",
"AnimatorInt",
"AnimatorFloat",
"AnimatorTrigger",
"Audio",
"SendMessage",
"SetActive",
"SetEnabled",
"SetPanel",
"SetPortrait",
"SetContinueMode",
"Continue",
"SetVariable",
"ShowAlert",
"UpdateTracker",
"RandomizeNextEntry",
        };

        private static void AddAllSequencerCommands(GenericMenu menu)
        {
            var list = new List<string>(InternalSequencerCommands);
            var assemblies = RuntimeTypeUtility.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                try
                {
                    foreach (var type in assembly.GetTypes().Where(t => typeof(PixelCrushers.DialogueSystem.SequencerCommands.SequencerCommand).IsAssignableFrom(t)))
                    {
                        var commandName = type.Name.Substring("SequencerCommand".Length);
                        list.Add(commandName);
                    }
                }
                catch (System.Exception) { }
            }
            list.Sort();
            for (int i = 0; i < list.Count; i++)
            {
                menu.AddItem(new GUIContent("All Sequencer Commands/" + list[i]), false, StartSequencerCommand, list[i]);
            }
        }

        private static void StartSequencerCommand(object data)
        {

            otherCommandName = (string)data + "(";
            SetMenuResult(MenuResult.OtherCommand);
        }

    }

}
