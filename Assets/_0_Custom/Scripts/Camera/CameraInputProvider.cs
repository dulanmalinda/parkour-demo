using UnityEngine;
using Unity.Cinemachine;

namespace ParkourLegion.Camera
{
    public class CameraInputProvider : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float mouseSensitivityX = 200f;
        [SerializeField] private float mouseSensitivityY = 2f;
        [SerializeField] private bool invertY = false;

        [Header("References")]
        [SerializeField] private CinemachineCamera cinemachineCamera;

        private CinemachineOrbitalFollow orbitalFollow;
        private CinemachineInputAxisController inputAxisController;

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

        private void Update()
        {
            HandleMouseInput();
            HandleCursorToggle();
        }

        private void HandleMouseInput()
        {
            if (Cursor.lockState == CursorLockMode.Locked && orbitalFollow != null)
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;

                if (invertY)
                {
                    mouseY = -mouseY;
                }

                orbitalFollow.HorizontalAxis.Value += mouseX;
                orbitalFollow.VerticalAxis.Value += mouseY;
            }
        }

        private void HandleCursorToggle()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    UnlockCursor();
                }
                else
                {
                    LockCursor();
                }
            }
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
