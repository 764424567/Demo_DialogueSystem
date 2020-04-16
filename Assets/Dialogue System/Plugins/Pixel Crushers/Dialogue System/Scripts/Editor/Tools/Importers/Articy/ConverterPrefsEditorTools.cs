#if USE_ARTICY
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEditor;
using System.Xml.Serialization;
using System.IO;

namespace PixelCrushers.DialogueSystem.Articy
{

    /// <summary>
    /// This class provides editor tools to manage articy converter prefs. It allows the converter to save
    /// prefs to EditorPrefs between sessions.
    /// </summary>
    public static class ConverterPrefsTools
    {

        private const string ArticyProjectFilenameKey = "PixelCrushers.DialogueSystem.ArticyProjectFilename";
        private const string ArticyPortraitFolderKey = "PixelCrushers.DialogueSystem.ArticyPortraitFolder";
        private const string ArticyStageDirectionsAreSequencesKey = "PixelCrushers.DialogueSystem.StageDirectionsAreSequences";
        private const string ArticyFlowFragmentModeKey = "PixelCrushers.DialogueSystem.FlowFragmentMode";
        private const string ArticyDocumentsSubmenuKey = "PixelCrushers.DialogueSystem.ArticyDocumentsSubmenu";
        private const string ArticyTextTableDocumentKey = "PixelCrushers.DialogueSystem.ArticyTextTableDocument";
        private const string ArticyOutputFolderKey = "PixelCrushers.DialogueSystem.ArticyOutput";
        private const string ArticyOverwriteKey = "PixelCrushers.DialogueSystem.ArticyOverwrite";
        private const string ArticyConversionSettingsKey = "PixelCrushers.DialogueSystem.ArticyConversionSettings";
        private const string ArticyEncodingKey = "PixelCrushers.DialogueSystem.ArticyEncoding";
        private const string ArticyRecursionKey = "PixelCrushers.DialogueSystem.ArticyRecursion";
        private const string ArticyDropdownsKey = "PixelCrushers.DialogueSystem.ArticyDropdowns";
        private const string ArticySlotsKey = "PixelCrushers.DialogueSystem.ArticySlots";
        private const string ArticyDirectConversationLinksToEntry1 = "PixelCrushers.DialogueSystem.DirectConversationLinksToEntry1";
        private const string ArticyConvertMarkupToRichText = "PixelCrushers.DialogueSystem.ArticyConvertMarkupToRichText";
        private const string ArticyFlowFragmentScriptKey = "PixelCrushers.DialogueSystem.ArticyFlowFragmentScript";
        private const string ArticyVoiceOverPropertyKey = "PixelCrushers.DialogueSystem.ArticyVoiceOverPropertyKey";
        private const string ArticyLocalizationXlsKey = "PixelCrushers.DialogueSystem.ArticyLocalizationXlsxKey";
        private const string ArticyEmVarsKey = "PixelCrushers.DialogueSystem.ArticyEmVars";

        public static ConverterPrefs Load()
        {
            var converterPrefs = new ConverterPrefs();
            converterPrefs.ProjectFilename = EditorPrefs.GetString(ArticyProjectFilenameKey);
            converterPrefs.PortraitFolder = EditorPrefs.GetString(ArticyPortraitFolderKey);
            converterPrefs.StageDirectionsAreSequences = EditorPrefs.HasKey(ArticyStageDirectionsAreSequencesKey) ? EditorPrefs.GetBool(ArticyStageDirectionsAreSequencesKey) : true;
            converterPrefs.FlowFragmentMode = (ConverterPrefs.FlowFragmentModes)(EditorPrefs.HasKey(ArticyFlowFragmentModeKey) ? EditorPrefs.GetInt(ArticyFlowFragmentModeKey) : 0);
            converterPrefs.DocumentsSubmenu = EditorPrefs.GetString(ArticyDocumentsSubmenuKey);
            converterPrefs.TextTableDocument = EditorPrefs.GetString(ArticyTextTableDocumentKey);
            converterPrefs.OutputFolder = EditorPrefs.GetString(ArticyOutputFolderKey, "Assets");
            converterPrefs.Overwrite = EditorPrefs.GetBool(ArticyOverwriteKey, false);
            converterPrefs.ConversionSettings = ConversionSettings.FromXml(EditorPrefs.GetString(ArticyConversionSettingsKey));
            converterPrefs.EncodingType = EditorPrefs.HasKey(ArticyEncodingKey) ? (EncodingType)EditorPrefs.GetInt(ArticyEncodingKey) : EncodingType.Default;
            converterPrefs.RecursionMode = EditorPrefs.HasKey(ArticyRecursionKey) ? (ConverterPrefs.RecursionModes)EditorPrefs.GetInt(ArticyRecursionKey) : ConverterPrefs.RecursionModes.On;
            converterPrefs.ConvertDropdownsAs = EditorPrefs.HasKey(ArticyDropdownsKey) ? (ConverterPrefs.ConvertDropdownsModes)EditorPrefs.GetInt(ArticyDropdownsKey) : ConverterPrefs.ConvertDropdownsModes.Ints;
            converterPrefs.ConvertSlotsAs = EditorPrefs.HasKey(ArticySlotsKey) ? (ConverterPrefs.ConvertSlotsModes)EditorPrefs.GetInt(ArticySlotsKey) : ConverterPrefs.ConvertSlotsModes.DisplayName;
            converterPrefs.DirectConversationLinksToEntry1 = EditorPrefs.GetBool(ArticyDirectConversationLinksToEntry1, false);
            converterPrefs.ConvertMarkupToRichText = EditorPrefs.GetBool(ArticyConvertMarkupToRichText, false);
            converterPrefs.FlowFragmentScript = EditorPrefs.GetString(ArticyFlowFragmentScriptKey, ConverterPrefs.DefaultFlowFragmentScript);
            converterPrefs.VoiceOverProperty = EditorPrefs.GetString(ArticyVoiceOverPropertyKey, ConverterPrefs.DefaultVoiceOverProperty);
            converterPrefs.LocalizationXlsx = EditorPrefs.GetString(ArticyLocalizationXlsKey);
            converterPrefs.emVarSet = ArticyEmVarSetFromXML(EditorPrefs.GetString(ArticyEmVarsKey));
            return converterPrefs;
        }

