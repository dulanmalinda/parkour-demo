# Multiplayer Architecture Design (Colyseus)

**Date:** 2025-11-14
**Component:** Multiplayer/Networking Systems
**Framework:** Colyseus with Node.js server
**Approach:** Client-authoritative for prototype (trust client positions)

## Design Requirements

### Core Goals
- ✅ Multiple players in same parkour race
- ✅ Real-time position synchronization
- ✅ See other players moving/jumping/sliding
- ✅ Race to finish line gameplay
- ✅ Simple checkpoint tracking

### Technical Approach
- **Client-authoritative:** Trust client positions (prototype-friendly, no server-side physics)
- **State synchronization:** Position, rotation, movement state only
- **Patch rate:** 33ms (30fps) for smooth movement
- **Room-based:** Each race is a separate Colyseus room

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    COLYSEUS SERVER                       │
│                     (Node.js)                            │
├─────────────────────────────────────────────────────────┤
│  ParkourRoom                                            │
│  ├── Room State (ParkourRoomState)                     │
│  │   └── Players Map (sessionId → PlayerState)         │
│  ├── onJoin: Add player to state                       │
│  ├── onLeave: Remove player from state                 │
│  └── onMessage: Receive player updates                 │
└─────────────────────────────────────────────────────────┘
                          ↕ WebSocket
┌─────────────────────────────────────────────────────────┐
│                  UNITY CLIENT                            │
├─────────────────────────────────────────────────────────┤
│  NetworkManager                                         │
│  ├── Connect to Colyseus                               │
│  ├── Join/Create room                                   │
│  └── Handle connection events                           │
│                                                          │
│  LocalPlayer                                            │
│  ├── PlayerController (existing)                        │
│  ├── Send position updates to server                    │
│  └── LocalPlayerNetworkSync component                   │
│                                                          │
│  RemotePlayers                                          │
│  ├── Spawn prefab for each remote player               │
│  ├── RemotePlayerNetworkSync component                  │
│  └── Interpolate received positions                     │
└─────────────────────────────────────────────────────────┘
```

---

## What Needs to be Synced?

### From Our Existing PlayerController:

**Essential (Must Sync):**
- Position (x, y, z)
- Rotation (only Y axis for orientation)
- Movement state (idle, walk, run, jump, fall, slide)
- Is grounded (for visual feedback)

**Optional (Nice to Have):**
- Velocity (for interpolation prediction)
- Last checkpoint (for race tracking)

**Not Synced:**
- Input (each client handles their own)
- Camera (each client has their own camera)
- Local physics calculations

---

## Server-Side Design

### File Structure

```
parkour-server/
├── src/
│   ├── rooms/
│   │   └── ParkourRoom.ts
│   ├── schema/
│   │   ├── ParkourRoomState.ts
│   │   └── PlayerState.ts
│   ├── index.ts
│   └── config.ts
├── package.json
└── tsconfig.json
```

### State Schema Design

#### PlayerState.ts (Server)

```typescript
import { Schema, type } from "@colyseus/schema";

export class PlayerState extends Schema {
    // Identity
    @type("string") id: string;
    @type("string") name: string = "Player";

    // Transform (frequently updated - put first for optimization)
    @type("float32") x: number = 0;
    @type("float32") y: number = 1;
    @type("float32") z: number = 0;
    @type("float32") rotY: number = 0;  // Only Y rotation needed

    // Movement state (0=idle, 1=walk, 2=run, 3=jump, 4=fall, 5=slide)
    @type("uint8") movementState: number = 0;
    @type("boolean") isGrounded: boolean = true;

    // Progress tracking (optional for prototype)
    @type("uint8") lastCheckpoint: number = 0;
}
```

**Why these types?**
- `float32` instead of `number` - smaller bandwidth
- `uint8` for movement state - only 0-5 values needed
- `rotY` only - third-person doesn't need pitch/roll sync

#### ParkourRoomState.ts (Server)

```typescript
import { Schema, type, MapSchema } from "@colyseus/schema";
import { PlayerState } from "./PlayerState";

