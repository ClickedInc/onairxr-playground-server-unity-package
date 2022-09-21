/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using LiteNetLib;
using LiteNetLib.Utils;

namespace onAirXR.Playground.Server {
    public abstract class AirXRPlaygroundGameExtension : AirXRPlaygroundExtension, AirXRPlaygroundGameClient.Delegate {
        protected abstract string OnEvaluateCurrentGameState();
        protected abstract bool OnCheckIfNeedToLoadScene(string state, ref string nextScene);
        protected abstract Task OnPreLoadScene(string nextScene);
        protected abstract Task OnDirectorUpdateGameState(string state);
        protected abstract Task OnPlayerUpdateGameState(string state);
        protected abstract void OnDirectorCommand(string command, string argument);
        protected abstract void OnPlayerCommand(string command, string argument);

        public void LoadOtherScene(string scene) {
            if (playground.mode != AirXRPlayground.Mode.Observer) { return; }
            if (string.IsNullOrEmpty(scene) || SceneManager.GetActiveScene().name == scene) { return; }

            AirXRPlaygroundGameClient.LoadScene(scene);
        }

        public void SendUpdateGameState() {
            AirXRPlaygroundGameClient.SendUpdateGameState();
        }

        public void SendCommandToPlayer(string command, string argument = null) {
            if (playground.mode != AirXRPlayground.Mode.Observer) { return; }

            AirXRPlaygroundGameClient.SendCommand(command, argument);
        }

        // implements AirXRPlaygroundExtension
        protected override string name => "circlevr-game-unity";

        protected override bool Configure(string address) {
            AirXRPlaygroundGameClient.LoadOnce();

            return AirXRPlaygroundGameClient.Configure(this, address, playground.mode == AirXRPlayground.Mode.Observer ? "director" : "player");
        }

        protected override void OnUpdate() {
            AirXRPlaygroundGameClient.Update();
        }

        protected override void OnQuit() {
            AirXRPlaygroundGameClient.Shutdown();
        }

        // implements AirXRPlaygroundGameClient.Delegate
        string AirXRPlaygroundGameClient.Delegate.gameState => OnEvaluateCurrentGameState();

        bool AirXRPlaygroundGameClient.Delegate.OnCheckIfNeedToLoadScene(string state, ref string nextScene) {
            return OnCheckIfNeedToLoadScene(state, ref nextScene);
        }

        Task AirXRPlaygroundGameClient.Delegate.OnPreLoadScene(string nextScene) {
            return OnPreLoadScene(nextScene);
        }

        Task AirXRPlaygroundGameClient.Delegate.OnUpdateGameState(string state) {
            switch (playground.mode) {
                case AirXRPlayground.Mode.Observer:
                    return OnDirectorUpdateGameState(state);
                case AirXRPlayground.Mode.Player:
                    return OnPlayerUpdateGameState(state);
            }
            return null;
        }

        void AirXRPlaygroundGameClient.Delegate.OnCommand(string command, string argument) {
            switch (playground.mode) {
                case AirXRPlayground.Mode.Observer:
                    OnDirectorCommand(command, argument);
                    break;
                case AirXRPlayground.Mode.Player:
                    OnPlayerCommand(command, argument);
                    break;
            }
        }
    }

    public class AirXRPlaygroundGameClient {
        public interface Delegate {
            string gameState { get; }

            bool OnCheckIfNeedToLoadScene(string state, ref string nextScene);
            Task OnPreLoadScene(string nextScene);
            Task OnUpdateGameState(string state);
            void OnCommand(string command, string argument);
        }

        private static AirXRPlaygroundGameClient _instance;

        public static void LoadOnce() {
            if (_instance != null) { return; }

            _instance = new AirXRPlaygroundGameClient();
        }

        public static bool Configure(Delegate aDelegate, string address, string role) {
            return _instance?.configure(aDelegate, address, role) ?? false;
        }

        public static void SendUpdateGameState() {
            _instance?.sendUpdateGameState();
        }

