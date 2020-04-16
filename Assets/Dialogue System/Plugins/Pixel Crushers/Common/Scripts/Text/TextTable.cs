// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;
using System.Collections.Generic;

namespace PixelCrushers
{

    /// <summary>
    /// A TextTable is a 2D table of languages and fields.
    /// </summary>
    public class TextTable : ScriptableObject, ISerializationCallbackReceiver
    {

        private static int s_currentLanguageID = 0;

        private Dictionary<string, int> m_languages = new Dictionary<string, int>(); // <languageName, languageID>

        private Dictionary<int, TextTableField> m_fields = new Dictionary<int, TextTableField>(); // <fieldID, {translations}>

        /// <summary>
        /// ID of the current language.
        /// </summary>
        public static int currentLanguageID
        {
            get { return s_currentLanguageID; }
            set { s_currentLanguageID = value; }
        }

        public Dictionary<string, int> languages // <languageName, languageID>
        {
            get { return m_languages; }
            set { m_languages = value; }
        }

        public Dictionary<int, TextTableField> fields // <fieldID, {translations}>
        {
            get { return m_fields; }
            set { m_fields = value; }
        }

        [SerializeField]
        private List<string> m_languageKeys = new List<string>();

        [SerializeField]
        private List<int> m_languageValues = new List<int>();

        [SerializeField]
        private List<int> m_fieldKeys = new List<int>();

        [SerializeField]
        private List<TextTableField> m_fieldValues = new List<TextTableField>();

        [SerializeField]
        private int m_nextLanguageID = 0;

        [SerializeField]
        private int m_nextFieldID = 1;

        public int nextLanguageID { get { return m_nextLanguageID; } }

        public int nextFieldID { get { return m_nextFieldID; } }

        #region Serialization

        public void OnBeforeSerialize()
        {
            m_languageKeys.Clear();
            m_languageValues.Clear();
            foreach (var kvp in languages)
            {
                m_languageKeys.Add(kvp.Key);
                m_languageValues.Add(kvp.Value);
            }
            m_fieldKeys.Clear();
            m_fieldValues.Clear();
            foreach (var kvp in fields)
            {
                m_fieldKeys.Add(kvp.Key);
                m_fieldValues.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            languages = new Dictionary<string, int>();
            for (int i = 0; i != Math.Min(m_languageKeys.Count, m_languageValues.Count); i++)
            {
                languages.Add(m_languageKeys[i], m_languageValues[i]);
            }
            fields = new Dictionary<int, TextTableField>();
            for (int i = 0; i != Math.Min(m_fieldKeys.Count, m_fieldValues.Count); i++)
            {
                fields.Add(m_fieldKeys[i], m_fieldValues[i]);
            }
        }

        #endregion

        #region Languages

        /// <summary>
        /// Returns true if the text table has the named language.
        /// </summary>
        public bool HasLanguage(string languageName)
        {
            return languages.ContainsKey(languageName);
        }

        /// <summary>
        /// Returns true if the text table has a language with the specified ID.
        /// </summary>
        public bool HasLanguage(int languageID)
        {
            return languages.ContainsValue(languageID);
        }

        /// <summary>
        /// Returns the name of the language with the specified ID.
        /// </summary>
        public string GetLanguageName(int languageID)
        {
            // Enumerate manually to avoid garbage:
            var enumerator = languages.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Value == languageID) return enumerator.Current.Key;
            }
            return string.Empty;
        }

        /// <summary>
        /// Returns the ID of the named language.
        /// </summary>
        public int GetLanguageID(string languageName)
        {
            return languages.ContainsKey(languageName) ? languages[languageName] : 0;
        }

        /// <summary>
        /// Returns the names of all languages in the text table.
        /// </summary>
        public string[] GetLanguageNames()
        {
            var names = new string[languages.Count];
            languages.Keys.CopyTo(names, 0);
            return names;
        }

        /// <summary>
        /// Gets the IDs of all languages in the text table.
        /// </summary>
        public int[] GetLanguageIDs()
        {
            var ids = new int[languages.Count];
            languages.Values.CopyTo(ids, 0);
            return ids;
        }

        /// <summary>
        /// Adds a language to the text table. The language will be assigned
        /// a unique ID.
        /// </summary>
        public void AddLanguage(string languageName)
        {
            if (languages.ContainsKey(languageName)) return;
            languages.Add(languageName, m_nextLanguageID++);
        }

        /// <summary>
        /// Removes a language from the text table, including all of its fields.
        /// </summary>
        public void RemoveLanguage(string languageName)
        {
            if (!languages.ContainsKey(languageName)) return;
            RemoveLanguageFromFields(languages[languageName]);
            languages.Remove(languageName);
        }

        /// <summary>
        /// Removes a language from the text table, including all of its fields.
        /// </summary>
        public void RemoveLanguage(int languageID)
        {
            RemoveLanguage(GetLanguageName(languageID));
        }

