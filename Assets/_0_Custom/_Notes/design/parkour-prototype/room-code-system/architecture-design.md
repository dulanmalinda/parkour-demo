# Room Code System - Architecture Design

**Date:** 2025-11-15
**Status:** Design Complete - Pending Implementation
**Feature:** Room Code Join/Create System with Ready States

---

## 📋 Requirements Summary

### Core Features
1. **Room Code System**
   - Players can create a room (generates unique room code)
   - Players can join room via room code input
   - Room code displayed in lobby (top-left corner: "Room Code: XXXX")
   - Max 4 players per room (reduced from 8)

2. **Ready State System**
   - Each player has "Ready" button in lobby
   - Countdown only starts when ALL players are ready
   - Minimum 2 players required to start

3. **UI Changes**
   - Menu: Room code input field + "Join Room" button (disabled when empty) + "Create Room" button
   - Lobby: Room code display + Ready button + player ready status

---

## 🏗️ Current Implementation Analysis

### Client-Side (Unity)
**MenuUI.cs** (`Scripts/UI/MenuUI.cs:1`)
- Programmatically creates single "PLAY" button
- Calls `GameUIManager.Instance.OnPlayButtonClicked()`
- Simple structure, needs expansion for room code UI

**LobbyUI.cs** (`Scripts/UI/LobbyUI.cs:1`)
- Shows center status text ("Waiting for players...")
- Shows countdown when game starts
- No room code display or ready buttons

**NetworkManager.cs** (`Scripts/Networking/NetworkManager.cs:49`)
- Uses `client.JoinOrCreate<ParkourRoomState>(roomName, options)` on line 77
- Single room name: "parkour"
- Auto-joins on Play button click
- No room code logic

**GameUIManager.cs** (`Scripts/UI/GameUIManager.cs:1`)
- Manages state transitions: Menu → Connecting → Waiting → Countdown → Playing
- `OnPlayButtonClicked()` triggers `NetworkManager.ConnectAndJoin()`

### Server-Side (Node.js)
**ParkourRoom.ts** (`src/rooms/ParkourRoom.ts:1`)
- `maxClients = 8` (line 6)
- `MIN_PLAYERS = 2` (line 7)
- Auto-starts countdown when ≥2 players join (line 102)
- No ready state logic
- No room code generation

**ParkourRoomState.ts** (`src/schema/ParkourRoomState.ts:1`)
- No `roomCode` field
- No ready state tracking

**PlayerState.ts** (`src/schema/PlayerState.ts:1`)
- No `isReady` field

**index.ts** (`src/index.ts:22`)
- Single room definition: `gameServer.define("parkour", ParkourRoom)`
- No private room logic

---

## 🎯 Proposed Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        MENU UI                              │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  Room Code: [____________]  [Join Room (disabled)]   │  │
│  │                                                        │  │
│  │                  [Create Room]                        │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓
                   (Join/Create via Colyseus)
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                       LOBBY UI                              │
│  ┌────────────────────┐                                     │
│  │ Room Code: AB3X    │ (top-left corner)                   │
│  └────────────────────┘                                     │
│                                                              │
│         Players: 2/4                                        │
│         - Player1 ✓ (Ready)                                 │
│         - Player2 ⏳ (Not Ready)                             │
│                                                              │
│                  [✓ Ready]                                  │
│         (Waiting for all players to be ready...)           │
└─────────────────────────────────────────────────────────────┘
                            ↓
                  (All players ready)
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                     COUNTDOWN UI                            │
│                 Game starts in 3...                         │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔄 State Machine Changes

### Current Flow
```
Menu → (Click PLAY) → Connecting → Waiting → (2+ players) → Countdown → Playing
```

### New Flow
```
Menu → (Create/Join Room) → Connecting → Waiting → (All Ready) → Countdown → Playing
```

**Key Changes:**
- Menu: User chooses Create or Join
- Waiting: Players click Ready button
- Countdown: Only starts when ALL players ready (min 2 players)

---

## 📊 Data Schema Changes

### Server Schema Updates

**ParkourRoomState.ts**
```typescript
export class ParkourRoomState extends Schema {
    // ... existing fields ...

    @type("string") roomCode: string = "";  // NEW: 4-character room code
}
```

**PlayerState.ts**
```typescript
export class PlayerState extends Schema {
    // ... existing fields ...

    @type("boolean") isReady: boolean = false;  // NEW: Ready state
}
```

### Unity Schema Updates (Mirror Server)

**ParkourRoomState.cs**
```csharp
[Colyseus.Schema.Type(6, "string")]
public string roomCode = "";
```

**PlayerState.cs**
```csharp
[Colyseus.Schema.Type(10, "boolean")]
public bool isReady = false;
```

---

## 🔧 Implementation Details

### 1. Room Code Generation (Server)

**Location:** `src/rooms/ParkourRoom.ts`

**Algorithm:**
```typescript
private generateRoomCode(): string {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let code = '';
    for (let i = 0; i < 4; i++) {
        code += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return code;
}
```

