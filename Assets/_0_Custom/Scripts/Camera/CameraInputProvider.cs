using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.EventSystems;
using System;

namespace ParkourLegion.Camera
{
    public class CameraInputProvider : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float mouseSensitivityX = 200f;
        [SerializeField] private float mouseSensitivityY = 2f;
        [SerializeField] private bool invertY = false;

        [Header("WebGL Spike Protection")]
        [SerializeField] private float maxDeltaThreshold = 50f;

        [Header("References")]
        [SerializeField] private CinemachineCamera cinemachineCamera;

        private CinemachineOrbitalFollow orbitalFollow;
        private CinemachineInputAxisController inputAxisController;
        private CursorLockMode previousLockState = CursorLockMode.None;

        public event Action OnCursorLocked;
        public event Action OnCursorUnlocked;

        private void Start()
        {
            if (cinemachineCamera == null)
            {
                cinemachineCamera = GetComponent<CinemachineCamera>();
            }

            if (cinemachineCamera != null)
            {
                orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
                inputAxisController = cinemachineCamera.GetComponent<CinemachineInputAxisController>();

                if (orbitalFollow != null)
                {
                    orbitalFollow.HorizontalAxis.Value = 0f;
                    orbitalFollow.VerticalAxis.Value = 0f;
                }
            }
        }

        private void LateUpdate()
        {
            HandleMouseInput();
            HandleCursorToggle();
            DetectExternalCursorUnlock();
        }

        private void HandleMouseInput()
        {
            if (Cursor.lockState == CursorLockMode.Locked && orbitalFollow != null)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                if (Mathf.Abs(mouseX) > maxDeltaThreshold || Mathf.Abs(mouseY) > maxDeltaThreshold)
                {
                    return;
                }

                if (invertY)
                {
                    mouseY = -mouseY;
                }

                orbitalFollow.HorizontalAxis.Value += mouseX * mouseSensitivityX;
                orbitalFollow.VerticalAxis.Value += mouseY * mouseSensitivityY;
            }
        }

        private void HandleCursorToggle()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockCursor();
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (Cursor.lockState != CursorLockMode.Locked && !IsPointerOverUI() && IsInPlayingState())
                {
                    LockCursor();
                }
            }
        }

        private bool IsInPlayingState()
        {
            return UI.GameUIManager.Instance != null &&
                   UI.GameUIManager.Instance.CurrentState == UI.GameState.Playing;
        }

        private void DetectExternalCursorUnlock()
        {
            if (previousLockState == CursorLockMode.Locked && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.visible = true;
            }

            previousLockState = Cursor.lockState;
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            OnCursorLocked?.Invoke();
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            OnCursorUnlocked?.Invoke();
        }
    }
}
