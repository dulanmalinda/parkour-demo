using UnityEngine;

namespace ParkourLegion.Player
{
    public class PlayerPhysics
    {
        private float gravity;
        private float groundCheckDistance;
        private LayerMask groundLayer;

        public float Gravity => gravity;

        public PlayerPhysics(float gravity, float groundCheckDistance, LayerMask groundLayer)
        {
            this.gravity = gravity;
            this.groundCheckDistance = groundCheckDistance;
            this.groundLayer = groundLayer;
        }

        public void ApplyGravity(ref Vector3 velocity, bool isGrounded, float deltaTime)
        {
            if (!isGrounded)
            {
                velocity.y += gravity * deltaTime;
            }
            else if (velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }

        public bool CheckGrounded(CharacterController controller, Transform transform)
        {
            Vector3 origin = transform.position;
            float checkDistance = (controller.height / 2f) + groundCheckDistance;
            return Physics.Raycast(origin, Vector3.down, checkDistance, groundLayer);
        }

        public float CalculateJumpVelocity(float jumpHeight)
        {
            return Mathf.Sqrt(jumpHeight * 2f * Mathf.Abs(gravity));
        }
    }
}