**Room Creation:**
- Server generates room code in `onCreate()`
- Store in `this.state.roomCode`
- Use `filterBy` option for private rooms

### 2. Colyseus Room Filtering

**Location:** `src/index.ts`

**Change from:**
```typescript
gameServer.define("parkour", ParkourRoom);
```

**To:**
```typescript
gameServer.define("parkour", ParkourRoom)
    .filterBy(['roomCode']);
```

This allows creating multiple private rooms with unique room codes.

### 3. Client Connection Changes

**Location:** `Scripts/Networking/NetworkManager.cs`

**Current:**
```csharp
room = await client.JoinOrCreate<ParkourRoomState>(roomName, options);
```

**Create Room:**
```csharp
public async void CreateRoom(int skinId) {
    var options = new Dictionary<string, object> {
        { "skinId", skinId },
        { "createRoom", true }
    };
    room = await client.Create<ParkourRoomState>("parkour", options);
}
```

**Join Room:**
```csharp
public async void JoinRoom(string roomCode, int skinId) {
    var options = new Dictionary<string, object> {
        { "skinId", skinId },
        { "roomCode", roomCode }
    };
    room = await client.JoinByRoomId<ParkourRoomState>(roomId, options);
}
```

**Note:** We need to implement room code → room ID lookup via Colyseus matchmaking API.

### 4. Ready State System

**Server Logic (`ParkourRoom.ts`):**

```typescript
onMessage("playerReady", (client, message) => {
    const player = this.state.players.get(client.sessionId);
    if (player) {
        player.isReady = message.isReady;
        console.log(`Player ${client.sessionId} ready: ${player.isReady}`);
        this.checkReadyState();
    }
});

private checkReadyState() {
    const playerCount = this.state.players.size;
    if (playerCount < this.MIN_PLAYERS) return;

    const allReady = Array.from(this.state.players.values())
        .every(p => p.isReady);

    if (allReady && this.state.gameState === "waiting") {
        this.startCountdown();
    } else if (!allReady && this.state.gameState === "countdown") {
        this.cancelCountdown();
    }
}
```

**Remove Auto-Start:**
- Delete `checkGameStart()` from `onJoin()` (line 80)
- Countdown only triggered by ready state

**Client Logic:**
```csharp
public void SetPlayerReady(bool ready) {
    room.Send("playerReady", new { isReady = ready });
}
```

### 5. UI Component Updates

#### MenuUI.cs Changes

**New Elements:**
- `TMP_InputField roomCodeInput`
- `Button joinRoomButton` (disabled by default)
- `Button createRoomButton`

**Input Validation:**
- Enable `joinRoomButton` when `roomCodeInput.text.Length == 4`
- Convert input to uppercase
- Alphanumeric validation

**Button Callbacks:**
- `OnJoinRoomClicked()` → `GameUIManager.JoinRoom(roomCode)`
- `OnCreateRoomClicked()` → `GameUIManager.CreateRoom()`

#### LobbyUI.cs Changes

**New Elements:**
- `TextMeshProUGUI roomCodeText` (top-left corner)
- `TextMeshProUGUI playerListText` (show ready states)
- `Button readyButton` (toggleable)

**Display Logic:**
- Show room code from `room.State.roomCode`
- List all players with ready indicators (✓/⏳)
- Toggle ready button visual state
- Update status text based on ready count

#### GameUIManager.cs Changes

**New Methods:**
```csharp
public void CreateRoom() {
    int skinId = GetCurrentSkinId();
    NetworkManager.Instance.CreateRoom(skinId);
}

public void JoinRoom(string roomCode) {
    int skinId = GetCurrentSkinId();
    NetworkManager.Instance.JoinRoom(roomCode, skinId);
}

public void SetReady(bool ready) {
    NetworkManager.Instance.SetPlayerReady(ready);
}
```

### 6. Max Players Change

**Server (`ParkourRoom.ts:6`):**
```typescript
maxClients = 4;  // Changed from 8
```

---

## 🔍 Room Code Discovery Approach

### Challenge
Colyseus `filterBy` creates metadata filters, but we need room ID for `JoinByRoomId()`.

### Solution: Matchmaking API

**Server Endpoint:**
```typescript
// src/index.ts
app.get("/find-room/:code", async (req, res) => {
    const roomCode = req.params.code.toUpperCase();
    const rooms = await gameServer.matchMaker.query({
        name: "parkour",
        roomCode: roomCode
    });

    if (rooms.length > 0) {
        res.json({ roomId: rooms[0].roomId });
    } else {
        res.status(404).json({ error: "Room not found" });
    }
});
```

**Client Request:**
```csharp
private async Task<string> GetRoomIdByCode(string roomCode) {
    using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/find-room/{roomCode}")) {
        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success) {
            var response = JsonUtility.FromJson<RoomIdResponse>(request.downloadHandler.text);
            return response.roomId;
        }
    }
    return null;
}
```

