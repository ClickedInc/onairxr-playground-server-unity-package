/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using UnityEngine;

namespace onAirXR.Playground.Server {
    public enum AirXRPlaygroundGameErrorCode {
        Unknown = 0,

        InconsistentState,
        UpdateStateTimeout
    }

    [Serializable]
    public class AirXRPlaygroundGameMessage {
        public const string TypeCommand = "command";
        public const string TypeRequestState = "request-state";
        public const string TypeUpdateStateWithTimeout = "update-state-with-timeout";
        public const string TypeUpdateState = "update-state";
        public const string TypeLoadScene = "load-scene";
        public const string TypeError = "error";

        public AirXRPlaygroundGameMessage(string type) {
            this.type = type;
        }

        // common
        [SerializeField] private string type;

        public string GetMessageType() { return type; }
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
}
