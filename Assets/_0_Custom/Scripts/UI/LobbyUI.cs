using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ParkourLegion.UI
{
    public class LobbyUI : MonoBehaviour
    {
        private GameObject lobbyCanvas;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI roomCodeText;
        private TextMeshProUGUI playerListText;
        private Button readyButton;
        private TextMeshProUGUI readyButtonText;
        private bool isReady = false;

        private void Awake()
        {
            CreateLobbyUI();
        }

        private void CreateLobbyUI()
        {
            lobbyCanvas = new GameObject("LobbyCanvas");
            lobbyCanvas.transform.SetParent(transform, false);

            Canvas canvas = lobbyCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = lobbyCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            lobbyCanvas.AddComponent<GraphicRaycaster>();

            GameObject roomCodeGO = new GameObject("RoomCodeText");
            roomCodeGO.transform.SetParent(lobbyCanvas.transform, false);
            RectTransform roomCodeRect = roomCodeGO.AddComponent<RectTransform>();
            roomCodeRect.anchorMin = new Vector2(0, 1);
            roomCodeRect.anchorMax = new Vector2(0, 1);
            roomCodeRect.pivot = new Vector2(0, 1);
            roomCodeRect.sizeDelta = new Vector2(500, 100);
            roomCodeRect.anchoredPosition = new Vector2(30, -30);

            roomCodeText = roomCodeGO.AddComponent<TextMeshProUGUI>();
            roomCodeText.fontSize = 48;
            roomCodeText.color = Color.yellow;
            roomCodeText.alignment = TextAlignmentOptions.TopLeft;
            roomCodeText.fontStyle = FontStyles.Bold;
            roomCodeText.text = "Room Code: ----";

            GameObject playerListGO = new GameObject("PlayerListText");
            playerListGO.transform.SetParent(lobbyCanvas.transform, false);
            RectTransform playerListRect = playerListGO.AddComponent<RectTransform>();
            playerListRect.anchorMin = new Vector2(0.5f, 0.6f);
            playerListRect.anchorMax = new Vector2(0.5f, 0.6f);
            playerListRect.sizeDelta = new Vector2(600, 300);
            playerListRect.anchoredPosition = Vector2.zero;

            playerListText = playerListGO.AddComponent<TextMeshProUGUI>();
            playerListText.fontSize = 36;
            playerListText.color = Color.black;
            playerListText.alignment = TextAlignmentOptions.Top;
            playerListText.text = "";

            GameObject textGO = new GameObject("StatusText");
            textGO.transform.SetParent(lobbyCanvas.transform, false);

            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(1000, 150);
            textRect.anchoredPosition = new Vector2(0, -80);

            statusText = textGO.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 48;
            statusText.color = Color.black;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.text = "";

            GameObject readyButtonGO = new GameObject("ReadyButton");
            readyButtonGO.transform.SetParent(lobbyCanvas.transform, false);
            RectTransform readyButtonRect = readyButtonGO.AddComponent<RectTransform>();
            readyButtonRect.anchorMin = new Vector2(0.5f, 0.35f);
            readyButtonRect.anchorMax = new Vector2(0.5f, 0.35f);
            readyButtonRect.sizeDelta = new Vector2(350, 90);
            readyButtonRect.anchoredPosition = Vector2.zero;

            readyButton = readyButtonGO.AddComponent<Button>();
            Image readyButtonImage = readyButtonGO.AddComponent<Image>();
            readyButtonImage.color = new Color(0.18f, 0.8f, 0.44f);

            ColorBlock colors = readyButton.colors;
            colors.normalColor = new Color(0.18f, 0.8f, 0.44f);
            colors.highlightedColor = new Color(0.32f, 0.85f, 0.53f);
            colors.pressedColor = new Color(0.15f, 0.7f, 0.38f);
            readyButton.colors = colors;

            GameObject readyTextGO = new GameObject("Text");
            readyTextGO.transform.SetParent(readyButtonGO.transform, false);
            RectTransform readyTextRect = readyTextGO.AddComponent<RectTransform>();
            readyTextRect.anchorMin = Vector2.zero;
            readyTextRect.anchorMax = Vector2.one;
            readyTextRect.sizeDelta = Vector2.zero;

            readyButtonText = readyTextGO.AddComponent<TextMeshProUGUI>();
            readyButtonText.text = "READY";
            readyButtonText.fontSize = 42;
            readyButtonText.color = Color.white;
            readyButtonText.alignment = TextAlignmentOptions.Center;
            readyButtonText.fontStyle = FontStyles.Bold;

            readyButton.onClick.AddListener(OnReadyButtonClicked);

            lobbyCanvas.SetActive(false);

            Debug.Log("LobbyUI created with ready system");
        }

        public void UpdateRoomCode(string code)
        {
            if (roomCodeText != null)
            {
                roomCodeText.text = $"Room Code: {code}";
            }
        }

        public void UpdatePlayerList(int playerCount, int maxPlayers, Dictionary<string, bool> playerReadyStates)
        {
            if (playerListText != null)
            {
                string list = $"Players: {playerCount}/{maxPlayers}\n\n";
                int index = 1;
                foreach (var kvp in playerReadyStates)
                {
                    string readyIcon = kvp.Value ? "[READY]" : "[WAITING]";
                    list += $"{index}. Player {readyIcon}\n";
                    index++;
                }
                playerListText.text = list;
            }
        }

        private void OnReadyButtonClicked()
        {
            isReady = !isReady;

            if (readyButtonText != null)
            {
                readyButtonText.text = isReady ? "READY!" : "READY";
            }

            var buttonImage = readyButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isReady ? new Color(0.8f, 0.8f, 0.18f) : new Color(0.18f, 0.8f, 0.44f);
            }

            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.OnReadyButtonClicked(isReady);
            }
        }

        public void ResetReadyState()
        {
            isReady = false;
            if (readyButtonText != null)
            {
                readyButtonText.text = "READY";
            }
            var buttonImage = readyButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.18f, 0.8f, 0.44f);
            }
        }

        public void ShowWaiting()
        {
            if (statusText != null)
            {
                statusText.text = "Waiting for players...";
            }

            if (lobbyCanvas != null)
            {
                lobbyCanvas.SetActive(true);
            }
        }

        public void ShowCountdown(int seconds)
        {
            if (statusText != null)
            {
                statusText.text = $"Game starts in {seconds}...";
            }
        }

        public void Show()
        {
            if (lobbyCanvas != null)
            {
                lobbyCanvas.SetActive(true);
            }
        }

        public void Hide()
        {
            if (lobbyCanvas != null)
            {
                lobbyCanvas.SetActive(false);
            }
        }
    }
}
