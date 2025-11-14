# Camera Controller Design (Cinemachine)

**Date:** 2025-11-14
**Component:** Camera Systems - Third Person Camera Controller
**Type:** Cinemachine-based follow camera with mouse rotation
**Updated:** Using Cinemachine Virtual Camera system

## Design Requirements

### Core Behavior
- ✅ **Third-person perspective** - Camera behind and above player
- ✅ **Mouse rotation** - Camera rotates around player based on mouse movement
- ✅ **Player follows camera forward** - When player moves, they move in camera's looking direction
- ✅ **Dynamic collision handling** - Cinemachine Collider handles wall detection

### Technical Constraints
- Must work with existing PlayerController
- Smooth camera movement (no jittering)
- Responsive mouse input
- Cursor locked during gameplay

---

## Cinemachine Architecture

### Why Cinemachine?

**Advantages:**
✅ Built-in collision detection (CinemachineCollider)
✅ Smooth damping and follow behavior
✅ Battle-tested, handles edge cases
✅ Easy to add camera effects later (shake, FOV changes)
✅ Industry standard for Unity games
✅ Less custom code to debug

### Core Components

```
Main Camera (Brain)
    └── Cinemachine Brain component

Virtual Camera (Controller)
    ├── Cinemachine Virtual Camera
    ├── Cinemachine Collider (collision)
    └── Custom Input Provider (mouse rotation)

CameraTarget (Follow Target)
    └── Empty GameObject on Player
```

**How it works:**
1. **Virtual Camera** defines desired camera behavior
2. **Cinemachine Brain** (on Main Camera) applies Virtual Camera's output
3. **CinemachineCollider** automatically handles wall collision
4. **Custom script** feeds mouse input to Virtual Camera

---

## Implementation Approach

### Option A: FreeLook Camera (Recommended for Third-Person)

**Cinemachine FreeLook** is designed for third-person orbiting cameras.

**Features:**
- Three vertical rigs (top, middle, bottom)
- Mouse X rotates around player (yaw)
- Mouse Y adjusts height (pitch)
- Built-in collision handling
- Smooth orbiting

**Setup:**
1. Create `CM vcam FreeLook` from Cinemachine menu
2. Set Follow & LookAt to CameraTarget
3. Configure orbits (top, middle, bottom distances)
4. Add CinemachineCollider extension
5. Connect mouse input via script

### Option B: Virtual Camera + Custom Rotation (Alternative)

**Standard Virtual Camera** with custom orbit script.

**Setup:**
1. Create `CM vcam1` from Cinemachine menu
2. Set Body to Transposer (follow)
3. Set Aim to Composer (look at)
4. Custom script handles orbit rotation
5. Add CinemachineCollider extension

**Recommendation:** Use **Option A (FreeLook)** - it's purpose-built for third-person cameras.

---

## Script Architecture

### Required Scripts

#### 1. **CameraInputProvider.cs**
Feeds mouse input to Cinemachine FreeLook.

**Responsibilities:**
- Read mouse input
- Provide axis values to Cinemachine
- Handle cursor lock/unlock

**Key Properties:**
```csharp
[Header("Settings")]
float mouseSensitivityX = 2f
float mouseSensitivityY = 2f
bool invertY = false

[Header("References")]
CinemachineFreeLook freeLookCamera
```

**Key Methods:**
```csharp
void Start() // Lock cursor, get FreeLook reference
void Update() // Read mouse input, apply to FreeLook axes
void LockCursor()
void UnlockCursor()
```

**Implementation:**
```csharp
void Update()
{
    if (Cursor.lockState == CursorLockMode.Locked)
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

        if (invertY) mouseY = -mouseY;

        freeLookCamera.m_XAxis.Value += mouseX;
        freeLookCamera.m_YAxis.Value += mouseY;
    }

    // Toggle cursor lock with Escape
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        if (Cursor.lockState == CursorLockMode.Locked)
            UnlockCursor();
        else
            LockCursor();
    }
}
```

---

#### 2. **PlayerController.cs Modifications**

Same as before - we need camera-relative movement.

**Add property:**
```csharp
public Transform CameraTransform { get; set; }
```

