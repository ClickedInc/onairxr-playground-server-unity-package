/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.XR;
using TMPro;
using onAirXR.Server;
using onAirXR.Playground.Server;

public class AirXRPlaygroundSampleScene : MonoBehaviour, AirXRPlayground.Delegate {
    private TextMeshPro _clock;
    private PlayableDirector _director;
    private AirXRPlaygroundLocalPlayer _localPlayer;

    private void Awake() {
        _clock = transform.Find("Clock")?.GetComponent<TextMeshPro>();
        _director = GetComponent<PlayableDirector>();

        AirXRPlayground.aDelegate = this;

        _localPlayer = FindObjectOfType<AirXRPlaygroundLocalPlayer>();
    }

    private void Update() {
        if (_clock == null || _director == null) { return; }

        _clock.text = string.Format("00:{0:D2}", (int)_director.time);
    }

    private void OnFire() {
        var bytes = System.Text.Encoding.UTF8.GetBytes($"Hello! {DateTime.Now.Second}");
        _localPlayer.SendUserdata(bytes, 0, bytes.Length);
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
            participant.attachment = animator;
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
        manager.PendInputIntStream((byte)AirXRPlaygroundParticipant.InputDevice.UserData, 1, 10);
        manager.PendInputUintStream((byte)AirXRPlaygroundParticipant.InputDevice.UserData, 2, 20);
        manager.PendInputFloatStream((byte)AirXRPlaygroundParticipant.InputDevice.UserData, 3, 10.5f);

        (player.attachment as Animator)?.SetInteger("status", playerStatus);
    }

    void AirXRPlayground.Delegate.OnGetParticipantDataPerFrame(AirXRPlayground playground, AXRMulticastManager manager, AirXRPlaygroundParticipant participant, string member) {
        byte playerStatus = 0;
        manager.GetInputByteStream(member, (byte)AirXRPlaygroundParticipant.InputDevice.UserData, 0, ref playerStatus);

        int intvalue = 0;
        manager.GetInputIntStream(member, (byte)AirXRPlaygroundParticipant.InputDevice.UserData, 1, ref intvalue);

        uint uintvalue = 0;
        manager.GetInputUintStream(member, (byte)AirXRPlaygroundParticipant.InputDevice.UserData, 2, ref uintvalue);

        float floatvalue = 0;
        manager.GetInputFloatStream(member, (byte)AirXRPlaygroundParticipant.InputDevice.UserData, 3, ref floatvalue);

        (participant.attachment as Animator)?.SetInteger("status", playerStatus);

        Debug.Log($"participant {member} input value: {intvalue}, {uintvalue}, {floatvalue}");
    }

    void AirXRPlayground.Delegate.OnParticipantUserdataReceived(AirXRPlayground playground, AXRMulticastManager manager, AirXRPlaygroundParticipant participant, byte[] data) {
        var textobj = participant.GetComponentInChildren<TextMeshPro>();
        if (textobj == null) { return; }

        var message = System.Text.Encoding.UTF8.GetString(data);
        textobj.text = message;
    }
}