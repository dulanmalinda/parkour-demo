using UnityEngine;

namespace ParkourLegion.Player
{
    public class PlayerInputHandler
    {
        private VariableJoystick variableJoystick;

        private Vector2 movementInput;
        private bool isRunning;
        private bool jumpPressed;
        private bool slidePressed;

        private bool runButtonHeld = false;
        private bool jumpButtonPressed = false;
        private bool slideButtonPressed = false;

        public Vector2 MovementInput => movementInput;
        public bool IsRunning => isRunning;
        public bool JumpPressed => jumpPressed;
        public bool SlidePressed => slidePressed;

        public PlayerInputHandler(VariableJoystick joystick = null)
        {
            variableJoystick = joystick;
        }

        public void Update()
        {
            Vector2 keyboardInput = new Vector2(
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Vertical")
            );

            Vector2 joystickInput = Vector2.zero;
            if (variableJoystick != null)
            {
                joystickInput = new Vector2(
                    variableJoystick.Horizontal,
                    variableJoystick.Vertical
                );
            }

            movementInput = (joystickInput.magnitude > keyboardInput.magnitude)
                ? joystickInput
                : keyboardInput;

            isRunning = Input.GetKey(KeyCode.LeftShift) || runButtonHeld;

            jumpPressed = Input.GetKeyDown(KeyCode.Space) || jumpButtonPressed;
            if (jumpButtonPressed) jumpButtonPressed = false;

            slidePressed = Input.GetKeyDown(KeyCode.C) ||
                           Input.GetKeyDown(KeyCode.LeftControl) ||
                           slideButtonPressed;
            if (slideButtonPressed) slideButtonPressed = false;
        }

        public void SetRunButton(bool held)
        {
            runButtonHeld = held;
        }

        public void PressJumpButton()
        {
            jumpButtonPressed = true;
        }

        public void PressSlideButton()
        {
            slideButtonPressed = true;
        }
    }
}
