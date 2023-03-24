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
    private InputDevice? _rightController;
    private bool _lastTriggerDown;

    private InputDevice? rightController {
        get { 
            if (_rightController != null) { return _rightController; }

            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, devices);
            if (devices.Count == 0) { return null; }

            _rightController = devices[0];
            return _rightController;
        }
    }

    private void Awake() {
        _clock = transform.Find("Clock")?.GetComponent<TextMeshPro>();
        _director = GetComponent<PlayableDirector>();

        AirXRPlayground.aDelegate = this;

        _localPlayer = FindObjectOfType<AirXRPlaygroundLocalPlayer>();
    }

    private void Update() {
        updateClock();

        processControllerInputs();
    }

    private void updateClock() {
        if (_clock == null || _director == null) { return; }

        _clock.text = string.Format("00:{0:D2}", (int)_director.time);
    }

    private void processControllerInputs() {
        if (rightController != null &&
            rightController.Value.TryGetFeatureValue(CommonUsages.triggerButton, out bool down)) {
            if (_lastTriggerDown == false && down) {
        var bytes = System.Text.Encoding.UTF8.GetBytes($"Hello! {DateTime.Now.Second}");
        _localPlayer.SendUserdata(bytes, 0, bytes.Length);
    }
            _lastTriggerDown = down;
        }
    }

    // implements AirXRPlayground.Delegate
    async void AirXRPlayground.Delegate.OnJoinParticipant(AirXRPlayground playground, AirXRPlaygroundParticipant participant) {
        Debug.Log($"[onairxr playground] participant joined: name = {participant.name}, userid = {participant.userID}, clientid = {participant.clientID}");

        if (participant.isLocalPlayer &&
            participant.type == AirXRPlaygroundParticipant.Type.Mono &&
            participant.userID == "10") {
            participant.transform.localPosition = Vector3.up * 5;
        }

        var animator = participant.GetComponentInChildren<Animator>();
        if (animator != null) {
            participant.attachment = animator;
        }

        var extension = playground.extension as AirXRPlaygroundGameExtension;
        if (extension != null) {
            await extension.WaitForConnected();

            extension.SendMessageTo(participant.clientID, 1, $"message from {extension.clientid} to {participant.clientID}");
            extension.SendMessageToAll(2, $"broadcast message from {extension.clientid} on player join");
        }
    }

    void AirXRPlayground.Delegate.OnLeaveParticipant(AirXRPlayground playground, AirXRPlaygroundParticipant participant) {
        Debug.Log($"[onairxr playground] participant left: name = {participant.name}, userid = {participant.userID}, clientid = {participant.clientID}");
    }

    async void AirXRPlayground.Delegate.OnJoinObserver(AirXRPlayground playground, AirXRPlaygroundObserverParticipant observer) {
        Debug.Log($"[onairxr playground] observer joined: clientid = {observer.clientID}");

        var extension = playground.extension as AirXRPlaygroundGameExtension;
        if (extension != null) {
            await extension.WaitForConnected();

            extension.SendMessageTo(observer.clientID, 3, $"message from {extension.clientid} to {observer.clientID}");
            extension.SendMessageToAll(4, $"broadcast message from {extension.clientid} on observer join");
        }
    }

    void AirXRPlayground.Delegate.OnLeaveObserver(AirXRPlayground playground, AirXRPlaygroundObserverParticipant observer) {
        Debug.Log($"[onairxr playground] observer left: clientid = {observer.clientID}");
    }

    void AirXRPlayground.Delegate.OnPendParticipantDataPerFrame(AirXRPlayground playground, AXRMulticastManager manager, AirXRPlaygroundLocalPlayer player) {
        var playerStatus = 0;
        if (rightController != null && 
            rightController.Value.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis)) {
            playerStatus = axis.x < -0.5f ? 1 :
                           axis.y < -0.5f ? 2 :
                           axis.x > 0.5f ?  3 :
                           axis.y > 0.5f ?  4 : 
                                            0;
        }

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
    }

    void AirXRPlayground.Delegate.OnParticipantUserdataReceived(AirXRPlayground playground, AXRMulticastManager manager, AirXRPlaygroundParticipant participant, byte[] data) {
        var message = System.Text.Encoding.UTF8.GetString(data);

        Debug.Log($"[onairxr playground] participant userdata received: {message}");
    }
}