using UnityEngine;

namespace ParkourLegion.Player.States
{
    public class FallState : PlayerState
    {
        private const float airControlMultiplier = 0.5f;

        public FallState(PlayerController controller) : base(controller, "Fall")
        {
        }

        public override void Enter()
        {
        }

        public override void Update()
        {
            Vector2 input = controller.InputHandler.MovementInput;
            Vector3 moveDirection = controller.GetCameraRelativeMovement(input);

            controller.Move(moveDirection * controller.RunSpeed * airControlMultiplier);

            Vector3 velocity = controller.Velocity;
            controller.Physics.ApplyGravity(ref velocity, controller.IsGrounded, Time.deltaTime);
            controller.Velocity = velocity;

            controller.ApplyVelocity();
        }

        public override void Exit()
        {
            Vector3 velocity = controller.Velocity;
            if (controller.IsGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
                controller.Velocity = velocity;
            }
        }

        public override void CheckTransitions()
        {
            if (!controller.IsGrounded)
                return;

            Vector2 input = controller.InputHandler.MovementInput;

            if (input.magnitude < 0.1f)
            {
                controller.StateMachine.ChangeState<IdleState>();
                return;
            }

            if (controller.InputHandler.IsRunning)
            {
                controller.StateMachine.ChangeState<RunState>();
            }
            else
            {
                controller.StateMachine.ChangeState<WalkState>();
            }
        }
    }
}
