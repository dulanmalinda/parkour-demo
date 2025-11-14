using UnityEngine;

namespace ParkourLegion.Player.States
{
    public class SlideState : PlayerState
    {
        private float slideTime;
        private Vector3 slideDirection;
        private float slideStartSpeed;
        private float originalHeight;
        private Vector3 originalCenter;

        public SlideState(PlayerController controller) : base(controller, "Slide")
        {
        }

        public override void Enter()
        {
            slideTime = 0f;
            slideDirection = controller.transform.forward;
            slideStartSpeed = controller.SlideSpeed;

            originalHeight = controller.CharacterController.height;
            originalCenter = controller.CharacterController.center;

            controller.CharacterController.height = originalHeight * 0.5f;
        }

        public override void Update()
        {
            slideTime += Time.deltaTime;

            float t = slideTime / controller.SlideDuration;
            float currentSpeed = Mathf.Lerp(slideStartSpeed, controller.WalkSpeed, t);

            controller.Move(slideDirection * currentSpeed);

            Vector3 velocity = controller.Velocity;
            controller.Physics.ApplyGravity(ref velocity, controller.IsGrounded, Time.deltaTime);
            controller.Velocity = velocity;

            controller.ApplyVelocity();
        }

        public override void Exit()
        {
            controller.CharacterController.height = originalHeight;
        }

        public override void CheckTransitions()
        {
            if (controller.InputHandler.JumpPressed)
            {
                controller.StateMachine.ChangeState<JumpState>();
                return;
            }

            if (slideTime >= controller.SlideDuration)
            {
                Vector2 input = controller.InputHandler.MovementInput;

                if (input.magnitude < 0.1f)
                {
                    controller.StateMachine.ChangeState<IdleState>();
                }
                else
                {
                    controller.StateMachine.ChangeState<WalkState>();
                }
            }
        }
    }
}
