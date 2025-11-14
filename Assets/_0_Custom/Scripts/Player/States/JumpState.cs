using UnityEngine;

namespace ParkourLegion.Player.States
{
    public class JumpState : PlayerState
    {
        private const float airControlMultiplier = 0.5f;

        public JumpState(PlayerController controller) : base(controller, "Jump")
        {
        }

        public override void Enter()
        {
            Vector3 velocity = controller.Velocity;
            velocity.y = controller.Physics.CalculateJumpVelocity(controller.JumpHeight);
            controller.Velocity = velocity;
        }

        public override void Update()
        {
            Vector2 input = controller.InputHandler.MovementInput;
            Vector3 moveDirection = controller.transform.right * input.x + controller.transform.forward * input.y;
            moveDirection.Normalize();

            controller.Move(moveDirection * controller.RunSpeed * airControlMultiplier);

            Vector3 velocity = controller.Velocity;
            controller.Physics.ApplyGravity(ref velocity, controller.IsGrounded, Time.deltaTime);
            controller.Velocity = velocity;

            controller.ApplyVelocity();
        }

        public override void Exit()
        {
        }

        public override void CheckTransitions()
        {
            if (controller.Velocity.y <= 0)
            {
                controller.StateMachine.ChangeState<FallState>();
            }
        }
    }
}
