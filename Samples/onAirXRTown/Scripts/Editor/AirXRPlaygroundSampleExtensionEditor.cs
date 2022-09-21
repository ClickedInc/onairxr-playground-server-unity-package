/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AirXRPlaygroundSampleExtension))]
public class AirXRPlaygroundSampleExtensionEditor : Editor {
    private SerializedProperty _propPlayOnAwake;
    private SerializedProperty _propDirector;
    private SerializedProperty _propNextScene;
    private SerializedProperty _propAddressInEditor;

    private void OnEnable() {
        _propPlayOnAwake = serializedObject.FindProperty("_playOnAwake");
        _propDirector = serializedObject.FindProperty("_director");
        _propNextScene = serializedObject.FindProperty("_nextScene");
        _propAddressInEditor = serializedObject.FindProperty("_addressInEditor");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_propPlayOnAwake);
        EditorGUILayout.PropertyField(_propDirector, new GUIContent("Playable Director"));
        EditorGUILayout.PropertyField(_propNextScene);

        EditorGUILayout.BeginVertical("Box");

        EditorGUILayout.LabelField("Emulate In Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_propAddressInEditor, new GUIContent("Address"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Press the below key to emulate console commands.", Styles.styleImportantNote);
        EditorGUILayout.LabelField("A = Play, S = Stop, D = Pause, N = Next", Styles.styleNote);

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private class Styles {
        public static GUIContent labelOtherPlayer = new GUIContent("Other Player");
        public static GUIStyle styleNote = new GUIStyle(EditorStyles.wordWrappedLabel);
        public static GUIStyle styleImportantNote = new GUIStyle(EditorStyles.wordWrappedLabel);

        static Styles() {
            styleNote.fontStyle = FontStyle.Italic;
            styleImportantNote.fontStyle = FontStyle.BoldAndItalic;
        }
    }
}
