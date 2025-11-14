# Parkour Multiplayer Prototype - Component Overview

**Date:** 2025-11-14
**Project:** Parkour Legion Demo
**Type:** Basic Multiplayer Prototype

## Project Scope

### Core Gameplay
- **Mode:** Race to finish line
- **Players:** Multiplayer (Colyseus)
- **Visuals:** Capsules initially, then 3D models later
- **Movement:** Basic parkour (jump, wallrun, slide)

### Technical Approach
- **Server Authority:** Trust client positions (prototype-friendly)
- **Camera:** Third-person, dynamic distance on collisions
- **Environment:** Basic shapes (cubes, platforms, ramps, walls)

---

## Component Categories

### 1. Player Systems
Components related to local player control and mechanics.

- **Character Controller**
  - Movement (walk, run, sprint)
  - Physics and grounding detection
  - Input handling (keyboard/mouse, gamepad)

- **Parkour Actions**
  - Jump mechanics
  - Wallrun mechanics
  - Slide mechanics

- **Player State Machine**
  - Idle state
  - Running state
  - Jumping state
  - Wallrunning state
  - Sliding state
  - State transitions and conditions

- **Animation Controller** (Future - after capsule phase)
  - Animation state machine
  - Animation blending
  - IK (Inverse Kinematics) if needed

---

### 2. Camera Systems
Third-person camera following the player.

- **Third-Person Camera Controller**
  - Follow player smoothly
  - Rotation based on mouse input
  - Distance and height offset settings

- **Camera Collision Detection**
  - Dynamic distance adjustment when hitting walls
  - Return to normal distance when clear
  - Consider Cinemachine for advanced features

- **Camera Settings**
  - Mouse sensitivity
  - Camera distance (default and min)
  - Vertical angle limits

---

### 3. Multiplayer/Networking
Colyseus integration for multiplayer functionality.

- **Network Manager**
  - Connect to Colyseus server
  - Room creation/joining
  - Connection error handling

- **Player Spawning**
  - Spawn local player at spawn point
  - Spawn remote players for other clients
  - Assign unique player IDs

- **State Synchronization**
  - Send local player position/rotation to server
  - Receive other players' positions/rotations
  - Sync movement state (idle, running, jumping, etc.)

- **Network Player Controller**
  - Visual representation of remote players
  - Position/rotation interpolation for smooth movement
  - Show movement state visually (color changes or simple effects)

- **Latency Handling**
  - Interpolation for remote players
  - Smooth position updates

---

### 4. Level/Environment
The parkour course and world structure.

- **Prototype Level Design**
  - Platforms (cubes, varied heights)
  - Ramps and slopes
  - Walls for wallrunning
  - Gaps for jumping
  - Sliding sections

- **Spawn Points**
  - Player starting positions
  - Multiple spawn points for multiplayer

- **Checkpoints**
  - Progress tracking markers
  - Visual feedback (particle effects, color change)
  - Finish line checkpoint

- **Collision Layers**
  - Ground layer
  - Wall layer
  - Player layer
  - Define interaction rules

---

### 5. Game Logic
Rules, progression, and session management.

- **Game Session Manager**
  - Race countdown/start
  - Track race time
  - Detect race completion
  - Handle player finishing order

- **Checkpoint System**
  - Detect checkpoint triggers
  - Track which checkpoints passed
  - Send checkpoint progress to server
  - Display checkpoint feedback

- **Respawn System**
  - Detect player falling off map (death zone)
  - Respawn at last checkpoint
  - Reset player state

- **UI/HUD**
  - Race timer display
  - Checkpoint counter (e.g., "3/10")
  - Player list with positions
  - Simple main menu (join/create room)

---

### 6. Server-Side
Node.js Colyseus server handling game sessions.

- **Colyseus Room**
  - ParkourRoom class
  - Room lifecycle (onCreate, onJoin, onLeave)
  - Handle player messages

- **Server State Schema**
  - Player schema (id, position, rotation, movement state)
  - Room state schema (players map, race state, checkpoint data)
  - TypeScript definitions

- **Input Validation** (Optional - skip for prototype)
  - Movement validation
  - Anti-cheat measures

- **Room Lifecycle Management**
  - Player join/leave handling
  - Room disposal when empty
  - Matchmaking options

---

## Component Dependencies

### Core Dependencies
```
Player Systems → Camera Systems (camera follows player)
Player Systems → Multiplayer (local player syncs to network)
Multiplayer → Server-Side (client connects to server)
Game Logic → Player Systems (respawn, checkpoints affect player)
Game Logic → Multiplayer (checkpoint progress sent to server)
Level/Environment → Player Systems (player interacts with environment)
```

### Build Order Recommendation
1. **Server-Side** - Set up Colyseus room and state schema
2. **Multiplayer** - Network manager and connection
3. **Player Systems** - Basic character controller (just movement)
4. **Camera Systems** - Basic third-person follow
5. **Level/Environment** - Simple test level
6. **Player Systems** - Add parkour actions (jump, wallrun, slide)
7. **Multiplayer** - Sync parkour actions and remote players
8. **Game Logic** - Checkpoints and race timer
9. **UI/HUD** - Display race info
10. **Polish** - Camera collisions, respawn system, visual feedback

---

## Next Steps

After this overview, we need to plan each component category in detail:

- [ ] Player Systems detailed design
- [ ] Camera Systems detailed design
- [ ] Multiplayer/Networking detailed design
- [ ] Level/Environment detailed design
- [ ] Game Logic detailed design
- [ ] Server-Side detailed design

Each detailed design should include:
- Specific classes/scripts needed
- Key methods and properties
- Interactions with other components
- Unity-specific implementation notes

---

## References

- [Colyseus Research](../../research/colyseus/README.md)
- [Colyseus Unity Integration](../../research/colyseus/unity-integration.md)
- [Colyseus State Synchronization](../../research/colyseus/state-synchronization.md)
