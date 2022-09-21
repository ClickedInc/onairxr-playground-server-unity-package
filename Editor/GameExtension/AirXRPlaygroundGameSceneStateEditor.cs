/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using UnityEngine;
using UnityEditor;

namespace onAirXR.Playground.Server {
    [CustomPropertyDrawer(typeof(AirXRPlaygroundGameSceneState.SceneField))]
    public class AirXRPlaygroundGameExtensionSceneFieldPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var sceneAsset = property.FindPropertyRelative("_sceneAsset");
            var sceneName = property.FindPropertyRelative("_sceneName");

            EditorGUI.BeginProperty(position, GUIContent.none, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            if (sceneAsset != null) {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUI.ObjectField(position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);

                if (EditorGUI.EndChangeCheck()) {
                    sceneAsset.objectReferenceValue = value;
                    if (sceneAsset.objectReferenceValue != null) {
                        sceneName.stringValue = (sceneAsset.objectReferenceValue as SceneAsset).name;
                    }
                }
            }
            EditorGUI.EndProperty();
        }
    }
}
