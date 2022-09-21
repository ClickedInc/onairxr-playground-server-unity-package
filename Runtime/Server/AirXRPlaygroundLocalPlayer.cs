/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using UnityEngine;
using UnityEngine.SpatialTracking;
using onAirXR.Server;

namespace onAirXR.Playground.Server {
    [ExecuteInEditMode]
    public class AirXRPlaygroundLocalPlayer : AirXRPlaygroundParticipant {
        public new Camera camera { get; private set; }
        public Transform cameraTransform { get; private set; }

        public bool activated => AXRServer.instance?.isOnStreaming ?? false;

        public override Type type {
            get {
                if (AXRServer.instance?.config == null) { return Type.Unknown; }

                switch (AXRServer.instance.config.type) {
                    case AXRPlayerType.Monoscopic:
                        return Type.Mono;
                    case AXRPlayerType.Stereoscopic:
                        return Type.Stereo;
                    default:
                        return Type.Unknown;
                }
            }
        }

        public void Init(Camera inCamera) {
            configurePlayerRig(inCamera);
            Activate(false);

            Destroy(inCamera.gameObject);
        }

        public void Activate(bool activate) {
            if (activate) {
                userID = AXRServer.instance?.config?.userID ?? "";
            }

            AXRUtils.ActivateChildren(stereoHeadAnchor, activate && type == Type.Stereo);
            AXRUtils.ActivateChildren(monoHeadAnchor, activate && type == Type.Mono);
            AXRUtils.ActivateChildren(leftHandAnchor, activate && type == Type.Stereo);
            AXRUtils.ActivateChildren(rightHandAnchor, activate && type == Type.Stereo);
            AXRUtils.ActivateChildren(trackerAnchor, false);
        }

        protected override void OnUpdate() {
            var active = activated;

            if (Application.isEditor && active == false) {
                simulatePlayerInEditor();
            }
            else {
                AXRUtils.ActivateChildren(stereoHeadAnchor, active && type == Type.Stereo);
                AXRUtils.ActivateChildren(monoHeadAnchor, active && type == Type.Mono);
                AXRUtils.ActivateChildren(leftHandAnchor, active && type == Type.Stereo &&  (AXRServer.instance?.input?.IsDeviceConnected(AXRInputDeviceID.LeftHandTracker) ?? false));
                AXRUtils.ActivateChildren(rightHandAnchor, active && type == Type.Stereo && (AXRServer.instance?.input?.IsDeviceConnected(AXRInputDeviceID.RightHandTracker) ?? false));
            }
        }

        private void configurePlayerRig(Camera inCamera) {
            var head = Instantiate(inCamera.gameObject);
            head.name = "Head";
            AXRUtils.AttachAndResetToOrigin(head.transform, transform);
            AXRUtils.AttachAndResetToOrigin(stereoHeadAnchor, head.transform);
            AXRUtils.AttachAndResetToOrigin(monoHeadAnchor, head.transform);
            AXRUtils.AttachAndResetToOrigin(leftHandAnchor, transform);
            AXRUtils.AttachAndResetToOrigin(rightHandAnchor, transform);

            camera = head.GetComponent<Camera>();
            cameraTransform = head.transform;

            addTrackedPoseDriver(cameraTransform, TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            addTrackedPoseDriver(leftHandAnchor, TrackedPoseDriver.DeviceType.GenericXRController, TrackedPoseDriver.TrackedPose.LeftPose);
            addTrackedPoseDriver(rightHandAnchor, TrackedPoseDriver.DeviceType.GenericXRController, TrackedPoseDriver.TrackedPose.RightPose);
        }

        private void addTrackedPoseDriver(Transform anchor, TrackedPoseDriver.DeviceType type, TrackedPoseDriver.TrackedPose pose) {
            var driver = anchor.gameObject.AddComponent<TrackedPoseDriver>();
            driver.SetPoseSource(type, pose);
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        }

        // simulate in editor
        private readonly Vector3 LeftHandPositionFromHead = new Vector3(-0.2f, -0.2f, 0.3f);
        private readonly Vector3 RightHandPositionFromHead = new Vector3(0.2f, -0.2f, 0.3f);
        private const float HeadMovementSpeed = 1.0f;
        private const float HeadRotationSpeed = 0.1f;

        private Vector3 _simulateHeadPositionInEditor = new Vector3(0, 1.5f, 0);
        private Quaternion _simulateHeadRotationInEditor = Quaternion.identity;
        private Vector3 _lastMousePosition = Vector3.zero;

        private void simulatePlayerInEditor() {
            AXRUtils.ActivateChildren(stereoHeadAnchor, true);
            AXRUtils.ActivateChildren(monoHeadAnchor, false);
            AXRUtils.ActivateChildren(leftHandAnchor, true);
            AXRUtils.ActivateChildren(rightHandAnchor, true);

            if (Input.GetMouseButtonDown(0)) {
                _lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0)) {
                var delta = Input.mousePosition - _lastMousePosition;

                var rot = _simulateHeadRotationInEditor.eulerAngles;
                rot.x += -delta.y * HeadRotationSpeed;
                rot.y += delta.x * HeadRotationSpeed;

                rot.x = rot.x < 180.0f ? Mathf.Min(rot.x, 75.0f) : Mathf.Max(rot.x, 285.0f);

                _simulateHeadRotationInEditor = Quaternion.Euler(rot);
                _lastMousePosition = Input.mousePosition;
            }

            if (Input.GetKey(KeyCode.W)) {
                _simulateHeadPositionInEditor += movingVelocity(_simulateHeadRotationInEditor, Vector3.forward) * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S)) {
                _simulateHeadPositionInEditor += movingVelocity(_simulateHeadRotationInEditor, Vector3.back) * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.A)) {
                _simulateHeadPositionInEditor += movingVelocity(_simulateHeadRotationInEditor, Vector3.left) * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.D)) {
                _simulateHeadPositionInEditor += movingVelocity(_simulateHeadRotationInEditor, Vector3.right) * HeadMovementSpeed * Time.deltaTime;
            }

            cameraTransform.localPosition = _simulateHeadPositionInEditor;
            cameraTransform.localRotation = _simulateHeadRotationInEditor;
            leftHandAnchor.position = cameraTransform.localToWorldMatrix.MultiplyPoint(LeftHandPositionFromHead);
            leftHandAnchor.rotation = cameraTransform.localToWorldMatrix.rotation;
            rightHandAnchor.position = cameraTransform.localToWorldMatrix.MultiplyPoint(RightHandPositionFromHead);
            rightHandAnchor.rotation = cameraTransform.localToWorldMatrix.rotation;
        }

        private Vector3 movingVelocity(Quaternion headRotation, Vector3 direction) {
            var dir = headRotation * direction * HeadMovementSpeed;
            dir.y = 0;

            return dir;
        }
    }
}
