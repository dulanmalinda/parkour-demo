using UnityEngine;
using Colyseus;
using ParkourLegion.Schema;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace ParkourLegion.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField] private string serverUrl = "wss://parkour-demo-colysues-server.onrender.com";
        [SerializeField] private string roomName = "parkour";

        [Header("Prefab References")]
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private GameObject remotePlayerPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(0, 1, 0);

        private ColyseusClient client;
        private ColyseusRoom<ParkourRoomState> room;
        private GameObject localPlayer;
        private Dictionary<string, GameObject> remotePlayers = new Dictionary<string, GameObject>();
        private string lastGameState = "";
        private byte lastCountdownValue = 0;

        public static NetworkManager Instance { get; private set; }
        public ColyseusRoom<ParkourRoomState> Room => room;
        public string LocalSessionId => room?.SessionId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
        }

        public async void CreateRoom(int skinId)
        {
            if (UI.GameUIManager.Instance != null)
            {
                UI.GameUIManager.Instance.SetState(UI.GameState.Connecting);
            }

            await CreateRoomAsync(skinId);
        }

        public async void JoinRoomByCode(string roomCode, int skinId)
        {
            if (UI.GameUIManager.Instance != null)
            {
                UI.GameUIManager.Instance.SetState(UI.GameState.Connecting);
            }

            await JoinRoomByCodeAsync(roomCode, skinId);
        }

        [System.Obsolete("Use CreateRoom() or JoinRoomByCode() instead")]
        public async void ConnectAndJoin()
        {
            if (UI.GameUIManager.Instance != null)
            {
                UI.GameUIManager.Instance.SetState(UI.GameState.Connecting);
            }

            await ConnectToServer();

            if (UI.GameUIManager.Instance != null)
            {
                UI.GameUIManager.Instance.SetState(UI.GameState.Waiting);
            }
        }

        private async Task ConnectToServer()
        {
            client = new ColyseusClient(serverUrl);

            try
            {
                int randomSkinId = Random.Range(0, 4);

                var options = new Dictionary<string, object>
                {
                    { "skinId", randomSkinId }
                };

                room = await client.JoinOrCreate<ParkourRoomState>(roomName, options);
                Debug.Log($"Connected to room: {room.Id}, Session: {room.SessionId}, Selected skinId: {randomSkinId}");

                SetupRoomHandlers();
                SpawnLocalPlayer();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to connect to server: {e.Message}");

                if (UI.GameUIManager.Instance != null)
                {
                    UI.GameUIManager.Instance.SetState(UI.GameState.Menu);
                }
            }
        }

        private void SetupRoomHandlers()
        {
            room.OnStateChange += (state, isFirstState) =>
            {
                if (isFirstState)
                {
                    UpdateLocalPlayerPositionFromState(state);
                    SpawnExistingPlayersFromState(state);

                    var callbacks = Colyseus.Schema.Callbacks.Get(room);

                    callbacks.OnAdd(s => s.players, (key, player) =>
                    {
                        if (key == room.SessionId)
                        {
                            return;
                        }

                        SpawnRemotePlayer(key, player);
                    });

                    callbacks.OnRemove(s => s.players, (key, player) =>
                    {
                        RemoveRemotePlayer(key);
                    });
                }
                else
                {
                    if (state.gameState != lastGameState)
                    {
                        lastGameState = state.gameState;
                        HandleGameStateChange(state.gameState);
                    }

                    if (state.countdownValue != lastCountdownValue)
                    {
                        lastCountdownValue = state.countdownValue;
                        HandleCountdownUpdate(state.countdownValue);
                    }
                }
            };

            room.OnLeave += (code) =>
            {
                Debug.Log($"Left room with code: {code}");
            };

            room.OnError += (code, message) =>
            {
                Debug.LogError($"Room error {code}: {message}");
            };
        }

        private void SpawnLocalPlayer()
        {
            if (localPlayerPrefab == null)
            {
                Debug.LogError("Local player prefab not assigned!");
                return;
            }

            localPlayer = Instantiate(localPlayerPrefab, spawnPosition, Quaternion.identity);
            localPlayer.name = "LocalPlayer";

            LocalPlayerNetworkSync networkSync = localPlayer.GetComponent<LocalPlayerNetworkSync>();
            if (networkSync != null)
            {
                networkSync.Initialize(room);
            }
            else
            {
                Debug.LogWarning("LocalPlayerNetworkSync component not found on local player prefab");
            }

            SetupCameraTarget();
        }

        private void SetupCameraTarget()
        {
            Transform cameraTarget = localPlayer.transform.Find("CameraTarget");
            if (cameraTarget == null)
            {
                Debug.LogWarning("CameraTarget not found on LocalPlayer. Camera may not follow player.");
                cameraTarget = localPlayer.transform;
            }

            var cinemachineCamera = FindObjectOfType<Unity.Cinemachine.CinemachineCamera>();
            if (cinemachineCamera != null)
            {
                cinemachineCamera.Follow = cameraTarget;
                cinemachineCamera.LookAt = cameraTarget;
            }
        }

        private void UpdateLocalPlayerPositionFromState(ParkourRoomState state)
        {
            if (state?.players == null || localPlayer == null) return;

            state.players.ForEach((sessionId, playerState) =>
            {
                if (sessionId == room.SessionId)
                {
                    Vector3 serverPosition = new Vector3(playerState.x, playerState.y, playerState.z);
                    localPlayer.transform.position = serverPosition;
                    Debug.Log($"Local player synced to server spawn position: {serverPosition}");

                    LocalPlayerNetworkSync networkSync = localPlayer.GetComponent<LocalPlayerNetworkSync>();
                    if (networkSync != null)
                    {
                        networkSync.InitializeSkin();
                    }
                }
            });
        }

        private void SpawnExistingPlayersFromState(ParkourRoomState state)
        {
            if (state?.players == null) return;

            state.players.ForEach((sessionId, playerState) =>
            {
                if (sessionId == room.SessionId) return;

                SpawnRemotePlayer(sessionId, playerState);
            });
        }

        private void SpawnRemotePlayer(string sessionId, PlayerState playerState)
        {
            if (remotePlayerPrefab == null)
            {
                Debug.LogError("Remote player prefab not assigned!");
                return;
            }

            if (remotePlayers.ContainsKey(sessionId))
            {
                return;
            }

            Vector3 position = new Vector3(playerState.x, playerState.y, playerState.z);
            Quaternion rotation = Quaternion.Euler(0, playerState.rotY, 0);

            GameObject remotePlayer = Instantiate(remotePlayerPrefab, position, rotation);
            remotePlayer.name = $"RemotePlayer_{sessionId}";

            RemotePlayerNetworkSync networkSync = remotePlayer.GetComponent<RemotePlayerNetworkSync>();
            if (networkSync != null)
            {
                networkSync.Initialize(playerState);
            }

            RemotePlayerController controller = remotePlayer.GetComponent<RemotePlayerController>();
            if (controller != null)
            {
                controller.Initialize(playerState);
            }

            remotePlayers.Add(sessionId, remotePlayer);
            Debug.Log($"Remote player connected: {sessionId}");
        }

        private void RemoveRemotePlayer(string sessionId)
        {
            if (remotePlayers.TryGetValue(sessionId, out GameObject remotePlayer))
            {
                Destroy(remotePlayer);
                remotePlayers.Remove(sessionId);
                Debug.Log($"Remote player disconnected: {sessionId}");
            }
        }

        private void OnDestroy()
        {
            if (room != null)
            {
                room.Leave();
            }
        }

        private async void OnApplicationQuit()
        {
            if (room != null)
            {
                await room.Leave();
            }
        }

        private void HandleGameStateChange(string newState)
        {
            Debug.Log($"Game state changed to: {newState}");

            if (UI.GameUIManager.Instance == null) return;

            switch (newState)
            {
                case "waiting":
                    UI.GameUIManager.Instance.SetState(UI.GameState.Waiting);
                    break;
                case "countdown":
                    UI.GameUIManager.Instance.SetState(UI.GameState.Countdown);
                    break;
                case "playing":
                    UI.GameUIManager.Instance.SetState(UI.GameState.Playing);
                    EnableLocalPlayerMovement();
                    break;
            }
        }

        private void HandleCountdownUpdate(byte countdown)
        {
            Debug.Log($"Countdown: {countdown}");

            if (UI.GameUIManager.Instance != null &&
                UI.GameUIManager.Instance.CurrentState == UI.GameState.Countdown)
            {
                var lobbyUI = FindObjectOfType<UI.LobbyUI>();
                lobbyUI?.ShowCountdown(countdown);
            }
        }

        private void EnableLocalPlayerMovement()
        {
            if (localPlayer != null)
            {
                var controller = localPlayer.GetComponent<Player.PlayerController>();
                if (controller != null)
                {
                    controller.MovementEnabled = true;
                    Debug.Log("Local player movement enabled");
                }
            }
        }

        private async Task CreateRoomAsync(int skinId)
        {
            client = new ColyseusClient(serverUrl);

            try
            {
                var options = new Dictionary<string, object>
                {
                    { "skinId", skinId }
                };

                room = await client.Create<ParkourRoomState>("parkour", options);
                Debug.Log($"Created room with code: {room.State.roomCode}, Session: {room.SessionId}");

                SetupRoomHandlers();
                SpawnLocalPlayer();

                if (UI.GameUIManager.Instance != null)
                {
                    UI.GameUIManager.Instance.SetState(UI.GameState.Waiting);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create room: {e.Message}");
                if (UI.GameUIManager.Instance != null)
                {
                    UI.GameUIManager.Instance.SetState(UI.GameState.Menu);
                }
            }
        }

        private async Task JoinRoomByCodeAsync(string roomCode, int skinId)
        {
            client = new ColyseusClient(serverUrl);

            try
            {
                string roomId = await GetRoomIdByCode(roomCode);

                if (string.IsNullOrEmpty(roomId))
                {
                    throw new System.Exception("Room not found");
                }

                var options = new Dictionary<string, object>
                {
                    { "skinId", skinId }
                };

                room = await client.JoinById<ParkourRoomState>(roomId, options);
                Debug.Log($"Joined room {roomCode}, Session: {room.SessionId}");

                SetupRoomHandlers();
                SpawnLocalPlayer();

                if (UI.GameUIManager.Instance != null)
                {
                    UI.GameUIManager.Instance.SetState(UI.GameState.Waiting);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to join room: {e.Message}");
                if (UI.GameUIManager.Instance != null)
                {
                    UI.GameUIManager.Instance.SetState(UI.GameState.Menu);
                }
            }
        }

        private async Task<string> GetRoomIdByCode(string roomCode)
        {
            string url = $"https://parkour-demo-colysues-server.onrender.com/api/find-room/{roomCode}";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<RoomIdResponse>(request.downloadHandler.text);
                    return response.roomId;
                }

                return null;
            }
        }

        public void SetPlayerReady(bool ready)
        {
            if (room != null)
            {
                room.Send("playerReady", new { isReady = ready });
                Debug.Log($"Sent ready state: {ready}");
            }
        }
    }

    [System.Serializable]
    public class RoomIdResponse
    {
        public string roomId;
        public int players;
        public int maxPlayers;
    }
}
