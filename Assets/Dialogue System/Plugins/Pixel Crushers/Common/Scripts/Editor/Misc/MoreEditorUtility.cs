// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;

namespace PixelCrushers
{

    public static class MoreEditorUtility
    {

        /// <summary>
        /// Try to add a symbol to the project's Scripting Define Symbols.
        /// </summary>
        public static void TryAddScriptingDefineSymbols(string newDefine)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!string.IsNullOrEmpty(defines)) defines += ";";
            defines += newDefine;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
        }

        /// <summary>
        /// Checks if a symbol exists in the project's Scripting Define Symbols.
        /// </summary>
        public static bool DoesScriptingDefineSymbolExist(string define)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';');
            for (int i = 0; i < defines.Length; i++)
            {
                if (string.Equals(define, defines[i].Trim())) return true;
            }
            return false;
        }

    }
}
