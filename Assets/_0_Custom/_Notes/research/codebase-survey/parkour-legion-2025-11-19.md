# Parkour Legion Demo - Codebase Survey

**Date:** 2025-11-19
**Survey Type:** Research Mode - Project Load
**Status:** Active Development - Multiplayer Parkour Racing Prototype

---

## 📋 Executive Summary

**Parkour Legion Demo** is a multiplayer parkour racing game prototype built with **Unity** and **Colyseus** for real-time networking. Players compete in a race-to-finish gameplay mode with parkour mechanics (walk, run, jump, slide). The project uses a state machine pattern for player control, Cinemachine for camera systems, and client-authoritative networking suitable for prototyping.

**Current Phase:** Post-MVP with character models, lobby system, and room code functionality implemented.

---

## 🏗️ Architecture Overview

### Technology Stack
- **Engine:** Unity (C#)
- **Multiplayer:** Colyseus (WebSocket, Node.js server)
- **Camera:** Cinemachine 3.x (OrbitalFollow)
- **Physics:** CharacterController (no Rigidbody)
- **Networking Model:** Client-authoritative (prototype-friendly)
- **State Sync:** 30fps patch rate via Colyseus state schema

### Server Details
- **Server URL:** wss://parkour-demo-colysues-server.onrender.com
- **Room Name:** "parkour"
- **Max Players:** 8 per room
- **Patch Rate:** 33ms (30 updates/sec)

---

## 📁 Project Structure

### Unity Project Layout
```
Assets/_0_Custom/
├── Scripts/
│   ├── Player/              # Player control & state machine
│   │   ├── PlayerController.cs         # Main orchestrator
│   │   ├── PlayerStateMachine.cs       # State pattern manager
│   │   ├── PlayerInputHandler.cs       # Input detection
│   │   ├── PlayerPhysics.cs            # Custom gravity/physics
│   │   ├── PlayerModelManager.cs       # 18 character models
│   │   └── States/                     # 6 movement states
│   │       ├── PlayerState.cs (abstract base)
│   │       ├── IdleState.cs
│   │       ├── WalkState.cs
│   │       ├── RunState.cs
│   │       ├── JumpState.cs
│   │       ├── FallState.cs
│   │       └── SlideState.cs
│   ├── Camera/
│   │   └── CameraInputProvider.cs      # Mouse input & cursor control
│   ├── Networking/
│   │   ├── NetworkManager.cs           # Colyseus connection manager
│   │   ├── LocalPlayerNetworkSync.cs   # Send local state
│   │   ├── RemotePlayerNetworkSync.cs  # Receive remote state
│   │   └── RemotePlayerController.cs   # Visual remote player
│   ├── Schema/                         # Colyseus state schemas
│   │   ├── ParkourRoomState.cs
│   │   └── PlayerState.cs
│   └── UI/                             # Game UI system
│       ├── GameUIManager.cs            # State orchestrator
│       ├── MenuUI.cs                   # Main menu
│       ├── LobbyUI.cs                  # Waiting/countdown
│       └── ClickToResumeOverlay.cs     # Cursor unlock overlay
├── Prefabs/
│   ├── LocalPlayer.prefab              # Controlled player
│   └── RemotePlayer.prefab             # Network players
├── _Notes/                             # Documentation
│   ├── design/                         # Component designs
│   ├── research/                       # Technical research
│   └── logs/                           # Development logs
└── CLAUDE.md                           # AI assistant instructions
```

### Server Project Structure
```
parkour-server/
└── src/
    ├── rooms/
    │   └── ParkourRoom.ts              # Game room logic
    └── schema/
        ├── ParkourRoomState.ts         # Room state schema
        └── PlayerState.ts              # Player state schema
```

---

## 🎮 Core Systems Deep Dive

### 1. Player Controller System
**Location:** `Scripts/Player/`

**Architecture:** State Machine Pattern

**Components:**
- **PlayerController.cs** - Main controller using CharacterController component
  - Manages movement speed settings (walk: 5 u/s, run: 8 u/s, slide: 10 u/s)
  - Provides camera-relative movement calculation
  - Orchestrates input, physics, and state machine
  - Has MovementEnabled property for freeze/unfreeze gameplay

- **PlayerStateMachine.cs** - Generic state machine
  - Dictionary-based state storage
  - Type-safe state transitions
  - Enter/Update/Exit lifecycle

- **PlayerPhysics.cs** - Custom physics calculations
  - Gravity application (-9.81 m/s²)
  - Jump velocity using kinematic equations
  - Ground detection via raycast

- **PlayerInputHandler.cs** - Input detection
  - WASD movement
  - Space for jump
  - Shift for run
  - C/Ctrl for slide

**Movement States:**
| State | Speed | Trigger | Description |
|-------|-------|---------|-------------|
| Idle | 0 | No input + grounded | Stationary |
| Walk | 5 u/s | WASD + grounded | Base movement |
| Run | 8 u/s | WASD + Shift + grounded | Sprint |
| Jump | Calculated | Space + grounded | Ascending phase |
| Fall | Gravity | Not grounded | Descending/falling |
| Slide | 10 u/s | C/Ctrl + grounded | Parkour slide |

**Design Reference:** [player-controller-design.md](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\player-controller\player-controller-design.md)

---

### 2. Camera System
**Location:** `Scripts/Camera/`

**Architecture:** Cinemachine 3.x OrbitalFollow

**Components:**
- **CameraInputProvider.cs** - Handles mouse input and cursor locking
  - Mouse sensitivity: 200 (horizontal), 2 (vertical)
  - ESC to unlock cursor
  - Lock/unlock methods called by GameUIManager

- **Cinemachine Camera** (Unity component)
  - Third-person orbital camera
  - Dynamic distance adjustment
  - Follows CameraTarget transform on LocalPlayer

**Design Reference:** [camera-controller-design-cinemachine.md](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\camera-controller\camera-controller-design-cinemachine.md)

---

### 3. Multiplayer System
**Location:** `Scripts/Networking/`

**Architecture:** Colyseus WebSocket Client-Authoritative

**Network Flow:**
1. **Client Connects** → NetworkManager.CreateRoom() or JoinRoomByCode()
2. **Local Player Spawns** → LocalPlayerNetworkSync sends updates (20/sec)
3. **Server Broadcasts** → State changes to all clients
4. **Remote Players Update** → RemotePlayerNetworkSync interpolates positions

**Components:**

- **NetworkManager.cs** - Connection orchestrator
  - Singleton pattern
  - CreateRoom(skinId) - Creates new room with 4-digit code
  - JoinRoomByCode(roomCode, skinId) - Joins existing room
  - SetPlayerReady(bool) - Sends ready state to server
  - Handles game state transitions (menu → connecting → waiting → countdown → playing)

- **LocalPlayerNetworkSync.cs** - Sends local player state
  - 20 updates/second (50ms intervals)
  - Synced data: position, rotation Y, movement state, grounded, skinId

- **RemotePlayerNetworkSync.cs** - Receives remote player state
  - Position interpolation for smooth movement
  - Listens to Colyseus state changes

- **RemotePlayerController.cs** - Visual representation
  - Model and animation updates
  - No physics simulation (display only)

**Synced Data:**
```csharp
{
  x, y, z,           // Position
  rotY,              // Y-axis rotation
  movementState,     // 0-5 enum (Idle/Walk/Run/Jump/Fall/Slide)
  isGrounded,        // Ground detection
  skinId,            // 0-17 character model
  isReady            // Lobby ready state
}
```

**Room Code System:**
- 4-digit alphanumeric codes (e.g., "AB12")
- Server endpoint: `/api/find-room/{roomCode}` → returns roomId
- NetworkManager uses roomId to join via Colyseus

**Design Reference:** [multiplayer-architecture-design.md](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\multiplayer\multiplayer-architecture-design.md)

---

### 4. Character Model System
**Location:** `Scripts/Player/PlayerModelManager.cs`

**Architecture:** Model Switching System

**Features:**
- **18 Character Models** (skinId 0-17)
- **GFXs Container** - Parent GameObject with 18 child models
- **SetActive() Switching** - No runtime instantiation (performance)
- **Animator Integration** - Single Animator with "state" parameter (0-5)

**PlayerModelManager.cs:**
- `SetModel(int skinId)` - Activates specific model, deactivates others
- `UpdateAnimation(int movementState)` - Sets Animator parameter
- `GetAvailableModelCount()` - Returns total model count
- Cached Animator reference for performance

**Network Integration:**
- LocalPlayerNetworkSync sends skinId on join (random or selected)
- RemotePlayerNetworkSync receives and displays skinId
- Skin selection UI during gameplay (GameUIManager)

**Status:** ✅ Code complete, requires Unity editor setup (assign GFXs container to prefabs)

**Design Reference:** [player-models-design.md](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\player-models\player-models-design.md)

---

### 5. UI & Game State System
**Location:** `Scripts/UI/`

**Architecture:** State-Driven UI Manager

**Game States:**
```
MENU → CONNECTING → WAITING → COUNTDOWN → PLAYING
```

**Components:**

- **GameUIManager.cs** - State orchestrator (Singleton)
  - Manages state transitions
  - Controls cursor lock/unlock
  - Handles skin selection UI
  - Coordinates MenuUI and LobbyUI
  - Methods: SetState(), OnCreateRoomClicked(), OnJoinRoomClicked(), OnReadyButtonClicked()

- **MenuUI.cs** - Main menu screen
  - Create Room button
  - Join Room input + button
  - Skin selection (left/right arrows + select)

- **LobbyUI.cs** - Waiting & countdown screen
  - Room code display
  - Player count (e.g., "2/4 players")
  - Ready button
  - Countdown display (3, 2, 1, GO!)

- **ClickToResumeOverlay.cs** - Cursor unlock helper
  - Shows when cursor is unlocked during gameplay
  - "Click to resume" message

**State Behaviors:**
| State | Menu | Lobby | Cursor | Movement |
|-------|------|-------|--------|----------|
| Menu | Visible | Hidden | Unlocked | Disabled |
| Connecting | Hidden | Hidden | Unlocked | Disabled |
| Waiting | Hidden | Visible (waiting) | Unlocked | Disabled |
| Countdown | Hidden | Visible (countdown) | Unlocked | Disabled |
| Playing | Hidden | Hidden | Locked | Enabled |

**Lobby Flow:**
1. Player creates/joins room → CONNECTING
2. Connection success → WAITING
3. Server detects min players (2) → COUNTDOWN (3 seconds)
4. Countdown ends → PLAYING (movement enabled)

**Design Reference:** [lobby-ui-design.md](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\design\parkour-prototype\lobby-ui\lobby-ui-design.md)

---

## 🔄 Data Flow Diagrams

### Player Movement Flow
```
User Input (WASD/Space/Shift/C)
  ↓
PlayerInputHandler.Update()
  ↓
PlayerStateMachine.Update()
  ↓
[CurrentState].Update() → Calculate movement
  ↓
PlayerController.Move() / ApplyVelocity()
  ↓
CharacterController.Move()
  ↓
LocalPlayerNetworkSync.Update() (50ms intervals)
  ↓
room.Send("playerUpdate", { x, y, z, rotY, state, ... })
  ↓
Colyseus Server broadcasts to all clients
  ↓
RemotePlayerNetworkSync receives state change
  ↓
RemotePlayerController updates visual position
```

### Network Connection Flow
```
GameUIManager.OnCreateRoomClicked(skinId)
  ↓
NetworkManager.CreateRoom(skinId)
  ↓
ColyseusClient.Create<ParkourRoomState>("parkour", { skinId })
  ↓
SetupRoomHandlers() - Listen to state changes
  ↓
SpawnLocalPlayer() - Instantiate LocalPlayer prefab
  ↓
LocalPlayerNetworkSync.Initialize(room)
  ↓
NetworkManager listens to gameState changes
  ↓
Server changes gameState: "waiting" → "countdown" → "playing"
  ↓
NetworkManager.HandleGameStateChange()
  ↓
GameUIManager.SetState(GameState.Playing)
  ↓
PlayerController.MovementEnabled = true
```

---

## 🎯 Key Design Decisions

### Why CharacterController over Rigidbody?
✅ Built-in collision detection
✅ No physics overhead
✅ Predictable movement
✅ Better network synchronization
✅ Easier to control (no forces/velocities)

### Why State Machine Pattern?
✅ Clean separation of logic (each state isolated)
✅ Easy to add new states
✅ Clear state transitions
✅ Better debugging (current state visible)
✅ Easier animation integration

### Why Client-Authoritative Networking?
✅ Prototype-friendly (no server physics simulation)
✅ Simpler implementation
✅ Lower latency for local player (instant response)
⚠️ Not production-ready (cheating possible)
🔜 Will need server validation in future

### Why Colyseus?
✅ Built specifically for multiplayer games
✅ Strong TypeScript typing
✅ State synchronization out-of-box
✅ Delta encoding (efficient bandwidth)
✅ Easy Unity integration
✅ WebSocket support (WebGL builds)

---

## 📊 Implementation Status

### ✅ Completed Features
- [x] Player state machine (6 states: Idle/Walk/Run/Jump/Fall/Slide)
- [x] Custom physics system (gravity, jump calculation)
- [x] Input handling (WASD, Space, Shift, C)
- [x] Camera controller (Cinemachine OrbitalFollow)
- [x] Network connection (Colyseus WebSocket)
- [x] Local player sync (20 updates/sec)
- [x] Remote player sync (interpolation)
- [x] Character model system (18 skins, code complete)
- [x] Lobby UI system (menu, waiting, countdown, ready button)
- [x] Room code system (4-digit codes, create/join)
- [x] Game state management (5 states)
- [x] Movement freeze/enable system

### ⏳ In Progress / Needs Unity Setup
- [ ] Character model Unity setup (assign GFXs container to prefabs)
- [ ] Animator controller verification (18 models)
- [ ] Joystick integration (mobile controls) - noted in joystick.md

### 🔮 Planned (Future Enhancements)
- [ ] Wallrun mechanics
- [ ] Vault mechanics
- [ ] Checkpoint system
- [ ] Race timer & finish line detection
- [ ] Leaderboard
- [ ] Respawn system
- [ ] Server-side validation (anti-cheat)
- [ ] WebGL build optimization

---

## 📝 Documentation Architecture

The project has extensive documentation organized hierarchically:

### Design Documentation
**Location:** `_Notes/design/parkour-prototype/`

**Structure:**
- `component-overview.md` - High-level system overview
- `player-controller/` - Player system design + implementation plan
- `camera-controller/` - Camera system design + Cinemachine setup guide
- `multiplayer/` - Network architecture + Unity setup instructions
- `player-models/` - Character model system + implementation status
- `lobby-ui/` - Lobby UI design + implementation plan + skin selection
- `room-code-system/` - Room code architecture + implementation complete

### Research Documentation
**Location:** `_Notes/research/`

**Structure:**
- `colyseus/` - Colyseus framework research (overview, Unity integration, state sync, server architecture)
- `codebase-survey/` - Project analysis notes
- `webgl-build-optimization/` - Build size optimization guide

### Development Logs
**Location:** `_Notes/logs/`

**Files:**
- `player1.md`, `player2.md` - Testing logs
- `logs.md` - General development log

---

## 🔧 Development Environment

### Unity Settings
- **Working Directory:** `D:\_UNITY\parkour legion demo\Assets\_0_Custom`
- **Git Repo:** Yes (main branch)
- **Platform:** Windows (win32)

### Input Configuration
```
Movement: WASD
Run: Left Shift (hold)
Jump: Space
Slide: C or Left Ctrl
Camera: Mouse (locked during gameplay)
Unlock Cursor: ESC
```

### Git Status (as of 2025-11-19)
**Branch:** main
**Modified Files:**
- Scenes/main.unity
- UserSettings/Layouts/default-6000.dwlt

**Untracked:**
- Joystick Pack/ (mobile joystick asset)
- _Notes/joystick.md

**Recent Commits:**
```
84d288a - safe commit
0c13a7b - birp optimization
5d1a536 - size fixes
0a194cc - ui fixes v2
d25c182 - ui fixes
```

---

## 🎨 Code Standards & Patterns

### Unity Script Organization
- **✅ All scripts inside `Scripts/` directory**
- **✅ Subdirectories by domain:** Player/, Camera/, Networking/, UI/, Schema/
- **✅ State pattern for player movement**
- **✅ Singleton pattern for managers** (NetworkManager, GameUIManager)
- **✅ Namespace organization:** ParkourLegion.Player, ParkourLegion.Networking, ParkourLegion.UI

### Code Comment Policy (from CLAUDE.md)
- **⚠️ ONLY comment functions/classes/properties** - Describe WHAT they do
- **🚫 NEVER comment inside function bodies** - No line-by-line explanations
- **✅ Clean code should be self-explanatory** - Use clear naming

### Development Principles (from CLAUDE.md)
- Pre-production assumption (no backward compatibility needed)
- No migrations by default
- Clean implementation first
- Path of least action
- Never use exceptions for business logic
- Cross-check claims with evidence
- Production-ready code every time
- Planning → Implementation workflow (Design Mode → document → confirm → code)

---

## 🚀 Recommended Next Steps

### Immediate (Current Sprint)
1. **Character Model Unity Setup** (15-30 min)
   - Add PlayerModelManager component to LocalPlayer/RemotePlayer prefabs
   - Assign GFXs container references
   - Verify Animator controllers on 18 models

2. **Joystick Integration** (mobile controls)
   - Review joystick.md notes
   - Integrate Joystick Pack asset
   - Update PlayerInputHandler to support touch input

### Short-Term (Next Sprint)
3. **Full Multiplayer Flow Testing**
   - Test room creation with 2-4 players
   - Test room code joining
   - Test ready system and countdown
   - Test skin synchronization
   - Edge case testing (disconnects, etc.)

### Medium-Term (Future)
4. **Game Logic Systems**
   - Checkpoint system (trigger zones, progress tracking)
   - Race timer (start on countdown end, stop on finish)
   - Finish line detection
   - Leaderboard (finish order, race times)

5. **Advanced Parkour Mechanics**
   - Wallrun state and physics
   - Vault state and triggers
   - Ledge climbing

6. **Polish & Optimization**
   - Animation refinement
   - Visual effects (particles, trails)
   - Sound effects (footsteps, jumps, slides)
   - Performance optimization
   - WebGL build size reduction

---

## 🔍 Key Files Reference

### Core Player Scripts
- **PlayerController.cs** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Player\PlayerController.cs:1`
- **PlayerStateMachine.cs** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Player\PlayerStateMachine.cs:1`
- **PlayerState.cs** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Player\States\PlayerState.cs:1`
- **PlayerModelManager.cs** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Player\PlayerModelManager.cs:1`

### Core Networking Scripts
- **NetworkManager.cs** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Networking\NetworkManager.cs:1`
- **LocalPlayerNetworkSync.cs** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Networking\LocalPlayerNetworkSync.cs:1`
- **RemotePlayerNetworkSync.cs** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Networking\RemotePlayerNetworkSync.cs:1`

### Core UI Scripts
- **GameUIManager.cs** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\UI\GameUIManager.cs:1`
- **MenuUI.cs** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\UI\MenuUI.cs:1`
- **LobbyUI.cs** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\UI\LobbyUI.cs:1`

### Documentation
- **Project Overview** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\project-overview.md:1`
- **CLAUDE.md** - `D:\_UNITY\parkour legion demo\Assets\_0_Custom\CLAUDE.md:1` (AI assistant instructions)

---

## 📚 Related Documentation

### External Resources
- [Colyseus Official Docs](https://docs.colyseus.io/)
- [Cinemachine 3.x Docs](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.0/manual/index.html)
- [Unity CharacterController API](https://docs.unity3d.com/ScriptReference/CharacterController.html)

### Internal Research
- [Colyseus Research Overview](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\research\colyseus\README.md)
- [WebGL Build Optimization](D:\_UNITY\parkour legion demo\Assets\_0_Custom\_Notes\research\webgl-build-optimization\build-size-optimization-guide.md)

---

**Document Version:** 1.0
**Survey Conducted By:** Cody (Research Mode)
**Last Updated:** 2025-11-19
**Confidence Level:** High (comprehensive code and documentation review)
