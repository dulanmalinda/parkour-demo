# Colyseus Server Architecture

**Research Date:** 2025-11-14
**Server Documentation:** https://docs.colyseus.io/server
**Room API:** https://docs.colyseus.io/server/room

## Core Server Concepts

### Authoritative Server Model
Colyseus follows an **authoritative server architecture**:
- Server is the source of truth
- Clients send input, server validates and processes
- Prevents client-side cheating
- Essential for competitive parkour multiplayer

### Room-Based System
The fundamental unit of Colyseus multiplayer is the **Room**:
- Each room represents a game session
- Rooms are spawned on-demand per client request
- Multiple room instances can exist from a single room definition
- Rooms are stateful and manage their own game logic

## Room Lifecycle

### Key Lifecycle Methods

#### onCreate(options)
Called once when the room is created by the matchmaker
```typescript
onCreate(options: any) {
    this.setState(new MyRoomState());
    this.maxClients = options.maxPlayers || 4;

    console.log("Room created with options:", options);
}
```

**Use Cases:**
- Initialize room state
- Set room configuration (max players, map, etc.)
- Load game data

#### onAuth(client, options)
Called before onJoin, validates client's request to join
```typescript
async onAuth(client: Client, options: any) {
    // Validate player token, check bans, etc.
    return { playerId: options.playerId };
}
```

**Use Cases:**
- Authentication/authorization
- Player validation
- Anti-cheat checks

#### onJoin(client, options)
Triggered when a client successfully joins the room
```typescript
onJoin(client: Client, options: any) {
    console.log(client.sessionId, "joined!");

    // Create player entity
    const player = new Player();
    player.id = client.sessionId;
    this.state.players.set(client.sessionId, player);
}
```

**Use Cases:**
- Spawn player entities
- Initialize player data
- Send welcome messages

#### onLeave(client, consented)
Triggered when a client leaves the room
```typescript
onLeave(client: Client, consented: boolean) {
    console.log(client.sessionId, "left!");

    // Remove player entity
    this.state.players.delete(client.sessionId);
}
```

**Use Cases:**
- Remove player entities
- Handle disconnections
- Save player progress

#### onDispose()
Called when the room is being disposed (no more clients)
```typescript
onDispose() {
    console.log("Room disposed");
    // Clean up resources
}
```

**Use Cases:**
- Clean up timers
- Save room data
- Release resources

## State Management

### State Synchronization
Colyseus **automatically synchronizes** the room state to all clients:

```typescript
import { Room } from "colyseus";
import { MyRoomState } from "./schema/MyRoomState";

export class MyRoom extends Room<MyRoomState> {
    onCreate(options: any) {
        this.setState(new MyRoomState());

        // Game loop
        this.setSimulationInterval((deltaTime) => {
            this.update(deltaTime);
        });
    }

    update(deltaTime: number) {
        // Update game state
        // Changes are automatically sent to clients
        this.state.gameTime += deltaTime;
    }
}
```

### Patch Rate Configuration
Control how often state updates are sent:

```typescript
onCreate() {
    // Default: 50ms (20fps)
    this.setPatchRate(50);

    // Fast-paced parkour: 33ms (30fps)
    this.setPatchRate(33);

    // Very fast: 16ms (60fps) - higher bandwidth
    this.setPatchRate(16);
}
```

**Recommendations for Parkour Game:**
- **33ms (30fps)** - good balance for parkour movement
- **16ms (60fps)** - if bandwidth allows, smoother movement
- Test with different rates to find optimal performance

## Message Handling

### Receiving Messages from Clients

```typescript
onCreate() {
    // Listen for specific message types
    this.onMessage("move", (client, message) => {
        const player = this.state.players.get(client.sessionId);
        player.x = message.x;
        player.y = message.y;
        player.z = message.z;
    });

    this.onMessage("jump", (client, message) => {
        const player = this.state.players.get(client.sessionId);
        this.handleJump(player, message);
    });

    this.onMessage("wallrun", (client, message) => {
        const player = this.state.players.get(client.sessionId);
        this.handleWallRun(player, message);
    });
}
```

### Sending Messages to Clients

```typescript
// Send to specific client
this.send(client, "game_event", { type: "checkpoint_reached" });

// Broadcast to all clients
this.broadcast("player_died", { playerId: client.sessionId });

// Broadcast to all except one
this.broadcast("player_action", data, { except: client });
```

## Matchmaking

Colyseus provides built-in matchmaking through the client connection methods:

### Room Options
Clients can specify room criteria:
```typescript
// Server-side: filter by options
onCreate(options: any) {
    if (options.mapName) {
        this.metadata = { mapName: options.mapName };
    }
}
```

