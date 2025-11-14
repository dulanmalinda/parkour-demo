# Cinemachine 3.x Setup Guide

**Date:** 2025-11-14
**Cinemachine Version:** 3.1.5
**Component:** Third Person Camera Controller

## Important: Cinemachine 3.x Changes

Cinemachine 3.x is a **complete rewrite** with breaking changes from 2.x:

### Key Changes:
- ✅ Namespace: `Unity.Cinemachine` (was `Cinemachine`)
- ✅ Component: `CinemachineCamera` (replaces `CinemachineVirtualCamera` and `CinemachineFreeLook`)
- ✅ FreeLook: Now uses `CinemachineOrbitalFollow` component
- ✅ Field naming: No more `m_` prefixes (e.g., `m_XAxis` → `HorizontalAxis`)
- ✅ Input: Uses `CinemachineInputAxisController` or manual input

---

## Scene Setup Instructions

### Step 1: Create CameraTarget

1. Select **Player** GameObject in Hierarchy
2. Right-click > **Create Empty**
3. Rename to **"CameraTarget"**
4. Set Position: `(0, 1.5, 0)` relative to Player

---

### Step 2: Create FreeLook Camera (IMPORTANT: Use Menu Shortcut!)

1. In Hierarchy, right-click (or use GameObject menu)
2. Select **Cinemachine > FreeLook Camera**
3. Rename to **"CM Third Person Camera"**

**This automatically creates a GameObject with:**
- ✅ `CinemachineCamera` component
- ✅ `CinemachineOrbitalFollow` (position control)
- ✅ `CinemachineRotationComposer` (rotation control)
- ✅ `CinemachineInputAxisController` (optional input handler)

**Do NOT manually create CinemachineCamera and try to add components - use the menu shortcut!**

---

### Step 3: Set Follow and Look At Targets

1. Select **CM Third Person Camera**
2. In Inspector, find **CinemachineCamera** component
3. Set **Tracking Target** to **CameraTarget**

The camera will now follow and look at the CameraTarget.

---

### Step 4: Configure Orbital Follow Settings

1. In the Inspector, find **CinemachineOrbitalFollow** component
2. Configure the following settings:

**Target Offset:**
- X: `0`
- Y: `0`
- Z: `0`

**Orbits (3 rings):**

**Top Orbit:**
- Height: `3.5`
- Radius: `4.0`

**Middle Orbit:**
- Height: `1.5`
- Radius: `5.0`

**Bottom Orbit:**
- Height: `0.5`
- Radius: `4.0`

**Horizontal Axis:**
- Value: `0`
- Range: `-180` to `180`
- Wrap: ✅ Enabled
- Recentering: ❌ Disabled

**Vertical Axis:**
- Value: `0.5` (starts at middle orbit)
- Range: `0` to `1`
- Wrap: ❌ Disabled
- Recentering: ❌ Disabled

**Radial Axis:**
- Value: `0`
- Range: `-10` to `10`
- Recentering: ✅ Enabled (pulls back to default)

---

### Step 5: Verify Rotation Composer (Already Set Up)

1. In Inspector, find **CinemachineRotationComposer** component
2. This was automatically added by the FreeLook Camera menu option
3. Should already be configured to look at the Tracking Target
4. No changes needed unless you want to customize composition

---

### Step 6: Add Collision Detection

1. Select **CM Third Person Camera**
2. In Inspector, click **Add Extension**
3. Select **Cinemachine Deoccluder**

**Configure Deoccluder:**
- Collide Against: **Ground** layer
- Ignore Tag: **Player**
- Distance Limit: `1.0`
- Camera Radius: `0.2`
- Strategy: **Pull Camera Forward**
- Damping: `0.5`
- Damping When Occluded: `0.1`

---

### Step 7: Disable Built-in Input Axis Controller (IMPORTANT!)

The FreeLook Camera menu creates a **CinemachineInputAxisController** component, but we're using our custom input script instead.

1. Select **CM Third Person Camera**
2. In Inspector, find **CinemachineInputAxisController** component
3. **Disable it** (uncheck the checkbox next to component name)
4. Or **Remove Component** entirely

**Why?** Our `CameraInputProvider` script manually controls the axes. Having both enabled will cause conflicts.

---

### Step 8: Add Input Provider Script

1. Select **CM Third Person Camera**
2. In Inspector, click **Add Component**
3. Search for **Camera Input Provider**
4. Add the script

