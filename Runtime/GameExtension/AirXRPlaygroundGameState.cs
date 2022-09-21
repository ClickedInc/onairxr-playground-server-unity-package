/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using UnityEngine;
using UnityEngine.Playables;

namespace onAirXR.Playground.Server {
    [Serializable]
    public struct AirXRPlaygroundGameSceneState {
        [Serializable]
        public class SceneField {
            [SerializeField] private UnityEngine.Object _sceneAsset;
            [SerializeField] private string _sceneName = null;

            public string sceneName => _sceneName;

            public static implicit operator string(SceneField sceneField) => sceneField.sceneName;
        }

        public string name;

        public bool isValid => string.IsNullOrEmpty(name) == false;

        public override bool Equals(object obj) {
            if ((obj is AirXRPlaygroundGameSceneState) == false) { return false; }

            var state = (AirXRPlaygroundGameSceneState)obj;
            return name.Equals(state.name);
        }

        public override int GetHashCode() {
            return name.GetHashCode();
        }
    }

    [Serializable]
    public struct AirXRPlaygroundGamePlayableDirectorState {
        public int state;
        public int time;

        public void UpdateDirector(PlayableDirector director) {
            var nextState = (PlayState)state;
            if (director == null) { return; }

            if (director.state != nextState) {
                switch (nextState) {
                    case PlayState.Paused:
                        director.Pause();
                        break;
                    case PlayState.Playing:
                        director.Play();
                        break;
                }
            }

            if (director.state == PlayState.Paused && time != Mathf.RoundToInt((float)director.time * 1000)) {
                director.time = time / 1000.0f;
                director.Evaluate();
            }
        }

        public override bool Equals(object obj) {
            if ((obj is AirXRPlaygroundGamePlayableDirectorState) == false) { return false; }

            var director = (AirXRPlaygroundGamePlayableDirectorState)obj;
            return state == director.state;
        }

        public override int GetHashCode() {
            return state;
        }
    }
}