        public static void SendUpdateGameState(string state) {
            _instance?.sendUpdateGameState(state);
        }

        public static void SendCommand(string command, string argument) {
            _instance?.sendCommand(command, argument);
        }

        public static void LoadScene(string scene) {
            _instance?.loadScene(scene);
        }

        public static void Update() {
            _instance?.update();
        }

        public static void Shutdown() {
            _instance?.shutdown();
        }

        private Delegate _delegate;
        private string _role;
        private string _host;
        private int _port;
        private NetManager _client;
        private NetDataWriter _dataWriter = new NetDataWriter();
        private string _stateToUpdateOnConfigure;

        private AirXRPlaygroundGameClient() {
            var listener = new EventBasedNetListener();
            _client = new NetManager(listener);

            listener.PeerConnectedEvent += onPeerConnected;
            listener.PeerDisconnectedEvent += onPeerDisconnected;
            listener.NetworkReceiveEvent += onNetworkReceive;
        }

        private bool configure(Delegate aDelegate, string address, string role) {
            _delegate = aDelegate;

            if (_client.IsRunning) {
                updateGameStateAfterFrame();
                return true;
            }

            try {
                var tokens = address.Split(':');
                if (tokens.Length != 2) {
                    throw new UnityException("invalid format: " + address);
                }

                _host = tokens[0];
                _port = int.Parse(tokens[1]);
            }
            catch (Exception e) {
                Debug.LogErrorFormat("[ERROR] invalid address: {0}", e);
                return false;
            }

            _role = role;

            _client.Start();
            _client.Connect(_host, _port, _role);
            return true;
        }

        private async void updateGameStateAfterFrame() {
            await Task.Yield();

            if (string.IsNullOrEmpty(_stateToUpdateOnConfigure) == false) {
                var task = _delegate.OnUpdateGameState(_stateToUpdateOnConfigure);
                if (task != null) {
                    await task;
                }

                _stateToUpdateOnConfigure = null;
            }
            sendUpdateGameState();
        }

        private void sendUpdateGameState() {
            if (_client.ConnectedPeersCount == 0) { return; }

            sendMessage(_client.ConnectedPeerList[0],
                        JsonUtility.ToJson(new AirXRPlaygroundGameUpdateStateMessage(_delegate.gameState)));
        }

        private void sendUpdateGameState(string state) {
            if (_client.ConnectedPeersCount == 0) { return; }

            sendMessage(_client.ConnectedPeerList[0],
                        JsonUtility.ToJson(new AirXRPlaygroundGameUpdateStateMessage(state)));
        }

        private void sendCommand(string command, string argument) {
            if (_client.ConnectedPeersCount == 0) { return; }

            sendMessage(_client.ConnectedPeerList[0], JsonUtility.ToJson(new AirXRPlaygroundGameCommandMessage(command, argument)));
        }

        private async void loadScene(string scene) {
            if (_role != "director") { return; }

            try {
                if (_client.ConnectedPeersCount > 0) {
                    sendMessage(_client.ConnectedPeerList[0], JsonUtility.ToJson(new AirXRPlaygroundGameLoadSceneMessage(scene)));
                }

                var task = _delegate.OnPreLoadScene(scene);
                if (task != null) {
                    await task;
                }

                SceneManager.LoadScene(scene);
            }
            catch (Exception e) {
                Debug.LogErrorFormat("[ERROR] failed to load other scene: {0}: {1}", scene, e);
            }
        }

        private void update() {
            if (_client.IsRunning == false) { return; }

            _client.PollEvents();
        }

        private void shutdown() {
            if (_client.IsRunning == false) { return; }

            _client.Stop();
        }

        private void onPeerConnected(NetPeer peer) {
            switch (_role) {
                case "director":
                    onDirectorPeerConnected(peer);
                    break;
                default:
                    onPlayerPeerConnected(peer);
                    break;
            }
        }

