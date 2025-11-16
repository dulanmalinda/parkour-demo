using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ParkourLegion.UI
{
    public class ClickToResumeOverlay : MonoBehaviour
    {
        [Header("Manual UI Panel")]
        [SerializeField] private GameObject clickToPlayPanel;

        [Header("Optional: Pulsing Effect")]
        [SerializeField] private bool enablePulsing = true;
        [SerializeField] private float pulseSpeed = 0.8f;
        [SerializeField] private TextMeshProUGUI pulsingText;

        private bool isVisible = false;
        private Camera.CameraInputProvider cameraInputProvider;

        private void Start()
        {
            Hide();
            SubscribeToCameraEvents();
        }


        private void SubscribeToCameraEvents()
        {
            cameraInputProvider = FindObjectOfType<Camera.CameraInputProvider>();

            if (cameraInputProvider != null)
            {
                cameraInputProvider.OnCursorUnlocked += HandleCursorUnlocked;
                cameraInputProvider.OnCursorLocked += HandleCursorLocked;
            }
            else
            {
                Debug.LogWarning("CameraInputProvider not found! ClickToResumeOverlay will not function.");
            }
        }

        private void HandleCursorUnlocked()
        {
            if (GameUIManager.Instance != null && GameUIManager.Instance.CurrentState == GameState.Playing)
            {
                Show();
            }
        }

        private void HandleCursorLocked()
        {
            Hide();
        }

        public void Show()
        {
            if (clickToPlayPanel != null)
            {
                clickToPlayPanel.SetActive(true);
                isVisible = true;
            }
        }

        public void Hide()
        {
            if (clickToPlayPanel != null)
            {
                clickToPlayPanel.SetActive(false);
                isVisible = false;
            }
        }

        public void OnPanelClicked()
        {
            if (cameraInputProvider != null)
            {
                cameraInputProvider.LockCursor();
            }
        }

        private void Update()
        {
            if (isVisible && enablePulsing && pulsingText != null)
            {
                float alpha = Mathf.PingPong(Time.time * pulseSpeed, 0.3f) + 0.7f;
                Color color = pulsingText.color;
                color.a = alpha;
                pulsingText.color = color;
            }
        }

        private void OnDestroy()
        {
            if (cameraInputProvider != null)
            {
                cameraInputProvider.OnCursorUnlocked -= HandleCursorUnlocked;
                cameraInputProvider.OnCursorLocked -= HandleCursorLocked;
            }
        }
    }
}
