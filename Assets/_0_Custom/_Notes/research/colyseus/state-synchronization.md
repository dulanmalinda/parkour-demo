# Colyseus State Synchronization

**Research Date:** 2025-11-14
**Schema Documentation:** https://docs.colyseus.io/state/schema
**Best Practices:** https://docs.colyseus.io/state/best-practices

## What is Colyseus Schema?

**Schema** is a special data type from Colyseus that is capable of:
- **Encoding changes/mutations incrementally** (delta encoding)
- **Binary serialization** for efficient network transmission
- **Automatic synchronization** from server to all clients
- **Property-level change tracking**

## Core Concepts

### Automatic State Synchronization
- Server maintains authoritative game state
- Binary patches of state changes are sent to clients
- Default patch rate: **50ms (20fps)**, configurable per room
- Only **changed properties** are transmitted (bandwidth efficient)

### Delta Encoding
Colyseus tracks property-level changes:
- Only the **latest mutation** of each property is queued
- Intermediate changes are optimized away
- Sent during the patchRate interval

**Example:**
```typescript
player.x = 10;  // Change 1
player.x = 15;  // Change 2
player.x = 20;  // Change 3
// Only the final value (20) is sent to clients
```

## Schema Definition

### TypeScript/JavaScript (Server-Side)

```typescript
import { Schema, type, MapSchema, ArraySchema } from "@colyseus/schema";

export class Player extends Schema {
    @type("string") id: string;
    @type("string") name: string;

    // Position
    @type("number") x: number = 0;
    @type("number") y: number = 0;
    @type("number") z: number = 0;

    // Rotation
    @type("number") rotX: number = 0;
    @type("number") rotY: number = 0;
    @type("number") rotZ: number = 0;

    // Movement state
    @type("number") velocityX: number = 0;
    @type("number") velocityY: number = 0;
    @type("number") velocityZ: number = 0;

    // Parkour state
    @type("boolean") isGrounded: boolean = true;
    @type("boolean") isWallRunning: boolean = false;
    @type("boolean") isSliding: boolean = false;

    @type("number") lastCheckpoint: number = 0;
}

export class ParkourRoomState extends Schema {
    @type({ map: Player }) players = new MapSchema<Player>();
    @type("number") gameTime: number = 0;
    @type("string") mapName: string = "";
}
```

### C# (Unity Client-Side)

```csharp
using Colyseus.Schema;

public class Player : Schema {
    [Type(0, "string")]
    public string id;

    [Type(1, "string")]
    public string name;

    // Position
    [Type(2, "number")]
    public float x = 0;

    [Type(3, "number")]
    public float y = 0;

    [Type(4, "number")]
    public float z = 0;

    // Rotation
    [Type(5, "number")]
    public float rotX = 0;

    [Type(6, "number")]
    public float rotY = 0;

    [Type(7, "number")]
    public float rotZ = 0;

    // Movement state
    [Type(8, "number")]
    public float velocityX = 0;

    [Type(9, "number")]
    public float velocityY = 0;

    [Type(10, "number")]
    public float velocityZ = 0;

    // Parkour state
    [Type(11, "boolean")]
    public bool isGrounded = true;

    [Type(12, "boolean")]
    public bool isWallRunning = false;

    [Type(13, "boolean")]
    public bool isSliding = false;

    [Type(14, "number")]
    public int lastCheckpoint = 0;
}

public class ParkourRoomState : Schema {
    [Type(0, "map", typeof(MapSchema<Player>))]
    public MapSchema<Player> players = new MapSchema<Player>();

    [Type(1, "number")]
    public float gameTime = 0;

    [Type(2, "string")]
    public string mapName = "";
}
```

## Data Types

### Primitive Types
- `"string"` - Text data
- `"number"` - Integers and floats
- `"boolean"` - True/false
- `"int8"`, `"uint8"`, `"int16"`, `"uint16"`, `"int32"`, `"uint32"`, `"int64"`, `"uint64"` - Specific integer types
- `"float32"`, `"float64"` - Specific float types

### Collection Types
- **MapSchema** - Dictionary/map of entities (key-value pairs)
- **ArraySchema** - List/array of entities
- **SetSchema** - Unique set of entities

## Best Practices

