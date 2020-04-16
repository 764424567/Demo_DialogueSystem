// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;
using UnityEditor.Graphs;

namespace PixelCrushers.DialogueSystem
{

    public static class EditorTools
    {

        public static DialogueDatabase selectedDatabase = null;

        public static GUIStyle textAreaGuiStyle
        {
            get
            {
                if (m_textAreaGuiStyle == null)
                {
                    m_textAreaGuiStyle = new GUIStyle(EditorStyles.textArea);
                    m_textAreaGuiStyle.fixedHeight = 0;
                    m_textAreaGuiStyle.stretchHeight = true;
                    m_textAreaGuiStyle.wordWrap = true;
                }
                return m_textAreaGuiStyle;
            }
        }

        private static GUIStyle m_textAreaGuiStyle = null;

        public static DialogueDatabase FindInitialDatabase()
        {
            var dialogueSystemController = Object.FindObjectOfType<DialogueSystemController>();
            return (dialogueSystemController == null) ? null : dialogueSystemController.initialDatabase;
        }

        public static void SetInitialDatabaseIfNull()
        {
            if (selectedDatabase == null)
            {
                selectedDatabase = FindInitialDatabase();
            }
        }

        public static void DrawReferenceDatabase()
        {
            selectedDatabase = EditorGUILayout.ObjectField(new GUIContent("Reference Database", "Database to use for pop-up menus. Assumes this database will be in memory at runtime."), selectedDatabase, typeof(DialogueDatabase), true) as DialogueDatabase;
        }

        public static void DrawReferenceDatabase(Rect rect)
        {
            selectedDatabase = EditorGUI.ObjectField(rect, new GUIContent("Reference Database", "Database to use for pop-up menus. Assumes this database will be in memory at runtime."), selectedDatabase, typeof(DialogueDatabase), true) as DialogueDatabase;
        }

        public static void DrawSerializedProperty(SerializedObject serializedObject, string propertyName)
        {
            serializedObject.Update();
            var property = serializedObject.FindProperty(propertyName);
            if (property == null) return;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        public static Color NodeColorStringToColor(string s)
        {
            switch (s)
            {
                case "Aqua":
                    return Color.cyan;
                case "Blue":
                    return NodeColor_Blue;
                case "Gray":
                    return NodeColor_Gray;
                case "Green":
                    return NodeColor_Green;
                case "Grey":
                    return Color.gray;
                case "Orange":
                    return NodeColor_Orange;
                case "Red":
                    return NodeColor_Red;
                case "Yellow":
                    return Color.yellow;
                default:
                    return Tools.WebColor(s);
            }
        }

        //---No longer used, now that we allow a full color palette:
        //public static string[] StylesColorStrings = new string[]
        //{
        //    "Aqua", "Blue", "Gray", "Green", "Orange", "Red", "Yellow"
        //};

        // Node style colors:
        public static Color NodeColor_Orange_Dark = new Color(0.875f, 0.475f, 0);
        public static Color NodeColor_Gray_Dark = new Color(0.33f, 0.33f, 0.33f);
        public static Color NodeColor_Blue_Dark = new Color(0.22f, 0.38f, 0.64f);
        public static Color NodeColor_Green_Dark = new Color(0, 0.6f, 0);
        public static Color NodeColor_Red_Dark = new Color(0.7f, 0.1f, 0.1f);

        public static Color NodeColor_Orange_Light = new Color(1f, 0.7f, 0.4f);
        public static Color NodeColor_Gray_Light = new Color(0.7f, 0.7f, 0.7f);
        public static Color NodeColor_Blue_Light = new Color(0.375f, 0.64f, 0.95f);
        public static Color NodeColor_Green_Light = new Color(0, 0.85f, 0);
        public static Color NodeColor_Red_Light = new Color(0.7f, 0.1f, 0.1f);

        public static Color NodeColor_Orange { get { return EditorGUIUtility.isProSkin ? NodeColor_Orange_Dark : NodeColor_Orange_Light; } }
        public static Color NodeColor_Gray { get { return EditorGUIUtility.isProSkin ? NodeColor_Gray_Dark : NodeColor_Gray_Light; } }
        public static Color NodeColor_Blue { get { return EditorGUIUtility.isProSkin ? NodeColor_Blue_Dark : NodeColor_Blue_Light; } }
        public static Color NodeColor_Green { get { return EditorGUIUtility.isProSkin ? NodeColor_Green_Dark : NodeColor_Green_Light; } }
        public static Color NodeColor_Red { get { return EditorGUIUtility.isProSkin ? NodeColor_Red_Dark : NodeColor_Red_Light; } }

        public static void SetDirtyBeforeChange(UnityEngine.Object obj, string name)
        {
            Undo.RecordObject(obj, name);
        }

        public static void SetDirtyAfterChange(UnityEngine.Object obj)
        {
            EditorUtility.SetDirty(obj);
        }

        public static void TryAddScriptingDefineSymbols(string newDefine)
        {
            MoreEditorUtility.TryAddScriptingDefineSymbols(newDefine);
        }

    }

}
