// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.DialogueSystem.Demo.Wrappers
{

    /// <summary>
    /// This wrapper class keeps references intact if you switch between the 
    /// compiled assembly and source code versions of the original class.
    /// </summary>
    [HelpURL("http://pixelcrushers.com/dialogue_system/manual/html/navigate_on_mouse_click.html")]
    [AddComponentMenu("Pixel Crushers/Dialogue System/Actor/Demo/Navigate On Mouse Click")]
#if UNITY_5_5_OR_NEWER || UNITY_2017_OR_NEWER
    [RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
#else
    [RequireComponent(typeof(NavMeshAgent))]
#endif
    public class NavigateOnMouseClick : PixelCrushers.DialogueSystem.Demo.NavigateOnMouseClick
    {
    }

}
