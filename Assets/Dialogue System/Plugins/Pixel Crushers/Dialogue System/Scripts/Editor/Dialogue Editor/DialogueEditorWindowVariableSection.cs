// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System;

namespace PixelCrushers.DialogueSystem.DialogueEditor
{

    /// <summary>
    /// This part of the Dialogue Editor window handles the Variables tab. Variables are
    /// just treated as basic assets, so it uses the generic asset methods.
    /// </summary>
    public partial class DialogueEditorWindow
    {

        [SerializeField]
        private bool useReorderableListForVariables = true;

        [SerializeField]
        private AssetFoldouts variableFoldouts = new AssetFoldouts();

        [SerializeField]
        private string variableFilter = string.Empty;

        private ReorderableList variableReorderableList = null;

        private double lastTimeVariableNamesChecked = 0;
        private const double VariableNameCheckFrequency = 0.5f;
        private HashSet<string> conflictedVariableNames = new HashSet<string>();

        // We filter slightly different for variables since there may be a large number. 
        // Instead of graying out elements, we hide them entirely by showing only
        // the filtered variables.
        private List<Variable> m_filteredVariableList = null;
        private List<Variable> filteredVariableList
        {
            get
            {
                if (m_filteredVariableList == null) m_filteredVariableList = GenerateFilteredVariableList();
                return m_filteredVariableList;
            }
        }

        private void ResetVariableSection()
        {
            variableReorderableList = null;
        }

        private void ResetFilteredVariableList()
        {
            m_filteredVariableList = null;
        }

        private List<Variable> GenerateFilteredVariableList()
        {
            var list = new List<Variable>();
            if (database == null || string.IsNullOrEmpty(variableFilter)) return list;
            for (int i = 0; i < database.variables.Count; i++)
            {
                var variable = database.variables[i];
                if (IsAssetInFilter(variable, variableFilter))
                {
                    list.Add(variable);
                }
            }
            return list;
        }

        private void DrawVariableSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Variables", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            variableFilter = EditorGUILayout.TextField(GUIContent.none, variableFilter, "ToolbarSeachTextField");
            GUILayout.Label(string.Empty, "ToolbarSeachCancelButtonEmpty");
            if (EditorGUI.EndChangeCheck())
            {
                ResetFilteredVariableList();
                ResetVariableSection();
            }

            DrawVariableMenu();
            EditorGUILayout.EndHorizontal();
            if (database.syncInfo.syncVariables) DrawVariableSyncDatabase();
            DrawVariables();
        }

        private void DrawVariableMenu()
        {
            if (GUILayout.Button("Menu", "MiniPullDown", GUILayout.Width(56)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("New Variable"), false, AddNewVariable);
                menu.AddItem(new GUIContent("Sort/By Name"), false, SortVariablesByName);
                menu.AddItem(new GUIContent("Sort/By ID"), false, SortVariablesByID);
                menu.AddItem(new GUIContent("Sync From DB"), database.syncInfo.syncVariables, ToggleSyncVariablesFromDB);
                menu.AddItem(new GUIContent("Use Reorderable List"), useReorderableListForVariables, ToggleUseReorderableListForVariables);
                menu.ShowAsContext();
            }
        }

        private void AddNewVariable()
        {
            CreateNewVariable();
        }

        private void SortVariablesByName()
        {
            database.variables.Sort((x, y) => x.Name.CompareTo(y.Name));
            variableReorderableList = null;
            SetDatabaseDirty("Sort Variables by Name");
        }

        private void SortVariablesByID()
        {
            database.variables.Sort((x, y) => x.id.CompareTo(y.id));
            variableReorderableList = null;
            SetDatabaseDirty("Sort Variables by ID");
        }

        private void ToggleSyncVariablesFromDB()
        {
            database.syncInfo.syncVariables = !database.syncInfo.syncVariables;
            SetDatabaseDirty("Toggle Sync Variables");
        }

