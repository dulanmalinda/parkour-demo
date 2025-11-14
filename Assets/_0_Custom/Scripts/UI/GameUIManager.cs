using UnityEngine;

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

        public GameState CurrentState => currentState;

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
                Debug.LogWarning("MenuUI not found in scene. Creating from GameUIManager.");
            }

            if (lobbyUI == null)
            {
                Debug.LogWarning("LobbyUI not found in scene. Creating from GameUIManager.");
            }

            SetState(GameState.Menu);
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
                    break;

                case GameState.Connecting:
                    HideAllUI();
                    SetCursorState(false);
                    break;

                case GameState.Waiting:
                    ShowLobbyUI();
                    if (lobbyUI != null) lobbyUI.ShowWaiting();
                    SetCursorState(false);
                    break;

                case GameState.Countdown:
                    ShowLobbyUI();
                    SetCursorState(false);
                    break;

                case GameState.Playing:
                    HideAllUI();
                    SetCursorState(true);
                    break;
            }
        }

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
    }
}
