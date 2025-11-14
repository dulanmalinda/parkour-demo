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
        private float updateTimer = 0f;

        public void Initialize(ColyseusRoom<ParkourRoomState> room)
        {
            this.room = room;
            playerController = GetComponent<PlayerController>();

            if (playerController == null)
            {
                Debug.LogError("PlayerController not found on LocalPlayer!");
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