**Configure in Inspector:**
- Mouse Sensitivity X: `200`
- Mouse Sensitivity Y: `2`
- Invert Y: ❌ (or ✅ if you prefer inverted)
- Cinemachine Camera: Drag **CM Third Person Camera** here (or leave empty, auto-assigns)

---

### Step 9: Verify Main Camera Setup

1. Select **Main Camera** in Hierarchy
2. Should have **Cinemachine Brain** component (auto-added)
3. If not, add it manually: **Add Component > Cinemachine Brain**

**Brain Settings:**
- Default Blend: **Cut** (instant switching)
- Update Method: **Smart Update**
- Blend Update Method: **Late Update**

---

## Testing the Camera

### Test 1: Press Play
- Camera should follow player
- Cursor should be locked

### Test 2: Mouse Movement
- **Mouse X**: Camera rotates around player (horizontal orbit)
- **Mouse Y**: Camera adjusts height (between top/middle/bottom orbits)
- **Escape**: Unlocks cursor

### Test 3: Player Movement
- **WASD**: Player moves in camera direction
- Player rotates to face movement direction

### Test 4: Collision
- Walk toward walls
- Camera should pull closer
- Camera returns to normal when clear

---

## Troubleshooting

### Camera doesn't follow player
- Check **Tracking Target** is set to CameraTarget (in CinemachineCamera component)
- Check CameraTarget is child of Player at (0, 1.5, 0)
- Verify FreeLook Camera was created via **Cinemachine > FreeLook Camera** menu

### Mouse doesn't rotate camera
- Check CameraInputProvider script is attached to CM Third Person Camera
- Check cinemachineCamera reference is assigned (or auto-finds it)
- Check cursor is locked (click in Game view)
- **IMPORTANT:** Disable or remove CinemachineInputAxisController component (conflicts with our script)

### Camera clips through walls
- Check Deoccluder extension is added
- Check Collide Against includes Ground layer
- Increase Camera Radius (try 0.3)

### Camera too close/far
- Adjust Middle Orbit Radius (try 4.0 to 6.0)
- Check Radial Axis range

### Player doesn't move in camera direction
- Check PlayerController.CameraTransform is set
- Check WalkState/RunState use GetCameraRelativeMovement()

### Compilation errors
- Check namespace is `Unity.Cinemachine` (not `Cinemachine`)
- Check Cinemachine 3.1.5 is installed in Package Manager

---

## Tuning Parameters

### Mouse Sensitivity
**In CameraInputProvider:**
- `mouseSensitivityX = 200f` - Horizontal rotation speed
- `mouseSensitivityY = 2f` - Vertical height adjustment speed

**Start with these values, adjust based on feel**

### Camera Distance
**In Orbital Follow > Orbits:**
- Increase/decrease **Radius** values for distance from player
- Middle Orbit Radius is most important (default position)

### Camera Height
**In Orbital Follow > Orbits:**
- Adjust **Height** values to change camera elevation
- Middle Orbit Height is most important

### Collision Response
**In Deoccluder:**
- **Distance Limit**: Minimum distance when colliding (1.0 = stays 1 unit away)
- **Damping**: How smoothly camera moves (0.5 = moderate smoothing)

### Player Rotation Speed
**In WalkState.cs and RunState.cs:**
- Line with `Time.deltaTime * 10f` in Quaternion.Slerp
- Lower = slower rotation (try 5f)
- Higher = faster rotation (try 15f)

---

## Key Differences from Cinemachine 2.x

| Cinemachine 2.x | Cinemachine 3.x |
|----------------|-----------------|
| `CinemachineFreeLook` | `CinemachineCamera` + `CinemachineOrbitalFollow` |
| `m_XAxis`, `m_YAxis` | `HorizontalAxis`, `VerticalAxis` |
| 3 separate rigs | 3 orbits in single component |
| `CinemachineCollider` | `CinemachineDeoccluder` |
| Input Axis Name fields | Manual input via script |

---

## Next Steps After Setup

1. ✅ Test camera follows player
2. ✅ Test mouse rotation
3. ✅ Test player movement in camera direction
4. ✅ Test collision detection
5. ⏳ Tune parameters to feel
6. ⏳ Move to next prototype feature

---

## Related Documentation

- [Camera Controller Design (Cinemachine)](./camera-controller-design-cinemachine.md) - Original 2.x design
- [Player Controller Design](../player-controller/player-controller-design.md)
- [Component Overview](../component-overview.md)

## Official Cinemachine 3.x Resources

- [Unity Cinemachine 3.1 Documentation](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html)
- [Upgrading from Cinemachine 2.x](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineUpgradeFrom2.html)
