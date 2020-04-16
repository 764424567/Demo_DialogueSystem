// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;

namespace PixelCrushers.DialogueSystem.DialogueEditor
{

    /// <summary>
    /// This part of the Dialogue Editor window handles the Database tab's Localization foldout.
    /// Credits: This section is based on code contributed by FourAttic / Devolver Digital. Thank you!
    /// </summary>
    public partial class DialogueEditorWindow
    {

        #region Database Tab > Localization Foldout Variables

        [Serializable]
        private class LocalizationLanguages
        {
            public List<string> languages = new List<string>();
            public string outputFolder;
        }

        [SerializeField]
        private LocalizationLanguages localizationLanguages = new LocalizationLanguages();

        private ReorderableList exportLanguageList = null;

        #endregion

        #region Draw Localization Foldout Section

        private void DrawLocalizationSection()
        {
            EditorWindowTools.StartIndentedSection();
            if (exportLanguageList == null)
            {
                exportLanguageList = new ReorderableList(localizationLanguages.languages, typeof(string), true, true, true, true);
                exportLanguageList.drawHeaderCallback += OnDrawExportLanguageListHeader;
                exportLanguageList.drawElementCallback = OnDrawExportLanguageListElement;
                exportLanguageList.onAddCallback += OnAddExportLanguageListElement;
            }
            exportLanguageList.DoLayoutList();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Find Languages", GUILayout.Width(120)))
            {
                FindLanguagesForLocalizationExportImport();
            }
            if (GUILayout.Button("Export...", GUILayout.Width(100)))
            {
                var newOutputFolder = EditorUtility.OpenFolderPanel("Export Localization Files", localizationLanguages.outputFolder, string.Empty);
                if (!string.IsNullOrEmpty(newOutputFolder))
                {
                    localizationLanguages.outputFolder = newOutputFolder;
                    ExportLocalizationFiles();
                }
            }
            if (GUILayout.Button("Import...", GUILayout.Width(100)))
            {
                var newOutputFolder = EditorUtility.OpenFolderPanel("Import Localization Files", localizationLanguages.outputFolder, string.Empty);
                if (!string.IsNullOrEmpty(newOutputFolder))
                {
                    localizationLanguages.outputFolder = newOutputFolder;
                    ImportLocalizationFiles();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorWindowTools.EndIndentedSection();
        }

        private void OnDrawExportLanguageListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Languages");
        }

        private void OnDrawExportLanguageListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (!(0 <= index && index < localizationLanguages.languages.Count)) return;
            localizationLanguages.languages[index] = EditorGUI.TextField(rect, localizationLanguages.languages[index]);
        }

        private void OnAddExportLanguageListElement(ReorderableList list)
        {
            localizationLanguages.languages.Add(string.Empty);
        }

        private void FindLanguagesForLocalizationExportImport()
        {
            if (database == null) return;
            EditorUtility.DisplayProgressBar("Finding Languages", "Scanning database. Please wait...", 0);
            try
            {
                // database.items.ForEach(item => FindLanguagesInFields(item.fields, true));
                database.conversations.ForEach(conversation => conversation.dialogueEntries.ForEach(entry => FindLanguagesInFields(entry.fields, false)));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void FindLanguagesInFields(List<Field> fields, bool isQuest)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field.type == FieldType.Localization && !string.IsNullOrEmpty(field.title))
                {
                    // Don't add Dialogue Text:
                    if (field.title.Equals("Dialogue Text")) break;

                    // Assume it's Chat Mapper-style localized dialogue text, in which case
                    // the language is the entire field title:
                    string language = field.title;

                    // If it's a different type of field, remove the prefix:
                    for (int j = 0; j < LanguageFieldPrefixes.Length; j++)
                    {
                        var prefix = LanguageFieldPrefixes[j];
                        if (field.title.StartsWith(prefix))
                        {
                            language = string.Empty; // Only want base language fields.
                        }
                        else if (isQuest)
                        {
                            // Handle "Entry X Language":
                            Match match = Regex.Match(field.title, @"Entry [0-9]+ .*");
                            if (match.Success)
                            {
                                language = string.Empty; // Only want base language fields.
                            }
                        }
                    }
                    if (!(string.IsNullOrEmpty(language) || localizationLanguages.languages.Contains(language)))
                    {
                        localizationLanguages.languages.Add(language);
                    }
                }
            }
        }

        #endregion

        #region Export Section

