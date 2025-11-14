using UnityEngine;
using Colyseus;
using ParkourLegion.Schema;
using ParkourLegion.Player;

namespace ParkourLegion.Networking
{
    public class LocalPlayerNetworkSync : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float updateRate = 0.05f;

        private ColyseusRoom<ParkourRoomState> room;
        private PlayerController playerController;
        private PlayerModelManager modelManager;
        private float updateTimer = 0f;
        private int lastMovementState = -1;

        public void Initialize(ColyseusRoom<ParkourRoomState> room)
        {
            this.room = room;
            playerController = GetComponent<PlayerController>();
            modelManager = GetComponent<PlayerModelManager>();

            if (playerController == null)
            {
                Debug.LogError("PlayerController not found on LocalPlayer!");
            }

            if (modelManager == null)
            {
                Debug.LogWarning("PlayerModelManager not found on LocalPlayer!");
            }
            else
            {
                int availableModels = modelManager.GetAvailableModelCount();
                if (availableModels > 0)
                {
                    int randomSkin = Random.Range(0, availableModels);
                    room.Send("selectSkin", new { skinId = randomSkin });
                    modelManager.SetModel(randomSkin);
                    Debug.Log($"LocalPlayer selected skin: {randomSkin} (out of {availableModels} available)");
                }
                else
                {
                    Debug.LogError("No models found in GFXs container!");
                }
            }
        }

        private void FixedUpdate()
        {
            if (room == null || playerController == null)
            {
                return;
            }

            updateTimer += Time.fixedDeltaTime;

            if (updateTimer >= updateRate)
            {
                SendPositionUpdate();
                updateTimer = 0f;
            }
        }

        private void Update()
        {
            if (modelManager == null || playerController == null)
            {
                return;
            }

            int currentMovementState = playerController.GetMovementStateInt();
            if (currentMovementState != lastMovementState)
            {
                modelManager.UpdateAnimation(currentMovementState);
                lastMovementState = currentMovementState;
            }
        }

        private void SendPositionUpdate()
        {
            Vector3 position = transform.position;
            float rotationY = transform.rotation.eulerAngles.y;
            int movementState = playerController.GetMovementStateInt();
            bool isGrounded = playerController.IsGrounded;

            room.Send("updatePosition", new
            {
                x = position.x,
                y = position.y,
                z = position.z,
                rotY = rotationY,
                movementState = movementState,
                isGrounded = isGrounded
            });
        }

        public void SendCheckpointReached(int checkpointId)
        {
            if (room != null)
            {
                room.Send("checkpointReached", new { checkpointId = checkpointId });
            }
        }
    }
}
