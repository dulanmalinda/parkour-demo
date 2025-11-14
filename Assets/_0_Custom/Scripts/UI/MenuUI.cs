using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ParkourLegion.UI
{
    public class MenuUI : MonoBehaviour
    {
        private GameObject menuCanvas;
        private GameObject menuPanel;
        private Button playButton;

        private void Awake()
        {
            CreateMenuUI();
        }

        private void CreateMenuUI()
        {
            menuCanvas = new GameObject("MenuCanvas");
            menuCanvas.transform.SetParent(transform, false);

            Canvas canvas = menuCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = menuCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            menuCanvas.AddComponent<GraphicRaycaster>();

            menuPanel = new GameObject("MenuPanel");
            menuPanel.transform.SetParent(menuCanvas.transform, false);

            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = menuPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            GameObject buttonGO = new GameObject("PlayButton");
            buttonGO.transform.SetParent(menuPanel.transform, false);

            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(200, 60);
            buttonRect.anchoredPosition = Vector2.zero;

            playButton = buttonGO.AddComponent<Button>();

            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.18f, 0.8f, 0.44f);

            ColorBlock colors = playButton.colors;
            colors.normalColor = new Color(0.18f, 0.8f, 0.44f);
            colors.highlightedColor = new Color(0.32f, 0.85f, 0.53f);
            colors.pressedColor = new Color(0.15f, 0.7f, 0.38f);
            colors.selectedColor = new Color(0.18f, 0.8f, 0.44f);
            playButton.colors = colors;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "PLAY";
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            playButton.onClick.AddListener(OnPlayButtonClicked);

            Debug.Log("MenuUI created programmatically");
        }

        private void OnPlayButtonClicked()
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.OnPlayButtonClicked();
            }
        }

        public void Show()
        {
            if (menuCanvas != null)
            {
                menuCanvas.SetActive(true);
            }
        }

        public void Hide()
        {
            if (menuCanvas != null)
            {
                menuCanvas.SetActive(false);
            }
        }
    }
}
