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
        [SerializeField] private RectTransform joystickArea;

        private CinemachineOrbitalFollow orbitalFollow;
        private CinemachineInputAxisController inputAxisController;
        private CursorLockMode previousLockState = CursorLockMode.None;
        private int joystickTouchId = -1;

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
                TrackJoystickTouch();

                float mouseX = 0f;
                float mouseY = 0f;

                if (Input.touchCount > 0)
                {
                    Vector2 cameraDelta = GetCameraRotationFromTouches();
                    mouseX = cameraDelta.x;
                    mouseY = cameraDelta.y;
                }
                else
                {
                    mouseX = Input.GetAxis("Mouse X");
                    mouseY = Input.GetAxis("Mouse Y");
                }

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

        private void TrackJoystickTouch()
        {
            if (joystickArea == null || !joystickArea.gameObject.activeInHierarchy)
            {
                joystickTouchId = -1;
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (touch.phase == TouchPhase.Began)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(joystickArea, touch.position, null))
                    {
                        joystickTouchId = touch.fingerId;
                        return;
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (touch.fingerId == joystickTouchId)
                    {
                        joystickTouchId = -1;
                    }
                }
            }
        }

        private Vector2 GetCameraRotationFromTouches()
        {
            Vector2 totalDelta = Vector2.zero;
            int validTouches = 0;

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                if (touch.fingerId == joystickTouchId)
                {
                    continue;
                }

                if (touch.phase == TouchPhase.Moved)
                {
                    totalDelta += touch.deltaPosition;
                    validTouches++;
                }
            }

            if (validTouches > 0)
            {
                totalDelta /= validTouches;
                return totalDelta * 0.1f;
            }

            return Vector2.zero;
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
