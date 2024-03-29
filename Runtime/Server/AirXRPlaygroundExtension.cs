﻿/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System.Threading.Tasks;
using UnityEngine;
using onAirXR.Server;

namespace onAirXR.Playground.Server {
    [RequireComponent(typeof(AirXRPlayground))]
    public abstract class AirXRPlaygroundExtension : MonoBehaviour {
        private bool _configured;

        [SerializeField] private string _addressInEditor = "127.0.0.1:9000";

        protected AirXRPlayground playground { get; private set; }

        protected abstract new string name { get; }

        protected abstract bool Configure(string address);
        protected abstract void OnUpdate();
        protected abstract void OnQuit();

        public abstract string clientid { get; }

        public async Task WaitForConnected() {
            while (string.IsNullOrEmpty(clientid)) { await Task.Yield(); }
        }

        public virtual void OnConnect(AXRPlayerConfig config) { }
        public virtual void OnActivate() { }
        public virtual void OnDeactivate() { }
        public virtual void OnDisconnect() { }
        public virtual void ProcessProfileData(string path) { }
        public virtual void ProcessProfileReport(string report) { }
        public virtual void ProcessQueryResponse(string statement, string body) { }

        private void Awake() {
            playground = GetComponent<AirXRPlayground>();
            if (playground == null) {
                throw new UnityException("[ERROR] AirXRPlaygroundExtension requires AirXRPlayground on the same game object.");
            }
        }

        private void Start() {
            if (Application.isEditor) {
                _configured = Configure(_addressInEditor);
                return;
            }
            else if (AirXRPlaygroundConfig.config.extensions == null) { return; }

            foreach (var extension in AirXRPlaygroundConfig.config.extensions) {
                if (extension.name == name) {
                    _configured = Configure(extension.address);
                    break;
                }
            }
        }

        private void Update() {
            if (_configured == false) { return; }

            OnUpdate();
        }

        private void OnApplicationQuit() {
            if (_configured == false) { return; }

            OnQuit();
        }
    }
}
