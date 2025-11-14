# Camera Controller Implementation Plan (Cinemachine)

**Date:** 2025-11-14
**Component:** Camera Systems - Third Person Camera Controller
**Design Reference:** [camera-controller-design-cinemachine.md](./camera-controller-design-cinemachine.md)
**Approach:** Using Cinemachine FreeLook Camera

---

## Implementation Phases

### Phase 1: Cinemachine Scene Setup
- [ ] Create CameraTarget GameObject (child of Player)
- [ ] Position CameraTarget at (0, 1.5, 0) relative to Player
- [ ] Create Cinemachine FreeLook Camera in scene
- [ ] Set Follow target to CameraTarget
- [ ] Set Look At target to CameraTarget
- [ ] Verify Cinemachine Brain on Main Camera

### Phase 2: FreeLook Camera Configuration
- [ ] Configure Top Rig (Height: 3.5, Radius: 4.0)
- [ ] Configure Middle Rig (Height: 1.5, Radius: 5.0)
- [ ] Configure Bottom Rig (Height: 0.5, Radius: 4.0)
- [ ] Set X Axis Max Speed to 300
- [ ] Set Y Axis Max Speed to 2
- [ ] Set Y Axis Value to 0.5 (start at middle)
- [ ] Clear Input Axis Names (we control via script)

### Phase 3: Collision Setup
- [ ] Add CinemachineCollider extension to FreeLook
- [ ] Set Collide Against to Ground layer
- [ ] Set Ignore Tag to Player
- [ ] Configure Distance Limit: 1.0
- [ ] Configure Camera Radius: 0.2
- [ ] Set Strategy to Pull Camera Forward
- [ ] Set Damping: 0.5

### Phase 4: Input Provider Script
- [x] Create Scripts/Camera/ directory
- [x] Implement CameraInputProvider.cs
- [ ] Add script to CM FreeLook Camera GameObject
- [ ] Assign FreeLook reference in Inspector
- [ ] Test mouse rotation with cursor locked
- [ ] Test Escape key toggles cursor lock

### Phase 5: Player Controller Modifications
- [x] Add CameraTransform property to PlayerController.cs
- [x] Implement GetCameraRelativeMovement() method
- [x] Set CameraTransform = Camera.main.transform in Start()
- [ ] Test compilation (no runtime testing yet)

### Phase 6: Movement States Update
- [x] Modify WalkState.cs for camera-relative movement
- [x] Add player rotation to face movement direction in WalkState
- [x] Modify RunState.cs (same changes as WalkState)
- [ ] Test player moves in camera direction
- [ ] Verify smooth player rotation

### Phase 7: Testing & Tuning
- [ ] Test camera follows player smoothly
- [ ] Test 360° horizontal rotation
- [ ] Test vertical rotation (between rigs)
- [ ] Test player movement in all directions
- [ ] Test camera collision with walls
- [ ] Test camera returns to normal distance
- [ ] Tune mouse sensitivity
- [ ] Tune orbit distances if needed
- [ ] Tune player rotation speed

### Phase 8: Polish
- [ ] Verify no jittering or stuttering
- [ ] Test edge cases (corners, tight spaces)
- [ ] Final parameter adjustments
- [ ] Ensure cursor management works properly
- [ ] Code cleanup

---

## Implementation Order

**Start with:**
1. Scene setup (Phase 1)
2. FreeLook configuration (Phase 2)
3. Collision setup (Phase 3)
4. Input script (Phase 4)
5. Player integration (Phase 5-6)
6. Testing (Phase 7)
7. Polish (Phase 8)

---

## Success Criteria

✅ Camera smoothly follows player
✅ Mouse rotates camera around player (horizontal 360°, vertical between rigs)
✅ Cursor locks during gameplay, unlocks with Escape
✅ Player moves in camera's looking direction
✅ Player smoothly rotates to face movement direction
✅ Camera pulls in when near walls
✅ Camera returns to normal distance when clear
✅ No jittering or camera glitches
✅ Responsive and smooth feel
✅ Code follows project patterns (no inner comments)

