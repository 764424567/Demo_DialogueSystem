#if USE_ARTICY
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem.Articy
{

    /// <summary>
    /// Implements articy:expresso functions.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class ArticyLuaFunctions : MonoBehaviour
    {
        private static bool s_registered = false;


        private void OnEnable()
        {
            if (s_registered) return;
            s_registered = true;
            Lua.RegisterFunction("getObj", this, SymbolExtensions.GetMethodInfo(() => getObj(string.Empty)));
            Lua.RegisterFunction("getObject", this, SymbolExtensions.GetMethodInfo(() => getObj(string.Empty)));
            Lua.RegisterFunction("getProp", this, SymbolExtensions.GetMethodInfo(() => getProp(string.Empty, string.Empty)));
            Lua.RegisterFunction("setProp", this, SymbolExtensions.GetMethodInfo(() => setProp(string.Empty, string.Empty, default(object))));
        }

        private void OnConversationLine(Subtitle subtitle)
        {
            var self = "\"Actor[\\\"" + DialogueLua.StringToTableIndex(subtitle.speakerInfo.nameInDatabase) + "\\\"]\"";
            Lua.Run("speaker = " + self + "; self = " + self, DialogueDebug.logInfo);
        }

        public static string getObj(string objectName)
        {
            var db = DialogueManager.MasterDatabase;
            var actor = db.actors.Find(x => string.Equals(objectName, x.Name) || string.Equals(objectName, x.LookupValue("Technical Name")) || string.Equals(objectName, x.LookupValue("Articy Id")));
            if (actor != null) return "Actor[\"" + DialogueLua.StringToTableIndex(actor.Name) + "\"]";
            var item = db.items.Find(x => string.Equals(objectName, x.Name) || string.Equals(objectName, x.LookupValue("Technical Name")) || string.Equals(objectName, x.LookupValue("Articy Id")));
            if (item!= null) return "Item[\"" + DialogueLua.StringToTableIndex(item.Name) + "\"]";
            var location = db.locations.Find(x => string.Equals(objectName, x.Name) || string.Equals(objectName, x.LookupValue("Technical Name")) || string.Equals(objectName, x.LookupValue("Articy Id")));
            if (location!= null) return "Location[\"" + DialogueLua.StringToTableIndex(location.Name) + "\"]";
            var conversation = db.conversations.Find(x => string.Equals(objectName, x.Title) || string.Equals(objectName, x.LookupValue("Technical Name")) || string.Equals(objectName, x.LookupValue("Articy Id")));
            if (conversation != null) return "Conversation[\"" + conversation.id + "\"]";
            return null;
        }

        public static object getProp(string objectIdentifier, string propertyName)
        {
            var result = Lua.Run("return " + objectIdentifier + "." + DialogueLua.StringToTableIndex(propertyName), DialogueDebug.logInfo);
            if (result.isBool)
            {
                return result.asBool;
            }
            else if (result.isNumber)
            {
                return result.asInt;
            }
            else
            {
                return result.asString;
            }
        }

        public static void setProp(string objectIdentifier, string propertyName, object value)
        {
            string rightSide;
            if (value == null)
            {
                rightSide = "nil";
            }
            else if (value.GetType() == typeof(string))
            {
                rightSide = "\"" + value.ToString() + "\"";
            }
            else if (value.GetType() == typeof(bool))
            {
                rightSide = value.ToString().ToLower();
            }
            else
            {
                rightSide = value.ToString();
            }
            Lua.Run(objectIdentifier + "." + propertyName + " = " + rightSide, DialogueDebug.logInfo);
        }
    }
}
#endif
