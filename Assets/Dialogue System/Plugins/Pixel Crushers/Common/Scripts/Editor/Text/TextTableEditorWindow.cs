// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;

namespace PixelCrushers
{

    /// <summary>
    /// Custom editor window for TextTable.
    /// </summary>
    public class TextTableEditorWindow : EditorWindow
    {

        [MenuItem("Tools/Pixel Crushers/Common/Text Table Editor")]
        public static void ShowWindow()
        {
            GetWindow<TextTableEditorWindow>();
        }

        public static bool isOpen { get { return instance != null; } }

        public static TextTableEditorWindow instance { get { return s_instance; } }

        private static TextTableEditorWindow s_instance = null;

        private const string WindowTitle = "Text Table";

        private static GUIContent[] ToolbarLabels = new GUIContent[]
            { new GUIContent("Languages"), new GUIContent("Fields") };

        [SerializeField]
        private int m_textTableInstanceID;

        [SerializeField]
        private Vector2 m_languageListScrollPosition;

        [SerializeField]
        private Vector2 m_fieldListScrollPosition;

        [SerializeField]
        private int m_toolbarSelection = 0;

        [SerializeField]
        private int m_selectedLanguageIndex = 0;

        [SerializeField]
        private int m_selectedLanguageID = 0;

        [SerializeField]
        private string m_csvFilename = string.Empty;

        private TextTable m_textTable;

        private bool m_needRefreshLists = true;
        private ReorderableList m_languageList = null;
        private ReorderableList m_fieldList = null;
        private SerializedObject m_serializedObject = null;
        private GUIStyle textAreaStyle = null;

        private const string EncodingTypeEditorPrefsKey = "PixelCrushers.EncodingType";

        #region Editor Entrypoints

        private void OnEnable()
        {
            s_instance = this;
            titleContent.text = "Text Table";
            m_needRefreshLists = true;
            Undo.undoRedoPerformed += Repaint;
            if (m_textTableInstanceID != 0) Selection.activeObject = EditorUtility.InstanceIDToObject(m_textTableInstanceID);
            OnSelectionChange();
        }

        private void OnDisable()
        {
            s_instance = null;
            Undo.undoRedoPerformed -= Repaint;
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is TextTable)
            {
                SelectTextTable(Selection.activeObject as TextTable);
                Repaint();
            }
            else if (m_textTable == null && m_textTableInstanceID != 0)
            {
                SelectTextTable(EditorUtility.InstanceIDToObject(m_textTableInstanceID) as TextTable);
                Repaint();
            }
        }

        private void SelectTextTable(TextTable newTable)
        {
            m_textTable = newTable;
            ResetLanguagesTab();
            ResetFieldsTab();
            m_needRefreshLists = true;
            m_serializedObject = (newTable != null) ? new SerializedObject(newTable) : null;
            if (m_textTable != null && m_textTable.languages.Count == 0) m_textTable.AddLanguage("Default");
            m_textTableInstanceID = (newTable != null) ? newTable.GetInstanceID() : 0;
        }

        private void OnGUI()
        {
            DrawWindowContents();
            if (m_needRefreshLists) Repaint();
        }

        private void DrawWindowContents()
        {
            DrawTextTableField();
            if (m_textTable == null || m_serializedObject == null) return;
            m_serializedObject.Update();
            var newToolbarSelection = GUILayout.Toolbar(m_toolbarSelection, ToolbarLabels);
            if (newToolbarSelection != m_toolbarSelection)
            {
                m_toolbarSelection = newToolbarSelection;
                if (newToolbarSelection == 1) m_languageDropdownList = null;
            }
            if (m_toolbarSelection == 0)
            {
                DrawLanguagesTab();
            }
            else
            {
                DrawFieldsTab();
            }
            m_serializedObject.ApplyModifiedProperties();
        }