export class ParkourRoomState extends Schema {
    @type({ map: PlayerState })
    players = new MapSchema<PlayerState>();

    @type("float32") raceStartTime: number = 0;
    @type("boolean") raceStarted: boolean = false;
}
```

### ParkourRoom.ts (Server)

```typescript
import { Room, Client } from "colyseus";
import { ParkourRoomState, PlayerState } from "../schema";

export class ParkourRoom extends Room<ParkourRoomState> {
    maxClients = 8;  // Max players per race

    onCreate(options: any) {
        this.setState(new ParkourRoomState());
        this.setPatchRate(33);  // 30fps updates

        // Handle player position updates
        this.onMessage("updatePosition", (client, message) => {
            const player = this.state.players.get(client.sessionId);
            if (player) {
                player.x = message.x;
                player.y = message.y;
                player.z = message.z;
                player.rotY = message.rotY;
                player.movementState = message.movementState;
                player.isGrounded = message.isGrounded;
            }
        });

        // Handle checkpoint reached
        this.onMessage("checkpointReached", (client, message) => {
            const player = this.state.players.get(client.sessionId);
            if (player) {
                player.lastCheckpoint = message.checkpointId;
            }
        });
    }

    onJoin(client: Client, options: any) {
        console.log(client.sessionId, "joined!");

        const player = new PlayerState();
        player.id = client.sessionId;
        player.name = options.playerName || "Player";

        // Spawn at starting position
        player.x = 0;
        player.y = 1;
        player.z = 0;

        this.state.players.set(client.sessionId, player);
    }

    onLeave(client: Client, consented: boolean) {
        console.log(client.sessionId, "left!");
        this.state.players.delete(client.sessionId);
    }
}
```

---

## Client-Side Design (Unity)

### Script Structure

```
Scripts/
├── Networking/
│   ├── NetworkManager.cs
│   ├── LocalPlayerNetworkSync.cs
│   ├── RemotePlayerNetworkSync.cs
│   └── RemotePlayerController.cs
└── Schema/
    ├── PlayerState.cs (generated from server schema)
    └── ParkourRoomState.cs (generated from server schema)
```

### Component Design

#### 1. NetworkManager.cs
Singleton that manages Colyseus connection.

**Responsibilities:**
- Connect to Colyseus server
- Join/Create parkour room
- Spawn local and remote players
- Handle room events

**Key Properties:**
```csharp
string serverUrl = "ws://localhost:2567"
string roomName = "parkour"
ColyseusRoom<ParkourRoomState> room
GameObject localPlayerPrefab
GameObject remotePlayerPrefab
```

**Key Methods:**
```csharp
async void Start() // Connect and join room
void SpawnLocalPlayer()
void SpawnRemotePlayer(string sessionId, PlayerState state)
void RemoveRemotePlayer(string sessionId)
```

#### 2. LocalPlayerNetworkSync.cs
Attached to local player, sends updates to server.

**Responsibilities:**
- Read position/rotation from PlayerController
- Read movement state from StateMachine
- Send updates to server periodically

**Key Properties:**
```csharp
float updateRate = 0.05f // 20 updates/sec
PlayerController playerController
ColyseusRoom room
```

**Key Methods:**
```csharp
void Start() // Get references
void FixedUpdate() // Send updates at fixed rate
void SendPositionUpdate()
```

**Update Logic:**
```csharp
void FixedUpdate() {
    updateTimer += Time.fixedDeltaTime;

    if (updateTimer >= updateRate) {
        SendPositionUpdate();
        updateTimer = 0;
    }
}

