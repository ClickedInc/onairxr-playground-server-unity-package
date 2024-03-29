﻿/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System.Collections.Generic;
using UnityEngine;
using onAirXR.Server;

namespace onAirXR.Playground.Server {
    public class AirXRPlaygroundObserverParticipant {
        internal string member { get; private set; }

        public string clientID { get; private set; }

        internal AirXRPlaygroundObserverParticipant(string member, string clientID) {
            this.member = member;
            this.clientID = clientID;
        }

        internal void UpdateClientID(string id) {
            clientID = id;
        }
    }

    public abstract class AirXRPlaygroundController : AXRMulticastManager.EventListener {
        private Dictionary<string, TrackerGroupDesc> _trackerGroups = new Dictionary<string, TrackerGroupDesc>();
        private AirXRPlaygroundObserverParticipant _observerParticipant;

        protected AirXRPlayground owner { get; private set; }

        public Dictionary<string, AirXRPlaygroundParticipant> participants { get; private set; } = new Dictionary<string, AirXRPlaygroundParticipant>();

        public AirXRPlaygroundController(AirXRPlayground owner) {
            this.owner = owner;

            if (AirXRPlaygroundConfig.config.multicast.isValid) {
                AXRMulticastManager.LoadOnce(AirXRPlaygroundConfig.config.multicast.address,
                                             AirXRPlaygroundConfig.config.multicast.port,
                                             AirXRPlaygroundConfig.config.multicast.hint);
                AXRMulticastManager.RegisterDelegate(this);
            }
        }

        public void AddParticipant(string id) {
            if (participants.ContainsKey(id)) { return; }

            participants.Add(id, null);
        }

        public AirXRPlaygroundParticipant GetParticipant(string id, AirXRPlaygroundParticipant.Type type, string userID, string extensionClientID, AirXRPlaygroundParticipant prefab) {
            if (participants.ContainsKey(id) == false) { return null; }
            if (participants[id] != null) { return participants[id]; }

            var participant = prefab.Instantiate(type, userID, extensionClientID);
            AXRUtils.AttachAndResetToOrigin(participant.transform, owner.transform);

            participants[id] = participant;
            OnJoinParticipant(id, participant);

            return participant;
        }

        public void RemoveParticipant(string id) {
            if (_observerParticipant?.member == id) {
                OnLeaveObserver(_observerParticipant.member, _observerParticipant);

                _observerParticipant = null;
            }
            if (participants.ContainsKey(id) == false) { return; }

            var participant = participants[id];
            participants.Remove(id);

            if (participant != null) {
                OnLeaveParticipant(id, participant);
            }

            participant?.Destroy();
        }

        public virtual void Init() { }
        public virtual void InitAfterUpdate() { }

        public virtual void Cleanup() { }

        protected abstract void OnJoinParticipant(string id, AirXRPlaygroundParticipant participant);
        protected abstract void OnLeaveParticipant(string id, AirXRPlaygroundParticipant participant);
        protected virtual void OnJoinObserver(string id, AirXRPlaygroundObserverParticipant observer) { }
        protected virtual void OnLeaveObserver(string id, AirXRPlaygroundObserverParticipant observer) { }
        protected abstract void OnMulticastPostGetInputsPerFrame(AXRMulticastManager manager);
        protected abstract bool OnMulticastPendInputsPerFrame(AXRMulticastManager manager);

        // implements AXRMulticastManager.EventListener
        void AXRMulticastManager.EventListener.MemberJoined(AXRMulticastManager manager, string member, byte subgroup) {
            AddParticipant(member);
        }

        void AXRMulticastManager.EventListener.MemberChangedMembership(AXRMulticastManager manager, string member, byte subgroup) {
            // do nothing
        }

        void AXRMulticastManager.EventListener.MemberLeft(AXRMulticastManager manager, string member) {
            RemoveParticipant(member);
            removeTrackerGroup(member);
        }

        void AXRMulticastManager.EventListener.MemberUserdataReceived(AXRMulticastManager manager, string member, byte subgroup, byte[] data) {
            if (participants.ContainsKey(member) == false) { return; }

            var participant = participants[member];
            if (participant == null) { return; }

            owner.OnParticipantUserdataReceived(manager, participant, data);
        }