        private void DrawTextTableField()
        {
            EditorGUILayout.BeginHorizontal();
            var newTable = EditorGUILayout.ObjectField(m_textTable, typeof(TextTable), false) as TextTable;
            if (newTable != m_textTable) SelectTextTable(newTable);
            DrawGearMenu();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Language List

        private void ResetLanguagesTab()
        {
            m_languageList = null;
            m_languageListScrollPosition = Vector2.zero;
        }

        private void DrawLanguagesTab()
        {
            if (m_languageList == null)
            {
                m_languageList = new ReorderableList(m_serializedObject, m_serializedObject.FindProperty("m_languageKeys"), true, true, true, true);
                m_languageList.drawHeaderCallback = OnDrawLanguageListHeader;
                m_languageList.drawElementCallback = OnDrawLanguageListElement;
                m_languageList.onAddCallback = OnAddLanguageListElement;
                m_languageList.onCanRemoveCallback = OnCanRemoveLanguageListElement;
                m_languageList.onRemoveCallback = OnRemoveLanguageListElement;
                m_languageList.onSelectCallback = OnSelectLanguageListElement;
                m_languageList.onReorderCallback = OnReorderLanguageListElement;
            }
            m_languageListScrollPosition = GUILayout.BeginScrollView(m_languageListScrollPosition, false, false);
            try
            {
                m_languageList.DoLayoutList();
            }
            finally
            {
                GUILayout.EndScrollView();
            }
        }

        private void OnDrawLanguageListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Languages");
        }