### 1. Minimize Synchronizable Data Structures
**Ideally**, each Schema class should only have **field definitions**:
- Keep data structures simple
- No heavy game logic in Schema classes
- Separate data from logic

```typescript
// ✅ GOOD: Simple data structure
export class Player extends Schema {
    @type("number") x: number;
    @type("number") y: number;
}

// ❌ BAD: Logic mixed with data
export class Player extends Schema {
    @type("number") x: number;
    @type("number") y: number;

    calculateDistance(other: Player) { /* ... */ }
    updatePhysics(deltaTime: number) { /* ... */ }
}
```

### 2. Keep Room Classes Small
Delegate game-specific functionality to other composable structures:

```typescript
// ✅ GOOD: Separated concerns
export class ParkourRoom extends Room<ParkourRoomState> {
    physics: PhysicsEngine;        // Separate module
    parkourActions: ParkourActions; // Separate module

    onCreate() {
        this.physics = new PhysicsEngine();
        this.parkourActions = new ParkourActions();
    }
}
```

### 3. Use MapSchema for Entities
**Maps** are recommended to track game entities by ID:
- Players
- Enemies
- Items
- Checkpoints

```typescript
export class RoomState extends Schema {
    @type({ map: Player }) players = new MapSchema<Player>();
    @type({ map: Checkpoint }) checkpoints = new MapSchema<Checkpoint>();
}
```

**Client-side tracking:**
```csharp
room.State.players.OnAdd += (key, player) => {
    Debug.Log($"Player {key} joined!");
    SpawnPlayerVisual(player);
};

room.State.players.OnRemove += (key, player) => {
    Debug.Log($"Player {key} left!");
    DestroyPlayerVisual(player);
};

room.State.players.OnChange += (key, player) => {
    // Individual player changed
    UpdatePlayerVisual(player);
};
```

### 4. Use ArraySchema for Static Collections
**Arrays** are recommended for:
- World maps
- Static level geometry
- Fixed-size collections

```typescript
export class RoomState extends Schema {
    @type([Platform]) platforms = new ArraySchema<Platform>();
}
```

### 5. Optimize Field Order
**Encoding order is based on field definition order**:
- Put **frequently updated fields first**
- This leverages encoding optimizations
- Critical for performance in fast-paced games

```typescript
// ✅ GOOD: Frequently updated fields first
export class Player extends Schema {
    @type("number") x: number;        // Updated every frame
    @type("number") y: number;        // Updated every frame
    @type("number") z: number;        // Updated every frame
    @type("string") name: string;     // Rarely changes
    @type("string") skinId: string;   // Rarely changes
}
```

## Technical Limitations

### Field Capacity
- Each Schema structure supports **maximum 64 serialized fields**
- For larger datasets, use **nested Schema structures**

```typescript
// ❌ BAD: Too many fields (>64)
export class Player extends Schema {
    @type("number") field1: number;
    @type("number") field2: number;
    // ... 63 more fields
    @type("number") field65: number; // ERROR!
}

// ✅ GOOD: Nested schemas
export class PlayerStats extends Schema {
    @type("number") health: number;
    @type("number") stamina: number;
    // ... more stats
}

export class Player extends Schema {
    @type("number") x: number;
    @type("number") y: number;
    @type(PlayerStats) stats: PlayerStats;
}
```

### Number Encoding
- **NaN and Infinity are encoded as 0**
- Avoid relying on NaN/Infinity in synchronized state

### String Handling
- **null strings are encoded as ""** (empty string)
- Plan for empty string handling on client

### Array Limitations
- **Multi-dimensional arrays NOT supported**
- Use flattened 1D arrays with manual indexing

```typescript
// ❌ BAD: Multi-dimensional array
@type(["number"]) grid: number[][]; // NOT SUPPORTED

// ✅ GOOD: Flattened array
@type(["number"]) grid: number[]; // [0,1,2,3,4,5,6,7,8]
// Access: grid[y * width + x]
```

## Schema Consistency Requirement