**Add helper method:**
```csharp
public Vector3 GetCameraRelativeMovement(Vector2 input)
{
    if (CameraTransform == null)
        return transform.right * input.x + transform.forward * input.y;

    Vector3 forward = CameraTransform.forward;
    Vector3 right = CameraTransform.right;

    forward.y = 0f;
    right.y = 0f;

    forward.Normalize();
    right.Normalize();

    return forward * input.y + right * input.x;
}
```

**In Start():**
```csharp
void Start()
{
    CameraTransform = Camera.main.transform;
    stateMachine.ChangeState<States.IdleState>();
}
```

---

#### 3. **Movement States Modifications**

**WalkState.cs** and **RunState.cs** need updates.

**Current code:**
```csharp
Vector3 moveDirection = controller.transform.right * input.x + controller.transform.forward * input.y;
```

**New code:**
```csharp
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
}

controller.Move(moveDirection.normalized * controller.WalkSpeed);
```

**SlideState.cs:** No changes needed (maintains forward direction).

---

## Cinemachine FreeLook Configuration

### Orbits Settings

FreeLook has 3 orbits (rigs):

**Top Rig:**
- Height: `3.5`
- Radius: `4.0`
- (Camera above and behind player)

**Middle Rig:**
- Height: `1.5`
- Radius: `5.0`
- (Camera at shoulder level - main position)

**Bottom Rig:**
- Height: `0.5`
- Radius: `4.0`
- (Camera lower, looking up)

**Mouse Y Input:** Blends between these three rigs.

### Axis Settings

**X Axis (Horizontal Rotation):**
- Value: `0`
- Max Speed: `300` (controlled by our script)
- Accel Time: `0.1`
- Decel Time: `0.1`
- Input Axis Name: `""` (we control via script)

**Y Axis (Vertical Rotation):**
- Value: `0.5` (starts at middle rig)
- Max Speed: `2` (controlled by our script)
- Accel Time: `0.1`
- Decel Time: `0.1`
- Input Axis Name: `""` (we control via script)

### Body Settings

**Binding Mode:** `Lock To Target On Assign`
**Follow Offset:** `(0, 0, 0)` (orbits handle positioning)

### Aim Settings

**Look At Target:** CameraTarget
**Tracked Object Offset:** `(0, 0, 0)`

### Extensions

**Add: Cinemachine Collider**

Settings:
- Collide Against: `Ground` layer
- Ignore Tag: `Player`
- Distance Limit: `1.0` (minimum distance)
- Camera Radius: `0.2`
- Strategy: `Pull Camera Forward`
- Damping: `0.5` (smooth collision response)
- Smoothing Time: `0.1`

---

## GameObject Setup

### Hierarchy Structure

```
Scene
├── Player
│   ├── (Capsule mesh)
│   ├── PlayerController
│   └── CameraTarget (Empty GameObject)
│       └── Position: (0, 1.5, 0)
│
├── Main Camera
│   └── Cinemachine Brain (auto-added)
│
└── CM FreeLook Camera
    ├── CinemachineFreeLook
    ├── CinemachineCollider
    └── CameraInputProvider (our script)
```

### Setup Steps

**1. Create CameraTarget:**
- Empty GameObject, child of Player
- Position: `(0, 1.5, 0)`

**2. Create FreeLook Camera:**
- Cinemachine > Create FreeLook Camera
- Name it `CM FreeLook Camera`
- Set Follow: CameraTarget
- Set Look At: CameraTarget

**3. Configure Main Camera:**
- Should automatically have Cinemachine Brain
- If not, add it manually

**4. Add Input Provider:**
- Add `CameraInputProvider.cs` to CM FreeLook Camera GameObject
- Assign FreeLook reference in Inspector

**5. Configure Collision:**
- On CM FreeLook Camera, Add Extension > CinemachineCollider
- Set Collide Against to `Ground` layer

---

## Cursor Management

Handled by `CameraInputProvider.cs`:

```csharp
void Start()
{
    LockCursor();
}

void LockCursor()
{
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}

void UnlockCursor()
{
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}

void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        if (Cursor.lockState == CursorLockMode.Locked)
            UnlockCursor();
        else
            LockCursor();
    }
}
```

---

## Tunable Parameters

### In CameraInputProvider.cs
```csharp
float mouseSensitivityX = 2f    // Horizontal rotation speed
float mouseSensitivityY = 2f    // Vertical rotation speed
bool invertY = false            // Invert vertical axis
```