        private void OnDrawLanguageListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var languageKeysProperty = m_serializedObject.FindProperty("m_languageKeys");
            var languageKeyProperty = languageKeysProperty.GetArrayElementAtIndex(index);
            var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
            var languageValueProperty = languageValuesProperty.GetArrayElementAtIndex(index);
            EditorGUI.BeginDisabledGroup(languageValueProperty.intValue == 0);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y + 1, rect.width, EditorGUIUtility.singleLineHeight), languageKeyProperty, GUIContent.none, false);
            EditorGUI.EndDisabledGroup();
        }

        private void OnAddLanguageListElement(ReorderableList list)
        {
            m_serializedObject.ApplyModifiedProperties();
            m_textTable.AddLanguage("Language " + m_textTable.nextLanguageID);
            m_serializedObject.Update();
            ResetFieldsTab();
        }

        private bool OnCanRemoveLanguageListElement(ReorderableList list)
        {
            var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
            var languageValueProperty = languageValuesProperty.GetArrayElementAtIndex(list.index);
            return languageValueProperty.intValue > 0;
        }

        private void OnRemoveLanguageListElement(ReorderableList list)
        {
            var languageKeysProperty = m_serializedObject.FindProperty("m_languageKeys");
            var languageKeyProperty = languageKeysProperty.GetArrayElementAtIndex(list.index);
            var languageName = languageKeyProperty.stringValue;
            var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
            var languageValueProperty = languageValuesProperty.GetArrayElementAtIndex(list.index);
            var languageID = languageValueProperty.intValue;
            if (!EditorUtility.DisplayDialog("Delete " + languageName, "Are you sure you want to delete the language '" + languageName +
                "' and all field values associated with it?", "OK", "Cancel")) return;
            m_serializedObject.ApplyModifiedProperties();
            m_textTable.RemoveLanguage(languageID);
            m_serializedObject.Update();
            ResetFieldsTab();
        }

        private int m_selectedLanguageListIndex = -1;

        private void OnSelectLanguageListElement(ReorderableList list)
        {
            m_selectedLanguageListIndex = list.index;
        }

        private void OnReorderLanguageListElement(ReorderableList list)
        {
            //Also reorder values:
            var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
            var value = languageValuesProperty.GetArrayElementAtIndex(m_selectedLanguageListIndex).intValue;
            languageValuesProperty.DeleteArrayElementAtIndex(m_selectedLanguageListIndex);
            languageValuesProperty.InsertArrayElementAtIndex(list.index);
            languageValuesProperty.GetArrayElementAtIndex(list.index).intValue = value;
            ResetFieldsTab();
        }

        #endregion

        #region Field List

        private void ResetFieldsTab()
        {
            m_fieldList = null;
            m_fieldListScrollPosition = Vector2.zero;
            m_selectedLanguageIndex = 0;
            m_selectedLanguageID = 0;
        }

        private void DrawFieldsTab()
        {
            DrawGrid();
            DrawEntryBox();
        }

        private const float MinColumnWidth = 100;

        private string[] m_languageDropdownList = null;

        private void DrawGrid()
        {
            if (m_textTable == null) return;
            try
            {
                var entryBoxHeight = IsAnyFieldSelected() ? (6 * EditorGUIUtility.singleLineHeight) : 0;
                GUILayout.BeginArea(new Rect(0, 2 * (EditorGUIUtility.singleLineHeight + 4), position.width,
                    position.height - (2 * (EditorGUIUtility.singleLineHeight + 4) + 4) - entryBoxHeight));
                m_fieldListScrollPosition = GUILayout.BeginScrollView(m_fieldListScrollPosition, false, false);

                if (m_needRefreshLists || m_fieldList == null || m_languageDropdownList == null)
                {
                    m_needRefreshLists = false;
                    m_fieldList = new ReorderableList(m_serializedObject, m_serializedObject.FindProperty("m_fieldValues"), true, true, true, true);
                    m_fieldList.drawHeaderCallback = OnDrawFieldListHeader;
                    m_fieldList.drawElementCallback = OnDrawFieldListElement;
                    m_fieldList.onAddCallback = OnAddFieldListElement;
                    m_fieldList.onRemoveCallback = OnRemoveFieldListElement;
                    m_fieldList.onSelectCallback = OnSelectFieldListElement;
                    m_fieldList.onReorderCallback = OnReorderFieldListElement;

                    var languages = new List<string>();
                    var languageKeysProperty = m_serializedObject.FindProperty("m_languageKeys");
                    for (int i = 0; i < languageKeysProperty.arraySize; i++)
                    {
                        languages.Add(languageKeysProperty.GetArrayElementAtIndex(i).stringValue);
                    }
                    m_languageDropdownList = languages.ToArray();
                }

                m_fieldList.DoLayoutList();
            }
            finally
            {
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private void OnDrawFieldListHeader(Rect rect)
        {
            var headerWidth = rect.width - 14;
            var columnWidth = headerWidth / 2;
            EditorGUI.LabelField(new Rect(rect.x + 14, rect.y, columnWidth, rect.height), "Field");
            var popupRect = new Rect(rect.x + rect.width - columnWidth, rect.y, columnWidth, rect.height);
            var newIndex = EditorGUI.Popup(popupRect, m_selectedLanguageIndex, m_languageDropdownList);
            if (newIndex != m_selectedLanguageIndex)
            {
                m_selectedLanguageIndex = newIndex;
                var languageValuesProperty = m_serializedObject.FindProperty("m_languageValues");
                var languageValueProperty = languageValuesProperty.GetArrayElementAtIndex(newIndex);
                m_selectedLanguageID = languageValueProperty.intValue;
            }
        }

        private void OnDrawFieldListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var columnWidth = (rect.width / 2) - 1;

            var nameControl = "Field" + index;
            var valueControl = "Value" + index;


            var fieldValuesProperty = m_serializedObject.FindProperty("m_fieldValues");
            var fieldValueProperty = fieldValuesProperty.GetArrayElementAtIndex(index);
            var fieldNameProperty = fieldValueProperty.FindPropertyRelative("m_fieldName");
            GUI.SetNextControlName(nameControl);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y + 1, columnWidth, EditorGUIUtility.singleLineHeight), fieldNameProperty, GUIContent.none, false);
            var keysProperty = fieldValueProperty.FindPropertyRelative("m_keys");
            var valuesProperty = fieldValueProperty.FindPropertyRelative("m_values");
            var valueIndex = -1;
            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                if (keysProperty.GetArrayElementAtIndex(i).intValue == m_selectedLanguageID)
                {
                    valueIndex = i;
                    break;
                }
            }
            if (valueIndex == -1)
            {
                valueIndex = keysProperty.arraySize;
                keysProperty.arraySize++;
                keysProperty.GetArrayElementAtIndex(valueIndex).intValue = m_selectedLanguageID;
                valuesProperty.arraySize++;
                valuesProperty.GetArrayElementAtIndex(valueIndex).stringValue = string.Empty;
            }
            var valueProperty = valuesProperty.GetArrayElementAtIndex(valueIndex);
            GUI.SetNextControlName(valueControl);
            EditorGUI.PropertyField(new Rect(rect.x + rect.width - columnWidth, rect.y + 1, columnWidth, EditorGUIUtility.singleLineHeight), valueProperty, GUIContent.none, false);
            var focusedControl = GUI.GetNameOfFocusedControl();
            if (string.Equals(nameControl, focusedControl) || string.Equals(valueControl, focusedControl))
            {
                m_selectedFieldListElement = index;
                m_fieldList.index = index;
            }
        }

        private void OnAddFieldListElement(ReorderableList list)
        {
            m_serializedObject.ApplyModifiedProperties();
            m_textTable.AddField("Field " + m_textTable.nextFieldID);
            m_serializedObject.Update();
        }

        private void OnRemoveFieldListElement(ReorderableList list)
        {
            var fieldKeysProperty = m_serializedObject.FindProperty("m_fieldKeys");
            var fieldKeyProperty = fieldKeysProperty.GetArrayElementAtIndex(list.index);
            var fieldID = fieldKeyProperty.intValue;
            var fieldValuesProperty = m_serializedObject.FindProperty("m_fieldValues");
            var fieldValueProperty = fieldValuesProperty.GetArrayElementAtIndex(list.index);
            var fieldNameProperty = fieldValueProperty.FindPropertyRelative("m_fieldName");
            var fieldName = fieldNameProperty.stringValue;
            if (!EditorUtility.DisplayDialog("Delete Field", "Are you sure you want to delete the field '" + fieldName +
                "' and all values associated with it?", "OK", "Cancel")) return;
            m_serializedObject.ApplyModifiedProperties();
            m_textTable.RemoveField(fieldID);
            m_serializedObject.Update();
        }

        private int m_selectedFieldListElement;

        private void OnSelectFieldListElement(ReorderableList list)
        {
            m_selectedFieldListElement = list.index;
        }

        private void OnReorderFieldListElement(ReorderableList list)
        {
            // Also reorder keys:
            var fieldKeysProperty = m_serializedObject.FindProperty("m_fieldKeys");
            var value = fieldKeysProperty.GetArrayElementAtIndex(m_selectedFieldListElement).intValue;
            fieldKeysProperty.DeleteArrayElementAtIndex(m_selectedFieldListElement);
            fieldKeysProperty.InsertArrayElementAtIndex(list.index);
            fieldKeysProperty.GetArrayElementAtIndex(list.index).intValue = value;
        }

        private bool IsAnyFieldSelected()
        {
            return m_fieldList != null && 0 <= m_fieldList.index && m_fieldList.index < m_fieldList.serializedProperty.arraySize;
        }

        private void DrawEntryBox()
        {
            if (m_needRefreshLists || !IsAnyFieldSelected()) return;
            var rect = new Rect(2, position.height - 6 * EditorGUIUtility.singleLineHeight, position.width - 4, 6 * EditorGUIUtility.singleLineHeight);
            var fieldValuesProperty = m_serializedObject.FindProperty("m_fieldValues");
            var fieldValueProperty = fieldValuesProperty.GetArrayElementAtIndex(m_fieldList.index);
            var keysProperty = fieldValueProperty.FindPropertyRelative("m_keys");
            var valuesProperty = fieldValueProperty.FindPropertyRelative("m_values");
            var valueIndex = -1;
            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                if (keysProperty.GetArrayElementAtIndex(i).intValue == m_selectedLanguageID)
                {
                    valueIndex = i;
                    break;
                }
            }
            if (valueIndex == -1)
            {
                valueIndex = keysProperty.arraySize;
                keysProperty.arraySize++;
                keysProperty.GetArrayElementAtIndex(valueIndex).intValue = m_selectedLanguageID;
                valuesProperty.arraySize++;
                valuesProperty.GetArrayElementAtIndex(valueIndex).stringValue = string.Empty;
            }
            if (textAreaStyle == null)
            {
                textAreaStyle = new GUIStyle(EditorStyles.textField);
                textAreaStyle.wordWrap = true;
            }
            var valueProperty = valuesProperty.GetArrayElementAtIndex(valueIndex);
            valueProperty.stringValue = EditorGUI.TextArea(rect, valueProperty.stringValue, textAreaStyle);
        }

        #endregion

        #region Gear Menu

        private void DrawGearMenu()
        {
            if (MoreEditorGuiUtility.DoLayoutGearMenu())
            {
                var menu = new GenericMenu();
                if (m_textTable == null)
                {
                    //menu.AddDisabledItem(new GUIContent("Sort"));
                    menu.AddDisabledItem(new GUIContent("Export/CSV..."));
                    menu.AddDisabledItem(new GUIContent("Import/CSV..."));
                }
                else
                {
                    //menu.AddItem(new GUIContent("Sort"), false, Sort);
                    menu.AddItem(new GUIContent("Export/CSV..."), false, ExportCSVDialogs);
                    menu.AddItem(new GUIContent("Import/CSV..."), false, ImportCSVDialogs);
                }
                menu.AddItem(new GUIContent("Encoding/UTF8"), GetEncodingType() == EncodingType.UTF8, SetEncodingType, EncodingType.UTF8);
                menu.AddItem(new GUIContent("Encoding/Unicode"), GetEncodingType() == EncodingType.Unicode, SetEncodingType, EncodingType.Unicode);
                menu.AddItem(new GUIContent("Encoding/ISO-8859-1"), GetEncodingType() == EncodingType.ISO_8859_1, SetEncodingType, EncodingType.ISO_8859_1);
                menu.ShowAsContext();
            }
        }

        private void ExportCSVDialogs()
        {
            string newFilename = EditorUtility.SaveFilePanel("Export to CSV", GetPath(m_csvFilename), m_csvFilename, "csv");
            if (string.IsNullOrEmpty(newFilename)) return;
            m_csvFilename = newFilename;
            if (Application.platform == RuntimePlatform.WindowsEditor) m_csvFilename = m_csvFilename.Replace("/", "\\");
            switch (EditorUtility.DisplayDialogComplex("Export CSV", "Export languages as columns in one file or as separate files?", "One", "Cancel", "Separate"))
            {
                case 0:
                    ExportCSV(m_csvFilename, false);
                    break;
                case 2:
                    ExportCSV(m_csvFilename, true);
                    break;
                default:
                    return;
            }
            EditorUtility.DisplayDialog("Export Complete", "The text table was exported to CSV (comma-separated values) format. ", "OK");
        }

        private void ImportCSVDialogs()
        {
            if (!EditorUtility.DisplayDialog("Import CSV?", "Importing from CSV will overwrite the current contents. Are you sure?", "Import", "Cancel")) return;
            string newFilename = EditorUtility.OpenFilePanel("Import from CSV", GetPath(m_csvFilename), "csv");
            if (string.IsNullOrEmpty(newFilename)) return;
            if (!File.Exists(newFilename))
            {
                EditorUtility.DisplayDialog("Import CSV", "Can't find the file " + newFilename + ".", "OK");
                return;
            }
            m_csvFilename = newFilename;
            if (Application.platform == RuntimePlatform.WindowsEditor) m_csvFilename = m_csvFilename.Replace("/", "\\");
            ImportCSV(m_csvFilename);
            OnSelectionChange();
            Repaint();
        }

        private string GetPath(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return string.Empty;
            try
            {
                return Path.GetDirectoryName(filename);
            }
            catch (System.ArgumentException)
            {
                return string.Empty;
            }
        }

        private EncodingType GetEncodingType()
        {
            return (EncodingType)EditorPrefs.GetInt(EncodingTypeEditorPrefsKey, (int)EncodingType.UTF8);
        }

        private void SetEncodingType(object data)
        {
            EditorPrefs.SetInt(EncodingTypeEditorPrefsKey, (int)((EncodingType)data));
        }

        private void ExportCSV(string csvFilename, bool separateFiles)
        {
            if (separateFiles)
            {
                foreach (var languageKvp in m_textTable.languages)
                {
                    var language = languageKvp.Key;
                    var languageID = languageKvp.Value;
                    var content = new List<List<string>>();
                    var row = new List<string>();
                    row.Add("Language");
                    row.Add(language);
                    content.Add(row);
                    foreach (var fieldKvp in m_textTable.fields)
                    {
                        var field = fieldKvp.Value;
                        row = new List<string>();
                        row.Add(field.fieldName);
                        row.Add(field.GetTextForLanguage(languageID));
                        content.Add(row);
                    }
                    var languageFilename = csvFilename.Substring(0, csvFilename.Length - 4) + "_" + language + ".csv";
                    CSVUtility.WriteCSVFile(content, languageFilename, GetEncodingType());
                }
            }
            else
            {
                // All in one file:
                var content = new List<List<string>>();
                var languageIDs = new List<int>();

                // Heading rows:
                var row = new List<string>();
                content.Add(row);
                row.Add("Field");
                foreach (var kvp in m_textTable.languages)
                {
                    var language = kvp.Key;
                    var languageID = kvp.Value;
                    languageIDs.Add(languageID);
                    row.Add(language);
                }

                // One row per field:
                foreach (var kvp in m_textTable.fields)
                {
                    var field = kvp.Value;
                    row = new List<string>();
                    content.Add(row);
                    row.Add(field.fieldName);
                    for (int i = 0; i < languageIDs.Count; i++)
                    {
                        var languageID = languageIDs[i];
                        var value = field.GetTextForLanguage(languageID);
                        row.Add(value);
                    }
                }
                CSVUtility.WriteCSVFile(content, csvFilename, GetEncodingType());
            }
        }

        private void ImportCSV(string csvFilename)
        {
            var content = CSVUtility.ReadCSVFile(csvFilename, GetEncodingType());
            if (content == null || content.Count < 1 || content[0].Count < 2) return;
            var firstCell = content[0][0];
            if (string.Equals(firstCell, "Language"))
            {
                // Single language file:
                var language = content[0][1];
                if (!m_textTable.HasLanguage(language)) m_textTable.AddLanguage(language);
                for (int y = 1; y < content.Count; y++)
                {
                    var field = content[y][0];
                    if (!m_textTable.HasField(field)) m_textTable.AddField(field);
                    for (int x = 1; x < content[y].Count; x++)
                    {
                        m_textTable.SetFieldTextForLanguage(field, language, content[y][x]);
                    }
                }
            }
            else
            {
                // All-in-one file:
                for (int x = 1; x < content[0].Count; x++)
                {
                    var language = content[0][x];
                    if (!m_textTable.HasLanguage(language)) m_textTable.AddLanguage(language);
                    for (int y = 1; y < content.Count; y++)
                    {
                        var field = content[y][0];
                        if (!m_textTable.HasField(field)) m_textTable.AddField(field);
                        m_textTable.SetFieldTextForLanguage(field, language, content[y][x]);
                    }
                }
            }
            EditorUtility.SetDirty(m_textTable);
        }

        private void Sort()
        {
            //[TODO] Sort text table.
        }

        #endregion

    }
}