        #endregion

        #region Fields

        /// <summary>
        /// Returns true if the text table has a field with the specified field ID.
        /// </summary>
        public bool HasField(int fieldID)
        {
            return fields.ContainsKey(fieldID);
        }

        /// <summary>
        /// Returns true if the text table has a field with the specified name.
        /// </summary>
        public bool HasField(string fieldName)
        {
            return GetField(fieldName) != null;
        }

        /// <summary>
        /// Looks up a field by ID.
        /// </summary>
        public TextTableField GetField(int fieldID)
        {
            return fields.ContainsKey(fieldID) ? fields[fieldID] : null;
        }

        /// <summary>
        /// Looks up a field by name.
        /// </summary>
        public TextTableField GetField(string fieldName)
        {
            return GetField(GetFieldID(fieldName));
        }

        /// <summary>
        /// Returns the ID associated with a field name.
        /// </summary>
        public int GetFieldID(string fieldName)
        {
            var enumerator = fields.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Value != null && string.Equals(enumerator.Current.Value.fieldName, fieldName)) return enumerator.Current.Key;
            }
            return 0;
        }

        /// <summary>
        /// Returns the name of the field with the specified ID.
        /// </summary>
        public string GetFieldName(int fieldID)
        {
            return fields.ContainsKey(fieldID) ? fields[fieldID].fieldName : string.Empty;
        }

        /// <summary>
        /// Returns true if the field has text for a specified language.
        /// </summary>
        public bool HasFieldTextForLanguage(int fieldID, int languageID)
        {
            var field = GetField(fieldID);
            return (field != null) ? field.HasTextForLanguage(languageID) : false;
        }

        /// <summary>
        /// Returns true if the field has text for a specified language.
        /// </summary>
        public bool HasFieldTextForLanguage(int fieldID, string languageName)
        {
            return HasFieldTextForLanguage(fieldID, GetLanguageID(languageName));
        }

        /// <summary>
        /// Returns true if the field has text for a specified language.
        /// </summary>
        public bool HasFieldTextForLanguage(string fieldName, int languageID)
        {
            return HasFieldTextForLanguage(GetFieldID(fieldName), languageID);
        }

        /// <summary>
        /// Returns true if the field has text for a specified language.
        /// </summary>
        public bool HasFieldTextForLanguage(string fieldName, string languageName)
        {
            return HasFieldTextForLanguage(GetFieldID(fieldName), GetLanguageID(languageName));
        }

        /// <summary>
        /// Looks up a field's localized text for a specified language.
        /// </summary>
        public string GetFieldTextForLanguage(int fieldID, int languageID)
        {
            var field = GetField(fieldID);
            if (field == null) return GetFieldName(fieldID);
            if (field.HasTextForLanguage(languageID)) return field.GetTextForLanguage(languageID);
            var defaultText = field.GetTextForLanguage(0);
            return !string.IsNullOrEmpty(defaultText) ? defaultText : GetFieldName(fieldID);
        }

        /// <summary>
        /// Looks up a field's localized text for a specified language.
        /// </summary>
        public string GetFieldTextForLanguage(int fieldID, string languageName)
        {
            return GetFieldTextForLanguage(fieldID, GetLanguageID(languageName));
        }

        /// <summary>
        /// Looks up a field's localized text for a specified language.
        /// </summary>
        public string GetFieldTextForLanguage(string fieldName, int languageID)
        {
            var field = GetField(fieldName);
            if (field == null) return fieldName;
            if (field.HasTextForLanguage(languageID)) return field.GetTextForLanguage(languageID);
            var defaultText = field.GetTextForLanguage(0);
            return !string.IsNullOrEmpty(defaultText) ? defaultText : fieldName;
        }

        /// <summary>
        /// Looks up a field's localized text for a specified language.
        /// </summary>
        public string GetFieldTextForLanguage(string fieldName, string languageName)
        {
            return GetFieldTextForLanguage(fieldName, GetLanguageID(languageName));
        }

        /// <summary>
        /// Looks up a fields localized text for the current language specified by TextTable.currentLanguageID.
        /// </summary>
        public string GetFieldText(int fieldID)
        {
            return GetFieldTextForLanguage(fieldID, TextTable.currentLanguageID);
        }

        /// <summary>
        /// Looks up a fields localized text for the current language specified by TextTable.currentLanguageID.
        /// </summary>
        public string GetFieldText(string fieldName)
        {
            return GetFieldTextForLanguage(fieldName, TextTable.currentLanguageID);
        }

        /// <summary>
        /// Returns all field IDs in the text table.
        /// </summary>
        /// <returns></returns>
        public int[] GetFieldIDs()
        {
            var ids = new int[fields.Count];
            fields.Keys.CopyTo(ids, 0);
            return ids;
        }

        /// <summary>
        /// Returns all field names in the text table.
        /// </summary>
        /// <returns></returns>
        public string[] GetFieldNames()
        {
            var names = new string[fields.Count];
            int i = 0;
            var enumerator = fields.GetEnumerator();
            while (enumerator.MoveNext())
            {
                names[i++] = (enumerator.Current.Value != null) ? enumerator.Current.Value.fieldName : string.Empty;
            }
            return names;
        }

        /// <summary>
        /// Adds a field to the text table.
        /// </summary>
        public void AddField(string fieldName)
        {
            if (HasField(fieldName)) return;
            fields.Add(m_nextFieldID++, new TextTableField(fieldName));
        }

        /// <summary>
        /// Sets a field's localized text for a specified language.
        /// </summary>
        public void SetFieldTextForLanguage(int fieldID, int languageID, string text)
        {
            if (!HasLanguage(languageID))
            {
                if (Debug.isDebugBuild) Debug.LogWarning("TextTable.SetLanguageText(" + fieldID + ", " + languageID + ", \"" + text + "\") failed: Language doesn't exist. Use Text Table Editor or AddLanguage() to add the language first.", this);
                return;
            }
            var field = GetField(fieldID);
            if (field == null)
            {
                if (Debug.isDebugBuild) Debug.LogWarning("TextTable.SetLanguageText(" + fieldID + ", " + languageID + ", \"" + text + "\") failed: Field doesn't exist. Use Text Table Editor or AddField() to add the field first.", this);
                return;
            }
            field.SetTextForLanguage(languageID, text);
        }

        /// <summary>
        /// Sets a field's localized text for a specified language.
        /// </summary>
        public void SetFieldTextForLanguage(string fieldName, int languageID, string text)
        {
            SetFieldTextForLanguage(GetFieldID(fieldName), languageID, text);
        }

        /// <summary>
        /// Sets a field's localized text for a specified language.
        /// </summary>
        public void SetFieldTextForLanguage(int fieldID, string languageName, string text)
        {
            SetFieldTextForLanguage(fieldID, GetLanguageID(languageName), text);
        }

        /// <summary>
        /// Sets a field's localized text for a specified language.
        /// </summary>
        public void SetFieldTextForLanguage(string fieldName, string languageName, string text)
        {
            SetFieldTextForLanguage(GetFieldID(fieldName), GetLanguageID(languageName), text);
        }

        /// <summary>
        /// Removes a field from the text table.
        /// </summary>
        public void RemoveField(int fieldID)
        {
            fields.Remove(fieldID);
        }

        /// <summary>
        /// Removes a field from the text table.
        /// </summary>
        public void RemoveField(string fieldName)
        {
            fields.Remove(GetFieldID(fieldName));
        }

        /// <summary>
        /// Removes a language from all fields in the table.
        /// </summary>
        private void RemoveLanguageFromFields(int languageID)
        {
            var enumerator = fields.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Value != null) enumerator.Current.Value.RemoveLanguage(languageID);
            }
        }

        #endregion

    }

    /// <summary>
    /// A field in a TextTable.
    /// </summary>
    [Serializable]
    public class TextTableField : ISerializationCallbackReceiver
    {

        [SerializeField]
        private string m_fieldName;

        private Dictionary<int, string> m_texts = new Dictionary<int, string>(); // <languageID, text>

        public string fieldName
        {
            get { return m_fieldName; }
            set { m_fieldName = value; }
        }

        public Dictionary<int, string> texts // <languageID, text>
        {
            get { return m_texts; }
            set { m_texts = value; }
        }

        [SerializeField]
        private List<int> m_keys = new List<int>();

        [SerializeField]
        private List<string> m_values = new List<string>();

        public TextTableField() { }

        public TextTableField(string fieldName)
        {
            this.m_fieldName = fieldName;
        }

        public void OnBeforeSerialize()
        {
            m_keys.Clear();
            m_values.Clear();
            foreach (var kvp in texts)
            {
                m_keys.Add(kvp.Key);
                m_values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            texts = new Dictionary<int, string>();
            for (int i = 0; i != Math.Min(m_keys.Count, m_values.Count); i++)
            {
                texts.Add(m_keys[i], m_values[i]);
            }
        }

        public bool HasTextForLanguage(int languageID)
        {
            return texts.ContainsKey(languageID);
        }

        public string GetTextForLanguage(int languageID)
        {
            return texts.ContainsKey(languageID) ? texts[languageID] : string.Empty;
        }

        public void SetTextForLanguage(int languageID, string text)
        {
            if (texts.ContainsKey(languageID))
            {
                texts[languageID] = text;
            }
            else
            {
                texts.Add(languageID, text);
            }
        }

        public void RemoveLanguage(int languageID)
        {
            texts.Remove(languageID);
        }

    }

}