---

## Files to Create

### New Scripts
- `Scripts/Camera/CameraInputProvider.cs`

---

## Files to Modify

### PlayerController.cs
**Changes:**
- Add property: `public Transform CameraTransform { get; set; }`
- Add method: `GetCameraRelativeMovement(Vector2 input)`
- In `Start()`: Set `CameraTransform = Camera.main.transform`

### WalkState.cs
**Changes:**
- Replace movement calculation with camera-relative
- Add player rotation to face movement direction

### RunState.cs
**Changes:**
- Same as WalkState

---

## Unity Scene Setup Checklist

### GameObject Hierarchy
```
Scene
├── Player
│   ├── PlayerController
│   └── CameraTarget (NEW)
├── Main Camera
│   └── Cinemachine Brain (should exist)
└── CM FreeLook Camera (NEW)
    ├── CinemachineFreeLook
    ├── CinemachineCollider
    └── CameraInputProvider.cs
```

### Layer Setup
- Ensure `Ground` layer exists (should already be set up)
- Ensure `Player` layer exists (should already be set up)

---

## Testing Scenarios

### Basic Camera
- [ ] Camera follows player when walking
- [ ] Camera follows player when running
- [ ] Camera follows player when jumping
- [ ] Camera doesn't jitter or lag

### Mouse Control
- [ ] Mouse X rotates camera horizontally
- [ ] Mouse Y adjusts camera height (rigs)
- [ ] Rotation feels smooth and responsive
- [ ] Sensitivity is comfortable

### Player Movement
- [ ] W moves player forward (camera direction)
- [ ] A/D strafe left/right (camera relative)
- [ ] S moves player backward (camera direction)
- [ ] Player rotates smoothly to face movement
- [ ] Works in all directions

### Collision
- [ ] Camera pulls closer near walls
- [ ] Camera doesn't clip through geometry
- [ ] Camera returns to normal distance smoothly
- [ ] No jittering against walls

### Edge Cases
- [ ] Camera in tight corners
- [ ] Camera near ceiling
- [ ] Quick 180° turns
- [ ] Jump while rotating camera
- [ ] Slide while rotating camera

---

## Tunable Parameters Reference

### CameraInputProvider.cs
```csharp
float mouseSensitivityX = 2f     // Start value, tune as needed
float mouseSensitivityY = 2f     // Start value, tune as needed
bool invertY = false             // Option for inverted controls
```

### Cinemachine FreeLook (Inspector)
```
Top Rig: Height 3.5, Radius 4.0
Middle Rig: Height 1.5, Radius 5.0
Bottom Rig: Height 0.5, Radius 4.0

X Axis Max Speed: 300
Y Axis Max Speed: 2
Y Axis Value: 0.5

Collision Distance Limit: 1.0
Collision Camera Radius: 0.2
Collision Damping: 0.5
```

### Player Rotation (in WalkState/RunState)
```csharp
float rotationSpeed = 10f        // Used in Quaternion.Slerp
```

---

## Common Issues & Solutions

### Issue: Camera spins too fast
**Solution:** Lower `mouseSensitivityX` and `mouseSensitivityY` to 1.0 or 0.5

### Issue: Camera too close/far
**Solution:** Adjust Middle Rig radius (try 4.0 to 6.0)

### Issue: Player rotation jerky
**Solution:** Adjust `rotationSpeed` in Slerp (try 5f to 15f)

### Issue: Camera clips through walls
**Solution:** Increase `Camera Radius` in CinemachineCollider (try 0.3)

### Issue: Camera stays too close after collision
**Solution:** Increase `Damping` value (try 1.0) or adjust `Smoothing Time`

### Issue: Camera jumps at start
**Solution:** Set FreeLook Y Axis Value to 0.5 in Inspector

---

## Next Step

Ready to begin **Phase 1: Cinemachine Scene Setup**

Once scene is set up, proceed to **Phase 4: Input Provider Script** implementation.
