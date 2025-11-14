using UnityEngine;

namespace ParkourLegion.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float slideSpeed = 10f;
        [SerializeField] private float slideDuration = 1f;

        [Header("Physics Settings")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundLayer;

        private CharacterController characterController;
        private PlayerInputHandler inputHandler;
        private PlayerPhysics physics;
        private PlayerStateMachine stateMachine;
        private Transform cameraTransform;

        private Vector3 velocity;
        private bool isGrounded;
        private bool movementEnabled = false;

        public CharacterController CharacterController => characterController;
        public PlayerInputHandler InputHandler => inputHandler;
        public PlayerPhysics Physics => physics;
        public PlayerStateMachine StateMachine => stateMachine;
        public Transform CameraTransform { get => cameraTransform; set => cameraTransform = value; }
        public Vector3 Velocity { get => velocity; set => velocity = value; }
        public bool IsGrounded => isGrounded;
        public bool MovementEnabled { get => movementEnabled; set => movementEnabled = value; }

        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float JumpHeight => jumpHeight;
        public float SlideSpeed => slideSpeed;
        public float SlideDuration => slideDuration;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputHandler = new PlayerInputHandler();
            physics = new PlayerPhysics(gravity, groundCheckDistance, groundLayer);
            stateMachine = new PlayerStateMachine();

            InitializeStates();
        }

        private void Start()
        {
            cameraTransform = UnityEngine.Camera.main.transform;
            stateMachine.ChangeState<States.IdleState>();
        }

        private void Update()
        {
            if (!movementEnabled) return;

            inputHandler.Update();
            isGrounded = physics.CheckGrounded(characterController, transform);
            stateMachine.Update();
        }

        public void Move(Vector3 moveDirection)
        {
            characterController.Move(moveDirection * Time.deltaTime);
        }

        public void ApplyVelocity()
        {
            characterController.Move(velocity * Time.deltaTime);
        }

        public Vector3 GetCameraRelativeMovement(Vector2 input)
        {
            if (cameraTransform == null)
            {
                return transform.right * input.x + transform.forward * input.y;
            }

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            return forward * input.y + right * input.x;
        }

        public int GetMovementStateInt()
        {
            if (stateMachine.CurrentState is States.IdleState) return 0;
            if (stateMachine.CurrentState is States.WalkState) return 1;
            if (stateMachine.CurrentState is States.RunState) return 2;
            if (stateMachine.CurrentState is States.JumpState) return 3;
            if (stateMachine.CurrentState is States.FallState) return 4;
            if (stateMachine.CurrentState is States.SlideState) return 5;
            return 0;
        }

        private void InitializeStates()
        {
            stateMachine.AddState(new States.IdleState(this));
            stateMachine.AddState(new States.WalkState(this));
            stateMachine.AddState(new States.RunState(this));
            stateMachine.AddState(new States.JumpState(this));
            stateMachine.AddState(new States.FallState(this));
            stateMachine.AddState(new States.SlideState(this));
        }
    }
}