### In Cinemachine FreeLook Inspector
```
Orbits:
  Top Rig: Height 3.5, Radius 4.0
  Middle Rig: Height 1.5, Radius 5.0
  Bottom Rig: Height 0.5, Radius 4.0

X Axis: Max Speed 300
Y Axis: Max Speed 2

Collision:
  Distance Limit: 1.0
  Camera Radius: 0.2
  Damping: 0.5
```

### In PlayerController (Movement States)
```csharp
float playerRotationSpeed = 10f  // How fast player turns to face movement
```

---

## Advantages Over Custom Script

| Feature | Custom Script | Cinemachine |
|---------|--------------|-------------|
| Collision Detection | Manual SphereCast, needs tuning | Built-in, battle-tested |
| Smooth Follow | Manual SmoothDamp | Automatic damping |
| Rotation Limits | Manual clamping | Configured in Inspector |
| Edge Cases | Need to debug | Handled by Cinemachine |
| Future Effects | Custom code | Extensions available |
| Setup Time | More coding | Inspector configuration |
| Performance | Good | Optimized by Unity |

---

## Files to Create/Modify

### New Files
- `Scripts/Camera/CameraInputProvider.cs`

### Modified Files
- `Scripts/Player/PlayerController.cs`
  - Add `CameraTransform` property
  - Add `GetCameraRelativeMovement()` method
  - Set `CameraTransform` in `Start()`
- `Scripts/Player/States/WalkState.cs`
  - Use camera-relative movement
  - Add player rotation to face movement
- `Scripts/Player/States/RunState.cs`
  - Same changes as WalkState

### No Changes Needed
- `SlideState.cs` - maintains forward direction
- All other states

---

## Testing Plan

### Phase 1: Cinemachine Setup
1. Create FreeLook camera in scene
2. Configure Follow/LookAt targets
3. Test camera follows player movement
4. Verify no jittering

### Phase 2: Mouse Input
1. Add CameraInputProvider script
2. Test horizontal rotation (360°)
3. Test vertical rotation (between rigs)
4. Verify cursor lock/unlock
5. Tune sensitivity

### Phase 3: Player Movement Integration
1. Modify PlayerController for camera-relative movement
2. Update WalkState and RunState
3. Test player moves in camera direction
4. Test player rotation to face movement
5. Verify smooth transitions

### Phase 4: Collision
1. Add CinemachineCollider extension
2. Build walls in test scene
3. Test camera pulls in when near wall
4. Test camera returns to normal distance
5. Tune collision parameters

### Phase 5: Polish
1. Fine-tune orbit distances
2. Adjust mouse sensitivity
3. Test all movement scenarios
4. Verify no edge case issues

---

## Known Challenges

### Challenge 1: Initial Camera Position
**Symptom:** Camera might start at weird angle
**Solution:** Set FreeLook Y Axis Value to `0.5` (middle rig)

### Challenge 2: Camera Spins Wildly
**Symptom:** High mouse sensitivity causes over-rotation
**Solution:** Lower `mouseSensitivityX/Y` values (try `1.0` first)

### Challenge 3: Player Rotation Jitter
**Symptom:** Player rotates jerkily when changing direction
**Solution:** Adjust `playerRotationSpeed` in Slerp (try `5f` to `15f`)

### Challenge 4: Collision Pushes Camera Too Close
**Symptom:** Camera gets uncomfortably close to player near walls
**Solution:** Increase `Distance Limit` in CinemachineCollider (try `1.5` or `2.0`)

---

## Future Enhancements (Post-Prototype)

- [ ] Camera shake on landing (Cinemachine Impulse)
- [ ] Dynamic FOV on sprint (adjust Lens settings)
- [ ] Different camera profiles for wallrun/slide
- [ ] Slow-motion camera on special moves
- [ ] Camera target offset during special actions
- [ ] Look-ahead system (camera predicts movement)

---

## Next Steps

1. ✅ Design updated for Cinemachine
2. ⏳ Create implementation workplan
3. ⏳ Implement CameraInputProvider.cs
4. ⏳ Modify PlayerController.cs
5. ⏳ Update movement states (Walk, Run)
6. ⏳ Set up Cinemachine in Unity scene
7. ⏳ Test and tune

---

## Related Documentation

- [Player Controller Design](../player-controller/player-controller-design.md)
- [Component Overview](../component-overview.md)
