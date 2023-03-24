/***********************************************************

  Copyright (c) 2021-present Clicked, Inc.

 ***********************************************************/

using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using LiteNetLib;
using LiteNetLib.Utils;
using onAirXR.Server;

namespace onAirXR.Playground.Server {
    public abstract class AirXRPlaygroundGameExtension : AirXRPlaygroundExtension, AirXRPlaygroundGameClient.Delegate {
        protected abstract string OnEvaluateCurrentGameState();
        protected abstract bool OnCheckIfNeedToLoadScene(string state, ref string nextScene);
        protected abstract Task OnPreLoadScene(string nextScene);
        protected abstract Task OnDirectorUpdateGameState(string state);
        protected abstract Task OnPlayerUpdateGameState(string state);
        protected abstract void OnDirectorCommand(string command, string argument);
        protected abstract void OnPlayerCommand(string command, string argument);
        protected abstract void OnMessageReceived(string clientID, int opcode, string data);

        public override string clientid => AirXRPlaygroundGameClient.id;

        public void LoadOtherScene(string scene) {
            if (playground.mode != AirXRPlayground.Mode.Observer) { return; }
            if (string.IsNullOrEmpty(scene) || SceneManager.GetActiveScene().name == scene) { return; }

            AirXRPlaygroundGameClient.LoadScene(scene);
        }

        public void SendMessageTo(string targetClientId, int opcode, string data = null) {
            if (string.IsNullOrEmpty(clientid) || string.IsNullOrEmpty(targetClientId)) { return; }
            
            AirXRPlaygroundGameClient.SendMessageTo(clientid, targetClientId, opcode, data);
        }

        public void SendMessageToAll(int opcode, string data = null) {
            if (string.IsNullOrEmpty(clientid)) { return; }

            AirXRPlaygroundGameClient.SendMessageTo(clientid, null, opcode, data);
        }

        public void SendUpdateGameState() {
            AirXRPlaygroundGameClient.SendUpdateGameState();
        }

        public void SendCommandToPlayer(string command, string argument = null) {
            if (playground.mode != AirXRPlayground.Mode.Observer) { return; }

            AirXRPlaygroundGameClient.SendCommand(command, argument);
        }

        public override void OnConnect(AXRPlayerConfig config) {
            AirXRPlaygroundGameClient.SendSessionState();
        }

        public override void OnActivate() {
            AirXRPlaygroundGameClient.SendSessionState();
        }

        public override void OnDeactivate() {
            AirXRPlaygroundGameClient.SendSessionState();
        }

        public override void OnDisconnect() {
            AirXRPlaygroundGameClient.SendSessionState();
        }

        public override void ProcessProfileData(string path) {
            if (File.Exists(path) == false) { return; }

            if (AirXRPlaygroundGameClient.SendProfileData(path)) {
                File.Delete(path);
            } 
        }

        public override void ProcessProfileReport(string report) {
            var config = AXRServer.instance.config;
            if (config == null) { return; }

            AirXRPlaygroundGameClient.SendProfileReport(config, report);
        }

