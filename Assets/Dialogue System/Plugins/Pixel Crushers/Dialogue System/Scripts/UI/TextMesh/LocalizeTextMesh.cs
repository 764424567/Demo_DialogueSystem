using UnityEngine;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// This script localizes the content of the TextMesh element on this 
    /// GameObject. You can assign the localized text table to this script 
    /// or the Dialogue Manager. The element's starting text value serves 
    /// as the field name to look up in the table.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class LocalizeTextMesh : LocalizeUIText
    {

        protected TextMesh m_textMesh;

        public override void LocalizeText()
        {
            if (!started) return;

            // Skip if no language set:
            if (string.IsNullOrEmpty(PixelCrushers.DialogueSystem.Localization.language)) return;
            if (textTable == null)
            {
                textTable = DialogueManager.displaySettings.localizationSettings.textTable;
                if (textTable == null)
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning(DialogueDebug.Prefix + ": No localized text table is assigned to " + name + " or the Dialogue Manager.", this);
                    return;
                }
            }

            if (!HasCurrentLanguage())
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning(DialogueDebug.Prefix + "Localized text table '" + textTable + "' does not have a language '" + PixelCrushers.DialogueSystem.Localization.language + "'", this);
                return;
            }

            // Make sure we have a Text or Dropdown:
            if (m_textMesh == null)
            {
                m_textMesh = GetComponent<TextMesh>();
                if (m_textMesh == null)
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning(DialogueDebug.Prefix + ": LocalizeUILabel didn't find a TextMesh component on " + name + ".", this);
                    return;
                }
            }

            // Get the original values to use as field lookups:
            if (string.IsNullOrEmpty(fieldName)) fieldName = (m_textMesh != null) ? m_textMesh.text : string.Empty;

            // Localize Text:
            if (m_textMesh != null)
            {
                if (!textTable.HasField(fieldName))
                {
                    if (DialogueDebug.logWarnings) Debug.LogWarning(DialogueDebug.Prefix + ": Localized text table '" + textTable.name + "' does not have a field: " + fieldName, this);
                }
                else
                {
                    m_textMesh.text = textTable.GetFieldTextForLanguage(fieldName, Localization.GetCurrentLanguageID(textTable));
                }
            }
        }

        public override void UpdateFieldName(string newFieldName = "")
        {
            if (m_textMesh == null) m_textMesh = GetComponent<TextMesh>();
            fieldName = string.IsNullOrEmpty(newFieldName) ? m_textMesh.text : newFieldName;
        }

    }

}
