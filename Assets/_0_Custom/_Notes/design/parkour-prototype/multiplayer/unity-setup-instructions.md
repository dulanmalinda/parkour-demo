# Unity Multiplayer Setup Instructions

**Date:** 2025-11-14
**Status:** Scripts completed, Unity Editor setup required

---

## ✅ Completed: All C# Scripts

All networking scripts have been implemented:

### Scripts Created

```
Scripts/
├── Schema/
│   ├── PlayerState.cs ✅
│   └── ParkourRoomState.cs ✅
└── Networking/
    ├── NetworkManager.cs ✅
    ├── LocalPlayerNetworkSync.cs ✅
    ├── RemotePlayerNetworkSync.cs ✅
    └── RemotePlayerController.cs ✅
```

### Modified Scripts

- `Scripts/Player/PlayerController.cs` ✅
  - Added `GetMovementStateInt()` method at line 95

---

## 🎯 Next Steps: Unity Editor Setup

### Step 1: Create NetworkManager GameObject

1. In your main scene, create empty GameObject
2. Name it `NetworkManager`
3. Add component: `NetworkManager` script
4. Configure in Inspector:
   - **Server URL:** `ws://localhost:2567`
   - **Room Name:** `parkour`
   - **Local Player Prefab:** (Assign after creating prefab)
   - **Remote Player Prefab:** (Assign after creating prefab)
   - **Spawn Position:** `(0, 1, 0)`

---

### Step 2: Create Local Player Prefab

**Option A: Convert Existing Player**

If you already have a Player GameObject in scene:

1. Select your Player GameObject
2. Add component: `LocalPlayerNetworkSync`
3. Configure in Inspector:
   - **Update Rate:** `0.05` (20 updates/sec)
4. Drag Player GameObject to Project window to create prefab
5. Assign this prefab to NetworkManager's "Local Player Prefab" slot
6. **Delete the Player GameObject from the scene** (NetworkManager will spawn it)

**Option B: Create From Scratch**

1. Create new GameObject → Name it `LocalPlayer`
2. Add `CharacterController` component:
   - Height: `2.0`
   - Radius: `0.5`
   - Center: `(0, 1, 0)`
3. Add `Capsule` mesh renderer (for visual)
4. Add `PlayerController` script
5. Add `LocalPlayerNetworkSync` script
   - Update Rate: `0.05`
6. Create empty child GameObject → Name it `CameraTarget`
   - Position: `(0, 1.5, 0)`
7. Configure PlayerController Inspector:
   - Walk Speed: `5`
   - Run Speed: `8`
   - Jump Height: `2`
   - Slide Speed: `10`
   - Slide Duration: `1`
   - Gravity: `-9.81`
   - Ground Check Distance: `0.2`
   - Ground Layer: Select `Ground` layer
8. Drag to Project window to create prefab
9. Assign to NetworkManager's "Local Player Prefab"
10. Delete from scene

---

### Step 3: Create Remote Player Prefab

1. Create new GameObject → Name it `RemotePlayer`
2. Add `Capsule` mesh renderer:
   - Scale: `(1, 1, 1)`
   - Position: `(0, 1, 0)`
3. Create new Material → Name it `RemotePlayerMat`
   - Set Shader to `Standard` or `URP/Lit`
   - Assign to Capsule's MeshRenderer
4. Add `RemotePlayerNetworkSync` script to RemotePlayer
   - Interpolation Speed: `10`
5. Add `RemotePlayerController` script to RemotePlayer
   - Idle Color: White `(1, 1, 1, 1)`
   - Walk Color: Green `(0, 1, 0, 1)`
   - Run Color: Blue `(0, 0, 1, 1)`
   - Jump Color: Yellow `(1, 1, 0, 1)`
   - Fall Color: Red `(1, 0, 0, 1)`
   - Slide Color: Magenta `(1, 0, 1, 1)`
6. Drag to Project window to create prefab
7. Assign to NetworkManager's "Remote Player Prefab"
8. Delete from scene

---

### Step 4: Setup Camera (If Not Already Done)

1. Create or find your Main Camera
2. Add Cinemachine Camera (or use existing)
3. Ensure camera will follow the spawned LocalPlayer's CameraTarget

**Note:** Camera should target dynamically spawned player, not scene instance

---

### Step 5: Test Connection

