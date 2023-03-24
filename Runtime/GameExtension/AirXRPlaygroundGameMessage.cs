/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using UnityEngine;
using LiteNetLib;

namespace onAirXR.Playground.Server {
    public enum AirXRPlaygroundGameErrorCode {
        Unknown = 0,

        InconsistentState,
        UpdateStateTimeout
    }

    [Serializable]
    public class AirXRPlaygroundGameMessage {
        public const string TypeConnected = "connected";
        public const string TypeOpcode = "opcode";
        public const string TypeCommand = "command";
        public const string TypeRequestState = "request-state";
        public const string TypeUpdateStateWithTimeout = "update-state-with-timeout";
        public const string TypeUpdateState = "update-state";
        public const string TypeLoadScene = "load-scene";
        public const string TypeError = "error";

        public const string TypeSessionRequestState = "session.request-state";
        public const string TypeSessionUpdateState = "session.update-state";
        public const string TypeSessionConfigure = "session.configure";
        public const string TypeSessionCheckSessionData = "session.check-session-data";
        public const string TypeSessionPlay = "session.play";
        public const string TypeSessionStop = "session.stop";
        public const string TypeSessionStartProfile = "session.start-profile";
        public const string TypeSessionStopProfile = "session.stop-profile";
        public const string TypeSessionProfileReport = "session.profile-report";

        public static string MakeIdFromPeer(NetPeer peer) => $"{peer.EndPoint.Address}:{peer.EndPoint.Port}";

        public AirXRPlaygroundGameMessage(string type) {
            this.type = type;
        }

        // common
        [SerializeField] private string type;

        public string GetMessageType() { return type; }
    }

    [Serializable]
    public class AirXRPlaygroundGameConnectedMessage : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameConnectedMessage(NetPeer peer) : base(TypeConnected) {
            id = MakeIdFromPeer(peer);
        }

        [SerializeField] private string id;