        private void ToggleUseReorderableListForVariables()
        {
            useReorderableListForVariables = !useReorderableListForVariables;
        }

        private void DrawVariableSyncDatabase()
        {
            EditorGUILayout.BeginHorizontal();
            DialogueDatabase newDatabase = EditorGUILayout.ObjectField(new GUIContent("Sync From", "Database to sync variables from."),
                                                                       database.syncInfo.syncVariablesDatabase, typeof(DialogueDatabase), false) as DialogueDatabase;
            if (newDatabase != database.syncInfo.syncVariablesDatabase)
            {
                database.syncInfo.syncVariablesDatabase = newDatabase;
                database.SyncVariables();
                SetDatabaseDirty("Change Sync Variables Database");
            }
            if (GUILayout.Button(new GUIContent("Sync Now", "Syncs from the database."), EditorStyles.miniButton, GUILayout.Width(72)))
            {
                database.SyncVariables();
                variableReorderableList = null;
                SetDatabaseDirty("Manual Sync Variables");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVariables()
        {
            if (EditorApplication.timeSinceStartup - lastTimeVariableNamesChecked >= VariableNameCheckFrequency)
            {
                lastTimeVariableNamesChecked = EditorApplication.timeSinceStartup;
                CheckVariableNamesForConflicts();
            }
            if (useReorderableListForVariables)
            {
                DrawVariablesReorderableList();
            }
            else
            {
                DrawVariablesTraditional();
            }
        }

        private void CheckVariableNamesForConflicts()
        {
            if (database == null) return;
            conflictedVariableNames.Clear();
            var variableNames = new HashSet<string>();
            for (int i = 0; i < database.variables.Count; i++)
            {
                var variableName = database.variables[i].Name;
                if (variableNames.Contains(variableName)) conflictedVariableNames.Add(variableName);
                variableNames.Add(variableName);
            }
        }

        private void DrawVariablesReorderableList()
        {
            if (variableReorderableList == null)
            {
                var useFilter = !string.IsNullOrEmpty(variableFilter);
                var listSource = useFilter ? filteredVariableList : database.variables;
                variableReorderableList = new ReorderableList(listSource, typeof(Variable), !useFilter, false, true, true);
                variableReorderableList.drawHeaderCallback = OnDrawVariableHeader;
                variableReorderableList.drawElementCallback = OnDrawVariableElement;
                variableReorderableList.onAddDropdownCallback = OnAddVariableDropdown;
                variableReorderableList.onRemoveCallback = OnRemoveVariable;
            }
            EditorWindowTools.StartIndentedSection();
            variableReorderableList.DoLayoutList();
            EditorWindowTools.EndIndentedSection();
        }

        private void OnDrawVariableHeader(Rect rect)
        {
            var handleWidth = 16f;
            var wholeWidth = rect.width - 6f - handleWidth;
            var typeWidth = Mathf.Min(wholeWidth / 4, 80f);
            var fieldWidth = (wholeWidth - typeWidth) / 3;
            EditorGUI.LabelField(new Rect(rect.x + handleWidth, rect.y, fieldWidth, EditorGUIUtility.singleLineHeight), "Name");
            EditorGUI.LabelField(new Rect(rect.x + handleWidth + fieldWidth + 2, rect.y, fieldWidth, EditorGUIUtility.singleLineHeight), "Initial Value");
            EditorGUI.LabelField(new Rect(rect.x + handleWidth + 2 * (fieldWidth + 2), rect.y, fieldWidth, EditorGUIUtility.singleLineHeight), "Description");
            EditorGUI.LabelField(new Rect(rect.x + handleWidth + 3 * (fieldWidth + 2), rect.y, typeWidth, EditorGUIUtility.singleLineHeight), "Type");
        }

        private void OnDrawVariableElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (!(variableReorderableList != null && 0 <= index && index < variableReorderableList.count)) return;
            var variable = variableReorderableList.list[index] as Variable;
            if (variable == null) return;
            var nameControl = "VarName" + index;
            var descriptionControl = "VarDescription" + index;
            if (!variable.FieldExists("Initial Value")) variable.fields.Add(new Field("Initial Value", string.Empty, FieldType.Text));
            if (!variable.FieldExists("Description")) variable.fields.Add(new Field("Description", string.Empty, FieldType.Text));
            var initialValueField = Field.Lookup(variable.fields, "Initial Value");
            var descriptionField = Field.Lookup(variable.fields, "Description");
            var wholeWidth = rect.width - 6f;
            var typeWidth = Mathf.Min(wholeWidth / 4, 80f);
            var fieldWidth = (wholeWidth - typeWidth) / 3;
            var originalColor = GUI.backgroundColor;
            var variableName = variable.Name;
            var conflicted = conflictedVariableNames.Contains(variableName);
            if (conflicted) GUI.backgroundColor = Color.red;
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName(nameControl);
            variable.Name = EditorGUI.TextField(new Rect(rect.x, rect.y + 2, fieldWidth, EditorGUIUtility.singleLineHeight), variableName);
            if (EditorGUI.EndChangeCheck()) ResetFilteredVariableList();
            if (conflicted) GUI.backgroundColor = originalColor;
            initialValueField.value = CustomFieldTypeService.DrawField(new Rect(rect.x + fieldWidth + 2, rect.y + 2, fieldWidth, EditorGUIUtility.singleLineHeight), initialValueField, database);
            GUI.SetNextControlName(descriptionControl);
            descriptionField.value = EditorGUI.TextField(new Rect(rect.x + 2 * (fieldWidth + 2), rect.y + 2, fieldWidth, EditorGUIUtility.singleLineHeight), descriptionField.value);
            CustomFieldTypeService.DrawFieldType(new Rect(rect.x + 3 * (fieldWidth + 2), rect.y + 2, typeWidth, EditorGUIUtility.singleLineHeight), initialValueField);
            var focusedControl = GUI.GetNameOfFocusedControl();
            if (string.Equals(nameControl, focusedControl) || string.Equals(descriptionControl, focusedControl))
            {
                inspectorSelection = variable;
            }
        }

        private void OnRemoveVariable(ReorderableList list)
        {
            if (!(variableReorderableList != null && 0 <= list.index && list.index < variableReorderableList.count)) return;
            var variable = variableReorderableList.list[list.index] as Variable;
            if (variable == null) return;
            if (EditorUtility.DisplayDialog(string.Format("Delete '{0}'?", GetAssetName(variable)), "Are you sure you want to delete this?", "Delete", "Cancel"))
            {
                database.variables.Remove(variable);
                SetDatabaseDirty("Delete Variable");
                ResetFilteredVariableList();
            }
        }

        private void OnAddVariableDropdown(Rect buttonRect, ReorderableList list)
        {
            var menu = new GenericMenu();
            string[] fieldTypes = CustomFieldTypeService.GetDialogueSystemTypes();
            string[] fieldPublicNames = CustomFieldTypeService.GetDialogueSystemPublicNames();
            for (int i = 0; i < fieldPublicNames.Length; i++)
            {
                var fieldType = (i < fieldTypes.Length) ? fieldTypes[i] : "CustomFieldType_Text";
                menu.AddItem(new GUIContent(fieldPublicNames[i]), false, OnSelectVariableTypeToAdd, fieldType);
            }
            menu.ShowAsContext();
        }

        private void OnSelectVariableTypeToAdd(object data)
        {
            if (data == null || data.GetType() != typeof(string)) return;
            var typeName = (string)data;
            Dictionary<string, CustomFieldType> types = CustomFieldTypeService.GetTypesDictionary();
            var variable = CreateNewVariable();
            var field = Field.Lookup(variable.fields, "Initial Value");
            if (types.ContainsKey(typeName) && field != null)
            {
                field.type = types[typeName].storeFieldAsType;
                field.typeString = typeName;
            }
        }

        private Variable CreateNewVariable()
        {
            Variable newVariable = AddNewAsset<Variable>(database.variables);
            if (!Field.FieldExists(newVariable.fields, "Name")) newVariable.fields.Add(new Field("Name", string.Empty, FieldType.Text));
            if (!Field.FieldExists(newVariable.fields, "Initial Value")) newVariable.fields.Add(new Field("Initial Value", string.Empty, FieldType.Text));
            if (!Field.FieldExists(newVariable.fields, "Description")) newVariable.fields.Add(new Field("Description", string.Empty, FieldType.Text));
            int index = database.variables.Count - 1;
            if (!variableFoldouts.properties.ContainsKey(index)) variableFoldouts.properties.Add(index, false);
            variableFoldouts.properties[index] = true;
            SetDatabaseDirty("Add New Variable");
            ResetFilteredVariableList();
            return newVariable;
        }

        private void DrawVariablesTraditional()
        {
            List<Variable> assets = database.variables;
            AssetFoldouts foldouts = variableFoldouts;
            EditorWindowTools.StartIndentedSection();
            Variable assetToRemove = null;
            int indexToMoveUp = -1;
            int indexToMoveDown = -1;
            for (int index = 0; index < assets.Count; index++)
            {
                Variable asset = assets[index];
                EditorGUILayout.BeginHorizontal();
                if (!foldouts.properties.ContainsKey(index)) foldouts.properties.Add(index, false);
                foldouts.properties[index] = EditorGUILayout.Foldout(foldouts.properties[index], GetAssetName(asset));
                EditorGUI.BeginDisabledGroup(index >= (assets.Count - 1));
                if (GUILayout.Button(new GUIContent("↓", "Move down"), GUILayout.Width(16))) indexToMoveDown = index;
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(index == 0);
                if (GUILayout.Button(new GUIContent("↑", "Move up"), GUILayout.Width(16))) indexToMoveUp = index;
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button(new GUIContent(" ", string.Format("Delete {0}.", GetAssetName(asset))), "OL Minus", GUILayout.Width(16))) assetToRemove = asset;
                EditorGUILayout.EndHorizontal();
                if (foldouts.properties[index]) DrawVariable(asset, index, foldouts);
            }
            if (indexToMoveDown >= 0)
            {
                Variable asset = assets[indexToMoveDown];
                assets.RemoveAt(indexToMoveDown);
                assets.Insert(indexToMoveDown + 1, asset);
                SetDatabaseDirty("Move Variable Down");
            }
            else if (indexToMoveUp >= 0)
            {
                Variable asset = assets[indexToMoveUp];
                assets.RemoveAt(indexToMoveUp);
                assets.Insert(indexToMoveUp - 1, asset);
                SetDatabaseDirty("Move Variable Up");
            }
            else if (assetToRemove != null)
            {
                if (EditorUtility.DisplayDialog(string.Format("Delete '{0}'?", GetAssetName(assetToRemove)), "Are you sure you want to delete this?", "Delete", "Cancel"))
                {
                    assets.Remove(assetToRemove);
                    SetDatabaseDirty("Delete Variable");
                }
            }
            EditorWindowTools.EndIndentedSection();
        }

        private void DrawVariable(Variable asset, int index, AssetFoldouts foldouts)
        {
            EditorWindowTools.StartIndentedSection();
            EditorGUILayout.BeginVertical("button");
            List<Field> fields = asset.fields;
            for (int i = 0; i < fields.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                if (IsTextAreaField(fields[i]))
                {
                    DrawTextAreaFirstPart(fields[i], false);
                    DrawTextAreaSecondPart(fields[i]);
                }
                else {
                    DrawField(fields[i], false);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorWindowTools.EndIndentedSection();
        }

    }

}