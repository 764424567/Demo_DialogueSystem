#if USE_ARTICY
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace PixelCrushers.DialogueSystem.Articy.Articy_3_1
{

    /// <summary>
    /// This static utility class contains editor-side tools to convert an articy:draft 3.1 XML schema into
    /// a schema-independent ArticyData object.
    /// </summary>
    public static class Articy_3_1_EditorTools
    {

        public static bool IsSchema(string xmlFilename)
        {
            return ArticyEditorTools.FileContainsSchemaId(xmlFilename, "http://www.nevigo.com/schemas/articydraft/3.1/XmlContentExport_FullProject.xsd");
        }

        public static ArticyData LoadArticyDataFromXmlFile(string xmlFilename, Encoding encoding, bool convertDropdownAsString = false, ConverterPrefs prefs = null)
        {
            return Articy_3_1_Tools.ConvertExportToArticyData(LoadExportFromXmlFile(xmlFilename, encoding), convertDropdownAsString, prefs);
        }

        public static ExportType LoadExportFromXmlFile(string xmlFilename, Encoding encoding)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ExportType));
            return xmlSerializer.Deserialize(new StreamReader(xmlFilename, encoding)) as ExportType;
        }

    }

}
#endif