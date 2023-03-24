/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Management;
using onAirXR.Server;

namespace onAirXR.Playground.Server {
    public class AirXRPlaygroundObserverController : AirXRPlaygroundController {
        private const float CameraFadeDuration = 0.5f;
        private const int CameraFadeLayer = 0;

        private ViewMode _viewMode;

        public AirXRPlaygroundObserverController(AirXRPlayground owner, AirXRPlaygroundLocalPlayer player, Camera camera) : base(owner) {
            player.gameObject.SetActive(false);

            if (owner.usingCustomCamera == false) {
                _viewMode = ViewMode.Create(AirXRPlaygroundConfig.config.displays.mode,
                                            owner,
                                            player,
                                            camera,
                                            AirXRPlaygroundConfig.config.displays.isValid ? AirXRPlaygroundConfig.config.displays : AirXRPlaygroundConfigDisplays.Default);
            }

            owner.gameObject.AddComponent<AudioListener>();
            camera.gameObject.SetActive(false);
        }

        public override void InitAfterUpdate() {
            AXRMulticastManager.Join();
        }

        public override void Cleanup() {
            AXRMulticastManager.Leave();
        }

        // implements AirXRPlaygroundController
        protected override void OnJoinParticipant(string id, AirXRPlaygroundParticipant participant) {
            _viewMode?.OnJoinParticipant(id, participant);

            owner.OnJoinParticipant(participant);
        }

        protected override void OnLeaveParticipant(string id, AirXRPlaygroundParticipant participant) {
            _viewMode?.OnLeaveParticipant(id, participant);

            owner.OnLeaveParticipant(participant);
        }

        protected override void OnMulticastPostGetInputsPerFrame(AXRMulticastManager manager) {
            _viewMode?.Update();
        }

        protected override bool OnMulticastPendInputsPerFrame(AXRMulticastManager manager) {
            manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.Description, (byte)AirXRPlaygroundParticipant.DescriptionControl.Type, (byte)AirXRPlaygroundParticipant.Type.Observer);

            if (string.IsNullOrEmpty(owner.extension?.clientid) == false) {
                manager.PendInputString((byte)AirXRPlaygroundParticipant.InputDevice.Description,
                                        (byte)AirXRPlaygroundParticipant.DescriptionControl.ExtensionClientID,
                                        owner.extension.clientid);
            }
            return true;
        }

        private struct ActiveView {
            public char id;
            public Camera camera;
            public Transform anchor;

            public void Update() {
                if (anchor != null) {
                    camera.transform.position = anchor.position;
                    camera.transform.rotation = anchor.rotation;
                }
            }
        }

        // view modes
        private abstract class ViewMode {
            public static ViewMode Create(string mode, AirXRPlayground owner, AirXRPlaygroundLocalPlayer player, Camera camera, AirXRPlaygroundConfigDisplays config) {
                switch (mode) {
                    case "circle-display":
                        return new CircleDisplayMode(owner, player, camera, config);
                    default:
                        return new DefaultViewMode(owner, player, camera, config);
                }
            }

            private Dictionary<char, Camera> _inactives = new Dictionary<char, Camera>();
            private Dictionary<string, ActiveView> _actives = new Dictionary<string, ActiveView>();

            public ViewMode(AirXRPlayground owner, AirXRPlaygroundLocalPlayer player, Camera camera, AirXRPlaygroundConfigDisplays config) {
                foreach (var view in config.GetViews()) {
                    var cam = Object.Instantiate(camera.gameObject).GetComponent<Camera>();
                    cam.gameObject.name = "DisplayView";
                    cam.stereoTargetEye = StereoTargetEyeMask.None;

                    var fade = cam.gameObject.GetComponent<AXRCameraFade>() ?? cam.gameObject.AddComponent<AXRCameraFade>();
                    fade.FadeImmediately(CameraFadeLayer, Color.black);

                    AXRUtils.AttachAndResetToOrigin(cam.transform, owner.transform);

                    if (view.fov > 0) {
                        cam.fieldOfView = view.fov;
                    }
                    if (view.aspect > 0) {
                        cam.aspect = view.aspect;
                    }
                    cam.depth = view.depth;
                    cam.rect = view.viewRect;
                    if (view.targetDisplayIndex != AirXRPlaygroundConfigDisplays.InvalidDisplayIndex) {
                        cam.targetDisplay = view.targetDisplayIndex;
                    }

                    if (view.id == 'o') {
                        _actives.Add("observer", new ActiveView {
                            id = view.id,
                            camera = cam,
                            anchor = camera.transform
                        });
                        fade.Fade(CameraFadeLayer, Color.black, Color.clear, CameraFadeDuration);
                    }
                    else {
                        _inactives[view.id] = cam;
                    }
                }

                if (config.layout.Length > 1) {
                    for (var index = 0; index < config.layout.Length; index++) {
                        if (index >= Display.displays.Length) { break; }

                        var display = config.layout[index];
                        if (display != null && display.Length > 0) {
                            try {
                                Display.displays[index].Activate();
                            }
                            catch (System.Exception e) {
                                Debug.LogWarning(string.Format("[WARNING] failed to activate the display of index {0}: {1}", display, e));
                            }
                        }
                    }
                }
            }

            public virtual void Update() {
                foreach (var active in _actives.Values) {
                    active.Update();
                }
            }

