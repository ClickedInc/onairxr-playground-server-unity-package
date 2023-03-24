/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.Assertions;

namespace onAirXR.Playground.Server {
    [ExecuteInEditMode]
    public class AirXRPlaygroundParticipant : MonoBehaviour {
        public enum InputDevice : byte {
            Description = 0,
            HeadTracker = 1,
            LeftHandTracker = 2,
            RightHandTracker = 3,

            UserData = 7
        }

        public enum DescriptionControl : byte {
            Type = 0,
            UserID = 1,
            ExtensionClientID = 2,

            // for trackers
            Group = 1,
            DeviceCount = 2
        }

        public enum Type : byte {
            Stereo = 0,
            Mono = 1,
            Tracker = 2,
            Observer = 3,

            Unknown
        }

        public enum TrackerControl : byte {
            Status = 0,
            Pose,
            Battery
        }

        public string userID { get; protected set; }
        public string clientID { get; private set; }
        public Transform stereoHeadAnchor { get; private set; }
        public Transform monoHeadAnchor { get; private set; }
        public Transform trackerAnchor { get; private set; }
        public Transform leftHandAnchor { get; private set; }
        public Transform rightHandAnchor { get; private set; }

        public object attachment { get; set; }

        public bool isLocalPlayer => this is AirXRPlaygroundLocalPlayer;

        public virtual Type type {
            get {
                if (stereoHeadAnchor != null && monoHeadAnchor != null && trackerAnchor != null) {
                    throw new UnityException("[ERROR] getting type from AirXRPlaygroundParticipant instance as a prefab is not allowed.");
                }
                else if (stereoHeadAnchor != null) {
                    return Type.Stereo;
                }
                else if (monoHeadAnchor != null) {
                    return Type.Mono;
                }
                else if (trackerAnchor != null) {
                    return Type.Tracker;
                }

                Assert.IsTrue(false);
                return Type.Stereo;
            }
        }

        public AirXRPlaygroundParticipant Instantiate(Type type, string userID, string extensionClientID) {
            switch (type) {
                case Type.Stereo: {
                        var go = new GameObject("Participant - " + gameObject.name);

                        var stereoHead = Instantiate(stereoHeadAnchor.gameObject);
                        stereoHead.name = "StereoHeadAnchor";
                        AXRUtils.AttachAndResetToOrigin(stereoHead.transform, go.transform);
                        AXRUtils.ActivateChildren(stereoHead.transform, true);

                        var leftHand = Instantiate(leftHandAnchor.gameObject);
                        leftHand.name = "LeftHandAnchor";
                        removeTrackedPoseDriver(leftHand);
                        AXRUtils.AttachAndResetToOrigin(leftHand.transform, go.transform);
                        AXRUtils.ActivateChildren(leftHand.transform, true);

                        var rightHand = Instantiate(rightHandAnchor.gameObject);
                        rightHand.name = "RightHandAnchor";
                        removeTrackedPoseDriver(rightHand);
                        AXRUtils.AttachAndResetToOrigin(rightHand.transform, go.transform);
                        AXRUtils.ActivateChildren(rightHand.transform, true);

                        var result = go.AddComponent<AirXRPlaygroundParticipant>();
                        result.userID = userID;
                        result.clientID = extensionClientID;

                        return result;
                    }
                case Type.Mono: {
                        var go = new GameObject("Participant - " + gameObject.name);

                        var monoHead = Instantiate(monoHeadAnchor.gameObject);
                        monoHead.name = "MonoHeadAnchor";
                        AXRUtils.AttachAndResetToOrigin(monoHead.transform, go.transform);
                        AXRUtils.ActivateChildren(monoHead.transform, true);

                        var result = go.AddComponent<AirXRPlaygroundParticipant>();
                        result.userID = userID;
                        result.clientID = extensionClientID;

                        return result;
                    }
                case Type.Tracker: {
                        var result = new GameObject("Tracker - " + gameObject.name);

                        var tracker = Instantiate(trackerAnchor.gameObject);
                        tracker.name = "TrackerAnchor";
                        AXRUtils.AttachAndResetToOrigin(tracker.transform, result.transform);
                        AXRUtils.ActivateChildren(tracker.transform, true);

                        return result.AddComponent<AirXRPlaygroundParticipant>();
                    }
                default:
                    break;
            }
            return null;
        }

        public void UpdateClientID(string id) {
            clientID = id;
        }

        public void UpdateHeadPose(Vector3 position, Quaternion rotation) {
            if (stereoHeadAnchor != null) {
                stereoHeadAnchor.localPosition = position;
                stereoHeadAnchor.localRotation = rotation;
            }
            if (monoHeadAnchor != null) {
                monoHeadAnchor.localPosition = position;
                monoHeadAnchor.localRotation = rotation;
            }
            if (trackerAnchor != null) {
                trackerAnchor.localPosition = position;
                trackerAnchor.localRotation = rotation;
            }
        }

        public void UpdateLeftHandPose(bool active, Vector3 position, Quaternion rotation) {
            if (leftHandAnchor == null) { return; }

            leftHandAnchor.gameObject.SetActive(active);
            if (active) {
                leftHandAnchor.localPosition = position;
                leftHandAnchor.localRotation = rotation;
            }
        }

        public void UpdateRightHandPose(bool active, Vector3 position, Quaternion rotation) {
            if (rightHandAnchor == null) { return; }

            rightHandAnchor.gameObject.SetActive(active);
            if (active) {
                rightHandAnchor.localPosition = position;
                rightHandAnchor.localRotation = rotation;
            }
        }

        public virtual void Destroy() {
            Destroy(gameObject);
        }

        protected virtual void EnsureGameObjectIntegrity(bool create) {
            if (stereoHeadAnchor == null) {
                stereoHeadAnchor = AXRUtils.GetChildTransform(transform, "StereoHeadAnchor", create);
            }
            if (monoHeadAnchor == null) {
                monoHeadAnchor = AXRUtils.GetChildTransform(transform, "MonoHeadAnchor", create);
            }
            if (trackerAnchor == null) {
                trackerAnchor = AXRUtils.GetChildTransform(transform, "TrackerAnchor", create);
            }
            if (leftHandAnchor == null) {
                leftHandAnchor = AXRUtils.GetChildTransform(transform, "LeftHandAnchor", create);
            }
            if (rightHandAnchor == null) {
                rightHandAnchor = AXRUtils.GetChildTransform(transform, "RightHandAnchor", create);
            }
        }

        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }

        private void Awake() {
            EnsureGameObjectIntegrity(false);
            if (Application.isPlaying == false) { return; }

            OnAwake();
        }

        private void Start() {
            if (Application.isPlaying == false) {
                EnsureGameObjectIntegrity(true);
                return;
            }

            OnStart();
        }

        private void Update() {
            if (Application.isPlaying == false) {
                EnsureGameObjectIntegrity(true);
                return;
            }

            OnUpdate();
        }

        private void removeTrackedPoseDriver(GameObject go) {
            var comp = go.GetComponent<TrackedPoseDriver>();
            if (comp == null) { return; }

            Destroy(comp);
        }
    }
}