1. Ensure Colyseus server is running (`npm run dev` in parkour-server)
2. Press Play in Unity
3. Check Console for messages:
   - ✅ "Connected to room: [roomId], Session: [sessionId]"
   - ✅ "Local player spawned"
   - ✅ No errors

---

### Step 6: Test with Multiple Clients

**Option A: Build and Run**

1. Build your Unity project (File → Build Settings → Build)
2. Run the built executable
3. Press Play in Unity Editor
4. Both should connect to same room
5. Move in one client, see movement in other

**Option B: Use ParrelSync (Recommended)**

1. Install ParrelSync from Package Manager
2. Create a clone project
3. Open clone in another Unity Editor instance
4. Press Play in both editors
5. Test multiplayer

---

### Step 7: Verify Synchronization

**Test Checklist:**

- [ ] Local player spawns at (0, 1, 0)
- [ ] Can move with WASD
- [ ] Can run with Shift
- [ ] Can jump with Space
- [ ] Can slide with C
- [ ] Console shows position updates being sent
- [ ] Second client sees first player as capsule
- [ ] Remote player moves smoothly
- [ ] Remote player changes color based on movement state:
  - White = Idle
  - Green = Walk
  - Blue = Run
  - Yellow = Jump
  - Red = Fall
  - Magenta = Slide
- [ ] Disconnect removes remote player

---

## 🔧 Troubleshooting

### "Failed to connect to server"

- Check server is running (`npm run dev` in parkour-server)
- Check console for "Colyseus server listening on ws://localhost:2567"
- Verify NetworkManager Server URL is `ws://localhost:2567`

### "PlayerController not found on LocalPlayer"

- Ensure LocalPlayer prefab has PlayerController component
- Check LocalPlayerNetworkSync is on same GameObject as PlayerController

### "No Renderer found on RemotePlayer"

- Ensure RemotePlayer prefab has a Capsule child with MeshRenderer
- RemotePlayerController will find renderer in children

### Remote players don't spawn

- Check Console for "Player added" messages
- Verify RemotePlayer prefab assigned to NetworkManager
- Ensure prefab has both RemotePlayerNetworkSync and RemotePlayerController

### Remote players don't move smoothly

- Increase Interpolation Speed (try 15 or 20)
- Check network latency (ping localhost should be <5ms)
- Verify server is sending updates (check server console)

### Colors don't change

- Verify RemotePlayerController colors are configured
- Check material uses `_Color` property (Standard shader)
- Try URP/Lit shader if using URP

---

## 📊 Performance Monitoring

### Console Messages (Normal Operation)

```
Connected to room: [roomId], Session: [sessionId]
Local player spawned
Player added: [otherSessionId]
Remote player spawned: [otherSessionId]
```

### Expected Network Traffic

- **Outgoing:** ~20 updates/second per player (~360 bytes/sec)
- **Incoming:** Server updates at 30fps (~18 bytes per remote player)
- **For 2 players:** <1 KB/s total
- **For 8 players:** ~4-5 KB/s total

---

## 🎮 Testing Scenarios

### Basic Movement

1. Walk forward with W → Remote player should move forward (green)
2. Hold Shift while moving → Remote player turns blue (run)
3. Press Space → Remote player turns yellow (jump), then red (fall)
4. Press C while moving → Remote player turns magenta (slide)

### State Transitions

1. Stand still → Remote player white (idle)
2. Walk → Green
3. Run → Blue
4. Jump → Yellow → Red → Back to walk/idle
5. Slide → Magenta → Back to walk/idle

### Multiple Clients

1. Start 2 clients
2. Move both independently
3. Each sees the other moving
4. Verify smooth interpolation
5. Verify color changes sync

---

## 📝 Notes

- Server must be running before starting Unity
- First player to connect creates the room
- Maximum 8 players per room (configurable in ParkourRoom.ts)
- Update rate tunable in LocalPlayerNetworkSync (default 0.05s = 20fps)
- Interpolation speed tunable in RemotePlayerNetworkSync (default 10)

---

## ✅ Success Criteria

When setup is complete, you should see:

1. ✅ Server running in console
2. ✅ Unity connects without errors
3. ✅ Local player spawns and is controllable
4. ✅ Second client spawns remote player
5. ✅ Movement syncs in real-time
6. ✅ Colors change with movement states
7. ✅ Smooth interpolation, no jittering
8. ✅ Disconnect cleanly removes players

---

## 🚀 Ready for Testing!

All code is complete. Follow the steps above to configure Unity Editor and test your multiplayer system!
