# Multiplayer Implementation Plan (Colyseus)

**Date:** 2025-11-14
**Component:** Multiplayer/Networking Systems
**Design Reference:** [multiplayer-architecture-design.md](./multiplayer-architecture-design.md)
**Approach:** Client-authoritative with Colyseus

---

## Implementation Phases

### Phase 1: Node.js Server Setup
- [x] Create Node.js project directory (D:\_UNITY\parkour-server)
- [x] Initialize package.json with npm init
- [x] Install Colyseus dependencies (@colyseus/core, @colyseus/schema, @colyseus/ws-transport)
- [x] Install TypeScript and dev dependencies
- [x] Create tsconfig.json
- [x] Set up project structure (src/, rooms/, schema/)
- [x] Create basic server entry point (src/index.ts)

### Phase 2: Server State Schema
- [x] Create src/schema/PlayerState.ts
- [x] Define PlayerState class with @colyseus/schema decorators
- [x] Create src/schema/ParkourRoomState.ts
- [x] Define ParkourRoomState with players MapSchema
- [x] Export schemas from index
- [x] Test schema compilation

### Phase 3: Server Room Implementation
- [x] Create src/rooms/ParkourRoom.ts
- [x] Implement onCreate with state initialization
- [x] Set patch rate to 33ms (30fps)
- [x] Implement onJoin (create PlayerState, add to map)
- [x] Implement onLeave (remove PlayerState from map)
- [x] Add message handler for "updatePosition"
- [x] Add message handler for "checkpointReached" (optional)
- [x] Register room in server index.ts

