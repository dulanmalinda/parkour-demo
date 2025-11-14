using UnityEngine;

namespace ParkourLegion.Player.States
{
    public class IdleState : PlayerState
    {
        public IdleState(PlayerController controller) : base(controller, "Idle")
        {
        }

        public override void Enter()
        {
            Vector3 velocity = controller.Velocity;
            velocity.x = 0f;
            velocity.z = 0f;
            controller.Velocity = velocity;
        }

        public override void Update()
        {
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
            if (controller.InputHandler.JumpPressed && controller.IsGrounded)
            {
                controller.StateMachine.ChangeState<JumpState>();
                return;
            }

            if (!controller.IsGrounded)
            {
                controller.StateMachine.ChangeState<FallState>();
                return;
            }

            Vector2 input = controller.InputHandler.MovementInput;
            if (input.magnitude > 0.1f)
            {
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
}
