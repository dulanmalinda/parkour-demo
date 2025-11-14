using UnityEngine;

namespace ParkourLegion.Player.States
{
    public class WalkState : PlayerState
    {
        public WalkState(PlayerController controller) : base(controller, "Walk")
        {
        }

        public override void Enter()
        {
        }

        public override void Update()
        {
            Vector2 input = controller.InputHandler.MovementInput;
            Vector3 moveDirection = controller.GetCameraRelativeMovement(input);

            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                controller.transform.rotation = Quaternion.Slerp(
                    controller.transform.rotation,
                    targetRotation,
                    Time.deltaTime * 10f
                );

                controller.Move(moveDirection.normalized * controller.WalkSpeed);
            }

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

            if (controller.InputHandler.SlidePressed)
            {
                controller.StateMachine.ChangeState<SlideState>();
                return;
            }

            if (!controller.IsGrounded)
            {
                controller.StateMachine.ChangeState<FallState>();
                return;
            }

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
        }
    }
}
