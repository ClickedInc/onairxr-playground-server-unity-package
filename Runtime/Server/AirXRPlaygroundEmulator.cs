/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace onAirXR.Playground.Server {
    public class AirXRPlaygroundEmulator {
        [Serializable]
        public struct ParticipantSpot {
            public AirXRPlaygroundParticipant.Type type;
            public Transform spot;
        }

        private AirXRPlaygroundController _controller;
        private Dictionary<uint, Transform> _spots = new Dictionary<uint, Transform>();

        public AirXRPlaygroundEmulator(AirXRPlayground owner, AirXRPlaygroundController controller, AirXRPlaygroundParticipant prefab, ParticipantSpot[] participants) {
            _controller = controller;

            if (participants != null) {
                uint number = 0;
                foreach (var participant in participants) {
                    if (participant.spot == null) { continue; }

                    var id = string.Format("Emulated-{0}", ++number);
                    _controller.AddParticipant(id);

                    var instantiated = _controller.GetParticipant(id, participant.type, "emulated", "", prefab);
                    _spots[number] = participant.spot;

                    instantiated.transform.parent = owner.transform;
                    instantiated.transform.position = participant.spot.position;
                    instantiated.transform.rotation = participant.spot.rotation;

                    emulateParticipantPose(instantiated);
                }
            }
        }

        private void emulateParticipantPose(AirXRPlaygroundParticipant participant) {
            switch (participant.type) {
                case AirXRPlaygroundParticipant.Type.Stereo:
                    participant.stereoHeadAnchor.localPosition = Vector3.up * 1.5f;
                    participant.leftHandAnchor.localPosition = Vector3.up * 1.1f + Vector3.left * 0.15f;
                    participant.rightHandAnchor.localPosition = Vector3.up * 1.1f + Vector3.right * 0.15f;
                    break;
                case AirXRPlaygroundParticipant.Type.Mono:
                    participant.monoHeadAnchor.localPosition = Vector3.up * 1.0f;
                    break;
            }
        }
    }
}