### Phase 4: Server Testing
- [x] Build and run server (npm start)
- [x] Test server starts without errors (running on ws://localhost:2567)
- [ ] Install @colyseus/monitor (optional devtools)
- [ ] Test room creation via monitor or client

### Phase 5: Unity Schema Generation
- [x] Generate C# schemas from TypeScript schemas
- [x] Create Scripts/Schema/ directory in Unity
- [x] Add PlayerState.cs (matching server schema)
- [x] Add ParkourRoomState.cs (matching server schema)
- [x] Ensure field types and indices match server exactly

### Phase 6: Unity NetworkManager
- [x] Create Scripts/Networking/ directory
- [x] Implement NetworkManager.cs (singleton)
- [x] Add ColyseusClient initialization
- [x] Implement ConnectToServer() method
- [x] Implement JoinOrCreateRoom() method
- [x] Add local player spawn logic
- [x] Add remote player spawn/despawn logic
- [x] Handle room.State.players OnAdd/OnRemove events (using Callbacks.Get API)

### Phase 7: Local Player Network Sync
- [x] Implement LocalPlayerNetworkSync.cs
- [x] Add update rate timer (20 updates/sec)
- [x] Get references to PlayerController and room
- [x] Implement SendPositionUpdate() method
- [x] Add GetMovementStateInt() helper to PlayerController
- [ ] Attach script to LocalPlayer prefab (Unity Editor setup)
- [ ] Test position updates are sent

### Phase 8: Remote Player Prefab
- [ ] Create RemotePlayerPrefab (capsule, different color)
- [ ] No PlayerController or CharacterController
- [ ] Add MeshRenderer for visual feedback
- [ ] Assign different material/color
- [ ] Save as prefab

### Phase 9: Remote Player Network Sync
- [x] Implement RemotePlayerNetworkSync.cs
- [x] Add Initialize(PlayerState) method
- [x] Store target position and rotation
- [x] Implement Update() with interpolation (Lerp/Slerp)
- [x] Poll Schema properties directly (no callback needed)
- [ ] Attach to RemotePlayerPrefab (Unity Editor setup)

### Phase 10: Remote Player Visual Feedback
- [x] Implement RemotePlayerController.cs
- [x] Add reference to PlayerState
- [x] Change capsule color based on movementState
- [x] Map state int to colors (idle=white, walk=green, run=blue, etc.)
- [ ] Attach to RemotePlayerPrefab (Unity Editor setup)

### Phase 11: NetworkManager Integration
- [x] Create NetworkManager GameObject in scene
- [x] Attach NetworkManager script
- [x] Configure server URL (ws://localhost:2567)
- [x] Assign LocalPlayer prefab reference
- [x] Assign RemotePlayer prefab reference
- [x] Set room name ("parkour")

### Phase 12: Local Player Setup
- [x] Add LocalPlayerNetworkSync to Player GameObject
- [x] Ensure PlayerController reference is set
- [x] Test script initializes without errors

### Phase 13: Connection Testing
- [x] Start Colyseus server (npm run dev)
- [x] Start Unity and press Play
- [x] Test connection to server succeeds
- [x] Test room join succeeds
- [x] Check Console for any errors
- [x] Verify local player spawns

### Phase 14: Multi-Client Testing
- [x] Build Unity project or use ParrelSync
- [x] Run 2 clients simultaneously
- [x] Test both connect to same room
- [x] Verify each client sees other player
- [x] Test movement synchronization
- [x] Test player leave/rejoin
- [x] Fixed race condition with initial state sync
- [x] Fixed duplicate player spawn error with OnAdd callback

### Phase 15: Interpolation Tuning
- [ ] Adjust interpolation speed in RemotePlayerNetworkSync
- [ ] Test different values (5f, 10f, 15f)
- [ ] Find balance between smoothness and lag
- [ ] Tune update rate if needed (10-30 updates/sec)

### Phase 16: Visual Feedback Testing
- [ ] Test color changes for each movement state
- [ ] Verify idle, walk, run, jump, fall, slide colors
- [ ] Ensure remote players show correct state
- [ ] Test transitions between states

### Phase 17: Checkpoint Integration (Optional)
- [ ] Add checkpoint trigger detection
- [ ] Send "checkpointReached" message to server
- [ ] Update PlayerState.lastCheckpoint on server
- [ ] Display checkpoint progress in UI (future)

### Phase 18: Polish & Bug Fixes
- [ ] Test with 4+ players
- [ ] Fix any synchronization issues
- [ ] Ensure smooth interpolation
- [ ] Handle edge cases (disconnect, reconnect)
- [ ] Add connection status UI (optional)
- [ ] Code cleanup and comments

---

## Implementation Order

**Start with:**
1. Server setup (Phase 1-4)
2. Unity schemas (Phase 5)
3. NetworkManager (Phase 6)
4. Local player sync (Phase 7)
5. Remote players (Phase 8-10)
6. Integration and testing (Phase 11-14)
7. Tuning and polish (Phase 15-18)

---

## Success Criteria

✅ Colyseus server runs without errors
✅ Unity client connects to server successfully
✅ Local player can join room
✅ Local player position updates sent to server
✅ Remote players spawn for other clients
✅ Remote players interpolate smoothly
✅ Movement states sync correctly (colors change)
✅ Multiple clients (2-4) can play together
✅ Players see each other move/jump/slide in real-time
✅ Remote players interpolate smoothly
✅ Movement states sync correctly (colors change)
✅ Multiple clients (2-4) can play together
✅ Players see each other move/jump/slide in real-time
✅ Disconnect/reconnect handled gracefully
✅ No major lag or jitter
✅ Code follows project patterns (no inner comments)

---

## Files to Create

### Server-Side (Node.js)
```
parkour-server/
├── src/
│   ├── index.ts
│   ├── rooms/
│   │   └── ParkourRoom.ts
│   └── schema/
│       ├── PlayerState.ts
│       └── ParkourRoomState.ts
├── package.json
└── tsconfig.json
```

### Client-Side (Unity)
```
Scripts/
├── Networking/
│   ├── NetworkManager.cs
│   ├── LocalPlayerNetworkSync.cs
│   ├── RemotePlayerNetworkSync.cs
│   └── RemotePlayerController.cs
└── Schema/
    ├── PlayerState.cs
    └── ParkourRoomState.cs
```

### Prefabs
```
Prefabs/
├── LocalPlayer (modified existing)
└── RemotePlayer (new)
```

---

## Testing Scenarios

### Basic Connection
- [ ] Server starts successfully
- [ ] Client connects to server
- [ ] Client joins room
- [ ] No Console errors

### Single Player
- [ ] Local player can move
- [ ] Position updates sent to server
- [ ] No performance issues

### Two Players
- [ ] Both clients connect to same room
- [ ] Each sees the other player
- [ ] Movement syncs in real-time
- [ ] Colors change with movement states
- [ ] Smooth interpolation

### Multiple Players (4+)
- [ ] All players see each other
- [ ] No significant lag
- [ ] Interpolation remains smooth
- [ ] Bandwidth usage acceptable

### Disconnect/Reconnect
- [ ] Player disconnect removes remote player
- [ ] Reconnect spawns player again
- [ ] No crashes or errors

### Edge Cases
- [ ] Rapid movement changes
- [ ] Jumping and landing
- [ ] Sliding
- [ ] All movement state transitions
- [ ] Simultaneous player actions

---

## Dependencies

### Server
```json
{
  "@colyseus/core": "latest",
  "@colyseus/schema": "latest",
  "express": "latest",
  "cors": "latest",
  "typescript": "latest",
  "@types/node": "latest",
  "ts-node": "latest",
  "nodemon": "latest"
}
```

### Unity
- Colyseus Unity SDK (already installed via UPM)
- Existing player controller scripts
- Unity Input System (already configured for Both)

---

## Configuration

### Server Settings
```typescript
// src/index.ts
const port = Number(process.env.PORT || 2567);
const gameServer = new Server({
    server: createServer()
});

gameServer.define("parkour", ParkourRoom);
gameServer.listen(port);
```

### Unity Settings
```csharp
// NetworkManager.cs
serverUrl = "ws://localhost:2567"
roomName = "parkour"
updateRate = 0.05f // 20 updates/sec
interpolationSpeed = 10f
```

### Room Settings
```typescript
// ParkourRoom.ts
maxClients = 8
patchRate = 33 // 30fps
```

---

## Troubleshooting Guide

### Server won't start
- Check Node.js installed (node -v)
- Check dependencies installed (npm install)
- Check TypeScript compiles (npm run build)
- Check port 2567 not in use

### Unity can't connect
- Check server is running
- Check server URL correct (ws://localhost:2567)
- Check firewall not blocking
- Check Console for error messages

### Remote players don't spawn
- Check room.State.players.OnAdd event subscribed
- Check RemotePlayer prefab assigned in NetworkManager
- Check Console for spawn errors

### Movement doesn't sync
- Check LocalPlayerNetworkSync sending updates
- Check "updatePosition" message handler on server
- Check server receiving messages (add debug logs)

### Remote players jitter
- Increase interpolation speed
- Decrease update rate
- Check network latency

### Colors don't change
- Check movementState being sent correctly
- Check RemotePlayerController updating material
- Check state int matches enum (0-5)

---

## Performance Targets

### Bandwidth
- **Per player:** ~360 bytes/sec (18 bytes × 20 updates/sec)
- **8 players:** ~4.3 KB/s total
- **Target:** < 10 KB/s

### Latency
- **LAN:** < 10ms
- **Local network:** < 50ms
- **Acceptable:** < 100ms

### Frame Rate
- **Target:** 60 FPS maintained with 8 players
- **Minimum:** 30 FPS

---

## Next Step

Ready to begin **Phase 1: Node.js Server Setup**

Create the server project directory and initialize the Node.js project!
