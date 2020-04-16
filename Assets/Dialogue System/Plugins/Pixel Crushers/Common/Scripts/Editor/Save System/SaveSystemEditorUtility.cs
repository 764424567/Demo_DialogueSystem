// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PixelCrushers
{

    /// <summary>
    /// 
    /// </summary>
    public static class SaveSystemEditorUtility
    {

        [MenuItem("Tools/Pixel Crushers/Common/Save System/Assign Unique Keys...", false, 0)]
        public static void AssignUniqueKeysDialog()
        {
            if (EditorUtility.DisplayDialog("Assign Unique Saver Keys", "Assign unique keys to all Saver components in the current scene whose Key fields are currently blank?", "OK", "Cancel"))
            {
                AssignUniqueKeysInScene();
            }
        }

        public static void AssignUniqueKeysInScene()
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var s = EditorSceneManager.GetSceneAt(i);
                if (s.isLoaded)
                {
                    var allGameObjects = s.GetRootGameObjects();
                    for (int j = 0; j < allGameObjects.Length; j++)
                    {
                        AssignUniqueKeysInTransformHierarchy(allGameObjects[j].transform);
                    }
                }
            }
        }

        private static void AssignUniqueKeysInTransformHierarchy(Transform t)
        {
            if (t == null) return;
            var savers = t.GetComponents<Saver>();
            for (int i = 0; i < savers.Length; i++)
            {
                var saver = savers[i];
                if (string.IsNullOrEmpty(saver._internalKeyValue))
                {
                    var key = saver.name + "_" + Mathf.Abs(saver.GetInstanceID());
                    Debug.Log(saver.name + "." + saver.GetType().Name + ".Key = " + key, saver);
                    Undo.RecordObject(saver, "Key");
                    saver._internalKeyValue = key;
                    saver.appendSaverTypeToKey = false;
                }
            }
            foreach (Transform child in t)
            {
                AssignUniqueKeysInTransformHierarchy(child);
            }
        }
    }
}