using UnityEngine;
using ParkourLegion.Schema;

namespace ParkourLegion.Networking
{
    public class RemotePlayerController : MonoBehaviour
    {
        [Header("Visual Feedback Colors")]
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color walkColor = Color.green;
        [SerializeField] private Color runColor = Color.blue;
        [SerializeField] private Color jumpColor = Color.yellow;
        [SerializeField] private Color fallColor = Color.red;
        [SerializeField] private Color slideColor = Color.magenta;

        private PlayerState playerState;
        private Renderer playerRenderer;
        private MaterialPropertyBlock propertyBlock;
        private byte lastMovementState = 255;

        public void Initialize(PlayerState state)
        {
            playerState = state;
            playerRenderer = GetComponentInChildren<Renderer>();

            if (playerRenderer == null)
            {
                Debug.LogWarning("No Renderer found on RemotePlayer or its children");
                return;
            }

            Debug.Log($"[RemotePlayerController] Initialized. Renderer: {playerRenderer.name}, Material: {playerRenderer.material.shader.name}");

            propertyBlock = new MaterialPropertyBlock();
            UpdateVisuals();
        }

        private void Update()
        {
            if (playerState != null && playerState.movementState != lastMovementState)
            {
                Debug.Log($"[RemotePlayerController] Movement state changed: {lastMovementState} -> {playerState.movementState}");
                UpdateVisuals();
                lastMovementState = playerState.movementState;
            }
        }

        private void UpdateVisuals()
        {
            if (playerState == null || playerRenderer == null) return;

            Color targetColor = idleColor;

            switch (playerState.movementState)
            {
                case 0:
                    targetColor = idleColor;
                    break;
                case 1:
                    targetColor = walkColor;
                    break;
                case 2:
                    targetColor = runColor;
                    break;
                case 3:
                    targetColor = jumpColor;
                    break;
                case 4:
                    targetColor = fallColor;
                    break;
                case 5:
                    targetColor = slideColor;
                    break;
            }

            Debug.Log($"[RemotePlayerController] Setting color for state {playerState.movementState}: {targetColor}");

            playerRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", targetColor);
            propertyBlock.SetColor("_BaseColor", targetColor);
            playerRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
