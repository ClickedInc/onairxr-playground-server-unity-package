/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using onAirXR.Server;

namespace onAirXR.Playground.Server {
    public class AirXRPlaygroundPlayerController : AirXRPlaygroundController, AXRServer.EventHandler {
        private Transform _ownerTransform;
        private Transform _playerTransform;

        public AirXRPlaygroundLocalPlayer player { get; private set; }

        public AirXRPlaygroundPlayerController(AirXRPlayground owner, AirXRPlaygroundLocalPlayer player, Camera camera) : base(owner) {
            this.player = player;
            _ownerTransform = owner.transform;
            _playerTransform = player.transform;

            AXRServer.instance.RegisterEventHandler(this);

            player.Init(camera);
        }

        public override void InitAfterUpdate() {
            XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }

        public override void Cleanup() {
            AXRServer.instance.UnregisterEventHandler(this);

            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }

        // implements AirXRPlaygroundController
        protected override void OnJoinParticipant(string id, AirXRPlaygroundParticipant participant) {
            owner.OnJoinParticipant(participant);
        }

        protected override void OnLeaveParticipant(string id, AirXRPlaygroundParticipant participant) {
            owner.OnLeaveParticipant(participant);
        }

        protected override void OnMulticastPostGetInputsPerFrame(AXRMulticastManager manager) { }

        protected override bool OnMulticastPendInputsPerFrame(AXRMulticastManager manager) {
            var playerLocalToOwnerLocal = _ownerTransform.worldToLocalMatrix * _playerTransform.localToWorldMatrix;

            if (player.activated) {
                if (player.type == AirXRPlaygroundParticipant.Type.Stereo) {
                    pendStereoPlayerInputs(manager, playerLocalToOwnerLocal);
                }
                else if (player.type == AirXRPlaygroundParticipant.Type.Mono) {
                    pendMonoPlayerInputs(manager, playerLocalToOwnerLocal);
                }

                owner.OnPendLocalPlayerDataPerFrame(manager, player);
            }

            return player.activated;
        }

        private void pendStereoPlayerInputs(AXRMulticastManager manager, Matrix4x4 playerLocalToOwnerLocal) {
            manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.Description, (byte)AirXRPlaygroundParticipant.DescriptionControl.Type, (byte)AirXRPlaygroundParticipant.Type.Stereo);
            if (string.IsNullOrEmpty(player.userID) == false) {
                try {
                    manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.Description,
                                                (byte)AirXRPlaygroundParticipant.DescriptionControl.UserID,
                                                byte.Parse(player.userID));
                }
                catch (Exception) { }
            }

            manager.PendInputPose((byte)AirXRPlaygroundParticipant.InputDevice.HeadTracker, (byte)AXRHeadTrackerControl.Pose,
                                  playerLocalToOwnerLocal.MultiplyPoint(player.cameraTransform.localPosition),
                                  playerLocalToOwnerLocal.rotation * player.cameraTransform.localRotation);

            var battery = 0f;
            AXRServer.instance?.input?.TryGetFeatureValue(AXRInputDeviceID.HeadTracker, CommonUsages.batteryLevel, ref battery);
            manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.HeadTracker, (byte)AXRHeadTrackerControl.Battery, (byte)Mathf.RoundToInt(battery * 100));

            var status = (AXRServer.instance?.input?.IsDeviceConnected(AXRInputDeviceID.LeftHandTracker) ?? false) ? AXRDeviceStatus.Ready : AXRDeviceStatus.Unavailable;
            manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.LeftHandTracker, (byte)AXRHandTrackerControl.Status, (byte)status);

            if (status == AXRDeviceStatus.Ready) {
                manager.PendInputPose((byte)AirXRPlaygroundParticipant.InputDevice.LeftHandTracker, (byte)AXRHandTrackerControl.Pose,
                                      playerLocalToOwnerLocal.MultiplyPoint(player.leftHandAnchor.localPosition),
                                      playerLocalToOwnerLocal.rotation * player.leftHandAnchor.localRotation);

                battery = 0f;
                AXRServer.instance?.input?.TryGetFeatureValue(AXRInputDeviceID.LeftHandTracker, CommonUsages.batteryLevel, ref battery);

                manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.LeftHandTracker, (byte)AXRHandTrackerControl.Battery, (byte)Mathf.RoundToInt(battery * 100));
            }

            status = (AXRServer.instance?.input?.IsDeviceConnected(AXRInputDeviceID.RightHandTracker) ?? false) ? AXRDeviceStatus.Ready : AXRDeviceStatus.Unavailable;
            manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.RightHandTracker, (byte)AXRHandTrackerControl.Status, (byte)status);

            if (status == AXRDeviceStatus.Ready) {
                manager.PendInputPose((byte)AirXRPlaygroundParticipant.InputDevice.RightHandTracker, (byte)AXRHandTrackerControl.Pose,
                                      playerLocalToOwnerLocal.MultiplyPoint(player.rightHandAnchor.localPosition),
                                      playerLocalToOwnerLocal.rotation * player.rightHandAnchor.localRotation);

                battery = 0f;
                AXRServer.instance?.input?.TryGetFeatureValue(AXRInputDeviceID.RightHandTracker, CommonUsages.batteryLevel, ref battery);

                manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.RightHandTracker, (byte)AXRHandTrackerControl.Battery, (byte)Mathf.RoundToInt(battery * 100));
            }
        }

        private void pendMonoPlayerInputs(AXRMulticastManager manager, Matrix4x4 playerLocalToOwnerLocal) {
            manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.Description, (byte)AirXRPlaygroundParticipant.DescriptionControl.Type, (byte)AirXRPlaygroundParticipant.Type.Mono);
            if (string.IsNullOrEmpty(player.userID) == false) {
                try {
                    manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.Description,
                                                (byte)AirXRPlaygroundParticipant.DescriptionControl.UserID,
                                                byte.Parse(player.userID));
                }
                catch (Exception) { }
            }

            manager.PendInputPose((byte)AirXRPlaygroundParticipant.InputDevice.HeadTracker, (byte)AXRHeadTrackerControl.Pose,
                                  playerLocalToOwnerLocal.MultiplyPoint(player.cameraTransform.localPosition),
                                  playerLocalToOwnerLocal.rotation * player.cameraTransform.localRotation);

            var battery = 0f;
            AXRServer.instance?.input?.TryGetFeatureValue(AXRInputDeviceID.HeadTracker, CommonUsages.batteryLevel, ref battery);
            manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.HeadTracker, (byte)AXRHeadTrackerControl.Battery, (byte)Mathf.RoundToInt(battery * 100));
        }

        // implements AXRServer.EventHandler
        void AXRServer.EventHandler.OnActivate() {
            AXRMulticastManager.Join();

            player.Activate(true);
            owner.OnJoinParticipant(player);
        }

        void AXRServer.EventHandler.OnDeactivate() {
            AXRMulticastManager.Leave();

            owner.OnLeaveParticipant(player);
            player.Activate(false);
        }

        void AXRServer.EventHandler.OnProfileDataReceived(string path) {
            owner.extension?.ProcessProfileData(path);
        }

        void AXRServer.EventHandler.OnQueryResponseReceived(string statement, string body) {
            owner.extension?.ProcessQueryResponse(statement, body);
        }

        void AXRServer.EventHandler.OnConnect(AXRPlayerConfig config) { }
        void AXRServer.EventHandler.OnDisconnect() { }
        void AXRServer.EventHandler.OnUserdataReceived(byte[] data) { }
    }
}