        private async void onPeerDisconnected(NetPeer peer, DisconnectInfo info) {
            Assert.IsTrue(_client.ConnectedPeersCount == 0);

            await Task.Delay((int)(UnityEngine.Random.Range(1.0f, 1.5f) * 1000));

            if (_client.IsRunning == false) { return; }

            _client.Connect(_host, _port, _role);
        }

        private void onNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
            try {
                var json = reader.GetString();
                var message = JsonUtility.FromJson<AirXRPlaygroundGameMessage>(json);

                switch (_role) {
                    case "director":
                        onDirectorNetworkReceive(peer, message.GetMessageType(), json);
                        break;
                    case "player":
                        onPlayerNetworkReceive(peer, message.GetMessageType(), json);
                        break;
                }
            }
            catch (Exception e) {
                Debug.LogWarningFormat("[WARNING] failed to handle playground game message: {0}", e);
            }
            finally {
                reader.Recycle();
            }
        }

        private void onDirectorPeerConnected(NetPeer peer) {
            sendMessage(peer, JsonUtility.ToJson(new AirXRPlaygroundGameUpdateStateMessage(_delegate.gameState)));
        }

        private async void onDirectorNetworkReceive(NetPeer peer, string type, string json) {
            switch (type) {
                case AirXRPlaygroundGameMessage.TypeRequestState:
                    sendMessage(peer, JsonUtility.ToJson(new AirXRPlaygroundGameUpdateStateMessage(_delegate.gameState)));
                    break;
                case AirXRPlaygroundGameMessage.TypeUpdateState:
                    var msgUpdateState = JsonUtility.FromJson<AirXRPlaygroundGameUpdateStateMessage>(json);

                    string nextScene = null;
                    if (_delegate.OnCheckIfNeedToLoadScene(msgUpdateState.GetState(), ref nextScene)) {
                        loadScene(nextScene);
                    }
                    else {
                        var task = _delegate.OnUpdateGameState(msgUpdateState.GetState());
                        if (task != null) {
                            await task;
                        }
                        sendUpdateGameState();
                    }
                    break;
                case AirXRPlaygroundGameMessage.TypeCommand:
                    var message = JsonUtility.FromJson<AirXRPlaygroundGameCommandMessage>(json);
                    _delegate.OnCommand(message.GetCommand(), message.GetArgument());
                    break;
            }
        }

        private void onPlayerPeerConnected(NetPeer peer) {
            // do nothing
        }

        private async void onPlayerNetworkReceive(NetPeer peer, string type, string json) {
            Task task = null;
            switch (type) {
                case AirXRPlaygroundGameMessage.TypeUpdateState:
                    var msgUpdateState = JsonUtility.FromJson<AirXRPlaygroundGameUpdateStateMessage>(json);

                    var nextScene = "";
                    if (_delegate.OnCheckIfNeedToLoadScene(msgUpdateState.GetState(), ref nextScene)) {
                        _stateToUpdateOnConfigure = msgUpdateState.GetState();

                        SceneManager.LoadScene(nextScene);
                    }
                    else {
                        task = _delegate.OnUpdateGameState(msgUpdateState.GetState());
                        if (task != null) {
                            await task;
                        }
                        sendUpdateGameState();
                    }
                    break;
                case AirXRPlaygroundGameMessage.TypeCommand:
                    var msgCommand = JsonUtility.FromJson<AirXRPlaygroundGameCommandMessage>(json);
                    _delegate.OnCommand(msgCommand.GetCommand(), msgCommand.GetArgument());
                    break;
                case AirXRPlaygroundGameMessage.TypeLoadScene:
                    var msgLoadScene = JsonUtility.FromJson<AirXRPlaygroundGameLoadSceneMessage>(json);

                    task = _delegate.OnPreLoadScene(msgLoadScene.GetScene());
                    if (task != null) {
                        await task;
                    }
                    SceneManager.LoadScene(msgLoadScene.GetScene());
                    break;
            }
        }

        private void sendMessage(NetPeer peer, string json) {
            _dataWriter.Put(json);
            peer.Send(_dataWriter, DeliveryMethod.ReliableOrdered);

            _dataWriter.Reset();
        }
    }
}
