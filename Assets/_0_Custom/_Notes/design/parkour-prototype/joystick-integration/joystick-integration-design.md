# Joystick Integration Design

**Date:** 2025-11-19
**Component:** Player Input System Enhancement
**Type:** Mobile/Touch Input Support

---

## 📋 Overview

Add **VariableJoystick** support to the existing player input system to enable mobile/touch controls while maintaining full backward compatibility with keyboard/mouse input. The system will support **dual input modes** (keyboard + joystick simultaneously) with priority given to whichever input is active.

---

## 🎯 Design Goals

### Primary Goals
- ✅ Support VariableJoystick for movement input (mobile/touch)
- ✅ Maintain existing keyboard/mouse input (desktop)
- ✅ Allow both input methods to work simultaneously
- ✅ Enable UI buttons for jump/run/slide actions (mobile)
- ✅ Zero breaking changes to existing code

### Non-Goals
- ❌ Removing keyboard/mouse support
- ❌ Automatic input method detection/switching
- ❌ Platform-specific input filtering
- ❌ Gyroscope/accelerometer support (future enhancement)

---

## 🏗️ Architecture Design

### Current Input Flow
```
User Input (Keyboard/Mouse)
  ↓
PlayerInputHandler.Update()
  ├─ Input.GetAxis("Horizontal/Vertical") → movementInput
  ├─ Input.GetKey(LeftShift) → isRunning
  ├─ Input.GetKeyDown(Space) → jumpPressed
  └─ Input.GetKeyDown(C/Ctrl) → slidePressed
  ↓
PlayerState reads from PlayerInputHandler
  ↓
Movement/Action execution
```

### New Input Flow (Hybrid)
```
User Input (Keyboard OR Touch)
  ↓
PlayerInputHandler.Update()
  ├─ Keyboard: Input.GetAxis() → keyboardInput
  ├─ Joystick: variableJoystick.Direction → joystickInput
  ├─ MERGE: movementInput = keyboardInput + joystickInput (prioritized)
  ├─ Keyboard/UI: Input.GetKey(LeftShift) OR runButton → isRunning
  ├─ Keyboard/UI: Input.GetKeyDown(Space) OR jumpButton → jumpPressed
  └─ Keyboard/UI: Input.GetKeyDown(C/Ctrl) OR slideButton → slidePressed
  ↓
PlayerState reads from PlayerInputHandler (unchanged)
  ↓
Movement/Action execution
```

---

## 📐 Component Design

### 1. PlayerInputHandler (Modified)

**New Fields:**
```csharp
// Joystick reference (optional, assigned in Inspector)
[SerializeField] private VariableJoystick variableJoystick;

// UI button states (set by UI buttons via public methods)
private bool runButtonHeld = false;
private bool jumpButtonPressed = false;
private bool slideButtonPressed = false;
```

**Modified Update() Logic:**
```csharp
public void Update()
{
    // --- MOVEMENT INPUT (Keyboard + Joystick) ---
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

    // Priority: Use whichever has greater magnitude
    movementInput = (joystickInput.magnitude > keyboardInput.magnitude)
        ? joystickInput
        : keyboardInput;

    // --- ACTION INPUTS (Keyboard + UI Buttons) ---
    isRunning = Input.GetKey(KeyCode.LeftShift) || runButtonHeld;

    jumpPressed = Input.GetKeyDown(KeyCode.Space) || jumpButtonPressed;
    if (jumpButtonPressed) jumpButtonPressed = false; // Consume button press

    slidePressed = Input.GetKeyDown(KeyCode.C) ||
                   Input.GetKeyDown(KeyCode.LeftControl) ||
                   slideButtonPressed;
    if (slideButtonPressed) slideButtonPressed = false; // Consume button press
}
```

**New Public Methods (for UI buttons):**
```csharp
// Called by UI buttons (OnPointerDown/OnPointerUp)
public void SetRunButton(bool held)
{
    runButtonHeld = held;
}

// Called by UI buttons (OnPointerDown)
public void PressJumpButton()
{
    jumpButtonPressed = true;
}

// Called by UI buttons (OnPointerDown)
public void PressSlideButton()
{
    slideButtonPressed = true;
}
```

**Constructor Signature Change:**
```csharp
// OLD: public PlayerInputHandler()
// NEW: public PlayerInputHandler(VariableJoystick joystick = null)

public PlayerInputHandler(VariableJoystick joystick = null)
{
    variableJoystick = joystick;
}
```

