// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This part of the Dialogue Editor window contains common code for the 
    /// Conditions and Script wizards.
    /// </summary>
    public class LuaWizardBase
    {

        public DialogueDatabase database;

        public enum ConditionWizardResourceType { Quest, QuestEntry, Variable, Actor, Item, Location, SimStatus, Custom }

        public enum ScriptWizardResourceType { Quest, QuestEntry, Variable, Actor, Item, Location, SimStatus, Alert, Custom }

        public enum EqualityType { Is, IsNot }

        public enum ComparisonType { Is, IsNot, Less, Greater, LessEqual, GreaterEqual, Between }

        public enum LogicalOperatorType { All, Any }

        public enum BooleanType { True, False }

        public enum SimStatusType { Untouched, WasOffered, WasDisplayed }

        public string[] questNames = new string[0];

        public string[] complexQuestNames = new string[0];

        public string[] variableNames = new string[0];

        public FieldType[] variableTypes = new FieldType[0];

        public string[] actorNames = new string[0];

        public string[] actorFieldNames = new string[0];

        public FieldType[] actorFieldTypes = new FieldType[0];

        public string[] itemNames = new string[0];

        public string[] itemFieldNames = new string[0];

        public FieldType[] itemFieldTypes = new FieldType[0];

        public string[] locationNames = new string[0];

        public string[] locationFieldNames = new string[0];

        public FieldType[] locationFieldTypes = new FieldType[0];

        public LuaWizardBase(DialogueDatabase database)
        {
            this.database = database;
        }

        public void RefreshWizardResources()
        {
            RefreshQuestNames();
            RefreshVariableNames();
            RefreshActorNames();
            RefreshItemNames();
            RefreshLocationNames();
        }

        public void RefreshQuestNames()
        {
            List<string> questList = new List<string>();
            List<string> complexQuestList = new List<string>();
            if (database != null)
            {
                foreach (Item item in database.items)
                {
                    if (!item.IsItem)
                    {
                        questList.Add(item.Name);
                        if (item.LookupInt("Entry Count") > 0)
                        {
                            complexQuestList.Add(item.Name);
                        }
                    }
                }
            }
            questNames = questList.ToArray();
            complexQuestNames = complexQuestList.ToArray();
        }

        public void RefreshVariableNames()
        {
            List<string> nameList = new List<string>();
            List<FieldType> typeList = new List<FieldType>();
            if (database != null)
            {
                database.variables.ForEach(variable => { nameList.Add(variable.Name); typeList.Add(variable.Type); });
            }
            variableNames = nameList.ToArray();
            variableTypes = typeList.ToArray();
        }

        public void RefreshActorNames()
        {
            List<string> nameList = new List<string>();
            List<string> fieldList = new List<string>();
            List<FieldType> typeList = new List<FieldType>();
            if (database != null)
            {
                foreach (var actor in database.actors)
                {
                    nameList.Add(actor.Name);
                    foreach (var field in actor.fields)
                    {
                        if (!fieldList.Contains(field.title))
                        {
                            fieldList.Add(field.title);
                            typeList.Add(field.type);
                        }
                    }
                }
            }
            actorNames = nameList.ToArray();
            actorFieldNames = fieldList.ToArray();
            actorFieldTypes = typeList.ToArray();
        }

        public void RefreshItemNames()
        {
            List<string> nameList = new List<string>();
            List<string> fieldList = new List<string>();
            List<FieldType> typeList = new List<FieldType>();
            if (database != null)
            {
                foreach (var item in database.items)
                {
                    if (item.IsItem)
                    {
                        nameList.Add(item.Name);
                        foreach (var field in item.fields)
                        {
                            if (!fieldList.Contains(field.title))
                            {
                                fieldList.Add(field.title);
                                typeList.Add(field.type);
                            }
                        }
                    }
                }
            }
            itemNames = nameList.ToArray();
            itemFieldNames = fieldList.ToArray();
            itemFieldTypes = typeList.ToArray();
        }

        public void RefreshLocationNames()
        {
            List<string> nameList = new List<string>();
            List<string> fieldList = new List<string>();
            List<FieldType> typeList = new List<FieldType>();
            if (database != null)
            {
                foreach (var location in database.locations)
                {
                    nameList.Add(location.Name);
                    foreach (var field in location.fields)
                    {
                        if (!fieldList.Contains(field.title))
                        {
                            fieldList.Add(field.title);
                            typeList.Add(field.type);
                        }
                    }
                }
            }
            locationNames = nameList.ToArray();
            locationFieldNames = fieldList.ToArray();
            locationFieldTypes = typeList.ToArray();
        }

        public string[] GetQuestEntryNames(string questName)
        {
            List<string> questEntryList = new List<string>();
            Item item = database.GetItem(questName);
            if (item != null)
            {
                int entryCount = item.LookupInt("Entry Count");
                if (entryCount > 0)
                {
                    for (int i = 1; i <= entryCount; i++)
                    {
                        string entryText = item.LookupValue(string.Format("Entry {0}", i)) ?? string.Empty;
                        string s = string.Format("{0}. {1}",
                                                 i,
                                                 ((entryText.Length < 20)
                                                     ? entryText
                                                     : entryText.Substring(0, 17) + "..."));
                        questEntryList.Add(s);
                    }
                }
            }
            return questEntryList.ToArray();
        }

        public string GetWizardQuestName(string[] questNames, int index)
        {
            return (0 <= index && index < questNames.Length) ? questNames[index] : "UNDEFINED";
        }

        public string GetLogicalOperatorText(LogicalOperatorType logicalOperator)
        {
            return (logicalOperator == LogicalOperatorType.All) ? "and" : "or";
        }

        public FieldType GetWizardVariableType(int variableIndex)
        {
            return (0 <= variableIndex && variableIndex < variableTypes.Length) ? variableTypes[variableIndex] : FieldType.Text;
        }

        public FieldType GetWizardActorFieldType(int actorFieldIndex)
        {
            return (0 <= actorFieldIndex && actorFieldIndex < actorFieldTypes.Length) ? actorFieldTypes[actorFieldIndex] : FieldType.Text;
        }

        public FieldType GetWizardItemFieldType(int itemFieldIndex)
        {
            return (0 <= itemFieldIndex && itemFieldIndex < itemFieldTypes.Length) ? itemFieldTypes[itemFieldIndex] : FieldType.Text;
        }

        public FieldType GetWizardLocationFieldType(int locationFieldIndex)
        {
            return (0 <= locationFieldIndex && locationFieldIndex < locationFieldTypes.Length) ? locationFieldTypes[locationFieldIndex] : FieldType.Text;
        }

        public string GetWizardEqualityText(EqualityType equalityType)
        {
            return (equalityType == EqualityType.Is) ? "==" : "~=";
        }

        public string GetWizardComparisonText(ComparisonType comparisonType)
        {
            switch (comparisonType)
            {
                case ComparisonType.Is:
                    return "==";
                case ComparisonType.IsNot:
                    return "~=";
                case ComparisonType.Less:
                    return "<";
                case ComparisonType.LessEqual:
                    return "<=";
                case ComparisonType.Greater:
                    return ">";
                case ComparisonType.GreaterEqual:
                    return ">=";
                default:
                    return "==";
            }
        }

    }

}