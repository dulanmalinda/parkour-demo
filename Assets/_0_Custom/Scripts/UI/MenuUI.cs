using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ParkourLegion.UI
{
    public class MenuUI : MonoBehaviour
    {
        private GameObject menuCanvas;
        private GameObject menuPanel;
        private TMP_InputField roomCodeInput;
        private Button joinRoomButton;
        private Button createRoomButton;

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

            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(menuPanel.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.7f);
            titleRect.anchorMax = new Vector2(0.5f, 0.7f);
            titleRect.sizeDelta = new Vector2(600, 80);
            titleRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "PARKOUR LEGION";
            titleText.fontSize = 48;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            GameObject inputGO = new GameObject("RoomCodeInput");
            inputGO.transform.SetParent(menuPanel.transform, false);
            RectTransform inputRect = inputGO.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.sizeDelta = new Vector2(400, 80);
            inputRect.anchoredPosition = new Vector2(-150, 80);

            Image inputBg = inputGO.AddComponent<Image>();
            inputBg.color = new Color(0.2f, 0.2f, 0.2f);

            roomCodeInput = inputGO.AddComponent<TMP_InputField>();
            roomCodeInput.characterLimit = 4;
            roomCodeInput.placeholder = CreatePlaceholder(inputGO.transform, "Room Code");
            roomCodeInput.textComponent = CreateInputText(inputGO.transform);
            roomCodeInput.onValueChanged.AddListener(OnRoomCodeChanged);

            joinRoomButton = CreateButton("JoinRoomButton", new Vector2(180, 80), new Vector2(220, 80), "JOIN", menuPanel.transform);
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            joinRoomButton.interactable = false;

            createRoomButton = CreateButton("CreateRoomButton", new Vector2(0, -50), new Vector2(350, 90), "CREATE ROOM", menuPanel.transform);
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);

            Debug.Log("MenuUI created with room code system");
        }

        private Button CreateButton(string name, Vector2 position, Vector2 size, string text, Transform parent)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = size;
            buttonRect.anchoredPosition = position;

            Button button = buttonGO.AddComponent<Button>();
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.18f, 0.8f, 0.44f);

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.8f, 0.44f);
            colors.highlightedColor = new Color(0.32f, 0.85f, 0.53f);
            colors.pressedColor = new Color(0.15f, 0.7f, 0.38f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f);
            button.colors = colors;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 32;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontStyle = FontStyles.Bold;

            return button;
        }

        private TextMeshProUGUI CreatePlaceholder(Transform parent, string text)
        {
            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(parent, false);
            RectTransform rect = placeholderGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = new Vector2(-10, 0);
            rect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = placeholderGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28;
            tmp.color = new Color(0.5f, 0.5f, 0.5f);
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.margin = new Vector4(15, 0, 0, 0);

            return tmp;
        }

        private TextMeshProUGUI CreateInputText(Transform parent)
        {
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(parent, false);
            RectTransform rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = new Vector2(-10, 0);
            rect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 32;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.margin = new Vector4(15, 0, 0, 0);

            return tmp;
        }

        private void OnRoomCodeChanged(string code)
        {
            roomCodeInput.text = code.ToUpper();
            joinRoomButton.interactable = code.Length == 4;
        }

        private void OnJoinRoomClicked()
        {
            string roomCode = roomCodeInput.text.ToUpper();
            if (roomCode.Length == 4 && GameUIManager.Instance != null)
            {
                GameUIManager.Instance.OnJoinRoomClicked(roomCode);
            }
        }

        private void OnCreateRoomClicked()
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.OnCreateRoomClicked();
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