        private void ExportLocalizationFiles()
        {
            try
            {
                var numLanguages = localizationLanguages.languages.Count;
                for (int i = 0; i < numLanguages; i++)
                {
                    var progress = (float)i / (float)numLanguages;
                    var language = localizationLanguages.languages[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Exporting Localization CSV", "Exporting CSV files for " + language, progress))
                    {
                        return;
                    }

                    // Write Dialogue_LN.csv file:
                    var filename = localizationLanguages.outputFolder + "/Dialogue_" + language + ".csv";
                    using (var file = new StreamWriter(filename, false, new UTF8Encoding(true)))
                    {
                        file.WriteLine(language);
                        var orderedFields = new string[] { "Dialogue Text", language, "Menu Text", "Menu Text " + language, "Description" };
                        file.WriteLine("{0},{1},{2},{3},{4},{5},{6}",
                            "Conversation ID",
                            "Entry ID",
                            "Original Text",
                            "Translated Text [" + language + "]",
                            "Original Menu",
                            "Translated Menu [" + language + "]",
                            "Description");
                        foreach (var c in database.conversations)
                        {
                            foreach (var de in c.dialogueEntries)
                            {
                                var fields = new List<string>();
                                foreach (string s in orderedFields)
                                {
                                    var f = de.fields.Find(x => x.title == s);
                                    fields.Add((f != null) ? f.value : string.Empty);
                                }
                                var sb = new StringBuilder();
                                sb.AppendFormat("{0},{1}", c.id, de.id);
                                foreach (string value in fields)
                                    sb.AppendFormat(",{0}", WrapCSVValue(value));
                                file.WriteLine(sb.ToString());
                            }
                        }
                        file.Close();
                    }

                    // Write Quests_LN.csv file:
                    int numQuests = 0;
                    int maxEntryCount = 0;
                    foreach (var item in database.items)
                    {
                        if (!item.IsItem)
                        {
                            numQuests++;
                            maxEntryCount = Mathf.Max(maxEntryCount, item.LookupInt("Entry Count"));
                        }
                    }
                    if (numQuests > 0)
                    {
                        filename = localizationLanguages.outputFolder + "/Quests_" + language + ".csv";
                        using (var file = new StreamWriter(filename, false, new UTF8Encoding(true)))
                        {
                            file.WriteLine(language);
                            var sb = new StringBuilder();
                            sb.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                                "Name",
                                "Display Name",
                                "Translated Display Name [" + language + "]",
                                "Group",
                                "Translated Group [" + language + "]",
                                "Description",
                                "Translated Description [" + language + "]",
                                "Success Description",
                                "Translated Success Description [" + language + "]",
                                "Failure Description",
                                "Translated Failure Description [" + language + "]");
                            for (int j = 0; j < maxEntryCount; j++)
                            {
                                sb.AppendFormat(",{0},{1}",
                                    "Entry " + (j + 1),
                                    "Translated Entry [" + (j + 1) + "]");
                            }
                            file.WriteLine(sb.ToString());
                            foreach (var item in database.items)
                            {
                                if (item.IsItem) continue;
                                var quest = item;
                                sb = new StringBuilder();
                                sb.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                                    WrapCSVValue(quest.Name),
                                    WrapCSVValue(quest.LookupValue("Display Name")),
                                    WrapCSVValue(quest.LookupValue("Display Name " + language)),
                                    WrapCSVValue(quest.LookupValue("Group")),
                                    WrapCSVValue(quest.LookupValue("Group " + language)),
                                    WrapCSVValue(quest.LookupValue("Description")),
                                    WrapCSVValue(quest.LookupValue("Description " + language)),
                                    WrapCSVValue(quest.LookupValue("Success Description")),
                                    WrapCSVValue(quest.LookupValue("Success Description " + language)),
                                    WrapCSVValue(quest.LookupValue("Failure Description")),
                                    WrapCSVValue(quest.LookupValue("Failure Description " + language)));
                                var entryCount = quest.LookupInt("Entry Count");
                                for (int j = 0; j < maxEntryCount; j++)
                                {
                                    if (j < entryCount)
                                    {
                                        sb.AppendFormat(",{0},{1}",
                                            WrapCSVValue(quest.LookupValue("Entry " + (j + 1))),
                                            WrapCSVValue(quest.LookupValue("Entry " + (j + 1) + " " + language)));
                                    }
                                    else
                                    {
                                        sb.Append(",,");
                                    }
                                }
                                file.WriteLine(sb.ToString());
                            }
                            file.Close();
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            EditorUtility.DisplayDialog("Exported Localization CSV", "Localization files are in " + localizationLanguages.outputFolder + ". Open these files in a spreadsheet application and add translations. Then import them by using the Import... button.", "OK");
        }

        string WrapCSVValue(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            string s2 = s.Contains("\n") ? s.Replace("\n", "\\n") : s;
            if (s2.Contains("\r")) s2 = s2.Replace("\r", "\\r");
            if (s2.Contains(",") || s2.Contains("\""))
            {
                return "\"" + s2.Replace("\"", "\"\"") + "\"";
            }
            else
            {
                return s2;
            }
        }

        #endregion

        #region Import Section

        private void ImportLocalizationFiles()
        {
            try
            {
                var numLanguages = localizationLanguages.languages.Count;
                for (int i = 0; i < numLanguages; i++)
                {
                    var progress = (float)i / (float)numLanguages;
                    var language = localizationLanguages.languages[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Importing Localization CSV", "Importing CSV files for " + language, progress))
                    {
                        return;
                    }

                    // Read dialogue CSV file:
                    var filename = localizationLanguages.outputFolder + "/Dialogue_" + language + ".csv";
                    var lines = ReadCSV(filename);
                    CombineMultilineCSVSourceLines(lines);
                    for (int j = 2; j < lines.Count; j++)
                    {
                        var columns = GetCSVColumnsFromLine(lines[j]);
                        if (columns.Length < 6)
                        {
                            Debug.LogError(filename + ":" + (j + 1) + " Invalid line: " + lines[j]);
                        }
                        else
                        {
                            var conversationID = Tools.StringToInt(columns[0]);
                            var entryID = Tools.StringToInt(columns[1]);
                            var entry = database.GetDialogueEntry(conversationID, entryID);
                            if (entry == null)
                            {
                                Debug.LogError(filename + ":" + (j + 1) + " Database doesn't contain conversation " + conversationID + " dialogue entry " + entryID);
                            }
                            else
                            {
                                Field.SetValue(entry.fields, language, columns[3], FieldType.Localization);
                                Field.SetValue(entry.fields, "Menu Text " + language, columns[5], FieldType.Localization);
                            }
                        }
                    }

                    // Read quests CSV file:
                    filename = localizationLanguages.outputFolder + "/Quests_" + language + ".csv";
                    if (File.Exists(filename))
                    {
                        lines = ReadCSV(filename);
                        CombineMultilineCSVSourceLines(lines);
                        for (int j = 2; j < lines.Count; j++)
                        {
                            var columns = GetCSVColumnsFromLine(lines[j]);
                            if (columns.Length < 11)
                            {
                                Debug.LogError(filename + ":" + (j + 1) + " Invalid line: " + lines[j]);
                            }
                            else
                            {
                                var quest = database.GetItem(columns[0]);
                                if (quest == null)
                                {

                                }
                                else
                                {
                                    var displayName = columns[2];
                                    if (!string.IsNullOrEmpty(displayName) && !quest.FieldExists("Display Name")) Field.SetValue(quest.fields, "Display Name", string.Empty);
                                    if (quest.FieldExists("Display Name")) Field.SetValue(quest.fields, "Display Name", displayName, FieldType.Localization);
                                    var group = columns[4];
                                    if (!string.IsNullOrEmpty(group) && !quest.FieldExists("Group")) Field.SetValue(quest.fields, "Group", string.Empty);
                                    if (quest.FieldExists("Group")) Field.SetValue(quest.fields, "Group", group, FieldType.Localization);
                                    Field.SetValue(quest.fields, "Description " + language, columns[6], FieldType.Localization);
                                    Field.SetValue(quest.fields, "Success Description " + language, columns[8], FieldType.Localization);
                                    Field.SetValue(quest.fields, "Failure Description " + language, columns[10], FieldType.Localization);
                                    var entryCount = quest.LookupInt("Entry Count");
                                    for (int k = 0; k < entryCount; k++)
                                    {
                                        Field.SetValue(quest.fields, "Entry " + (k + 1) + " " + language, columns[12 + 2 * k], FieldType.Localization);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            EditorUtility.DisplayDialog("Imported Localization CSV", "The CSV files have been imported back into your dialogue database.", "OK");
        }

        private string[] GetCSVColumnsFromLine(string line)
        {
            Regex csvSplit = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)");
            List<string> values = new List<string>();
            foreach (Match match in csvSplit.Matches(line))
            {
                values.Add(UnwrapCSVValue(match.Value.TrimStart(',')));
            }
            return values.ToArray();
        }

        private List<string> ReadCSV(string filename)
        {
            var lines = new List<string>();
            StreamReader sr = new StreamReader(filename, new UTF8Encoding(true));
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                lines.Add(line.TrimEnd());
            }
            sr.Close();
            return lines;
        }

        /// <summary>
        /// Combines lines that are actually a multiline CSV row. This also helps prevent the 
        /// CSV-splitting regex from hanging due to catastrophic backtracking on unterminated quotes.
        /// </summary>
        private void CombineMultilineCSVSourceLines(List<string> sourceLines)
        {
            int lineNum = 0;
            int safeguard = 0;
            int MaxIterations = 999999;
            while ((lineNum < sourceLines.Count) && (safeguard < MaxIterations))
            {
                safeguard++;
                string line = sourceLines[lineNum];
                if (line == null)
                {
                    sourceLines.RemoveAt(lineNum);
                }
                else
                {
                    bool terminated = true;
                    char previousChar = (char)0;
                    for (int i = 0; i < line.Length; i++)
                    {
                        char currentChar = line[i];
                        bool isQuote = (currentChar == '"') && (previousChar != '\\');
                        if (isQuote) terminated = !terminated;
                        previousChar = currentChar;
                    }
                    if (terminated || (lineNum + 1) >= sourceLines.Count)
                    {
                        if (!terminated) sourceLines[lineNum] = line + '"';
                        lineNum++;
                    }
                    else
                    {
                        sourceLines[lineNum] = line + "\\n" + sourceLines[lineNum + 1];
                        sourceLines.RemoveAt(lineNum + 1);
                    }
                }
            }
        }

        string UnwrapCSVValue(string s)
        {
            string s2 = s.Replace("\\n", "\n").Replace("\\r", "\r");
            if (s2.StartsWith("\"") && s2.EndsWith("\""))
            {
                s2 = s2.Substring(1, s2.Length - 2).Replace("\"\"", "\"");
            }
            return s2;
        }

        #endregion

    }

}