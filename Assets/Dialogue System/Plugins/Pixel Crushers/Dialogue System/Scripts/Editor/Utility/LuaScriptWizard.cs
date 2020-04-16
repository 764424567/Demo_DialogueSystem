// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This Lua script wizard is meant to be called from a custom editor's
    /// OnInspectorGUI() method.
    /// </summary>
    public class LuaScriptWizard : LuaWizardBase
    {

        private enum ValueSetMode
        {
            To,
            Add
        }

        private enum NetSetMode
        {
            Set,
            NetSet
        }

        private class ScriptItem
        {
            public ScriptWizardResourceType resourceType = ScriptWizardResourceType.Quest;
            public int questNamesIndex = 0;
            public int questEntryIndex = 0;
            public int variableNamesIndex = 0;
            public int actorNamesIndex = 0;
            public int actorFieldIndex = 0;
            public int itemNamesIndex = 0;
            public int itemFieldIndex = 0;
            public int locationNamesIndex = 0;
            public int locationFieldIndex = 0;
            public int simStatusID = 0;
            public SimStatusType simStatusType = SimStatusType.Untouched;
            public QuestState questState = QuestState.Unassigned;
            public string stringValue = string.Empty;
            public BooleanType booleanValue = BooleanType.True;
            public float floatValue = 0;
            public ValueSetMode valueSetMode = ValueSetMode.To;
            public NetSetMode netSetMode = NetSetMode.Set;
            public string[] scriptQuestEntryNames = new string[0];
        }

        private bool isOpen = false;
        private List<ScriptItem> scriptItems = new List<ScriptItem>();
        private string savedLuaCode = string.Empty;
        private bool append = true;

        public bool IsOpen { get { return isOpen; } }

        public LuaScriptWizard(DialogueDatabase database) :
        base(database)
        {
        }

        public float GetHeight()
        {
            if (database == null) return 0;
            if (!isOpen) return EditorGUIUtility.singleLineHeight;
            return 4 + ((3 + scriptItems.Count) * (EditorGUIUtility.singleLineHeight + 2f));
        }

        public string Draw(GUIContent guiContent, string luaCode)
        {
            if (database == null) isOpen = false;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(guiContent);
            EditorGUI.BeginDisabledGroup(database == null);
            if (GUILayout.Button(new GUIContent("...", "Open Lua wizard."), EditorStyles.miniButton, GUILayout.Width(22)))
            {
                ToggleScriptWizard();
                if (isOpen) savedLuaCode = luaCode;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (isOpen)
            {
                luaCode = DrawScriptWizard(luaCode);
            }

            luaCode = EditorGUILayout.TextArea(luaCode);

            return luaCode;
        }

        public void OpenWizard(string luaCode)
        {
            if (isOpen) return;
            ToggleScriptWizard();
            if (isOpen) savedLuaCode = luaCode;
            append = true;
        }

        public void ResetWizard()
        {
            isOpen = false;
            savedLuaCode = string.Empty;
        }

        private void ToggleScriptWizard()
        {
            isOpen = !isOpen;
            if (isOpen) append = true;
            scriptItems.Clear();
            RefreshWizardResources();
        }

        private string DrawScriptWizard(string luaCode)
        {
            EditorGUILayout.BeginVertical("button");

            EditorGUI.BeginChangeCheck();

            // Script items:
            ScriptItem itemToDelete = null;
            foreach (ScriptItem item in scriptItems)
            {
                DrawScriptItem(item, ref itemToDelete);
            }
            if (itemToDelete != null) scriptItems.Remove(itemToDelete);

            // "+", Revert, and Apply buttons:
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("+", "Add a new script action."), EditorStyles.miniButton, GUILayout.Width(22)))
            {
                scriptItems.Add(new ScriptItem());
            }

            GUILayout.FlexibleSpace();
            append = EditorGUILayout.ToggleLeft("Append", append, GUILayout.Width(60));

            if (EditorGUI.EndChangeCheck()) ApplyScriptWizard();

            if (GUILayout.Button(new GUIContent("Revert", "Cancel these settings."), EditorStyles.miniButton, GUILayout.Width(48)))
            {
                luaCode = CancelScriptWizard();
            }
            if (GUILayout.Button(new GUIContent("Apply", "Apply these settings"), EditorStyles.miniButton, GUILayout.Width(48)))
            {
                luaCode = AcceptScriptWizard();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            return luaCode;
        }

        private void DrawScriptItem(ScriptItem item, ref ScriptItem itemToDelete)
        {
            EditorGUILayout.BeginHorizontal();

#if USE_UNET
            if (item.resourceType == ScriptWizardResourceType.Quest || item.resourceType == ScriptWizardResourceType.QuestEntry || item.resourceType == ScriptWizardResourceType.Variable)
            {
                item.netSetMode = (NetSetMode)EditorGUILayout.EnumPopup(item.netSetMode, GUILayout.Width(36));
            }
            else
            {
                item.netSetMode = NetSetMode.Set;
                EditorGUILayout.LabelField("Set", GUILayout.Width(32));
            }
#else
            EditorGUILayout.LabelField("Set", GUILayout.Width(32));
#endif
            ScriptWizardResourceType newResourceType = (ScriptWizardResourceType)EditorGUILayout.EnumPopup(item.resourceType, GUILayout.Width(96));
            if (newResourceType != item.resourceType)
            {
                item.resourceType = newResourceType;
                item.scriptQuestEntryNames = new string[0];
            }

            if (item.resourceType == ScriptWizardResourceType.Quest)
            {

                // Quest:
                item.questNamesIndex = EditorGUILayout.Popup(item.questNamesIndex, questNames);
                EditorGUILayout.LabelField("to", GUILayout.Width(22));
                //---Was: item.questState = (QuestState) EditorGUILayout.EnumPopup(item.questState, GUILayout.Width(96));
                item.questState = QuestStateDrawer.LayoutQuestStatePopup(item.questState, 96);

            }
            else if (item.resourceType == ScriptWizardResourceType.QuestEntry)
            {

                // Quest Entry:
                int newQuestNamesIndex = EditorGUILayout.Popup(item.questNamesIndex, complexQuestNames);
                if (newQuestNamesIndex != item.questNamesIndex)
                {
                    item.questNamesIndex = newQuestNamesIndex;
                    item.scriptQuestEntryNames = new string[0];
                }
                if ((item.scriptQuestEntryNames.Length == 0) && (item.questNamesIndex < complexQuestNames.Length))
                {
                    item.scriptQuestEntryNames = GetQuestEntryNames(complexQuestNames[item.questNamesIndex]);
                }
                item.questEntryIndex = EditorGUILayout.Popup(item.questEntryIndex, item.scriptQuestEntryNames);
                EditorGUILayout.LabelField("to", GUILayout.Width(22));
                //---Was: item.questState = (QuestState) EditorGUILayout.EnumPopup(item.questState, GUILayout.Width(96));
                item.questState = QuestStateDrawer.LayoutQuestStatePopup(item.questState, 96);

            }
            else if (item.resourceType == ScriptWizardResourceType.Variable)
            {

                // Variable:
                item.variableNamesIndex = EditorGUILayout.Popup(item.variableNamesIndex, variableNames);
                var variableType = GetWizardVariableType(item.variableNamesIndex);
                DrawValueSetMode(item, variableType);
                switch (variableType)
                {
                    case FieldType.Boolean:
                        item.booleanValue = (BooleanType)EditorGUILayout.EnumPopup(item.booleanValue);
                        break;
                    case FieldType.Number:
                        item.floatValue = EditorGUILayout.FloatField(item.floatValue);
                        break;
                    default:
                        item.stringValue = EditorGUILayout.TextField(item.stringValue);
                        break;
                }
            }
            else if (item.resourceType == ScriptWizardResourceType.Actor)
            {

                // Actor:
                item.actorNamesIndex = EditorGUILayout.Popup(item.actorNamesIndex, actorNames);
                item.actorFieldIndex = EditorGUILayout.Popup(item.actorFieldIndex, actorFieldNames);
                var actorFieldType = GetWizardActorFieldType(item.actorFieldIndex);
                DrawValueSetMode(item, actorFieldType);
                switch (actorFieldType)
                {
                    case FieldType.Boolean:
                        item.booleanValue = (BooleanType)EditorGUILayout.EnumPopup(item.booleanValue);
                        break;
                    case FieldType.Number:
                        item.floatValue = EditorGUILayout.FloatField(item.floatValue);
                        break;
                    default:
                        item.stringValue = EditorGUILayout.TextField(item.stringValue);
                        break;
                }

            }
            else if (item.resourceType == ScriptWizardResourceType.Item)
            {

                // Item:
                item.itemNamesIndex = EditorGUILayout.Popup(item.itemNamesIndex, itemNames);
                item.itemFieldIndex = EditorGUILayout.Popup(item.itemFieldIndex, itemFieldNames);
                var itemFieldType = GetWizardItemFieldType(item.itemFieldIndex);
                DrawValueSetMode(item, itemFieldType);
                switch (itemFieldType)
                {
                    case FieldType.Boolean:
                        item.booleanValue = (BooleanType)EditorGUILayout.EnumPopup(item.booleanValue);
                        break;
                    case FieldType.Number:
                        item.floatValue = EditorGUILayout.FloatField(item.floatValue);
                        break;
                    default:
                        item.stringValue = EditorGUILayout.TextField(item.stringValue);
                        break;
                }

            }
            else if (item.resourceType == ScriptWizardResourceType.Location)
            {

                // Location:
                item.locationNamesIndex = EditorGUILayout.Popup(item.locationNamesIndex, locationNames);
                item.locationFieldIndex = EditorGUILayout.Popup(item.locationFieldIndex, locationFieldNames);
                var locationFieldType = GetWizardLocationFieldType(item.locationFieldIndex);
                DrawValueSetMode(item, locationFieldType);
                switch (locationFieldType)
                {
                    case FieldType.Boolean:
                        item.booleanValue = (BooleanType)EditorGUILayout.EnumPopup(item.booleanValue);
                        break;
                    case FieldType.Number:
                        item.floatValue = EditorGUILayout.FloatField(item.floatValue);
                        break;
                    default:
                        item.stringValue = EditorGUILayout.TextField(item.stringValue);
                        break;
                }

            }
            else if (item.resourceType == ScriptWizardResourceType.SimStatus)
            {

                // SimStatus:
                item.simStatusID = EditorGUILayout.IntField(item.simStatusID, GUILayout.Width(50));
                item.simStatusType = (SimStatusType)EditorGUILayout.EnumPopup(item.simStatusType);

            }
            else if (item.resourceType == ScriptWizardResourceType.Alert)
            {

                // Alert:
                item.stringValue = EditorGUILayout.TextField(item.stringValue);

            }
            else if (item.resourceType == ScriptWizardResourceType.Custom)
            {

                // Custom:
                item.stringValue = EditorGUILayout.TextField(item.stringValue);
            }

            if (GUILayout.Button(new GUIContent("-", "Delete this script action."), EditorStyles.miniButton, GUILayout.Width(22)))
            {
                itemToDelete = item;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawValueSetMode(ScriptItem item, FieldType fieldType)
        {
            if (fieldType == FieldType.Number)
            {
                item.valueSetMode = (ValueSetMode)EditorGUILayout.EnumPopup(item.valueSetMode, GUILayout.Width(40));
            }
            else
            {
                EditorGUILayout.LabelField("to", GUILayout.Width(22));
            }
        }

        public string CancelScriptWizard()
        {
            isOpen = false;
            return savedLuaCode;
        }

        public string AcceptScriptWizard()
        {
            isOpen = false;
            return ApplyScriptWizard();
        }

        private string ApplyScriptWizard()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                if (append && !string.IsNullOrEmpty(savedLuaCode)) sb.AppendFormat("{0};\n", savedLuaCode);
                string endText = (scriptItems.Count > 1) ? ";\n" : string.Empty;
                for (int i = 0; i < scriptItems.Count; i++)
                {
                    var item = scriptItems[i];
                    if (item.resourceType == ScriptWizardResourceType.Quest)
                    {

                        // Quest:
                        string questName = GetWizardQuestName(questNames, item.questNamesIndex);
                        if (item.netSetMode == NetSetMode.NetSet) sb.Append("Net");
                        sb.AppendFormat("SetQuestState(\"{0}\", \"{1}\")",
                                        questName,
                                        QuestLog.StateToString(item.questState));

                    }
                    else if (item.resourceType == ScriptWizardResourceType.QuestEntry)
                    {

                        // Quest Entry:
                        string questName = GetWizardQuestName(complexQuestNames, item.questNamesIndex);
                        if (item.netSetMode == NetSetMode.NetSet) sb.Append("Net");
                        sb.AppendFormat("SetQuestEntryState(\"{0}\", {1}, \"{2}\")",
                                        questName,
                                        item.questEntryIndex + 1,
                                        QuestLog.StateToString(item.questState));

                    }
                    else if (item.resourceType == ScriptWizardResourceType.Variable)
                    {

                        // Variable:
                        string variableName = variableNames[item.variableNamesIndex];
                        switch (GetWizardVariableType(item.variableNamesIndex))
                        {
                            case FieldType.Boolean:
                                if (item.netSetMode == NetSetMode.NetSet)
                                {
                                    sb.AppendFormat("NetSetBool(\"{0}\", {1})",
                                                    DialogueLua.StringToTableIndex(variableName),
                                                    (item.booleanValue == BooleanType.True) ? "true" : "false");
                                }
                                else
                                {
                                    sb.AppendFormat("Variable[\"{0}\"] = {1}",
                                                    DialogueLua.StringToTableIndex(variableName),
                                                    (item.booleanValue == BooleanType.True) ? "true" : "false");
                                }
                                break;
                            case FieldType.Number:
                                if (item.netSetMode == NetSetMode.NetSet)
                                {

                                    if (item.valueSetMode == ValueSetMode.To)
                                    {
                                        sb.AppendFormat("NetSetNumber(\"{0}\", {1})",
                                                        DialogueLua.StringToTableIndex(variableName),
                                                        item.floatValue);
                                    }
                                    else
                                    {
                                        sb.AppendFormat("NetSetNumber(\"{0}\", Variable[\"{0}\"] + {1})",
                                                        DialogueLua.StringToTableIndex(variableName),
                                                        item.floatValue);
                                    }
                                }
                                else
                                {
                                    if (item.valueSetMode == ValueSetMode.To)
                                    {
                                        sb.AppendFormat("Variable[\"{0}\"] = {1}",
                                                        DialogueLua.StringToTableIndex(variableName),
                                                        item.floatValue);
                                    }
                                    else
                                    {
                                        sb.AppendFormat("Variable[\"{0}\"] = Variable[\"{0}\"] + {1}",
                                                        DialogueLua.StringToTableIndex(variableName),
                                                        item.floatValue);
                                    }
                                }
                                break;
                            default:
                                if (item.netSetMode == NetSetMode.NetSet)
                                {
                                    sb.AppendFormat("NetSetString(\"{0}\", \"{1}\")",
                                                    DialogueLua.StringToTableIndex(variableName),
                                                    item.stringValue);
                                }
                                else
                                {
                                    sb.AppendFormat("Variable[\"{0}\"] = \"{1}\"",
                                                    DialogueLua.StringToTableIndex(variableName),
                                                    item.stringValue);
                                }
                                break;
                        }

                    }
                    else if (item.resourceType == ScriptWizardResourceType.Actor)
                    {

                        // Actor:
                        if (item.actorNamesIndex < actorNames.Length)
                        {
                            var actorName = actorNames[item.actorNamesIndex];
                            var actorFieldName = actorFieldNames[item.actorFieldIndex];
                            var fieldType = GetWizardActorFieldType(item.actorFieldIndex);
                            AppendFormat(sb, "Actor", actorName, actorFieldName, fieldType, item);
                        }

                    }
                    else if (item.resourceType == ScriptWizardResourceType.Item)
                    {

                        // Item:
                        if (item.itemNamesIndex < itemNames.Length)
                        {
                            var itemName = itemNames[item.itemNamesIndex];
                            var itemFieldName = itemFieldNames[item.itemFieldIndex];
                            var fieldType = GetWizardItemFieldType(item.itemFieldIndex);
                            AppendFormat(sb, "Item", itemName, itemFieldName, fieldType, item);
                        }

                    }
                    else if (item.resourceType == ScriptWizardResourceType.Location)
                    {

                        // Location:
                        if (item.locationNamesIndex < locationNames.Length)
                        {
                            var locationName = locationNames[item.locationNamesIndex];
                            var locationFieldName = locationFieldNames[item.locationFieldIndex];
                            var fieldType = GetWizardLocationFieldType(item.locationFieldIndex);
                            AppendFormat(sb, "Location", locationName, locationFieldName, fieldType, item);
                        }
                    }
                    else if (item.resourceType == ScriptWizardResourceType.SimStatus)
                    {

                        // SimStatus:
                        sb.AppendFormat("Dialog[{0}].SimStatus = \"{1}\"", item.simStatusID, item.simStatusType);
                    }
                    else if (item.resourceType == ScriptWizardResourceType.Alert)
                    {

                        // Custom:
                        sb.Append("ShowAlert(\"" + item.stringValue.Replace("\"", "\\\"") + "\")");
                    }
                    else if (item.resourceType == ScriptWizardResourceType.Custom)
                    {

                        // Custom:
                        sb.Append(item.stringValue);
                    }

                    if (i < (scriptItems.Count - 1)) sb.AppendFormat(endText);
                }
                return sb.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError(string.Format("{0}: Internal error building script: {1}", DialogueDebug.Prefix, e.Message));
                return savedLuaCode;
            }
        }

        private void AppendFormat(StringBuilder sb, string tableName, string elementName, string fieldName, FieldType fieldType, ScriptItem item)
        {
            switch (fieldType)
            {
                case FieldType.Boolean:
                    sb.AppendFormat("{0}[\"{1}\"].{2} = {3}",
                                    tableName,
                                    DialogueLua.StringToTableIndex(elementName),
                                    DialogueLua.StringToTableIndex(fieldName),
                                    (item.booleanValue == BooleanType.True) ? "true" : "false");
                    break;
                case FieldType.Number:
                    if (item.valueSetMode == ValueSetMode.To)
                    {
                        sb.AppendFormat("{0}[\"{1}\"].{2} = {3}",
                                        tableName,
                                        DialogueLua.StringToTableIndex(elementName),
                                        DialogueLua.StringToTableIndex(fieldName),
                                        item.floatValue);
                    }
                    else
                    {
                        sb.AppendFormat("{0}[\"{1}\"].{2} = {0}[\"{1}\"].{2} + {3}",
                                        tableName,
                                        DialogueLua.StringToTableIndex(elementName),
                                        DialogueLua.StringToTableIndex(fieldName),
                                        item.floatValue);
                    }
                    break;
                default:
                    sb.AppendFormat("{0}[\"{1}\"].{2} = \"{3}\"",
                                    tableName,
                                    DialogueLua.StringToTableIndex(elementName),
                                    DialogueLua.StringToTableIndex(fieldName),
                                    item.stringValue);
                    break;
            }
        }


        //====================================================================

        public string Draw(Rect position, GUIContent guiContent, string luaCode)
        {
            if (database == null) isOpen = false;

            // Title label:
            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rect, guiContent);

            var textAreaHeight = Mathf.Max(EditorGUIUtility.singleLineHeight, GUI.skin.textArea.CalcHeight(new GUIContent(luaCode), position.width));

            if (isOpen)
            {
                // Lua wizard content:
                rect = new Rect(position.x + 16, position.y + EditorGUIUtility.singleLineHeight + 2f, position.width - 16,
                    position.height - (4f + EditorGUIUtility.singleLineHeight + textAreaHeight));
                EditorGUI.BeginDisabledGroup(true);
                GUI.Button(rect, GUIContent.none);
                EditorGUI.EndDisabledGroup();

                luaCode = DrawScriptWizard(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), luaCode);
            }

            rect = new Rect(position.x, position.y + position.height - textAreaHeight, position.width, textAreaHeight);
            luaCode = EditorGUI.TextArea(rect, luaCode);

            return luaCode;
        }

        private string DrawScriptWizard(Rect position, string luaCode)
        {
            int originalIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var rect = position;
            var x = position.x;
            var y = position.y;

            EditorGUI.BeginChangeCheck();

            // Script items:
            ScriptItem itemToDelete = null;
            foreach (ScriptItem item in scriptItems)
            {
                var innerHeight = EditorGUIUtility.singleLineHeight + 2;
                DrawScriptItem(new Rect(x, y, position.width, innerHeight), item, ref itemToDelete);
                y += EditorGUIUtility.singleLineHeight + 2;
            }
            if (itemToDelete != null) scriptItems.Remove(itemToDelete);

            // "+", Revert, and Apply buttons:
            x = position.x;
            y = position.y + position.height - EditorGUIUtility.singleLineHeight;
            rect = new Rect(x, y, 22, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(rect, new GUIContent("+", "Add a new script action."), EditorStyles.miniButton))
            {
                scriptItems.Add(new ScriptItem());
            }
            x += rect.width + 2;

            rect = new Rect(x, y, 72, EditorGUIUtility.singleLineHeight);
            append = EditorGUI.ToggleLeft(rect, "Append", append);

            if (EditorGUI.EndChangeCheck()) ApplyScriptWizard();

            EditorGUI.BeginDisabledGroup(scriptItems.Count <= 0);
            rect = new Rect(position.x + position.width - 48 - 4 - 48, y, 48, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(rect, new GUIContent("Revert", "Cancel these settings."), EditorStyles.miniButton))
            {
                luaCode = CancelScriptWizard();
            }
            rect = new Rect(position.x + position.width - 48, y, 48, EditorGUIUtility.singleLineHeight);
            GUI.Box(rect, GUIContent.none);
            if (GUI.Button(rect, new GUIContent("Apply", "Apply these settings"), EditorStyles.miniButton))
            {
                luaCode = AcceptScriptWizard();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel = originalIndentLevel;

            return luaCode;
        }

        private void DrawScriptItem(Rect position, ScriptItem item, ref ScriptItem itemToDelete)
        {
            const float setLabelWidth = 32;
            const float deleteButtonWidth = 22;

            int originalIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var x = position.x;
            var y = position.y;
            var rect = new Rect(x, y, setLabelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rect, "Set");
            x += rect.width + 2;

            rect = new Rect(x, y, 96, EditorGUIUtility.singleLineHeight);
            ScriptWizardResourceType newResourceType = (ScriptWizardResourceType)EditorGUI.EnumPopup(rect, item.resourceType);
            x += rect.width + 2;
            if (newResourceType != item.resourceType)
            {
                item.resourceType = newResourceType;
                item.scriptQuestEntryNames = new string[0];
            }

            if (item.resourceType == ScriptWizardResourceType.Quest)
            {

                // Quest:
                var questNameWidth = Mathf.Max(1, position.width - x - 98 - 22);
                rect = new Rect(x, y, questNameWidth, rect.height);
                x += rect.width + 2;
                item.questNamesIndex = EditorGUI.Popup(rect, item.questNamesIndex, questNames);

                rect = new Rect(x, y, 22, rect.height);
                x += rect.width + 2;
                EditorGUI.LabelField(rect, "to");

                rect = new Rect(x, y, 96, EditorGUIUtility.singleLineHeight);
                item.questState = QuestStateDrawer.RectQuestStatePopup(rect, item.questState);

            }
            else if (item.resourceType == ScriptWizardResourceType.QuestEntry)
            {

                // Quest Entry:
                var questNameWidth = Mathf.Max(1, position.width - x - 98 - 98 - 22);
                rect = new Rect(x, y, questNameWidth, EditorGUIUtility.singleLineHeight);
                x += rect.width + 2;
                int newQuestNamesIndex = EditorGUI.Popup(rect, item.questNamesIndex, complexQuestNames);

                if (newQuestNamesIndex != item.questNamesIndex)
                {
                    item.questNamesIndex = newQuestNamesIndex;
                    item.scriptQuestEntryNames = new string[0];
                }
                if ((item.scriptQuestEntryNames.Length == 0) && (item.questNamesIndex < complexQuestNames.Length))
                {
                    item.scriptQuestEntryNames = GetQuestEntryNames(complexQuestNames[item.questNamesIndex]);
                }

                rect = new Rect(x, y, 96, EditorGUIUtility.singleLineHeight);
                x += rect.width + 2;
                item.questEntryIndex = EditorGUI.Popup(rect, item.questEntryIndex, item.scriptQuestEntryNames);

                rect = new Rect(x, y, 22, rect.height);
                x += rect.width + 2;
                EditorGUI.LabelField(rect, "to");

                rect = new Rect(x, y, 96, EditorGUIUtility.singleLineHeight);
                item.questState = QuestStateDrawer.RectQuestStatePopup(rect, item.questState);

            }
            else if (item.resourceType == ScriptWizardResourceType.Variable)
            {

                // Variable:
                var availableWidth = position.width - rect.x - deleteButtonWidth - 2;
                var fieldWidth = availableWidth / 3;
                rect = new Rect(x, y, fieldWidth - 1, rect.height);
                x += rect.width + 2;
                item.variableNamesIndex = EditorGUI.Popup(rect, item.variableNamesIndex, variableNames);

                var variableType = GetWizardVariableType(item.variableNamesIndex);
                rect = new Rect(x, y, 40, rect.height);
                x += rect.width + 2;
                DrawValueSetMode(rect, item, variableType);

                rect = new Rect(x, y, fieldWidth - 1, rect.height);
                switch (variableType)
                {
                    case FieldType.Boolean:
                        item.booleanValue = (BooleanType)EditorGUI.EnumPopup(rect, item.booleanValue);
                        break;
                    case FieldType.Number:
                        item.floatValue = EditorGUI.FloatField(rect, item.floatValue);
                        break;
                    default:
                        item.stringValue = EditorGUI.TextField(rect, item.stringValue);
                        break;
                }
            }
            else if (item.resourceType == ScriptWizardResourceType.Actor)
            {

                // Actor:
                var availableWidth = position.width - rect.x - deleteButtonWidth - 2;
                var fieldWidth = availableWidth / 4;
                rect = new Rect(x, y, fieldWidth - 1, rect.height);
                x += rect.width + 2;
                item.actorNamesIndex = EditorGUI.Popup(rect, item.actorNamesIndex, actorNames);

                rect = new Rect(x, y, fieldWidth - 1, rect.height);
                x += rect.width + 2;
                item.actorFieldIndex = EditorGUI.Popup(rect, item.actorFieldIndex, actorFieldNames);

                var actorFieldType = GetWizardActorFieldType(item.actorFieldIndex);
                rect = new Rect(x, y, 40, rect.height);
                x += rect.width + 2;
                DrawValueSetMode(rect, item, actorFieldType);

                rect = new Rect(position.x + position.width - deleteButtonWidth - 2 - fieldWidth - 2, y, fieldWidth - 1, rect.height);
                switch (actorFieldType)
                {
                    case FieldType.Boolean:
                        item.booleanValue = (BooleanType)EditorGUI.EnumPopup(rect, item.booleanValue);
                        break;
                    case FieldType.Number:
                        item.floatValue = EditorGUI.FloatField(rect, item.floatValue);
                        break;
                    default:
                        item.stringValue = EditorGUI.TextField(rect, item.stringValue);
                        break;
                }

            }
            else if (item.resourceType == ScriptWizardResourceType.Item)
            {

                // Item:
                var availableWidth = position.width - rect.x - deleteButtonWidth - 2;
                var fieldWidth = availableWidth / 4;
                rect = new Rect(x, y, fieldWidth - 1, rect.height);
                x += rect.width + 2;
                item.itemNamesIndex = EditorGUI.Popup(rect, item.itemNamesIndex, itemNames);

                rect = new Rect(x, y, fieldWidth - 1, rect.height);
                x += rect.width + 2;
                item.itemFieldIndex = EditorGUI.Popup(rect, item.itemFieldIndex, itemFieldNames);

                var itemFieldType = GetWizardItemFieldType(item.itemFieldIndex);
                rect = new Rect(x, y, 40 - 1, rect.height);
                x += rect.width + 2;
                DrawValueSetMode(rect, item, itemFieldType);

                rect = new Rect(position.x + position.width - deleteButtonWidth - 2 - fieldWidth - 2, y, fieldWidth - 1, rect.height);
                switch (itemFieldType)
                {
                    case FieldType.Boolean:
                        item.booleanValue = (BooleanType)EditorGUI.EnumPopup(rect, item.booleanValue);
                        break;
                    case FieldType.Number:
                        item.floatValue = EditorGUI.FloatField(rect, item.floatValue);
                        break;
                    default:
                        item.stringValue = EditorGUI.TextField(rect, item.stringValue);
                        break;
                }

            }
            else if (item.resourceType == ScriptWizardResourceType.Location)
            {

                // Location:
                var availableWidth = position.width - rect.x - deleteButtonWidth - 2;
                var fieldWidth = availableWidth / 4;
                rect = new Rect(x, y, fieldWidth - 1, rect.height);
                x += rect.width + 2;
                item.locationNamesIndex = EditorGUI.Popup(rect, item.locationNamesIndex, locationNames);

                rect = new Rect(x, y, fieldWidth - 1, rect.height);
                x += rect.width + 2;
                item.locationFieldIndex = EditorGUI.Popup(rect, item.locationFieldIndex, locationFieldNames);

                var locationFieldType = GetWizardLocationFieldType(item.locationFieldIndex);
                rect = new Rect(x, y, 40 - 1, rect.height);
                x += rect.width + 2;
                DrawValueSetMode(rect, item, locationFieldType);

                rect = new Rect(position.x + position.width - deleteButtonWidth - 2 - fieldWidth - 2, y, fieldWidth - 1, rect.height);
                switch (locationFieldType)
                {
                    case FieldType.Boolean:
                        item.booleanValue = (BooleanType)EditorGUI.EnumPopup(rect, item.booleanValue);
                        break;
                    case FieldType.Number:
                        item.floatValue = EditorGUI.FloatField(rect, item.floatValue);
                        break;
                    default:
                        item.stringValue = EditorGUI.TextField(rect, item.stringValue);
                        break;
                }

            }

            else if (item.resourceType == ScriptWizardResourceType.SimStatus)
            {

                // SimStatus:
                rect = new Rect(x, y, 50, rect.height);
                x += rect.width + 2;
                item.simStatusID = EditorGUI.IntField(rect, item.simStatusID);
                rect = new Rect(x, y, position.width - x - 2, rect.height);
                item.simStatusType = (SimStatusType)EditorGUI.EnumPopup(rect, item.simStatusType);
            }
            else if (item.resourceType == ScriptWizardResourceType.Custom)
            {

                // Custom:
                rect = new Rect(x, y, position.width - rect.width - 2, rect.height);
                item.stringValue = EditorGUI.TextField(rect, item.stringValue);
            }

            rect = new Rect(position.x + position.width - deleteButtonWidth, y, deleteButtonWidth, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(rect, new GUIContent("-", "Delete this condition."), EditorStyles.miniButton))
            {
                itemToDelete = item;
            }

            EditorGUI.indentLevel = originalIndentLevel;
        }

        private void DrawValueSetMode(Rect position, ScriptItem item, FieldType fieldType)
        {
            var rect = new Rect(position.x, position.y, 40f, position.height);
            if (fieldType == FieldType.Number)
            {
                item.valueSetMode = (ValueSetMode)EditorGUI.EnumPopup(rect, item.valueSetMode);
            }
            else
            {
                EditorGUI.LabelField(rect, "to");
            }
        }


    }

}