void SendPositionUpdate() {
    room.Send("updatePosition", new {
        x = transform.position.x,
        y = transform.position.y,
        z = transform.position.z,
        rotY = transform.rotation.eulerAngles.y,
        movementState = GetMovementStateInt(),
        isGrounded = playerController.IsGrounded
    });
}
```

#### 3. RemotePlayerNetworkSync.cs
Attached to remote player prefabs, receives and interpolates state.

**Responsibilities:**
- Listen for state changes from Colyseus
- Interpolate position/rotation smoothly
- Update visual representation

**Key Properties:**
```csharp
PlayerState playerState // Reference to Colyseus state
Vector3 targetPosition
float targetRotationY
float interpolationSpeed = 10f
```

**Key Methods:**
```csharp
void Initialize(PlayerState state) // Set up state listening
void Update() // Interpolate to target position
void OnStateChange(PlayerState state) // Called by NetworkManager
```

**Interpolation Logic:**
```csharp
void Update() {
    // Interpolate position
    Vector3 current = transform.position;
    transform.position = Vector3.Lerp(current, targetPosition, Time.deltaTime * interpolationSpeed);

    // Interpolate rotation
    Quaternion currentRot = transform.rotation;
    Quaternion targetRot = Quaternion.Euler(0, targetRotationY, 0);
    transform.rotation = Quaternion.Slerp(currentRot, targetRot, Time.deltaTime * interpolationSpeed);
}

void OnStateChange(PlayerState state) {
    targetPosition = new Vector3(state.x, state.y, state.z);
    targetRotationY = state.rotY;
}
```

#### 4. RemotePlayerController.cs
Visual representation of remote players.

**Responsibilities:**
- Display remote player's movement state
- Show animations based on state (future)
- Simple visual feedback for prototype

**Key Properties:**
```csharp
PlayerState playerState
Material playerMaterial
Color idleColor
Color walkColor
Color runColor
Color jumpColor
```

**Visual Feedback:**
```csharp
void Update() {
    // Change color based on movement state (temporary visual feedback)
    switch (playerState.movementState) {
        case 0: playerMaterial.color = idleColor; break;  // White
        case 1: playerMaterial.color = walkColor; break;  // Green
        case 2: playerMaterial.color = runColor; break;   // Blue
        case 3: playerMaterial.color = jumpColor; break;  // Yellow
        case 4: playerMaterial.color = Color.red; break;  // Fall
        case 5: playerMaterial.color = Color.magenta; break; // Slide
    }
}
```

---

## GameObject Setup

### Local Player Hierarchy

```
LocalPlayer
├── PlayerController (existing)
├── LocalPlayerNetworkSync (NEW)
├── CharacterController
├── CameraTarget
└── (Capsule mesh)
```

### Remote Player Prefab

```
RemotePlayerPrefab
├── RemotePlayerNetworkSync (NEW)
├── RemotePlayerController (NEW)
└── Capsule (visual only, different color)
    └── MeshRenderer
```

**Note:** Remote players do NOT have:
- PlayerController (no input)
- CharacterController (no physics)
- Camera (each client has their own)

They are purely visual representations driven by network state.

---

## Data Flow

### Local Player Update Flow

```
PlayerController moves
    ↓
LocalPlayerNetworkSync reads position/state
    ↓
Send "updatePosition" message to server
    ↓
Server updates PlayerState in room state
    ↓
Colyseus sends delta patch to ALL clients
    ↓
Remote clients receive updated PlayerState
    ↓
RemotePlayerNetworkSync interpolates to new position
```

### Remote Player Spawn Flow

```
Player joins room
    ↓
Server: onJoin creates new PlayerState
    ↓
Colyseus sends state.players.OnAdd event
    ↓
NetworkManager spawns RemotePlayerPrefab
    ↓
RemotePlayerNetworkSync initialized with PlayerState
    ↓
