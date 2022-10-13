/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using onAirXR.Server;

namespace onAirXR.Playground.Server {
    [ExecuteInEditMode]
    public class AirXRPlayground : MonoBehaviour {
        public interface Delegate {
            void OnJoinParticipant(AirXRPlayground playground, AirXRPlaygroundParticipant participant);
            void OnLeaveParticipant(AirXRPlayground playground, AirXRPlaygroundParticipant participant);
            void OnPendParticipantDataPerFrame(AirXRPlayground playground, AXRMulticastManager manager, AirXRPlaygroundLocalPlayer player);
            void OnGetParticipantDataPerFrame(AirXRPlayground playground, AXRMulticastManager manager, AirXRPlaygroundParticipant participant, string member);
        }

        public enum Mode {
            Player,
            Observer
        }

        public static Delegate aDelegate { get; set; }

        private AirXRPlaygroundLocalPlayer _localPlayer;
        private AirXRPlaygroundController _controller;
        private Camera _camera;
        private AirXRPlaygroundEmulator _emulator;

        [SerializeField] private Mode _mode = Mode.Observer;
        [SerializeField] private AirXRPlaygroundParticipant _otherPlayerPrefab = null;
        [SerializeField] private bool _usingCustomCamera = false;
        [SerializeField] private bool _emulateParticipants = false;
        [SerializeField] private AirXRPlaygroundEmulator.ParticipantSpot[] _participants = null;
        [SerializeField]
        private AirXRPlaygroundConfigMulticast _multicastInEditor = new AirXRPlaygroundConfigMulticast {
            address = "239.18.0.1",
            port = 1888,
            hint = "192.168.1.0/24"
        };

        public Mode mode => AirXRPlaygroundConfig.config.GetMode();
        public bool usingCustomCamera => Application.isEditor && _mode == Mode.Observer ? _usingCustomCamera : false;
        public AirXRPlaygroundParticipant otherPlayerPrefab => _otherPlayerPrefab ?? _localPlayer;
        public AirXRPlaygroundExtension extension => GetComponent<AirXRPlaygroundExtension>();

        public List<AirXRPlaygroundParticipant> GetParticipants() {
            var result = new List<AirXRPlaygroundParticipant>();
            foreach (var participant in _controller.participants.Values) {
                if (participant == null) { continue; }

                result.Add(participant);
            }

            if (_controller is AirXRPlaygroundPlayerController) {
                var controller = _controller as AirXRPlaygroundPlayerController;
                if (controller.player.activated) {
                    result.Add(controller.player);
                }
            }
            return result;
        }

        private void Awake() {
            ensureGameObjectIntegrity(false);
            if (Application.isPlaying == false) { return; }

            AirXRPlaygroundConfig.LoadOnce(_mode, _multicastInEditor);
        }

        private async void Start() {
            if (Application.isPlaying == false) {
                ensureGameObjectIntegrity(true);
                return;
            }

            switch (mode) {
                case Mode.Player:
                    _controller = new AirXRPlaygroundPlayerController(this, _localPlayer, _camera);
                    break;
                case Mode.Observer:
                    _controller = new AirXRPlaygroundObserverController(this, _localPlayer, _camera);
                    break;
                default:
                    Assert.IsTrue(false);
                    break;
            }

            _controller.Init();

            if (Application.isEditor && _emulateParticipants) {
                _emulator = new AirXRPlaygroundEmulator(this, _controller, otherPlayerPrefab, _participants);
            }
            else if (_emulateParticipants && _participants != null) {
                foreach (var participant in _participants) {
                    if (participant.spot == null) { continue; }

                    Destroy(participant.spot.gameObject);
                }
            }

            _otherPlayerPrefab?.gameObject?.SetActive(false);

            await Task.Yield();

            _controller.InitAfterUpdate();
        }

        private void Update() {
            if (Application.isPlaying == false) {
                ensureGameObjectIntegrity(true);
                return;
            }
        }

        private void OnDestroy() {
            _controller?.Cleanup();
        }

        private void ensureGameObjectIntegrity(bool create) {
            if (_localPlayer == null) {
                var xform = AXRUtils.GetChildTransform(transform, "LocalPlayer", create);
                if (xform) {
                    _localPlayer = AXRUtils.GetComponent<AirXRPlaygroundLocalPlayer>(xform.gameObject, true);
                }
            }
            if (_camera == null) {
                var xform = AXRUtils.GetChildTransform(transform, "Camera", new Vector3(3.0f, 2.0f, -3.0f), Quaternion.Euler(20.0f, -45.0f, 0), create);
                if (xform) {
                    _camera = AXRUtils.GetComponent<Camera>(xform.gameObject, true);
                }
            }
        }

        // for AirXRPlaygroundController
        public void OnJoinParticipant(AirXRPlaygroundParticipant participant) {
            aDelegate?.OnJoinParticipant(this, participant);
        }

        public void OnLeaveParticipant(AirXRPlaygroundParticipant participant) {
            aDelegate?.OnLeaveParticipant(this, participant);
        }

        public void OnPendLocalPlayerDataPerFrame(AXRMulticastManager manager, AirXRPlaygroundLocalPlayer player) {
            aDelegate?.OnPendParticipantDataPerFrame(this, manager, player);
        }

        public void OnGetParticipantDataPerFrame(AXRMulticastManager manager, AirXRPlaygroundParticipant participant, string member) {
            aDelegate?.OnGetParticipantDataPerFrame(this, manager, participant, member);
        }
    }
}