        public static void Save(ConverterPrefs converterPrefs)
        {
            EditorPrefs.SetString(ArticyProjectFilenameKey, converterPrefs.ProjectFilename);
            EditorPrefs.SetString(ArticyPortraitFolderKey, converterPrefs.PortraitFolder);
            EditorPrefs.SetBool(ArticyStageDirectionsAreSequencesKey, converterPrefs.StageDirectionsAreSequences);
            EditorPrefs.SetInt(ArticyFlowFragmentModeKey, (int)converterPrefs.FlowFragmentMode);
            EditorPrefs.SetString(ArticyDocumentsSubmenuKey, converterPrefs.DocumentsSubmenu);
            EditorPrefs.SetString(ArticyTextTableDocumentKey, converterPrefs.TextTableDocument);
            EditorPrefs.SetString(ArticyOutputFolderKey, converterPrefs.OutputFolder);
            EditorPrefs.SetBool(ArticyOverwriteKey, converterPrefs.Overwrite);
            EditorPrefs.SetString(ArticyConversionSettingsKey, converterPrefs.ConversionSettings.ToXml());
            EditorPrefs.SetInt(ArticyEncodingKey, (int)converterPrefs.EncodingType);
            EditorPrefs.SetInt(ArticyRecursionKey, (int)converterPrefs.RecursionMode);
            EditorPrefs.SetInt(ArticyDropdownsKey, (int)converterPrefs.ConvertDropdownsAs);
            EditorPrefs.SetInt(ArticySlotsKey, (int)converterPrefs.ConvertSlotsAs);
            EditorPrefs.SetBool(ArticyDirectConversationLinksToEntry1, converterPrefs.DirectConversationLinksToEntry1);
            EditorPrefs.SetBool(ArticyConvertMarkupToRichText, converterPrefs.ConvertMarkupToRichText);
            EditorPrefs.SetString(ArticyFlowFragmentScriptKey, converterPrefs.FlowFragmentScript);
            EditorPrefs.SetString(ArticyVoiceOverPropertyKey, converterPrefs.VoiceOverProperty);
            EditorPrefs.SetString(ArticyLocalizationXlsKey, converterPrefs.LocalizationXlsx);
            EditorPrefs.SetString(ArticyEmVarsKey, ArticyEmVarSetToXML(converterPrefs.emVarSet));
        }

        public static void DeleteEditorPrefs()
        {
            EditorPrefs.DeleteKey(ArticyProjectFilenameKey);
            EditorPrefs.DeleteKey(ArticyPortraitFolderKey);
            EditorPrefs.DeleteKey(ArticyStageDirectionsAreSequencesKey);
            EditorPrefs.DeleteKey(ArticyFlowFragmentModeKey);
            EditorPrefs.DeleteKey(ArticyDocumentsSubmenuKey);
            EditorPrefs.DeleteKey(ArticyTextTableDocumentKey);
            EditorPrefs.DeleteKey(ArticyOutputFolderKey);
            EditorPrefs.DeleteKey(ArticyOverwriteKey);
            EditorPrefs.DeleteKey(ArticyConversionSettingsKey);
            EditorPrefs.DeleteKey(ArticyEncodingKey);
            EditorPrefs.DeleteKey(ArticyRecursionKey);
            EditorPrefs.DeleteKey(ArticyDropdownsKey);
            EditorPrefs.DeleteKey(ArticySlotsKey);
            EditorPrefs.DeleteKey(ArticyDirectConversationLinksToEntry1);
            EditorPrefs.DeleteKey(ArticyConvertMarkupToRichText);
            EditorPrefs.DeleteKey(ArticyFlowFragmentScriptKey);
            EditorPrefs.DeleteKey(ArticyVoiceOverPropertyKey);
            EditorPrefs.DeleteKey(ArticyLocalizationXlsKey);
            EditorPrefs.DeleteKey(ArticyEmVarsKey);
        }

        private static ArticyEmVarSet ArticyEmVarSetFromXML(string xml)
        {
            ArticyEmVarSet emVarSet = null;
            if (!string.IsNullOrEmpty(xml))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ArticyEmVarSet));
                emVarSet = xmlSerializer.Deserialize(new StringReader(xml)) as ArticyEmVarSet;
            }
            return (emVarSet != null) ? emVarSet : new ArticyEmVarSet();
        }

        private static string ArticyEmVarSetToXML(ArticyEmVarSet emVarSet)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ArticyEmVarSet));
            StringWriter writer = new StringWriter();
            xmlSerializer.Serialize(writer, emVarSet);
            return writer.ToString();
        }

    }

}
#endif
