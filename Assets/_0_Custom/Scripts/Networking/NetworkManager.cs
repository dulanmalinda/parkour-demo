using UnityEngine;
using Colyseus;
using ParkourLegion.Schema;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParkourLegion.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField] private string serverUrl = "ws://localhost:2567";
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

        private async void Start()
        {
            await ConnectToServer();
        }

        private async Task ConnectToServer()
        {
            client = new ColyseusClient(serverUrl);

            try
            {
                room = await client.JoinOrCreate<ParkourRoomState>(roomName);
                Debug.Log($"Connected to room: {room.Id}, Session: {room.SessionId}");

                SetupRoomHandlers();
                SpawnLocalPlayer();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to connect to server: {e.Message}");
            }
        }

        private void SetupRoomHandlers()
        {
            room.OnStateChange += (state, isFirstState) =>
            {
                if (isFirstState)
                {
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
    }
}