---

### 2. PlayerController (Modified)

**Changes Required:**
```csharp
[Header("Input References")]
[SerializeField] private VariableJoystick variableJoystick; // Optional joystick

private void Awake()
{
    characterController = GetComponent<CharacterController>();

    // Pass joystick to InputHandler constructor
    inputHandler = new PlayerInputHandler(variableJoystick);

    physics = new PlayerPhysics(gravity, groundCheckDistance, groundLayer);
    stateMachine = new PlayerStateMachine();

    InitializeStates();
}

// Expose InputHandler for UI buttons to access
public PlayerInputHandler InputHandler => inputHandler;
```

**No other changes needed** - All states use `controller.InputHandler.MovementInput` which will now contain merged input.

---

### 3. UI Button Components (New)

**Location:** `Scripts/UI/MobileInputButtons.cs`

**Purpose:** Handle UI button events and forward to PlayerInputHandler

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ParkourLegion.UI
{
    public class MobileInputButtons : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button jumpButton;
        [SerializeField] private Button slideButton;
        [SerializeField] private Button runButton; // Toggle or hold button

        private Player.PlayerInputHandler inputHandler;

        private void Start()
        {
            // Find local player's InputHandler
            var localPlayer = GameObject.Find("LocalPlayer");
            if (localPlayer != null)
            {
                var controller = localPlayer.GetComponent<Player.PlayerController>();
                if (controller != null)
                {
                    inputHandler = controller.InputHandler;
                }
            }

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (inputHandler == null) return;

            // Jump button (one-time press)
            if (jumpButton != null)
            {
                AddEventTrigger(jumpButton.gameObject, EventTriggerType.PointerDown,
                    (data) => inputHandler.PressJumpButton());
            }

            // Slide button (one-time press)
            if (slideButton != null)
            {
                AddEventTrigger(slideButton.gameObject, EventTriggerType.PointerDown,
                    (data) => inputHandler.PressSlideButton());
            }

            // Run button (hold/release)
            if (runButton != null)
            {
                AddEventTrigger(runButton.gameObject, EventTriggerType.PointerDown,
                    (data) => inputHandler.SetRunButton(true));
                AddEventTrigger(runButton.gameObject, EventTriggerType.PointerUp,
                    (data) => inputHandler.SetRunButton(false));
            }
        }

        private void AddEventTrigger(GameObject target, EventTriggerType eventType,
            System.Action<BaseEventData> callback)
        {
            EventTrigger trigger = target.GetComponent<EventTrigger>();
            if (trigger == null) trigger = target.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener((data) => callback(data));
            trigger.triggers.Add(entry);
        }
    }
}
```

---

## 🎨 Unity Setup Requirements

### LocalPlayer Prefab Changes
1. **Add VariableJoystick Reference:**
   - Open LocalPlayer prefab
   - In PlayerController component, assign VariableJoystick from Canvas

### Canvas Setup (Mobile UI)
1. **Add VariableJoystick:**
   - Drag `Joystick Pack/Prefabs/Variable Joystick` into Canvas
   - Position in bottom-left corner
   - Configure settings:
     - Joystick Type: Floating or Dynamic
     - Handle Range: 1
     - Dead Zone: 0.1

2. **Add Action Buttons:**
   - Create UI Buttons for Jump, Slide, Run
   - Position appropriately (bottom-right area)
   - Add MobileInputButtons component to Canvas or UI manager
   - Assign button references

3. **Platform-Specific Visibility (Optional):**
   - Add Canvas Group to mobile controls
   - Toggle visibility based on platform:
     ```csharp
     #if UNITY_STANDALONE || UNITY_EDITOR
         mobileControls.SetActive(false);
     #else
         mobileControls.SetActive(true);
     #endif
     ```

---

## 🔄 Input Priority & Merging Strategy

### Movement Input Merging
**Strategy:** Magnitude-based priority
```csharp
// Prioritize input source with greater magnitude
movementInput = (joystickInput.magnitude > keyboardInput.magnitude)
    ? joystickInput
    : keyboardInput;
