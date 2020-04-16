#if TMP_PRESENT
// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem.Wrappers
{

    /// <summary>
    /// This wrapper class keeps references intact if you switch between the 
    /// compiled assembly and source code versions of the original class.
    /// </summary>
    [AddComponentMenu("Pixel Crushers/Dialogue System/UI/TextMesh Pro/Effects/TextMesh Pro Typewriter Effect")]
    [DisallowMultipleComponent]
    public class TextMeshProTypewriterEffect : PixelCrushers.DialogueSystem.TextMeshProTypewriterEffect
    {
    }

}
#endif
