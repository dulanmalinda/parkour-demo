# Colyseus Research for Parkour Multiplayer Game

**Research Date:** 2025-11-14
**Project:** Parkour Legion Demo - Multiplayer Prototype
**Framework:** Colyseus (https://colyseus.io/)

## Research Overview

This directory contains comprehensive research on Colyseus multiplayer framework for building a parkour multiplayer game prototype in Unity.

## Documentation Structure

### 1. [Overview](./overview.md)
**What it covers:**
- What is Colyseus and why use it
- Core architecture and design principles
- Key features (state sync, matchmaking, scalability)
- Platform support and SDKs
- Community adoption and deployment options
- Advantages for parkour multiplayer prototype

**Key Takeaways:**
- ✅ Free, open-source MIT license
- ✅ Authoritative server architecture (anti-cheat)
- ✅ Automatic state synchronization
- ✅ Official Unity SDK with first-class support
- ✅ Room-based matchmaking system
- ✅ Actively maintained (updated November 2025)

### 2. [Unity Integration](./unity-integration.md)
**What it covers:**
- Installation methods (UPM recommended)
- Client initialization and setup
- Room connection methods (Create, Join, JoinById, JoinOrCreate)
- Room events (OnJoin, OnLeave, OnStateChange, OnError)
- Communication patterns (send/receive messages)
- Testing with Multiplayer Play Mode / ParrelSync
- State schema integration in Unity
- Best practices and error handling

**Key Takeaways:**
- ✅ Simple UPM installation via git URL
- ✅ JoinOrCreate for automatic matchmaking
- ✅ Event-driven architecture for state changes
- ✅ Built-in testing support in Unity 6000.1.0b1+
- ✅ Async/await pattern for clean code

### 3. [Server Architecture](./server-architecture.md)
**What it covers:**
- Authoritative server model
- Room-based system and lifecycle
- Key lifecycle methods (onCreate, onAuth, onJoin, onLeave, onDispose)
- State management and patch rate configuration
- Message handling (client-to-server, server-to-client)
- Matchmaking and room filtering
- Scalability (single process vs distributed)
- Recommended server structure for parkour game
- Best practices (server-side validation, input-based movement, checkpoint system)

**Key Takeaways:**
- ✅ Room lifecycle provides clear hooks for game logic
- ✅ Configurable patch rate (recommend 33ms for parkour)
- ✅ Server validates all movement (prevents cheating)
- ✅ Simple Node.js/TypeScript setup
- ✅ Scalable to multiple processes with Redis

### 4. [State Synchronization](./state-synchronization.md)
**What it covers:**
- What is Colyseus Schema
- Delta encoding and automatic synchronization
- Schema definition (TypeScript server, C# Unity client)
- Supported data types (primitives, collections)
- Best practices (minimize data, optimize field order, use maps for entities)
- Technical limitations (64 field max, no multi-dimensional arrays)
- State listening in Unity
- Recommended state structure for parkour game
- Patch rate tuning

**Key Takeaways:**
- ✅ Binary delta encoding for efficient bandwidth
- ✅ Only changed properties transmitted
- ✅ Schema definitions must match exactly on server/client
- ✅ Put frequently updated fields first for optimization
- ✅ Use MapSchema for players, ArraySchema for static data
- ✅ Single movement state enum better than multiple booleans

## Quick Start Recommendations

### For Prototype Development

**1. Server Setup:**
- Use Node.js with TypeScript
- Start with single process (no Redis needed)
- Set patch rate to 33ms (30fps)
- Implement basic room with player join/leave

**2. Unity Client:**
- Install via UPM: `https://github.com/colyseus/colyseus-unity3d.git#upm`
- Use JoinOrCreate for matchmaking
- Implement state change listeners
- Test with Multiplayer Play Mode (Unity 6+) or ParrelSync

**3. State Design:**
- Keep state minimal for prototype
- Sync: position, rotation, velocity, movement state
- Use enum for movement state (idle, running, jumping, wallrun, sliding)
- Store checkpoint progress

**4. Movement Approach:**
- **Option A (Simple):** Sync position directly with server validation
- **Option B (Better):** Input-based movement with server-side physics

**5. Testing:**
- Start with 2 clients locally
- Test connection, disconnection, reconnection
- Verify state synchronization
- Test parkour actions (jump, wallrun, slide)

## Recommended State Schema for Prototype

**Server (TypeScript):**
```typescript
export class Player extends Schema {
    @type("string") id: string;
    @type("float32") x: number;
    @type("float32") y: number;
    @type("float32") z: number;
    @type("float32") rotY: number;
    @type("uint8") movementState: number; // 0=idle, 1=run, 2=jump, 3=wallrun, 4=slide
}

export class ParkourRoomState extends Schema {
    @type({ map: Player }) players = new MapSchema<Player>();
}
```

**Client (C#):**
```csharp
public class Player : Schema {
    [Type(0, "string")] public string id;
    [Type(1, "number")] public float x;
    [Type(2, "number")] public float y;
    [Type(3, "number")] public float z;
    [Type(4, "number")] public float rotY;
    [Type(5, "number")] public byte movementState;
}

public class ParkourRoomState : Schema {
    [Type(0, "map", typeof(MapSchema<Player>))]
    public MapSchema<Player> players = new MapSchema<Player>();
}
```

## Next Steps

After completing research, proceed to project planning:

1. **Define prototype scope** - What parkour actions to implement?
2. **Plan Unity architecture** - Character controller, camera, networking layer
3. **Plan server architecture** - Room logic, physics validation
4. **Create task breakdown** - Prioritize features for MVP
5. **Set up development environment** - Install dependencies, create projects

## Additional Resources

- **Official Docs:** https://docs.colyseus.io/
- **Unity SDK:** https://github.com/colyseus/colyseus-unity-sdk
- **Examples:** https://github.com/colyseus/colyseus-unity-sdk/tree/master
- **Discord Community:** Active support for questions

## Research Completed By

Cody (Claude Code Assistant)
Date: 2025-11-14