```

**Rationale:**
- ✅ Allows seamless switching between keyboard and joystick
- ✅ No explicit mode switching required
- ✅ Natural feel - whichever input is actively used takes control
- ✅ Prevents input conflicts (no additive input that could exceed 1.0)

**Alternative Strategies (Rejected):**
- ❌ **Additive:** `keyboardInput + joystickInput` - Can exceed normalized range
- ❌ **Exclusive:** Force one mode at a time - Poor UX, requires mode detection
- ❌ **Joystick-only when present:** Breaks keyboard input when joystick assigned

### Action Input Merging
**Strategy:** Logical OR (any source triggers action)
```csharp
isRunning = Input.GetKey(KeyCode.LeftShift) || runButtonHeld;
jumpPressed = Input.GetKeyDown(KeyCode.Space) || jumpButtonPressed;
slidePressed = Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl) || slideButtonPressed;
```

**Rationale:**
- ✅ Multiple input methods supported simultaneously
- ✅ No priority conflicts (binary states)
- ✅ UI buttons act as "virtual keys"

---

## 📊 Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    USER INPUT SOURCES                        │
├─────────────────┬─────────────────┬─────────────────────────┤
│   Keyboard      │  VariableJoystick│    UI Buttons          │
│  (WASD/Arrows)  │  (Touch/Mouse)   │  (Jump/Run/Slide)      │
└────────┬────────┴────────┬─────────┴────────┬───────────────┘
         │                 │                  │
         ▼                 ▼                  ▼
┌─────────────────────────────────────────────────────────────┐
│            PlayerInputHandler.Update()                       │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ Movement Input Merge (Magnitude Priority)            │   │
│  │   keyboardInput = GetAxis()                          │   │
│  │   joystickInput = variableJoystick.Direction         │   │
│  │   movementInput = (joystick.mag > keyboard.mag)      │   │
│  │                   ? joystick : keyboard              │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ Action Input Merge (Logical OR)                      │   │
│  │   isRunning = GetKey(Shift) || runButtonHeld         │   │
│  │   jumpPressed = GetKeyDown(Space) || jumpButtonPressed│  │
│  │   slidePressed = GetKeyDown(C/Ctrl) || slideButtonPressed│ │
│  └──────────────────────────────────────────────────────┘   │
└────────┬────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│            PlayerState (Unchanged)                           │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ Vector2 input = controller.InputHandler.MovementInput│   │
│  │ Vector3 moveDir = GetCameraRelativeMovement(input)   │   │
│  │ controller.Move(moveDir * speed)                     │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ if (InputHandler.JumpPressed) → JumpState            │   │
│  │ if (InputHandler.IsRunning) → RunState               │   │
│  │ if (InputHandler.SlidePressed) → SlideState          │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 🧪 Testing Strategy

### Test Cases

#### TC1: Keyboard-Only Input
- **Setup:** No joystick assigned, no UI buttons
- **Actions:** Use WASD, Space, Shift, C
- **Expected:** All movement and actions work as before

#### TC2: Joystick-Only Input
- **Setup:** Joystick assigned, keyboard not used
- **Actions:** Move joystick, press UI buttons
- **Expected:** Character moves based on joystick direction, actions trigger

#### TC3: Hybrid Input (Keyboard + Joystick)
- **Setup:** Both joystick and keyboard available
- **Actions:**
  - Use keyboard WASD → character moves
  - While moving, touch joystick → character follows joystick
  - Release joystick → character follows keyboard again
- **Expected:** Smooth transition based on magnitude priority

#### TC4: Action Button Combinations
- **Setup:** All inputs available
- **Actions:**
  - Press keyboard Space → Jump
  - Press UI Jump button → Jump
  - Hold keyboard Shift → Run
  - Hold UI Run button → Run
  - Press keyboard C + UI Slide button simultaneously
- **Expected:** All combinations trigger respective actions

#### TC5: Network Sync
- **Setup:** Multiplayer session with 2 players (one keyboard, one joystick)
- **Actions:** Both players move and perform actions
- **Expected:** Remote player sees correct movement/actions regardless of input method

#### TC6: Mobile Platform
- **Setup:** WebGL or mobile build
- **Actions:** Use only touch controls (joystick + UI buttons)
- **Expected:** Full gameplay functionality without keyboard

---

## 🚨 Edge Cases & Considerations

### Edge Case 1: Joystick Reference Missing
**Scenario:** VariableJoystick not assigned in Inspector
**Handling:**
```csharp
if (variableJoystick != null)
{
    joystickInput = new Vector2(...);
}
// Falls back to keyboard-only mode
```

### Edge Case 2: Button Spam Protection
**Scenario:** User rapidly taps jump/slide buttons
**Handling:**
- Use `GetKeyDown()` pattern (one-time press)
- Consume button press flags after reading
- State machine prevents invalid transitions

### Edge Case 3: Run Button Toggle vs Hold
**Design Decision:** Use HOLD behavior (matches keyboard Shift)
**Rationale:**
- ✅ Consistent with keyboard behavior
- ✅ More intuitive for mobile users
- ❌ Toggle would be inconsistent with PC controls

### Edge Case 4: Dead Zone Handling
**Scenario:** Joystick slight drift when not touched
**Handling:**
- VariableJoystick has built-in dead zone (0.1 default)
- States check `input.magnitude < 0.1f` for idle detection
- No additional handling needed

### Edge Case 5: Camera Control + Movement
**Scenario:** On mobile, need to control camera AND movement
**Current Limitation:**
- Camera uses CameraInputProvider (mouse-only)
- Mobile users can't rotate camera while moving

**Future Enhancement (Not in this iteration):**
- Add second joystick for camera rotation
- Add swipe gestures for camera
- Outside scope of current design

---

## 📝 Code Changes Summary

### Files to Modify
1. **PlayerInputHandler.cs** ✏️ MODIFY
   - Add joystick reference field
   - Add UI button state fields
   - Modify Update() logic (merge inputs)
   - Add public button methods

2. **PlayerController.cs** ✏️ MODIFY
   - Add joystick reference field
   - Pass joystick to InputHandler constructor
   - Make InputHandler publicly accessible (already is via property)

### Files to Create
3. **MobileInputButtons.cs** ➕ NEW
   - UI button event handling
   - Forward button presses to InputHandler

### Files Unchanged (Zero Impact)
- ✅ All PlayerState classes (IdleState, WalkState, RunState, etc.)
- ✅ PlayerStateMachine.cs
- ✅ PlayerPhysics.cs
- ✅ NetworkManager.cs
- ✅ LocalPlayerNetworkSync.cs
- ✅ RemotePlayerNetworkSync.cs
- ✅ GameUIManager.cs

---

## 🔄 Backward Compatibility Guarantee

### Compatibility Matrix
| Scenario | Before Changes | After Changes | Status |
|----------|---------------|---------------|--------|
| Desktop keyboard-only | ✅ Works | ✅ Works | ✅ Compatible |
| No joystick assigned | ✅ Works | ✅ Works | ✅ Compatible |
| Existing LocalPlayer prefab | ✅ Works | ✅ Works | ✅ Compatible |
| Existing player states | ✅ Works | ✅ Works | ✅ Compatible |
| Network synchronization | ✅ Works | ✅ Works | ✅ Compatible |

**Guarantee:** All existing gameplay functionality remains intact. Changes are purely additive.

---

## 🎯 Implementation Phases

### Phase 1: Core Input Merging (Essential)
- ✅ Modify PlayerInputHandler (movement + action merging)
- ✅ Modify PlayerController (joystick reference)
- ✅ Test keyboard-only (regression test)
- ✅ Test joystick-only (new functionality)
- ✅ Test hybrid input (priority switching)

### Phase 2: UI Button Support (Mobile Essential)
- ✅ Create MobileInputButtons component
- ✅ Add button press methods to InputHandler
- ✅ Test action buttons (jump/run/slide)
- ✅ Test button + keyboard combinations

### Phase 3: Unity Setup (Integration)
- ✅ Add VariableJoystick to Canvas
- ✅ Create action button UI
- ✅ Assign references in LocalPlayer prefab
- ✅ Configure joystick settings (type, dead zone)

### Phase 4: Testing & Polish (Validation)
- ✅ Run all test cases (TC1-TC6)
- ✅ Multiplayer testing (keyboard vs joystick players)
- ✅ Mobile platform testing (WebGL/Android)
- ✅ Edge case verification

---

## 📚 Related Documentation

### Existing Design Docs
- [Player Controller Design](../player-controller/player-controller-design.md)
- [Player Controller Implementation Plan](../player-controller/implementation-plan.md)
- [Multiplayer Architecture](../multiplayer/multiplayer-architecture-design.md)

### External References
- Joystick Pack Documentation: `Assets/Joystick Pack/Documentation.pdf`
- VariableJoystick Script: `Assets/Joystick Pack/Scripts/Joysticks/VariableJoystick.cs:1`
- Base Joystick Script: `Assets/Joystick Pack/Scripts/Base/Joystick.cs:1`

---

**Document Version:** 1.0
**Status:** Design Complete - Ready for Implementation Planning
**Author:** Cody (Design Mode)
**Last Updated:** 2025-11-19
