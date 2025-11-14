using UnityEngine;
using ParkourLegion.Schema;
using ParkourLegion.Player;

namespace ParkourLegion.Networking
{
    public class RemotePlayerNetworkSync : MonoBehaviour
    {
        [Header("Interpolation Settings")]
        [SerializeField] private float interpolationSpeed = 10f;

        private Schema.PlayerState playerState;
        private PlayerModelManager modelManager;
        private Vector3 targetPosition;
        private float targetRotationY;
        private int lastMovementState = -1;

        public void Initialize(Schema.PlayerState state)
        {
            playerState = state;
            modelManager = GetComponent<PlayerModelManager>();

            targetPosition = new Vector3(state.x, state.y, state.z);
            targetRotationY = state.rotY;

            transform.position = targetPosition;
            transform.rotation = Quaternion.Euler(0, targetRotationY, 0);

            if (modelManager == null)
            {
                Debug.LogWarning("PlayerModelManager not found on RemotePlayer!");
            }
            else
            {
                modelManager.SetModel(state.skinId);
                lastMovementState = state.movementState;
                modelManager.UpdateAnimation(lastMovementState);
            }
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

            if (modelManager != null && playerState.movementState != lastMovementState)
            {
                modelManager.UpdateAnimation(playerState.movementState);
                lastMovementState = playerState.movementState;
            }
        }
    }
}
