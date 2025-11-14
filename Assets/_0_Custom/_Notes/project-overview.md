# Parkour Legion Demo - Project Overview

**Date:** 2025-11-15
**Status:** Active Development - Multiplayer Prototype Phase
**Framework:** Unity with Colyseus Multiplayer

---

## 📋 Project Summary

A multiplayer parkour racing game prototype where players compete to reach the finish line. Built with Unity and Colyseus for real-time multiplayer synchronization.

### Core Gameplay
- **Mode:** Race to finish line
- **Players:** Multiplayer (2-8 players per room)
- **Movement:** Parkour mechanics (walk, run, jump, slide)
- **Visuals:** 18 character model skins
- **Camera:** Third-person Cinemachine-based

### Technical Stack
- **Engine:** Unity (C#)
- **Multiplayer:** Colyseus (Node.js WebSocket server)
- **Camera:** Cinemachine 3.x
- **Network Protocol:** Client-authoritative (prototype-friendly)
- **State Sync:** 30fps patch rate via Colyseus state schema

---

## 🏗️ Architecture Overview

### Project Structure
```
Assets/_0_Custom/
├── Scripts/
│   ├── Player/              # Player controller, state machine, physics
│   │   ├── PlayerController.cs
│   │   ├── PlayerStateMachine.cs
│   │   ├── PlayerInputHandler.cs
│   │   ├── PlayerPhysics.cs
│   │   ├── PlayerModelManager.cs
│   │   └── States/          # State pattern implementation
│   │       ├── PlayerState.cs (abstract)
│   │       ├── IdleState.cs
│   │       ├── WalkState.cs
│   │       ├── RunState.cs
│   │       ├── JumpState.cs
│   │       ├── FallState.cs
│   │       └── SlideState.cs
│   ├── Camera/              # Camera input handling
│   │   └── CameraInputProvider.cs
│   ├── Networking/          # Multiplayer synchronization
│   │   ├── NetworkManager.cs
│   │   ├── LocalPlayerNetworkSync.cs
│   │   ├── RemotePlayerNetworkSync.cs
│   │   └── RemotePlayerController.cs
│   └── Schema/              # Colyseus state schemas
│       ├── ParkourRoomState.cs
│       └── PlayerState.cs
└── _Notes/                  # Design documentation
    ├── design/              # Component designs
    ├── research/            # Technical research
    └── logs/                # Development logs
```

### Server Structure
```
parkour-server/
└── src/
    ├── rooms/
    │   └── ParkourRoom.ts   # Game room logic
    └── schema/
        ├── ParkourRoomState.ts
        └── PlayerState.ts
```

---

## 🎮 Core Systems

### 1. Player Controller System
**Location:** `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Player\`

**Architecture:** State Machine Pattern
- **PlayerController:** Main orchestrator, uses CharacterController component
- **PlayerStateMachine:** Manages state transitions
- **PlayerPhysics:** Custom gravity and physics calculations
- **PlayerInputHandler:** Input detection (WASD, Space, Shift, C)

**Movement States:**
- **Idle:** Stationary on ground
- **Walk:** Base movement (5 units/s)
- **Run:** Sprint movement (8 units/s, hold Shift)
- **Jump:** Ascending phase after jump
- **Fall:** Descending/falling phase
- **Slide:** Parkour slide action (10 units/s initial)

**Physics:**
- Custom gravity application (-9.81 m/s²)
- Jump velocity calculation using kinematic equations
- Ground detection via raycast
- CharacterController-based movement (no Rigidbody)

**Design Docs:** [player-controller-design.md](_Notes/design/parkour-prototype/player-controller/player-controller-design.md)

---

### 2. Camera System
**Location:** `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Camera\`

**Architecture:** Cinemachine 3.x
- **CameraInputProvider:** Handles mouse input for camera rotation
- **Cinemachine Camera:** Uses OrbitalFollow for third-person view
- **Cursor Management:** Lock/unlock cursor (ESC to toggle)

**Settings:**
- Mouse sensitivity: 200 (horizontal), 2 (vertical)
- Orbital follow with dynamic distance
- Cursor locked during gameplay

**Design Docs:** [camera-controller-design-cinemachine.md](_Notes/design/parkour-prototype/camera-controller/camera-controller-design-cinemachine.md)

---

### 3. Multiplayer System
**Location:** `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Networking\`

**Architecture:** Colyseus WebSocket
- **Server URL:** ws://localhost:2567
- **Room Name:** "parkour"
- **Max Players:** 8 per room
- **Patch Rate:** 33ms (30fps updates)

**Network Flow:**
1. **Client Connects** → NetworkManager joins room
2. **Local Player Spawns** → LocalPlayerNetworkSync sends updates
3. **Server Broadcasts** → State changes to all clients
4. **Remote Players Update** → RemotePlayerNetworkSync interpolates positions

**Synced Data:**
- Position (x, y, z)
- Rotation (Y-axis only)
- Movement state (0-5 enum)
- Grounded status
- Skin ID (0-17)

**Components:**
- **NetworkManager:** Connection manager, spawns local/remote players
- **LocalPlayerNetworkSync:** Sends local player state to server (20 updates/sec)
- **RemotePlayerNetworkSync:** Interpolates remote player positions
- **RemotePlayerController:** Visual representation of remote players

**Design Docs:** [multiplayer-architecture-design.md](_Notes/design/parkour-prototype/multiplayer/multiplayer-architecture-design.md)

---

### 4. Character Model System
**Location:** `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Player\PlayerModelManager.cs`

**Status:** ✅ CODE COMPLETE - Needs Unity Editor Setup

**Architecture:** Model Switching System
- **18 Character Models:** skinId 0-17
- **Model Container:** GFXs GameObject with child models
- **Activation:** SetActive() switching (no instantiation)
- **Animation:** Single Animator with "state" parameter (0-5)

**Features:**
- Random skin selection on join
- Network-synced skin IDs
- Animation state updates based on movement
- Cached Animator reference for performance

**Implementation Status:**
- ✅ Server schema updated (skinId field)
- ✅ Unity schema updated
- ✅ PlayerModelManager script created
- ✅ LocalPlayerNetworkSync integrated
- ✅ RemotePlayerNetworkSync integrated
- ⏳ Unity Editor setup pending (add components to prefabs)
- ⏳ Animator controllers need verification

**Design Docs:** [player-models-design.md](_Notes/design/parkour-prototype/player-models/player-models-design.md)
**Status Doc:** [IMPLEMENTATION-STATUS.md](_Notes/design/parkour-prototype/player-models/IMPLEMENTATION-STATUS.md)

---

### 5. Lobby & Game Start System (PLANNED)
**Location:** Not yet implemented

**Status:** 📝 DESIGN COMPLETE - Not Implemented

**Planned Architecture:** State-driven UI system
- **Game States:** MENU → CONNECTING → WAITING → COUNTDOWN → PLAYING
- **Min Players:** 2 to start countdown
- **Countdown:** 3 seconds
- **Player Freeze:** Players spawn frozen until game starts

**Planned Components:**
- GameUIManager.cs (state orchestrator)
- MenuUI.cs (Play button)
- LobbyUI.cs (waiting/countdown display)
- Modified NetworkManager (manual connection)
- Modified PlayerController (MovementEnabled property)

**Design Docs:** [lobby-ui-design.md](_Notes/design/parkour-prototype/lobby-ui/lobby-ui-design.md)

---

## 📊 Current Implementation Status

### ✅ Completed Systems
- [x] Player state machine (all 6 states)
- [x] Custom physics system
- [x] Input handling
- [x] Camera controller (Cinemachine)
- [x] Network connection (Colyseus)
- [x] Local player sync
- [x] Remote player sync
- [x] Character model system (code complete)

### ⏳ In Progress
- [ ] Character model Unity setup (add components to prefabs)
- [ ] Animator controller verification (18 models)

### 📝 Planned (Designed but Not Implemented)
- [ ] Lobby/menu UI system
- [ ] Game start countdown
- [ ] Player movement freeze system
- [ ] Cursor control integration

### 🔮 Future Enhancements
- [ ] Wallrun mechanics
- [ ] Vault mechanics
- [ ] Checkpoint system
- [ ] Race timer/leaderboard
- [ ] Respawn system
- [ ] Server-side validation

---

## 🔄 Git History

**Recent Commits:**
```
0fcba90 - safe commit v2
efd1108 - safe commit
f795b5e - character models syc fix
e0a4f92 - character models
364b6f7 - safe commit 2
ab4f17c - safe commit
3b37909 - colyseus base v2
232b8d0 - colyseus base v1
b159389 - cam controller
97c038e - first commit
```

**Branch:** main
**Untracked:** `_Notes/design/parkour-prototype/lobby-ui/`

---

## 🎯 Key Design Decisions

### Why CharacterController over Rigidbody?
- Built-in collision detection
- No physics overhead
- Predictable movement
- Better network synchronization

### Why State Machine Pattern?
- Clean separation of logic
- Easy to add new states
- Clear state transitions
- Better debugging
- Easier animation integration

### Why Client-Authoritative?
- ✅ Prototype-friendly (no server physics)
- ✅ Simpler implementation
- ✅ Lower latency for local player
- ⚠️ Not production-ready (cheating possible)
- 🔜 Will need server validation later

### Why Colyseus?
- Built for multiplayer games
- Strong TypeScript typing
- State synchronization out-of-box
- Delta encoding (efficient bandwidth)
- Easy Unity integration

---

## 📖 Documentation Structure

### Design Documents
**Location:** `_Notes/design/parkour-prototype/`

- [component-overview.md](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\component-overview.md) - High-level system overview
- [player-controller/](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\player-controller/) - Player system design
- [camera-controller/](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\camera-controller/) - Camera system design
- [multiplayer/](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\multiplayer/) - Network architecture
- [player-models/](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\player-models/) - Character model system
- [lobby-ui/](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\lobby-ui/) - Lobby/menu design

### Research Notes
**Location:** `_Notes/research/`

- [colyseus/](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\research\colyseus/) - Colyseus framework research

### Development Logs
**Location:** `_Notes/logs/`

- player1.md, player2.md - Testing logs

---

## 🔧 Development Environment

### Unity Project Settings
- **Working Directory:** `D:\_UNITY\parkour legion demo\Assets\_0_Custom`
- **Git Repo:** Yes (main branch)
- **Platform:** Windows (win32)

### Server Setup
- **Location:** `D:\_UNITY\parkour-server`
- **Command:** `npm run dev`
- **Port:** 2567

### Input Configuration
```
Movement: WASD
Run: Left Shift (hold)
Jump: Space
Slide: C or Left Ctrl
Camera: Mouse (locked during gameplay)
Unlock Cursor: ESC
```

---

## 🎨 Code Style Standards

### Project-Specific Rules (from CLAUDE.md)
- **NO inner-function comments** (only comment class/method definitions)
- **NO explanatory comments** (code should be self-documenting)
- **Structural comments only** (WHAT, not HOW)
- **Unity script organization:** All scripts in `Scripts/` subdirectories
- **Planning → Implementation workflow:** Design → document → confirm → code
- **No patch files:** Always use Read → Edit/Write directly
- **Never run dev servers:** Assume user is running their own

### Development Principles
- Pre-production assumption (no backward compatibility needed)
- No migrations by default
- Clean implementation first
- Path of least action
- Never use exceptions for business logic
- Cross-check claims with evidence
- Production-ready code every time

---

## 🚀 Next Steps (Recommended)

### Immediate (Current Sprint)
1. **Character Models Unity Setup** (15-30 min)
   - Add PlayerModelManager to LocalPlayer/RemotePlayer prefabs
   - Assign GFXs container references
   - Verify Animator controllers on 18 models

2. **Testing** (30-60 min)
   - Test single player model switching
   - Test multi-client skin synchronization
   - Test animation state changes

### Short-Term (Next Sprint)
3. **Lobby UI Implementation** (4-6 hours)
   - Implement GameUIManager state system
   - Create MenuUI/LobbyUI components
   - Update NetworkManager for manual connection
   - Add PlayerController movement freeze
   - Integrate cursor control

4. **Full Flow Testing**
   - Menu → Connection → Waiting → Countdown → Playing
   - Test with 2-4 players
   - Edge case testing (disconnects, etc.)

### Medium-Term (Future)
5. **Game Logic Systems**
   - Checkpoint system
   - Race timer
   - Finish line detection
   - Leaderboard

6. **Advanced Parkour**
   - Wallrun mechanics
   - Vault mechanics
   - Ledge climbing

7. **Polish & Optimization**
   - Animation refinement
   - Visual effects
   - Sound effects
   - Performance optimization

---

## 📞 Related Documentation

- [Colyseus Official Docs](https://docs.colyseus.io/)
- [Cinemachine 3.x Docs](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/index.html)
- [Unity CharacterController API](https://docs.unity3d.com/ScriptReference/CharacterController.html)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-15
**Author:** Research Mode Survey