        public override void ProcessQueryResponse(string statement, string body) {
            AirXRPlaygroundGameClient.ProcessQueryResponse(statement, body);
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

        void AirXRPlaygroundGameClient.Delegate.OnMessageReceived(string clientID, int opcode, string data) {
            OnMessageReceived(clientID, opcode, data);
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
            void OnMessageReceived(string clientID, int opcode, string data);
            void OnCommand(string command, string argument);
        }

        private static AirXRPlaygroundGameClient _instance;

        public static string id => _instance?._id;

        public static void LoadOnce() {
            if (_instance != null) { return; }

            _instance = new AirXRPlaygroundGameClient();
        }

        public static bool Configure(Delegate aDelegate, string address, string role) {
            return _instance?.configure(aDelegate, address, role) ?? false;
        }

        public static void SendMessageTo(string sourceClientId, string targetClientId, int opcode, string data) {
            _instance?.sendMessageTo(sourceClientId, targetClientId, opcode, data);
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

        public static void SendSessionState() {
            if (_instance == null || _instance._client.ConnectedPeersCount <= 0) { return; }

            _instance.sendSessionState(_instance._client.ConnectedPeerList[0]);
        }

        public static bool SendProfileData(string path) {
            return _instance?.sendProfileData(path) ?? false;
        }

        public static void SendProfileReport(AXRPlayerConfig config, string report) {
            _instance?.sendProfileReport(config, report);
        }

        public static void ProcessQueryResponse(string statement, string body) {
            _instance?.processQueryResponse(statement, body);
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
        private string _id;
        private NetDataWriter _dataWriter = new NetDataWriter();
        private string _stateToUpdateOnConfigure;
        private AirXRPlaygroundGameSessionState _sessionState = new AirXRPlaygroundGameSessionState();
        private FileStream _sessionDataFile;

        private string tempDirectory => Application.persistentDataPath;

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

        private void sendMessageTo(string sourceClientId, string targetClientId, int opcode, string data) {
            if (_client.ConnectedPeersCount == 0) { return; }

            sendMessage(_client.ConnectedPeerList[0],
                        JsonUtility.ToJson(new AirXRPlaygroundGameOpcodeMessage(sourceClientId, targetClientId, opcode, data)));
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

        private bool sendProfileData(string path) {
            if (_client.ConnectedPeersCount == 0) { return false; }

            const int MaxPayloadSize = 1200;

            var peer = _client.ConnectedPeerList[0];
            var filename = Path.GetFileName(path);
            var buffer = new byte[MaxPayloadSize];
            var total = 0L;

            using (var stream = File.OpenRead(path)) {
                var read = stream.Read(buffer, 0, MaxPayloadSize);
                while (read > 0) {
                    var begin = total == 0;
                    total += read;
                    var end = total == stream.Length;

                    sendProfileDataPartial(peer, filename, begin, end, buffer, 0, read);

                    read = stream.Read(buffer, 0, MaxPayloadSize);
                }
            }

            return true;
        }

        private void sendProfileReport(AXRPlayerConfig config, string report) {
            if (_client.ConnectedPeersCount == 0) { return; }

            var peer = _client.ConnectedPeerList[0];
            sendMessage(peer, JsonUtility.ToJson(new AirXRPlaygroundGameSessionProfileReport(report)));
        }

        private void processQueryResponse(string statement, string body) {
            if (statement.StartsWith("check-session-data ")) {
                _sessionState.sessionDataName = body;

                if (_client.ConnectedPeersCount > 0) {
                    sendSessionState(_client.ConnectedPeerList[0]);
                }
            }
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

                SceneManager.LoadSceneAsync(scene);
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

            _id = null;
            await Task.Delay((int)(UnityEngine.Random.Range(1.0f, 1.5f) * 1000));

            if (_client.IsRunning == false) { return; }

            _client.Connect(_host, _port, _role);
        }

        private void onNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
            try {
                var fcc = reader.PeekString(4);
                switch (fcc) {
                    case AirXRPlaygroundGameBinaryMessage.FCCSessionData:
                        if (_role == "player") {
                            onPlayerSessionDataReceived(peer, reader);
                        }
                        return;
                    default:
                        break;
                }

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
                case AirXRPlaygroundGameMessage.TypeConnected:
                    var msgConnected = JsonUtility.FromJson<AirXRPlaygroundGameConnectedMessage>(json);

                    _id = msgConnected.GetID();
                    break;
                case AirXRPlaygroundGameMessage.TypeOpcode:
                    var msgOpcode = JsonUtility.FromJson<AirXRPlaygroundGameOpcodeMessage>(json);

                    _delegate.OnMessageReceived(msgOpcode.GetSourceClientID(), msgOpcode.GetOpcode(), msgOpcode.GetData());
                    break;
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
                case AirXRPlaygroundGameMessage.TypeConnected:
                    var msgConnected = JsonUtility.FromJson<AirXRPlaygroundGameConnectedMessage>(json);

                    _id = msgConnected.GetID();
                    break;
                case AirXRPlaygroundGameMessage.TypeOpcode:
                    var msgOpcode = JsonUtility.FromJson<AirXRPlaygroundGameOpcodeMessage>(json);

                    _delegate.OnMessageReceived(msgOpcode.GetSourceClientID(), msgOpcode.GetOpcode(), msgOpcode.GetData());
                    break;
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
                case AirXRPlaygroundGameMessage.TypeSessionRequestState:
                    sendSessionState(peer);
                    break;
                case AirXRPlaygroundGameMessage.TypeSessionConfigure:
                    handleSessionConfigureMessage(peer, json);
                    break;
                case AirXRPlaygroundGameMessage.TypeSessionCheckSessionData:
                    handleSessionCheckSessionDataMessage(peer, json);
                    break;
                case AirXRPlaygroundGameMessage.TypeSessionPlay:
                    handleSessionPlayMessage(peer, json);
                    break;
                case AirXRPlaygroundGameMessage.TypeSessionStop:
                    handleSessionStopMessage(peer);
                    break;
                case AirXRPlaygroundGameMessage.TypeSessionStartProfile:
                    handleSessionStartProfile(peer, json);
                    break;
                case AirXRPlaygroundGameMessage.TypeSessionStopProfile:
                    handleSessionStopProfile(peer);
                    break;
            }
        }

        private void onPlayerSessionDataReceived(NetPeer peer, NetPacketReader reader) {
            reader.GetString(4); // skip FCC

            var filename = reader.GetString();
            var begin = reader.GetBool();
            var end = reader.GetBool();
            var data = reader.GetRemainingBytes();

            if (begin) {
                _sessionDataFile = new FileStream(Path.Combine(tempDirectory, filename), FileMode.OpenOrCreate, FileAccess.Write);
            }

            _sessionDataFile.Write(data, 0, data.Length);

            if (end) {
                _sessionDataFile.Close();
                _sessionDataFile.Dispose();
                _sessionDataFile = null;

                AXRServer.instance.RequestImportSessionData(Path.Combine(tempDirectory, filename));
            }
        }

        private void sendMessage(NetPeer peer, string json) {
            _dataWriter.Put(json);
            peer.Send(_dataWriter, DeliveryMethod.ReliableOrdered);

            _dataWriter.Reset();
        }

        private void sendProfileDataPartial(NetPeer peer, string filename, bool begin, bool end, byte[] data, int offset, int length) {
            _dataWriter.Put(AirXRPlaygroundGameBinaryMessage.FCCProfileData, 4);
            _dataWriter.Put(filename);
            _dataWriter.Put(begin);
            _dataWriter.Put(end);
            _dataWriter.Put(data, offset, length);

            peer.Send(_dataWriter, DeliveryMethod.ReliableOrdered);

            _dataWriter.Reset();
        }

        private void handleSessionConfigureMessage(NetPeer peer, string json) {
            if (AXRServer.instance.connected) {
                var msg = JsonUtility.FromJson<AirXRPlaygroundGameSessionConfigure>(json);

                _sessionState.minBitrate = msg.GetMinBitrate();
                _sessionState.startBitrate = msg.GetStartBitrate();
                _sessionState.maxBitrate = msg.GetMaxBitrate();

                AXRServer.instance.RequestConfigureSession(_sessionState.minBitrate, _sessionState.startBitrate, _sessionState.maxBitrate);
            }

            sendSessionState(peer);
        }

        private void handleSessionCheckSessionDataMessage(NetPeer peer, string json) {
            if (AXRServer.instance.connected) {
                var msg = JsonUtility.FromJson<AirXRPlaygroundGameSessionCheckSessionData>(json);

                if (string.IsNullOrEmpty(msg.GetSessionDataName()) == false) {
                    AXRServer.instance.RequestQuery($"check-session-data {msg.GetSessionDataName()}");
                }
            }
        }

        private async void handleSessionPlayMessage(NetPeer peer, string json) {
            if (AXRServer.instance.isOnStreaming == false && AXRServer.instance.connected) {
                var msg = JsonUtility.FromJson<AirXRPlaygroundGameSessionPlay>(json);

                AXRServer.instance.RequestPlay(string.IsNullOrEmpty(msg.GetSessionDataName()) == false ? msg.GetSessionDataName() : null);

                await Task.Delay(300);
            }

            sendSessionState(peer);
        }

        private async void handleSessionStopMessage(NetPeer peer) {
            if (AXRServer.instance.isOnStreaming) {
                AXRServer.instance.RequestStop();

                await Task.Delay(100);
            }

            sendSessionState(peer);
        }

        private void handleSessionStartProfile(NetPeer peer, string json) {
            if (AXRServer.instance.isProfiling == false) {
                var msg = JsonUtility.FromJson<AirXRPlaygroundGameSessionStartProfile>(json);
                _sessionState.sessionName = msg.GetSessionName();

                AXRServer.instance.RequestStartProfile(tempDirectory, msg.GetSessionName(), string.IsNullOrEmpty(msg.GetSessionDataName()) == false ? msg.GetSessionDataName() : null);
            }

            sendSessionState(peer);
        }

        private void handleSessionStopProfile(NetPeer peer) {
            if (AXRServer.instance.isProfiling) {
                AXRServer.instance.RequestStopProfile();
            }

            sendSessionState(peer);
        }

        private void sendSessionState(NetPeer peer) {
            var config = AXRServer.instance.connected ? AXRServer.instance.config : null;

            _sessionState.SetState(config?.userID ?? "",
                                   config != null ? (config.type == AXRPlayerType.Stereoscopic ? "stereo" : "mono") : "",
                                   config?.videoWidth ?? 0,
                                   config?.videoHeight ?? 0,
                                   config != null,
                                   AXRServer.instance.isOnStreaming, 
                                   AXRServer.instance.isProfiling);

            sendMessage(peer, JsonUtility.ToJson(new AirXRPlaygroundGameSessionUpdateState(_sessionState)));
        }
    }
}
