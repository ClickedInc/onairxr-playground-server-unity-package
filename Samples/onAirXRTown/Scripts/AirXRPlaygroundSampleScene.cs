/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using UnityEngine;
using UnityEngine.Playables;
using TMPro;
using onAirXR.Server;
using onAirXR.Playground.Server;

public class AirXRPlaygroundSampleScene : MonoBehaviour, AirXRPlayground.Delegate {
    private TextMeshPro _clock;
    private PlayableDirector _director;

    private void Awake() {
        _clock = transform.Find("Clock")?.GetComponent<TextMeshPro>();
        _director = GetComponent<PlayableDirector>();

        AirXRPlayground.aDelegate = this;
    }

    private void Update() {
        if (_clock == null || _director == null) { return; }

        _clock.text = string.Format("00:{0:D2}", (int)_director.time);
    }

    // implements AirXRPlayground.Delegate
    void AirXRPlayground.Delegate.OnJoinParticipant(AirXRPlayground playground, AirXRPlaygroundParticipant participant) {
        Debug.LogFormat("Participant joined: {0}({1}) : userID {2}", participant.name, participant.GetHashCode(), participant.userID);

        if (participant.isLocalPlayer &&
            participant.type == AirXRPlaygroundParticipant.Type.Mono &&
            participant.userID == "10") {
            participant.transform.localPosition = Vector3.up * 5;
        }

        var animator = participant.GetComponentInChildren<Animator>();
        if (animator != null) {
            participant.userdata = animator;
        }
    }

    void AirXRPlayground.Delegate.OnLeaveParticipant(AirXRPlayground playground, AirXRPlaygroundParticipant participant) {
        Debug.LogFormat("Participant left: {0}({1}) : userID {2}", participant.name, participant.GetHashCode(), participant.userID);
    }

    void AirXRPlayground.Delegate.OnPendParticipantDataPerFrame(AirXRPlayground playground, AXRMulticastManager manager, AirXRPlaygroundLocalPlayer player) {
        //var playerStatus = AirXRInput.Get(player.vrcamera, AirXRInput.Button.RThumbstickLeft) ? 1 :
        //                   AirXRInput.Get(player.vrcamera, AirXRInput.Button.RThumbstickDown) ? 2 :
        //                   AirXRInput.Get(player.vrcamera, AirXRInput.Button.RThumbstickRight) ? 3 :
        //                   AirXRInput.Get(player.vrcamera, AirXRInput.Button.RThumbstickUp) ? 4 : 0;

        var playerStatus = 0;

        manager.PendInputByteStream((byte)AirXRPlaygroundParticipant.InputDevice.UserData, 0, (byte)playerStatus);

        (player.userdata as Animator)?.SetInteger("status", playerStatus);
    }

    void AirXRPlayground.Delegate.OnGetParticipantDataPerFrame(AirXRPlayground playground, AXRMulticastManager manager, AirXRPlaygroundParticipant participant, string member) {
        byte playerStatus = 0;
        manager.GetInputByteStream(member, (byte)AirXRPlaygroundParticipant.InputDevice.UserData, 0, ref playerStatus);

        (participant.userdata as Animator)?.SetInteger("status", playerStatus);
    }
}
