/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using onAirXR.Server;
using onAirXR.Playground.Server;

[Serializable]
public struct AirXRPlaygroundSampleGameState {
    public string content;
    public AirXRPlaygroundGameSceneState scene;
    public AirXRPlaygroundGamePlayableDirectorState director;

    public bool isValid => scene.isValid;

    public override bool Equals(object obj) {
        if ((obj is AirXRPlaygroundSampleGameState) == false) { return false; }
        var state = (AirXRPlaygroundSampleGameState)obj;

        if (isValid == false && state.isValid == false) { return true; }
        else if (isValid != state.isValid) { return false; }

        return content.Equals(state.content) &&
                scene.Equals(state.scene) &&
                director.Equals(state.director);
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }
}

public class AirXRPlaygroundSampleExtension : AirXRPlaygroundGameExtension {
    public interface Delegate {
        bool OnCommandForDirector(AirXRPlaygroundGameExtension extension, string command, string argument);
        bool OnCommandForPlayer(AirXRPlaygroundGameExtension extension, string command, string argument);
    }

    public static Delegate aDelegate { get; set; } // must be set on Awake

    [SerializeField] private bool _playOnAwake = false;
    [SerializeField] private PlayableDirector _director = null;
    [SerializeField] private AirXRPlaygroundGameSceneState.SceneField _nextScene = null;

    private AirXRPlaygroundSampleGameState gameState => new AirXRPlaygroundSampleGameState {
        content = Application.productName,
        scene = new AirXRPlaygroundGameSceneState { name = SceneManager.GetActiveScene().name },
        director = new AirXRPlaygroundGamePlayableDirectorState {
            state = _director != null ? (int)_director.state : (int)PlayState.Paused,
            time = _director != null ? Mathf.RoundToInt((float)_director.time * 1000.0f) : 0
        }
    };

    // implements AirXRPlaygroundGameExtension
    protected override bool Configure(string address) {
        Task.Run(async () => {
            await AXRCameraFade.FadeAllCameras("content", 1, Color.black, Color.clear, 1.5f);
        });

        if (_playOnAwake) {
            _director?.Play();
        }

        return base.Configure(address);
    }

    protected override void OnUpdate() {
        base.OnUpdate();

        if (Application.isEditor) {
            emulateCommandsInEditor();
        }
    }

    protected override string OnEvaluateCurrentGameState() {
        return JsonUtility.ToJson(gameState);
    }

    protected override bool OnCheckIfNeedToLoadScene(string state, ref string nextScene) {
        var current = gameState;
        var next = JsonUtility.FromJson<AirXRPlaygroundSampleGameState>(state);

        if (current.scene.Equals(next.scene) == false) {
            nextScene = next.scene.name;
            return true;
        }
        return false;
    }

    protected override async Task OnPreLoadScene(string nextScene) {
        await AXRCameraFade.FadeAllCameras("content", 1, Color.clear, Color.black, 1.5f);
    }

    protected override Task OnDirectorUpdateGameState(string state) {
        return onUpdateGameState(state);
    }

    protected override Task OnPlayerUpdateGameState(string state) {
        return onUpdateGameState(state);
    }

    protected override void OnDirectorCommand(string command, string argument) {
        if (aDelegate?.OnCommandForDirector(this, command, argument) ?? false) { return; }

        switch (command) {
            case "play":
                if (_director != null) {
                    _director.Play();
                }
                SendUpdateGameState();
                break;
            case "pause":
                if (_director != null) {
                    _director?.Pause();
                }
                SendUpdateGameState();
                break;
            case "stop":
                if (_director != null) {
                    _director?.Stop();
                    _director?.Evaluate();
                }
                SendUpdateGameState();
                break;
            case "next":
                LoadOtherScene(_nextScene);
                break;
        }
    }

    protected override void OnPlayerCommand(string command, string argument) {
        aDelegate?.OnCommandForPlayer(this, command, argument);
    }

    protected override void OnMessageReceived(string clientID, int opcode, string data) {
        Debug.Log($"[onairxr playground] game extension ({clientid}): message received: from = {clientID}, opcode = {opcode}, data = {data}");
    }

    private Task onUpdateGameState(string state) {
        var current = gameState;
        var next = JsonUtility.FromJson<AirXRPlaygroundSampleGameState>(state);

        next.director.UpdateDirector(_director);
        return null;
    }

    private void emulateCommandsInEditor() {
        if (playground.mode != AirXRPlayground.Mode.Observer) { return; }

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.A)) {
            OnDirectorCommand("play", null);
        }
        else if (Input.GetKeyDown(KeyCode.S)) {
            OnDirectorCommand("stop", null);
        }
        else if (Input.GetKeyDown(KeyCode.D)) {
            OnDirectorCommand("pause", null);
        }
        else if (Input.GetKeyDown(KeyCode.N)) {
            OnDirectorCommand("next", null);
        }
#endif
    }
}
