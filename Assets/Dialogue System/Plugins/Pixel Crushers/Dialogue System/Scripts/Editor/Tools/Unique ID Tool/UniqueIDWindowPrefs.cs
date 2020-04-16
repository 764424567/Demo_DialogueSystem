// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This class manages the Unique ID Window prefs. It allows the window to save
    /// prefs to EditorPrefs between sessions.
    /// </summary>
    [Serializable]
    public class UniqueIDWindowPrefs
    {

        private const string UniqueIDWindowPrefsKey = "PixelCrushers.DialogueSystem.UniqueIDTool";

        public List<DialogueDatabase> databases = new List<DialogueDatabase>();

        public UniqueIDWindowPrefs() { }

        /// <summary>
        /// Clears the prefs.
        /// </summary>
        public void Clear()
        {
            databases.Clear();
        }

        /// <summary>
        /// Deletes the prefs from EditorPrefs.
        /// </summary>
        public static void DeleteEditorPrefs()
        {
            EditorPrefs.DeleteKey(UniqueIDWindowPrefsKey);
        }

        /// <summary>
        /// Load the prefs from EditorPrefs.
        /// </summary>
        public static UniqueIDWindowPrefs Load()
        {
            return EditorPrefs.HasKey(UniqueIDWindowPrefsKey) ? JsonUtility.FromJson<UniqueIDWindowPrefs>(EditorPrefs.GetString(UniqueIDWindowPrefsKey))
                : new UniqueIDWindowPrefs();
        }

        /// <summary>
        /// Save the prefs to EditorPrefs.
        /// </summary>
        public void Save()
        {
            EditorPrefs.SetString(UniqueIDWindowPrefsKey, JsonUtility.ToJson(this));
        }

    }
}