**CRITICAL:** Server encoder and client decoder must use **identical schema definitions**:
- Same field types
- Same field order
- Same type indices (C# [Type(0, ...)])

**Mismatch will cause synchronization failures!**

## State Listening in Unity

### Property-Level Changes
```csharp
player.OnChange += (changes) => {
    foreach (var change in changes) {
        switch(change.Field) {
            case "x":
            case "y":
            case "z":
                UpdatePlayerPosition(player);
                break;
            case "isWallRunning":
                if (player.isWallRunning) {
                    PlayWallRunAnimation();
                }
                break;
        }
    }
};
```

### Collection Changes
```csharp
room.State.players.OnAdd += (sessionId, player) => {
    // Spawn new player
};

room.State.players.OnRemove += (sessionId, player) => {
    // Remove player
};

room.State.players.OnChange += (sessionId, player) => {
    // Player updated
};
```

## Parkour Game State Design

### Recommended State Structure

```typescript
// Player entity
export class Player extends Schema {
    // Identity (rarely changes)
    @type("string") id: string;
    @type("string") name: string;

    // Transform (updated frequently) - PUT FIRST!
    @type("float32") x: number = 0;
    @type("float32") y: number = 0;
    @type("float32") z: number = 0;
    @type("float32") rotY: number = 0; // Only Y rotation for third-person

    // Velocity (updated frequently)
    @type("float32") velocityX: number = 0;
    @type("float32") velocityY: number = 0;
    @type("float32") velocityZ: number = 0;

    // Parkour state (changes occasionally)
    @type("uint8") movementState: number = 0; // 0=idle, 1=running, 2=jumping, 3=wallrun, 4=sliding
    @type("boolean") isGrounded: boolean = true;

    // Progress tracking (rarely changes)
    @type("uint8") lastCheckpoint: number = 0;
    @type("float32") raceTime: number = 0;
}

// Checkpoint entity
export class Checkpoint extends Schema {
    @type("uint8") id: number;
    @type("float32") x: number;
    @type("float32") y: number;
    @type("float32") z: number;
}

// Room state
export class ParkourRoomState extends Schema {
    @type({ map: Player }) players = new MapSchema<Player>();
    @type([Checkpoint]) checkpoints = new ArraySchema<Checkpoint>();
    @type("string") mapName: string = "parkour_01";
    @type("float32") raceStartTime: number = 0;
    @type("boolean") raceStarted: boolean = false;
}
```

### Movement State Enum
Instead of multiple booleans, use a single enum:

**Benefits:**
- Fewer fields to synchronize
- Mutually exclusive states
- Clearer state machine

```typescript
// Server-side
enum MovementState {
    IDLE = 0,
    RUNNING = 1,
    JUMPING = 2,
    WALLRUNNING = 3,
    SLIDING = 4,
    CLIMBING = 5
}

// Client-side (Unity)
public enum MovementState {
    IDLE = 0,
    RUNNING = 1,
    JUMPING = 2,
    WALLRUNNING = 3,
    SLIDING = 4,
    CLIMBING = 5
}
```

## Patch Rate Tuning for Parkour

### Recommended Settings

```typescript
onCreate() {
    this.setState(new ParkourRoomState());

    // For smooth parkour movement
    this.setPatchRate(33); // 30fps - good balance

    // If bandwidth allows
    // this.setPatchRate(16); // 60fps - very smooth

    // For slow-paced or turn-based
    // this.setPatchRate(50); // 20fps - default
}
```

**Considerations:**
- **33ms (30fps)**: Recommended starting point for parkour
- **16ms (60fps)**: Smoother but uses more bandwidth
- **Lower patch rate = less bandwidth, more "jumpy" movement**
- **Higher patch rate = more bandwidth, smoother movement**

## Client-Side Prediction (Optional Advanced Topic)

For ultra-responsive controls, implement client-side prediction:

1. **Client** predicts movement locally
2. **Server** validates and sends corrections
3. **Client** reconciles if mismatch

**Note:** Complex to implement, not required for basic prototype

## Next Steps for Implementation

1. Define TypeScript schema on server
2. Generate or manually create C# schema for Unity
3. Ensure field types and order match exactly
4. Test synchronization with 2+ clients
5. Tune patch rate for optimal performance

## Related Documentation
- [Overview](./overview.md)
- [Unity Integration](./unity-integration.md)
- [Server Architecture](./server-architecture.md)

## References
- Schema Documentation: https://docs.colyseus.io/state/schema
- Best Practices: https://docs.colyseus.io/state/best-practices
- Schema GitHub: https://github.com/colyseus/schema