**Alternative: Use `metadata` in room options**
```typescript
// onCreate
this.setMetadata({ roomCode: this.state.roomCode });
```

Then query via:
```csharp
var availableRooms = await client.GetAvailableRooms("parkour");
var targetRoom = availableRooms.FirstOrDefault(r => r.metadata["roomCode"] == roomCode);
```

---

## 📝 Implementation Checklist

### Backend (Server)
- [ ] Update `PlayerState.ts` - add `isReady` field
- [ ] Update `ParkourRoomState.ts` - add `roomCode` field
- [ ] Update `ParkourRoom.ts`:
  - [ ] Change `maxClients` to 4
  - [ ] Add room code generation in `onCreate()`
  - [ ] Add `playerReady` message handler
  - [ ] Add `checkReadyState()` logic
  - [ ] Remove auto-start from `onJoin()`
  - [ ] Set room metadata with `roomCode`
- [ ] Update `index.ts`:
  - [ ] Add `.filterBy(['roomCode'])`
  - [ ] Add `/find-room/:code` endpoint (optional)
- [ ] Run schema codegen to update Unity schemas

### Frontend (Unity)
- [ ] Update `Scripts/Schema/PlayerState.cs` - add `isReady` field
- [ ] Update `Scripts/Schema/ParkourRoomState.cs` - add `roomCode` field
- [ ] Update `NetworkManager.cs`:
  - [ ] Add `CreateRoom(int skinId)` method
  - [ ] Add `JoinRoom(string roomCode, int skinId)` method
  - [ ] Add `SetPlayerReady(bool ready)` method
  - [ ] Add room code lookup logic
- [ ] Update `MenuUI.cs`:
  - [ ] Add room code input field
  - [ ] Add Join Room button (with validation)
  - [ ] Add Create Room button
  - [ ] Wire up callbacks
- [ ] Update `LobbyUI.cs`:
  - [ ] Add room code display (top-left)
  - [ ] Add player list with ready states
  - [ ] Add Ready button (toggleable)
  - [ ] Update status text for ready logic
- [ ] Update `GameUIManager.cs`:
  - [ ] Add `CreateRoom()` method
  - [ ] Add `JoinRoom(string code)` method
  - [ ] Add `SetReady(bool ready)` method

### Testing
- [ ] Test room creation (code generation)
- [ ] Test room joining via code
- [ ] Test invalid room codes (error handling)
- [ ] Test ready state sync (2+ players)
- [ ] Test countdown with all ready
- [ ] Test countdown cancel when player unreadies
- [ ] Test player disconnect during ready state
- [ ] Test max 4 players per room

---

## 🎨 UI Layout Specifications

### MenuUI Layout
```
┌─────────────────────────────────────────┐
│                                         │
│                                         │
│         PARKOUR LEGION                  │
│                                         │
│    ┌────────────────┐  ┌──────────┐    │
│    │ Room Code: □□□□ │  │ JOIN ROOM│    │
│    └────────────────┘  └──────────┘    │
│                                         │
│           ┌──────────────┐              │
│           │ CREATE ROOM  │              │
│           └──────────────┘              │
│                                         │
└─────────────────────────────────────────┘
```

### LobbyUI Layout
```
┌─────────────────────────────────────────┐
│ Room Code: AB3X                         │
│                                         │
│         WAITING FOR PLAYERS             │
│                                         │
│         Players: 2/4                    │
│         • Player1 ✓                     │
│         • Player2 ⏳                     │
│                                         │
│           ┌──────────────┐              │
│           │   ✓ READY    │              │
│           └──────────────┘              │
│                                         │
│    Waiting for all players to be ready │
└─────────────────────────────────────────┘
```

---

## ⚠️ Edge Cases & Error Handling

### Room Code Validation
- **Invalid length:** Show error "Room code must be 4 characters"
- **Room not found:** Show error "Room not found or full"
- **Room full:** Show error "Room is full (4/4 players)"

### Ready State Edge Cases
- **Player disconnects while ready:** Remove from ready count, recalculate
- **Countdown starts, player leaves:** Cancel countdown if < 2 players
- **Player joins during countdown:** Reset to waiting state
- **Player unreadies during countdown:** Cancel countdown immediately

### Network Errors
- **Connection timeout:** Show "Failed to connect to server"
- **Room creation failed:** Show "Failed to create room, try again"
- **Join failed:** Show "Failed to join room: [reason]"

---

## 🚀 Deployment Notes

### Database/Persistence
- Room codes are ephemeral (exist only while room active)
- No database storage needed for this prototype
- Room destroyed when all players leave

### Server Scalability
- Each room is isolated instance
- Room codes must be unique across server
- Low collision probability with 36^4 = 1,679,616 combinations

### Future Enhancements
- [ ] Room passwords (optional)
- [ ] Room host controls (kick player, start override)
- [ ] Room browser (show available public rooms)
- [ ] Friend invite system
- [ ] Persistent room stats

---

**Design Version:** 1.0
**Last Updated:** 2025-11-15
**Next Step:** Implementation Planning