        void AXRMulticastManager.EventListener.GetInputsPerFrame(AXRMulticastManager manager) {
            var ids = new List<string>(participants.Keys);
            foreach (var id in ids) {
                byte type = 0;
                if (manager.GetInputByteStream(id, (byte)AirXRPlaygroundParticipant.InputDevice.Description, (byte)AirXRPlaygroundParticipant.DescriptionControl.Type, ref type) == false) { continue; }

                string userID = "";
                var hasUserID = manager.GetInputString(id, (byte)AirXRPlaygroundParticipant.InputDevice.Description, (byte)AirXRPlaygroundParticipant.DescriptionControl.UserID, ref userID);

                if (type == (byte)AirXRPlaygroundParticipant.Type.Observer) {
                    getObserverParticipantInputsPerFrame(manager, id);
                }
                else if (type == (byte)AirXRPlaygroundParticipant.Type.Tracker) {
                    getTrackerParticipantInputsPerFrame(manager, id, (AirXRPlaygroundParticipant.Type)type, hasUserID ? userID : null);
                }
                else {
                    getPlayerParticipantInputsPerFrame(manager, id, (AirXRPlaygroundParticipant.Type)type, hasUserID ? userID : null);
                }
            }

            OnMulticastPostGetInputsPerFrame(manager);
        }

        bool AXRMulticastManager.EventListener.PendInputsPerFrame(AXRMulticastManager manager) {
            return OnMulticastPendInputsPerFrame(manager);
        }

        private void getObserverParticipantInputsPerFrame(AXRMulticastManager manager, string id) {
            var extensionClientID = "";
            if (manager.GetInputString(id,
                                       (byte)AirXRPlaygroundParticipant.InputDevice.Description,
                                       (byte)AirXRPlaygroundParticipant.DescriptionControl.ExtensionClientID,
                                       ref extensionClientID) == false ||
                string.IsNullOrEmpty(extensionClientID)) { return; }

            if (_observerParticipant == null || _observerParticipant.member != id) {
                if (_observerParticipant != null) {
                    OnLeaveObserver(_observerParticipant.member, _observerParticipant);
                }

                _observerParticipant = new AirXRPlaygroundObserverParticipant(id, extensionClientID);

                OnJoinObserver(_observerParticipant.member, _observerParticipant);
            }
            else {
                _observerParticipant.UpdateClientID(extensionClientID);
            }
        }

        private void getPlayerParticipantInputsPerFrame(AXRMulticastManager manager, string id, AirXRPlaygroundParticipant.Type type, string userID) {
            var extensionClientID = "";
            manager.GetInputString(id,
                                   (byte)AirXRPlaygroundParticipant.InputDevice.Description,
                                   (byte)AirXRPlaygroundParticipant.DescriptionControl.ExtensionClientID,
                                   ref extensionClientID);

            var participant = GetParticipant(id, type, userID, extensionClientID, owner.otherPlayerPrefab);
            if (participant == null) { return; }

            var status = (byte)AXRDeviceStatus.Unavailable;
            var position = Vector3.zero;
            var rotation = Quaternion.identity;

            switch (participant.type) {
                case AirXRPlaygroundParticipant.Type.Stereo:
                    if (manager.GetInputString(id,
                                               (byte)AirXRPlaygroundParticipant.InputDevice.Description,
                                               (byte)AirXRPlaygroundParticipant.DescriptionControl.ExtensionClientID,
                                               ref extensionClientID)) {
                        participant.UpdateClientID(extensionClientID);
                    }
                    if (manager.GetInputPose(id, (byte)AirXRPlaygroundParticipant.InputDevice.HeadTracker, (byte)AXRHeadTrackerControl.Pose, ref position, ref rotation)) {
                        participant.UpdateHeadPose(position, rotation);
                    }
                    if (manager.GetInputByteStream(id, (byte)AirXRPlaygroundParticipant.InputDevice.LeftHandTracker, (byte)AXRHandTrackerControl.Status, ref status)) {
                        manager.GetInputPose(id, (byte)AirXRPlaygroundParticipant.InputDevice.LeftHandTracker, (byte)AXRHandTrackerControl.Pose, ref position, ref rotation);

                        participant.UpdateLeftHandPose(status == (byte)AXRDeviceStatus.Ready, position, rotation);
                    }
                    if (manager.GetInputByteStream(id, (byte)AirXRPlaygroundParticipant.InputDevice.RightHandTracker, (byte)AXRHandTrackerControl.Status, ref status)) {
                        manager.GetInputPose(id, (byte)AirXRPlaygroundParticipant.InputDevice.RightHandTracker, (byte)AXRHandTrackerControl.Pose, ref position, ref rotation);

                        participant.UpdateRightHandPose(status == (byte)AXRDeviceStatus.Ready, position, rotation);
                    }
                    break;
                case AirXRPlaygroundParticipant.Type.Mono:
                    if (manager.GetInputString(id,
                                               (byte)AirXRPlaygroundParticipant.InputDevice.Description,
                                               (byte)AirXRPlaygroundParticipant.DescriptionControl.ExtensionClientID,
                                               ref extensionClientID)) {
                        participant.UpdateClientID(extensionClientID);
                    }
                    if (manager.GetInputPose(id, (byte)AirXRPlaygroundParticipant.InputDevice.HeadTracker, (byte)AXRHeadTrackerControl.Pose, ref position, ref rotation)) {
                        participant.UpdateHeadPose(position, rotation);
                    }
                    break;
                default:
                    break;
            }

            owner.OnGetParticipantDataPerFrame(manager, participant, id);
        }