Starts interpolating positions
```

---

## Movement State Enum

**Shared between client and server:**

```typescript
// Server (TypeScript)
enum MovementState {
    IDLE = 0,
    WALK = 1,
    RUN = 2,
    JUMP = 3,
    FALL = 4,
    SLIDE = 5
}
```

```csharp
// Client (C#)
public enum MovementState {
    Idle = 0,
    Walk = 1,
    Run = 2,
    Jump = 3,
    Fall = 4,
    Slide = 5
}
```

**Helper in PlayerController:**
```csharp
public int GetMovementStateInt() {
    if (stateMachine.CurrentState is States.IdleState) return 0;
    if (stateMachine.CurrentState is States.WalkState) return 1;
    if (stateMachine.CurrentState is States.RunState) return 2;
    if (stateMachine.CurrentState is States.JumpState) return 3;
    if (stateMachine.CurrentState is States.FallState) return 4;
    if (stateMachine.CurrentState is States.SlideState) return 5;
    return 0;
}
```

---

## Network Optimization

### Update Rate Strategy

**Local Player Sends:**
- **Rate:** 20 updates/second (every 0.05s)
- **Why:** Balance between smoothness and bandwidth
- **What:** Position, rotation, movement state

**Server Broadcasts:**
- **Patch Rate:** 33ms (30fps)
- **Delta encoding:** Only changed properties sent
- **Efficient for 8 players**

### Bandwidth Estimation

**Per player update (delta encoded):**
- Position (3 × float32): 12 bytes
- Rotation Y (float32): 4 bytes
- Movement state (uint8): 1 byte
- IsGrounded (bool): 1 byte
- **Total:** ~18 bytes per player per update

**For 8 players at 30fps:**
- 18 bytes × 8 players × 30 fps = 4.32 KB/s
- Very manageable for prototype

---

## Checkpoint System (Optional for Prototype)

### Simple Implementation

**When player reaches checkpoint:**
```csharp
void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Checkpoint")) {
        int checkpointId = other.GetComponent<Checkpoint>().id;
        room.Send("checkpointReached", new { checkpointId });
    }
}
```

**Server updates last checkpoint:**
```typescript
this.onMessage("checkpointReached", (client, message) => {
    const player = this.state.players.get(client.sessionId);
    if (player) {
        player.lastCheckpoint = message.checkpointId;

        // Broadcast to all for race tracking
        this.broadcast("playerCheckpoint", {
            playerId: client.sessionId,
            checkpointId: message.checkpointId
        });
    }
});
```

---

## Testing Strategy

### Phase 1: Server Setup
- Create Node.js Colyseus server
- Implement ParkourRoom with schema
- Test with Colyseus devtools

### Phase 2: Connection
- Implement NetworkManager
- Test connection to server
- Test join/leave events

### Phase 3: Local Player Sync
- Add LocalPlayerNetworkSync
- Verify position updates sent
- Check server receives updates

### Phase 4: Remote Players
- Spawn remote player prefabs
- Test interpolation
- Verify multiple clients see each other

### Phase 5: Polish
- Tune interpolation speed
- Add visual feedback
- Test with 4+ players

---

## Known Limitations (Prototype)

### Client-Authoritative Risks
⚠️ **Cheating possible** - Client controls their position
⚠️ **No server validation** - Players can teleport/speed hack
✅ **Acceptable for prototype** - Focus on gameplay, not anti-cheat

### No Lag Compensation
⚠️ **High latency visible** - Remote players will lag
⚠️ **No prediction** - Just interpolation
✅ **Acceptable for prototype** - LAN/low-latency testing

### Simple Interpolation
⚠️ **Basic lerp only** - No advanced smoothing
✅ **Good enough for prototype**

---

## Future Enhancements (Post-Prototype)

- [ ] Server-side physics validation
- [ ] Client-side prediction for local player
- [ ] Lag compensation for remote players
- [ ] Snapshot interpolation
- [ ] Movement reconciliation
- [ ] Anti-cheat measures
- [ ] Leaderboard/race results
- [ ] Spectator mode
- [ ] Replays

---

## Next Steps

1. ✅ Design complete
2. ⏳ Set up Node.js Colyseus server project
3. ⏳ Implement server-side schemas and room
4. ⏳ Implement Unity networking scripts
5. ⏳ Test with 2 clients locally
6. ⏳ Tune and polish

---

## Related Documentation

- [Colyseus Research](../../research/colyseus/README.md)
- [Player Controller Design](../player-controller/player-controller-design.md)
- [Component Overview](../component-overview.md)
