using UnityEngine;
using ParkourLegion.Schema;

namespace ParkourLegion.Networking
{
    public class RemotePlayerNetworkSync : MonoBehaviour
    {
        [Header("Interpolation Settings")]
        [SerializeField] private float interpolationSpeed = 10f;

        private PlayerState playerState;
        private Vector3 targetPosition;
        private float targetRotationY;

        public void Initialize(PlayerState state)
        {
            playerState = state;

            targetPosition = new Vector3(state.x, state.y, state.z);
            targetRotationY = state.rotY;

            transform.position = targetPosition;
            transform.rotation = Quaternion.Euler(0, targetRotationY, 0);
        }

        private void Update()
        {
            if (playerState == null) return;

            targetPosition = new Vector3(playerState.x, playerState.y, playerState.z);
            targetRotationY = playerState.rotY;

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * interpolationSpeed
            );

            Quaternion currentRotation = transform.rotation;
            Quaternion targetRotation = Quaternion.Euler(0, targetRotationY, 0);
            transform.rotation = Quaternion.Slerp(
                currentRotation,
                targetRotation,
                Time.deltaTime * interpolationSpeed
            );
        }
    }
}
