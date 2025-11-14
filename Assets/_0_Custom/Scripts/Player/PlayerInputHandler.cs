using UnityEngine;

namespace ParkourLegion.Player
{
    public class PlayerInputHandler
    {
        private Vector2 movementInput;
        private bool isRunning;
        private bool jumpPressed;
        private bool slidePressed;

        public Vector2 MovementInput => movementInput;
        public bool IsRunning => isRunning;
        public bool JumpPressed => jumpPressed;
        public bool SlidePressed => slidePressed;

        public void Update()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            movementInput = new Vector2(horizontal, vertical);

            isRunning = Input.GetKey(KeyCode.LeftShift);
            jumpPressed = Input.GetKeyDown(KeyCode.Space);
            slidePressed = Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl);
        }
    }
}
