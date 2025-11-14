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

        private Vector3 velocity;
        private bool isGrounded;

        public CharacterController CharacterController => characterController;
        public PlayerInputHandler InputHandler => inputHandler;
        public PlayerPhysics Physics => physics;
        public PlayerStateMachine StateMachine => stateMachine;
        public Vector3 Velocity { get => velocity; set => velocity = value; }
        public bool IsGrounded => isGrounded;

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
            stateMachine.ChangeState<States.IdleState>();
        }

        private void Update()
        {
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
