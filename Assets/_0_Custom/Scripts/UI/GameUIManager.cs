using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ParkourLegion.UI
{
    public enum GameState
    {
        Menu,
        Connecting,
        Waiting,
        Countdown,
        Playing
    }

    public class GameUIManager : MonoBehaviour
    {
        public static GameUIManager Instance { get; private set; }

        private MenuUI menuUI;
        private LobbyUI lobbyUI;

        private GameState currentState = GameState.Menu;
        private float lobbyUpdateTimer = 0f;
        private readonly float lobbyUpdateInterval = 0.1f;

        public GameState CurrentState => currentState;

        [Header("Gameplay UI")]
        [SerializeField] private Canvas gameplayCanvas;

        [Header("Skin Selection")]
        [SerializeField] private Transform skinModelsContainer;
        [SerializeField] private Button skinLeftButton;
        [SerializeField] private Button skinRightButton;
        [SerializeField] private Button skinSelectButton;

        private int currentSkinIndex = 0;
        private int totalSkins = 0;

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
            menuUI = FindObjectOfType<MenuUI>();
            lobbyUI = FindObjectOfType<LobbyUI>();

            if (menuUI == null)
            {
                Debug.LogWarning("MenuUI not found in scene.");
            }

            if (lobbyUI == null)
            {
                Debug.LogWarning("LobbyUI not found in scene.");
            }

            if (gameplayCanvas != null)
            {
                gameplayCanvas.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Gameplay Canvas not assigned to GameUIManager!");
            }

            InitializeSkinSelection();

            SetState(GameState.Menu);
        }

        private void Update()
        {
            if (currentState == GameState.Waiting)
            {
                lobbyUpdateTimer += Time.deltaTime;
                if (lobbyUpdateTimer >= lobbyUpdateInterval)
                {
                    UpdateLobbyDisplay();
                    lobbyUpdateTimer = 0f;
                }
            }
        }

        private void UpdateLobbyDisplay()
        {
            var networkManager = Networking.NetworkManager.Instance;
            if (networkManager != null && networkManager.Room != null && lobbyUI != null)
            {
                string roomCode = networkManager.Room.State.roomCode;
                lobbyUI.UpdateRoomCode(roomCode);

                var playerStates = new Dictionary<string, bool>();
                networkManager.Room.State.players.ForEach((sessionId, playerState) =>
                {
                    playerStates[sessionId] = playerState.isReady;
                });

                int playerCount = networkManager.Room.State.playerCount;
                lobbyUI.UpdatePlayerList(playerCount, 4, playerStates);
            }
        }

        private void InitializeSkinSelection()
        {
            if (skinModelsContainer == null)
            {
                Debug.LogWarning("Skin models container not assigned");
                return;
            }

            totalSkins = skinModelsContainer.childCount;

            if (totalSkins == 0)
            {
                Debug.LogWarning("Skin models container has no children!");
                return;
            }

            if (skinLeftButton != null)
            {
                skinLeftButton.onClick.AddListener(OnSkinLeftClicked);
            }

            if (skinRightButton != null)
            {
                skinRightButton.onClick.AddListener(OnSkinRightClicked);
            }

            if (skinSelectButton != null)
            {
                skinSelectButton.onClick.AddListener(OnSkinSelectClicked);
            }

            ShowSkinModel(currentSkinIndex);
            Debug.Log($"Skin selection initialized with {totalSkins} skins");
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            Debug.Log($"GameUIManager: {currentState} → {newState}");
            currentState = newState;

            switch (currentState)
            {
                case GameState.Menu:
                    ShowMenuUI();
                    SetCursorState(false);
                    HideGameplayUI();
                    break;

                case GameState.Connecting:
                    HideAllUI();
                    SetCursorState(false);
                    HideGameplayUI();
                    break;

                case GameState.Waiting:
                    ShowLobbyUI();
                    if (lobbyUI != null)
                    {
                        lobbyUI.ShowWaiting();
                        lobbyUI.ResetReadyState();
                    }
                    SetCursorState(false);
                    HideGameplayUI();
                    break;

                case GameState.Countdown:
                    ShowLobbyUI();
                    SetCursorState(false);
                    HideGameplayUI();
                    break;

                case GameState.Playing:
                    HideAllUI();
                    SetCursorState(true);
                    ShowGameplayUI();
                    break;
            }
        }

        private void ShowGameplayUI()
        {
            if (gameplayCanvas != null)
            {
                SyncSkinSelectorWithPlayer();
                gameplayCanvas.gameObject.SetActive(true);
                Debug.Log("Gameplay UI shown");
            }
        }

        private void SyncSkinSelectorWithPlayer()
        {
            var networkManager = Networking.NetworkManager.Instance;
            if (networkManager != null && networkManager.Room != null)
            {
                var localPlayerState = networkManager.Room.State.players[networkManager.Room.SessionId];
                if (localPlayerState != null)
                {
                    currentSkinIndex = localPlayerState.skinId;
                    ShowSkinModel(currentSkinIndex);
                    Debug.Log($"Skin selector synced to player's current skin: {currentSkinIndex}");
                }
            }
        }

        private void HideGameplayUI()
        {
            if (gameplayCanvas != null)
            {
                gameplayCanvas.gameObject.SetActive(false);
            }
        }

        public void OnCreateRoomClicked()
        {
            Debug.Log("Create room button clicked");

            int skinId = UnityEngine.Random.Range(0, totalSkins);

            var networkManager = Networking.NetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.CreateRoom(skinId);
            }
            else
            {
                Debug.LogError("NetworkManager instance not found!");
            }
        }

        public void OnJoinRoomClicked(string roomCode)
        {
            Debug.Log($"Join room button clicked: {roomCode}");

            int skinId = UnityEngine.Random.Range(0, totalSkins);

            var networkManager = Networking.NetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.JoinRoomByCode(roomCode, skinId);
            }
            else
            {
                Debug.LogError("NetworkManager instance not found!");
            }
        }

        public void OnReadyButtonClicked(bool ready)
        {
            Debug.Log($"Ready button clicked: {ready}");

            var networkManager = Networking.NetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.SetPlayerReady(ready);
            }
        }

        [System.Obsolete("Use OnCreateRoomClicked() or OnJoinRoomClicked() instead")]
        public void OnPlayButtonClicked()
        {
            Debug.Log("Play button clicked - connecting to server");

            var networkManager = Networking.NetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.ConnectAndJoin();
            }
            else
            {
                Debug.LogError("NetworkManager instance not found!");
            }
        }

        private void ShowMenuUI()
        {
            if (menuUI != null) menuUI.Show();
            if (lobbyUI != null) lobbyUI.Hide();
        }

        private void ShowLobbyUI()
        {
            if (menuUI != null) menuUI.Hide();
            if (lobbyUI != null) lobbyUI.Show();
        }

        private void HideAllUI()
        {
            if (menuUI != null) menuUI.Hide();
            if (lobbyUI != null) lobbyUI.Hide();
        }

        private void SetCursorState(bool locked)
        {
            var cameraInput = FindObjectOfType<Camera.CameraInputProvider>();
            if (cameraInput != null)
            {
                if (locked)
                {
                    cameraInput.LockCursor();
                }
                else
                {
                    cameraInput.UnlockCursor();
                }
            }
        }

        private void OnSkinLeftClicked()
        {
            currentSkinIndex--;
            if (currentSkinIndex < 0)
            {
                currentSkinIndex = totalSkins - 1;
            }

            ShowSkinModel(currentSkinIndex);
            Debug.Log($"Skin changed: {currentSkinIndex}");
        }

        private void OnSkinRightClicked()
        {
            currentSkinIndex++;
            if (currentSkinIndex >= totalSkins)
            {
                currentSkinIndex = 0;
            }

            ShowSkinModel(currentSkinIndex);
            Debug.Log($"Skin changed: {currentSkinIndex}");
        }

        private void OnSkinSelectClicked()
        {
            Debug.Log($"Skin selected: {currentSkinIndex}");
            SendSkinChangeToServer(currentSkinIndex);
        }

        private void ShowSkinModel(int skinIndex)
        {
            if (skinModelsContainer == null) return;

            for (int i = 0; i < skinModelsContainer.childCount; i++)
            {
                skinModelsContainer.GetChild(i).gameObject.SetActive(i == skinIndex);
            }
        }

        private void SendSkinChangeToServer(int skinId)
        {
            var networkManager = Networking.NetworkManager.Instance;
            if (networkManager != null && networkManager.Room != null)
            {
                networkManager.Room.Send("selectSkin", new { skinId = skinId });
                Debug.Log($"Sent skin change to server: {skinId}");
            }
            else
            {
                Debug.LogWarning("Cannot send skin change - not connected to server");
            }
        }
    }
}