        public string GetID() => id;
    }

    [Serializable]
    public class AirXRPlaygroundGameOpcodeMessage : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameOpcodeMessage(string sourceClientId, string targetClientId, int opcode, string data = null) : base(TypeOpcode) {
            sourceid = sourceClientId;
            targetid = targetClientId;
            this.opcode = opcode;
            this.data = data;
        }

        [SerializeField] private string sourceid;
        [SerializeField] private string targetid;
        [SerializeField] private int opcode;
        [SerializeField] private string data;

        public string GetSourceClientID() => sourceid;
        public string GetTargetClientID() => targetid;
        public int GetOpcode() => opcode;
        public string GetData() => data;
    }

    [Serializable]
    public class AirXRPlaygroundGameCommandMessage : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameCommandMessage(string command, string argument = "") : base(TypeCommand) {
            this.command = command;
            this.argument = argument;
        }

        [SerializeField] private string command;
        [SerializeField] private string argument;

        public string GetCommand() { return command; }
        public string GetArgument() { return argument; }
    }

    [Serializable]
    public class AirXRPlaygroundGameRequestStateMessage : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameRequestStateMessage() : base(TypeRequestState) { }
    }

    [Serializable]
    public class AirXRPlaygroundGameUpdateStateMessage : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameUpdateStateMessage(string state) : base(TypeUpdateState) {
            this.state = state;
        }

        [SerializeField] private string state;

        public string GetState() { return state; }
    }

    [Serializable]
    public class AirXRPlaygroundGameLoadSceneMessage : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameLoadSceneMessage(string scene) : base(TypeLoadScene) {
            this.scene = scene;
        }

        [SerializeField] private string scene;

        public string GetScene() { return scene; }
    }

    [Serializable]
    public class AirXRPlaygroundGameUpdateStateWithTimeoutMessage : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameUpdateStateWithTimeoutMessage(string state, float timeout = -1.0f) : base(TypeUpdateStateWithTimeout) {
            this.state = state;
            this.timeout = timeout;
        }

        [SerializeField] private string state;
        [SerializeField] private float timeout;

        public string GetState() { return state; }
        public float GetTimeout() { return timeout; }
    }

    [Serializable]
    public class AirXRPlaygroundGameErrorMessage : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameErrorMessage(AirXRPlaygroundGameErrorCode code, string info = "") : base(TypeError) {
            this.code = (int)code;
            this.info = info;
        }

        [SerializeField] private int code;
        [SerializeField] private string info;

        public AirXRPlaygroundGameErrorCode GetCode() { return (AirXRPlaygroundGameErrorCode)code; }
        public string GetInfo() { return info; }
    }

    // session
    [Serializable]
    public struct AirXRPlaygroundGameSessionState {
        public enum State { 
            Disconnected,
            Stopped,
            Playing,
            Profiling
        }

        public string userid;
        public string displayType;
        public int videoWidth;
        public int videoHeight;
        public State state;
        public string sessionName;
        public ulong minBitrate;
        public ulong startBitrate;
        public ulong maxBitrate;
        public string sessionDataName;

        public void SetState(string userid, string displayType, int videoWidth, int videoHeight, bool connected, bool playing, bool profiling) {
            this.userid = userid;
            this.displayType = displayType;
            this.videoWidth = videoWidth;
            this.videoHeight = videoHeight;

            state = profiling ? State.Profiling :
                    playing ?   State.Playing :
                    connected ? State.Stopped :
                                State.Disconnected;
        }
    }

    [Serializable]
    public class AirXRPlaygroundGameSessionRequestState : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameSessionRequestState() : base(TypeSessionRequestState) { }
    }

    [Serializable]
    public class AirXRPlaygroundGameSessionUpdateState : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameSessionUpdateState(AirXRPlaygroundGameSessionState state, string source = "") : base(TypeSessionUpdateState) {
            this.state = state;
            this.source = source;
        }

        [SerializeField] private string source;
        [SerializeField] private AirXRPlaygroundGameSessionState state;

        public string GetSource() => source;
        public AirXRPlaygroundGameSessionState GetState() => state;

        public void SetSource(string source) {
            this.source = source;
        }
    }

    [Serializable]
    public class AirXRPlaygroundGameSessionConfigure : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameSessionConfigure(ulong minBitrate, ulong startBitrate, ulong maxBitrate, string targetSource) : base(TypeSessionConfigure) {
            this.minBitrate = minBitrate;
            this.startBitrate = startBitrate;
            this.maxBitrate = maxBitrate;
            this.targetSource = targetSource;
        }

        [SerializeField] private ulong minBitrate;
        [SerializeField] private ulong startBitrate;
        [SerializeField] private ulong maxBitrate;
        [SerializeField] private string targetSource;

        public ulong GetMinBitrate() => minBitrate;
        public ulong GetStartBitrate() => startBitrate;
        public ulong GetMaxBitrate() => maxBitrate;
        public string GetTargetSource() => targetSource;
    }

    [Serializable]
    public class AirXRPlaygroundGameSessionCheckSessionData : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameSessionCheckSessionData(string sessionDataName) : base(TypeSessionCheckSessionData) {
            this.sessionDataName = sessionDataName;
        }

        [SerializeField] private string sessionDataName;

        public string GetSessionDataName() => sessionDataName;
    }

    [Serializable]
    public class AirXRPlaygroundGameSessionPlay : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameSessionPlay(string sessionDataName) : base(TypeSessionPlay) {
            this.sessionDataName = sessionDataName;
        }

        [SerializeField] private string sessionDataName;

        public string GetSessionDataName() => sessionDataName;
    }

    [Serializable]
    public class AirXRPlaygroundGameSessionStop : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameSessionStop() : base(TypeSessionStop) { }
    }

    [Serializable]
    public class AirXRPlaygroundGameSessionStartProfile : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameSessionStartProfile(string sessionName, string sessionDataName = null) : base(TypeSessionStartProfile) {
            this.sessionName = sessionName;
            this.sessionDataName = sessionDataName;
        }

        [SerializeField] private string sessionName;
        [SerializeField] private string sessionDataName;

        public string GetSessionName() => sessionName;
        public string GetSessionDataName() => sessionDataName;

        public void SetSessionName(string name) {
            sessionName = name;
        }
    }

    [Serializable]
    public class AirXRPlaygroundGameSessionStopProfile : AirXRPlaygroundGameMessage { 
        public AirXRPlaygroundGameSessionStopProfile() : base(TypeSessionStopProfile) { }
    }

    [Serializable]
    public class AirXRPlaygroundGameSessionProfileReport : AirXRPlaygroundGameMessage {
        public AirXRPlaygroundGameSessionProfileReport(string report, string source = "") : base(TypeSessionProfileReport) {
            this.report = report;
            this.source = source;
        }

        [SerializeField] private string source;
        [SerializeField] private string report;

        public string GetSource() => source;
        public string GetReport() => report;

        public void SetSource(string source) {
            this.source = source;
        }
    }

    public static class AirXRPlaygroundGameBinaryMessage {
        public const string FCCProfileData = "pfdt";
        public const string FCCSessionData = "ssdt";
    }
}
