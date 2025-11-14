using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ParkourLegion.UI
{
    public class LobbyUI : MonoBehaviour
    {
        private GameObject lobbyCanvas;
        private TextMeshProUGUI statusText;

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

            GameObject textGO = new GameObject("StatusText");
            textGO.transform.SetParent(lobbyCanvas.transform, false);

            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(800, 100);
            textRect.anchoredPosition = Vector2.zero;

            statusText = textGO.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 36;
            statusText.color = Color.white;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.text = "";

            lobbyCanvas.SetActive(false);

            Debug.Log("LobbyUI created programmatically");
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
