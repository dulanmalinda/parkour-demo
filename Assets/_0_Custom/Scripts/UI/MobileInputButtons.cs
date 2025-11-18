using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace ParkourLegion.UI
{
    public class MobileInputButtons : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button jumpButton;
        [SerializeField] private Button slideButton;
        [SerializeField] private Button runButton;
        [SerializeField] private VariableJoystick variableJoystick;

        private Player.PlayerInputHandler inputHandler;
        private bool isSetup = false;

        private void Start()
        {
            StartCoroutine(WaitForLocalPlayerAndSetup());
        }

        private IEnumerator WaitForLocalPlayerAndSetup()
        {
            int maxAttempts = 100;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                if (FindInputHandler())
                {
                    SetupButtons();
                    isSetup = true;
                    yield break;
                }

                attempts++;
                yield return new WaitForSeconds(0.1f);
            }

            Debug.LogWarning("MobileInputButtons: LocalPlayer not found after 10 seconds. Buttons will not work.");
        }

        private bool FindInputHandler()
        {
            var localPlayer = GameObject.Find("LocalPlayer");
            if (localPlayer != null)
            {
                var controller = localPlayer.GetComponent<Player.PlayerController>();
                if (controller != null)
                {
                    if (variableJoystick != null)
                    {
                        controller.SetJoystick(variableJoystick);
                        Debug.Log("MobileInputButtons: Assigned VariableJoystick to PlayerController");
                    }

                    inputHandler = controller.InputHandler;
                    Debug.Log("MobileInputButtons: Found InputHandler");
                    return true;
                }
                else
                {
                    Debug.LogWarning("MobileInputButtons: PlayerController not found on LocalPlayer");
                }
            }

            return false;
        }

        private void SetupButtons()
        {
            if (inputHandler == null)
            {
                Debug.LogWarning("MobileInputButtons: InputHandler is null, cannot setup buttons");
                return;
            }

            if (jumpButton != null)
            {
                AddEventTrigger(jumpButton.gameObject, EventTriggerType.PointerDown,
                    (data) => inputHandler.PressJumpButton());
            }

            if (slideButton != null)
            {
                AddEventTrigger(slideButton.gameObject, EventTriggerType.PointerDown,
                    (data) => inputHandler.PressSlideButton());
            }

            if (runButton != null)
            {
                AddEventTrigger(runButton.gameObject, EventTriggerType.PointerDown,
                    (data) => inputHandler.SetRunButton(true));
                AddEventTrigger(runButton.gameObject, EventTriggerType.PointerUp,
                    (data) => inputHandler.SetRunButton(false));
            }

            Debug.Log("MobileInputButtons: Buttons setup complete");
        }

        private void AddEventTrigger(GameObject target, EventTriggerType eventType,
            System.Action<BaseEventData> callback)
        {
            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = target.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener((data) => callback(data));
            trigger.triggers.Add(entry);
        }
    }
}
