/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using UnityEditor;
using UnityEngine;
using onAirXR.Playground.Server;

namespace onAirXR.Playground.Server.Editor {
    [CustomEditor(typeof(AirXRPlayground))]
    public class AirXRPlaygroundEditor : UnityEditor.Editor {
        private SerializedProperty _propOtherPlayerPrefab;
        private SerializedProperty _propMode;
        private SerializedProperty _propUsingCustomCamera;
        private SerializedProperty _propEmulateParticipants;
        private SerializedProperty _propParticipants;
        private SerializedProperty _propMulticastInEditor;

        private void OnEnable() {
            _propOtherPlayerPrefab = serializedObject.FindProperty("_otherPlayerPrefab");
            _propMode = serializedObject.FindProperty("_mode");
            _propUsingCustomCamera = serializedObject.FindProperty("_usingCustomCamera");
            _propEmulateParticipants = serializedObject.FindProperty("_emulateParticipants");
            _propParticipants = serializedObject.FindProperty("_participants");
            _propMulticastInEditor = serializedObject.FindProperty("_multicastInEditor");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.labelOtherPlayer, GUILayout.Width(102));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(_propOtherPlayerPrefab, GUIContent.none);
            EditorGUILayout.LabelField("If none, LocalPlayer under AirXRPlayground component will be used.", Styles.styleNote);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Emulate In Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_propMode);

            if ((AirXRPlayground.Mode)_propMode.intValue == AirXRPlayground.Mode.Observer) {
                EditorGUILayout.PropertyField(_propUsingCustomCamera);
            }
            EditorGUILayout.PropertyField(_propEmulateParticipants);

            if (_propEmulateParticipants.boolValue) {
                var prevLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80;

                EditorGUILayout.BeginVertical("Box");
                var count = EditorGUILayout.DelayedIntField("Count", _propParticipants.arraySize);
                if (_propParticipants.arraySize != count) {
                    _propParticipants.arraySize = count;
                }

                if (count > 0) {
                    EditorGUILayout.Space();
                }

                for (var index = 0; index < _propParticipants.arraySize; index++) {
                    var element = _propParticipants.GetArrayElementAtIndex(index);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("type"), GUIContent.none, GUILayout.Width(76));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("spot"), GUIContent.none);
                    EditorGUILayout.EndHorizontal();
                }

                if (count > 0) {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("NOTE: DO NOT USE these transforms in your game play. The emulation spots are destroyed in the build. ", Styles.styleImportantNote);
                }
                EditorGUILayout.EndVertical();

                EditorGUIUtility.labelWidth = prevLabelWidth;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_propMulticastInEditor.FindPropertyRelative("address"), Styles.labelMulticastAddress);
            EditorGUILayout.PropertyField(_propMulticastInEditor.FindPropertyRelative("port"), Styles.labelMulticastPort);
            EditorGUILayout.PropertyField(_propMulticastInEditor.FindPropertyRelative("hint"), Styles.labelMulticastHint);

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private class Styles {
            public static GUIContent labelOtherPlayer = new GUIContent("Other Player");
            public static GUIContent labelMulticastAddress = new GUIContent("Multicast Address");
            public static GUIContent labelMulticastPort = new GUIContent("Multicast Port");
            public static GUIContent labelMulticastHint = new GUIContent("Multicast Hint");
            public static GUIStyle styleNote = new GUIStyle(EditorStyles.wordWrappedLabel);
            public static GUIStyle styleImportantNote = new GUIStyle(EditorStyles.wordWrappedLabel);

            static Styles() {
                styleNote.fontStyle = FontStyle.Italic;
                styleImportantNote.fontStyle = FontStyle.BoldAndItalic;
            }
        }
    }
}