        private void getTrackerParticipantInputsPerFrame(AXRMulticastManager manager, string id, AirXRPlaygroundParticipant.Type type, string userID) {
            byte group = 0, deviceCount = 0;
            if (manager.GetInputByteStream(id, (byte)AirXRPlaygroundParticipant.InputDevice.Description, (byte)AirXRPlaygroundParticipant.DescriptionControl.Group, ref group) == false ||
                manager.GetInputByteStream(id, (byte)AirXRPlaygroundParticipant.InputDevice.Description, (byte)AirXRPlaygroundParticipant.DescriptionControl.DeviceCount, ref deviceCount) == false) { return; }

            if (_trackerGroups.ContainsKey(id) == false) {
                _trackerGroups.Add(id, new TrackerGroupDesc());
            }
            _trackerGroups[id] = new TrackerGroupDesc {
                group = group,
                deviceCount = deviceCount
            };

            for (byte device = 1; device <= deviceCount; device++) {
                var status = (byte)AXRDeviceStatus.Unavailable;
                manager.GetInputByteStream(id, device, (byte)AirXRPlaygroundParticipant.TrackerControl.Status, ref status);

                if (status == (byte)AXRDeviceStatus.Ready) {
                    addOrUpdateTracker(manager, id, group, device, type, userID);
                }
                else {
                    removeTracker(id, group, device);
                }
            }
        }

        private void addOrUpdateTracker(AXRMulticastManager manager, string id, byte group, byte device, AirXRPlaygroundParticipant.Type type, string userID) {
            var trackerid = makeTrackerID(group, device);
            if (participants.ContainsKey(trackerid) == false) {
                participants.Add(trackerid, null);
            }

            var participant = GetParticipant(trackerid, type, userID, "", owner.otherPlayerPrefab);
            if (participant == null) { return; }

            var position = Vector3.zero;
            var rotation = Quaternion.identity;
            if (manager.GetInputPose(id, device, (byte)AirXRPlaygroundParticipant.TrackerControl.Pose, ref position, ref rotation)) {
                participant.UpdateHeadPose(position, rotation);
            }
        }

        private void removeTracker(string id, byte group, byte device) {
            RemoveParticipant(makeTrackerID(group, device));
        }

        private void removeTrackerGroup(string id) {
            if (_trackerGroups.ContainsKey(id) == false) { return; }

            var trackerGroup = _trackerGroups[id];
            _trackerGroups.Remove(id);

            for (byte device = 1; device <= trackerGroup.deviceCount; device++) {
                removeTracker(id, trackerGroup.group, device);
            }
        }

        private string makeTrackerID(byte group, byte device) {
            return string.Format("tracker-{0}", (group << 8) + device);
        }

        private struct TrackerGroupDesc {
            public byte group;
            public byte deviceCount;
        }
    }
}
