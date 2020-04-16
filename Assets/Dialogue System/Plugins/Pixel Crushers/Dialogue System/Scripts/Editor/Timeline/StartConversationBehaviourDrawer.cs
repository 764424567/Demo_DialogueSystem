#if UNITY_2017_1_OR_NEWER
using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem
{

    [CustomPropertyDrawer(typeof(StartConversationBehaviour))]
    public class StartConversationBehaviourDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int fieldCount = 3;
            return fieldCount * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty conversationProp = property.FindPropertyRelative("conversation");
            SerializedProperty jumpToSpecificEntryProp = property.FindPropertyRelative("jumpToSpecificEntry");
            SerializedProperty entryIDProp = property.FindPropertyRelative("entryID");

            Rect singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(singleFieldRect, conversationProp);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(singleFieldRect, jumpToSpecificEntryProp);

            if (jumpToSpecificEntryProp.boolValue)
            {
                singleFieldRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(singleFieldRect, entryIDProp);
            }
        }
    }
}
#endif
