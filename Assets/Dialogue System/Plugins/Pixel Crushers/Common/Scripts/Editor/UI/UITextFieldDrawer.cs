// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;

namespace PixelCrushers
{

    [CustomPropertyDrawer(typeof(UITextField), true)]
    public class UITextFieldDrawer : PropertyDrawer
    {

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var uiTextProperty = property.FindPropertyRelative("m_uiText");
            var textMeshProUGUIProperty = property.FindPropertyRelative("m_textMeshProUGUI");
            var isUiTextAssigned = (uiTextProperty != null) && (uiTextProperty.objectReferenceValue != null);
            var isTextMeshProUGUIAssigned = (textMeshProUGUIProperty != null) && (textMeshProUGUIProperty.objectReferenceValue != null);
            var isContentAssigned = (isUiTextAssigned && !isTextMeshProUGUIAssigned) || (!isUiTextAssigned && isTextMeshProUGUIAssigned) || (textMeshProUGUIProperty == null);
            return (isContentAssigned ? 1 : 2) * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            try
            {
                EditorGUI.BeginProperty(position, label, property);

                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                var uiTextProperty = property.FindPropertyRelative("m_uiText");
                var textMeshProUGUIProperty = property.FindPropertyRelative("m_textMeshProUGUI");
                if (uiTextProperty == null)
                {
                    Debug.LogError("Sorry! There was an internal editor error with a UI Text Field. Please contact Pixel Crushers for support.");
                    return;
                }
                var isUiTextAssigned = (uiTextProperty != null) && (uiTextProperty.objectReferenceValue != null);
                var isTextMeshProUGUIAssigned = (textMeshProUGUIProperty != null) && (textMeshProUGUIProperty.objectReferenceValue != null);
                var isContentAssigned = (isUiTextAssigned && !isTextMeshProUGUIAssigned) || (!isUiTextAssigned && isTextMeshProUGUIAssigned);

                float yOffset = 0;

                if (isUiTextAssigned|| !isContentAssigned)
                {
                    EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), uiTextProperty, GUIContent.none);
                    yOffset += EditorGUIUtility.singleLineHeight;
                }

                if (isTextMeshProUGUIAssigned || (textMeshProUGUIProperty != null && !isContentAssigned))
                {
                    EditorGUI.PropertyField(new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight), textMeshProUGUIProperty, GUIContent.none);
                }
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }

    }
}