            public abstract void OnJoinParticipant(string member, AirXRPlaygroundParticipant participant);
            public abstract void OnLeaveParticipant(string member, AirXRPlaygroundParticipant participant);

            protected int inactiveViewCount => _inactives.Count;
            protected int activeViewCount => _actives.Count;

            protected bool HasActiveView(string id) => _actives.ContainsKey(id);

            protected List<char> GetAllInactiveViewIDs() {
                var keys = new List<char>(_inactives.Keys);
                keys.Sort();

                return keys;
            }

            protected List<string> GetAllActiveViewIDs(bool includeObserverView = false) {
                var keys = new List<string>(_actives.Keys);
                if (includeObserverView == false) {
                    keys.Remove("observer");
                }
                keys.Sort();

                return keys;
            }

            protected AXRCameraFade ActivateView(string id, char viewid, Transform anchor) {
                if (_inactives.ContainsKey(viewid) == false) { return null; }

                var cam = _inactives[viewid];
                _actives.Add(id, new ActiveView {
                    id = viewid,
                    camera = cam,
                    anchor = anchor
                });

                _inactives.Remove(viewid);

                return cam.gameObject.GetComponent<AXRCameraFade>();
            }

            protected AXRCameraFade DeactivateView(string id) {
                if (_actives.ContainsKey(id) == false) { return null; }

                var view = _actives[id];
                _actives.Remove(id);

                Assert.IsFalse(_inactives.ContainsKey(view.id));
                _inactives.Add(view.id, view.camera);

                return view.camera.gameObject.GetComponent<AXRCameraFade>();
            }

            protected void UpdateActiveViewAnchor(string id, Transform anchor) {
                if (_actives.ContainsKey(id) == false) { return; }

                var view = _actives[id];
                view.anchor = anchor;
                _actives[id] = view;
            }
        }

        private class DefaultViewMode : ViewMode {
            public DefaultViewMode(AirXRPlayground owner, AirXRPlaygroundLocalPlayer player, Camera camera, AirXRPlaygroundConfigDisplays config)
                : base(owner, player, camera, config) { }

            public override void OnJoinParticipant(string member, AirXRPlaygroundParticipant participant) {
                if (inactiveViewCount == 0 || HasActiveView(member)) { return; }

                var viewid = GetAllInactiveViewIDs()[0];
                var fade = ActivateView(member, viewid, participant.type == AirXRPlaygroundParticipant.Type.Stereo ? participant.stereoHeadAnchor : participant.monoHeadAnchor);

                fade?.Fade(CameraFadeLayer, Color.black, Color.clear, CameraFadeDuration);
            }

            public override void OnLeaveParticipant(string member, AirXRPlaygroundParticipant participant) {
                var fade = DeactivateView(member);
                fade?.Fade(CameraFadeLayer, Color.clear, Color.black, CameraFadeDuration);
            }
        }

        private class CircleDisplayMode : ViewMode {
            private List<(string member, Transform anchor)> _anchors = new List<(string member, Transform anchor)>();

            public CircleDisplayMode(AirXRPlayground owner, AirXRPlaygroundLocalPlayer player, Camera camera, AirXRPlaygroundConfigDisplays config)
                : base(owner, player, camera, config) { }

            public override void OnJoinParticipant(string member, AirXRPlaygroundParticipant participant) {
                if (anchorExists(member)) { return; }

                (string member, Transform anchor) anchor = (member, participant.type == AirXRPlaygroundParticipant.Type.Stereo ? participant.stereoHeadAnchor : participant.monoHeadAnchor);
                _anchors.Add(anchor);

                if (inactiveViewCount > 0) {
                    var viewids = GetAllInactiveViewIDs();
                    foreach (var viewid in viewids) {
                        ActivateView(viewid.ToString(), viewid, anchor.anchor)?.Fade(CameraFadeLayer, Color.black, Color.clear, CameraFadeDuration);
                    }
                }

                updateActiveViewAnchors();
            }

            public override void OnLeaveParticipant(string member, AirXRPlaygroundParticipant participant) {
                if (anchorExists(member) == false) { return; }

                (string member, Transform anchor) anchor = (null, null);
                for (int index = 0; index < _anchors.Count; index++) {
                    if (_anchors[index].member.Equals(member) == false) { continue; }

                    anchor = _anchors[index];
                    _anchors.RemoveAt(index);
                    break;
                }

                Assert.IsNotNull(anchor.anchor);
                if (_anchors.Count == 0) {
                    var ids = GetAllActiveViewIDs();
                    foreach (var id in ids) {
                        DeactivateView(id)?.Fade(CameraFadeLayer, Color.clear, Color.black, CameraFadeDuration);
                    }
                }
                else {
                    updateActiveViewAnchors();
                }
            }

            private bool anchorExists(string member) {
                foreach (var anchor in _anchors) {
                    if (anchor.member.Equals(member)) {
                        return true;
                    }
                }
                return false;
            }

            private void updateActiveViewAnchors() {
                if (_anchors.Count == 0) { return; }

                var ids = GetAllActiveViewIDs();
                for (var index = 0; index < ids.Count; index++) {
                    UpdateActiveViewAnchor(ids[index], _anchors[index % _anchors.Count].anchor);
                }
            }
        }
    }
}