### Matchmaker Integration
```typescript
// Define multiple room types
gameServer.define("parkour_easy", ParkourRoom)
    .filterBy(["difficulty"]);

gameServer.define("parkour_hard", ParkourRoom)
    .filterBy(["difficulty"]);
```

## Scalability

### Single Process
For prototyping and small games:
- Run on single Node.js process
- Handles hundreds of concurrent players
- Simple deployment

### Multiple Processes/Machines
For production scale:
- Requires **Presence Server** (Redis)
- Horizontal scaling across machines
- Load balancing
- Session persistence

```typescript
import { RedisPresence } from "@colyseus/redis-presence";

const gameServer = new Server({
    presence: new RedisPresence()
});
```

## Server Structure for Parkour Game

### Recommended File Structure
```
server/
├── src/
│   ├── rooms/
│   │   └── ParkourRoom.ts          # Main room logic
│   ├── schema/
│   │   ├── ParkourRoomState.ts     # State definition
│   │   ├── Player.ts                # Player entity
│   │   └── Checkpoint.ts            # Checkpoint entity
│   ├── game/
│   │   ├── physics.ts               # Server-side physics
│   │   ├── parkourActions.ts        # Jump, wallrun, etc.
│   │   └── collisionDetection.ts    # Collision checks
│   └── index.ts                     # Server entry point
├── package.json
└── tsconfig.json
```

### Basic Server Setup

```typescript
// src/index.ts
import { Server } from "colyseus";
import { createServer } from "http";
import { ParkourRoom } from "./rooms/ParkourRoom";

const port = Number(process.env.PORT || 2567);
const gameServer = new Server({
    server: createServer()
});

// Define room
gameServer.define("parkour", ParkourRoom);

gameServer.listen(port);
console.log(`Colyseus server listening on port ${port}`);
```

## Best Practices for Parkour Game Server

### 1. Server-Side Physics Validation
```typescript
onMessage("move", (client, message) => {
    const player = this.state.players.get(client.sessionId);

    // Validate movement is physically possible
    if (this.isValidMove(player, message)) {
        player.position = message.position;
    } else {
        // Reject and send correction
        this.send(client, "position_correction", player.position);
    }
});
```

### 2. Input-Based Movement
Instead of syncing position directly, sync inputs:
```typescript
onMessage("input", (client, message) => {
    const player = this.state.players.get(client.sessionId);
    player.input = message; // { forward, right, jump, etc. }
});

update(deltaTime) {
    // Process all player inputs server-side
    this.state.players.forEach(player => {
        this.processPlayerInput(player, deltaTime);
    });
}
```

### 3. Checkpoint System
```typescript
onMessage("checkpoint_reached", (client, message) => {
    const player = this.state.players.get(client.sessionId);

    // Validate checkpoint is reachable
    if (this.validateCheckpoint(player, message.checkpointId)) {
        player.lastCheckpoint = message.checkpointId;
        this.broadcast("checkpoint_reached", {
            playerId: client.sessionId,
            checkpointId: message.checkpointId,
            time: this.state.gameTime
        });
    }
});
```

### 4. Keep Rooms Small
Delegate game logic to separate modules:
```typescript
// ParkourRoom.ts
import { PhysicsEngine } from "../game/physics";
import { ParkourActions } from "../game/parkourActions";

export class ParkourRoom extends Room<ParkourRoomState> {
    physics: PhysicsEngine;
    parkourActions: ParkourActions;

    onCreate() {
        this.physics = new PhysicsEngine();
        this.parkourActions = new ParkourActions(this.physics);
    }
}
```

## Transport Layer

### WebSockets
Colyseus uses **WebSockets** for real-time bidirectional communication:
- Low latency
- Full-duplex communication
- Efficient for game networking

### Message Serialization
- **State sync**: Custom binary delta serializer (Colyseus Schema)
- **Custom messages**: MessagePack (fastest for JavaScript)

## Next Steps for Server Implementation

1. Set up Node.js project with TypeScript
2. Install Colyseus dependencies
3. Define room state schema (see [State Synchronization](./state-synchronization.md))
4. Implement basic ParkourRoom with player join/leave
5. Add server-side movement validation
6. Test with Unity client

## Related Documentation
- [Overview](./overview.md)
- [Unity Integration](./unity-integration.md)
- [State Synchronization](./state-synchronization.md)

## References
- Server Documentation: https://docs.colyseus.io/server
- Room API: https://docs.colyseus.io/server/room
- Tutorials: https://www.imini.app/docs/tutorial-multiple-player/server-colyseus/
