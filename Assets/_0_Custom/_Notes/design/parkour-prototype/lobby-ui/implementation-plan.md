# Lobby UI Implementation Workplan

**Date:** 2025-11-15
**Status:** Ready for Implementation
**Estimated Time:** 4-6 hours
**Design Reference:** [lobby-ui-design.md](./lobby-ui-design.md)

---

## 📋 **Implementation Overview**

This workplan implements a lobby system with the following flow:
```
MENU (Play button) → CONNECTING → WAITING (frozen) → COUNTDOWN (2+ players) → PLAYING (movement enabled, cursor locked)
```

---

## ✅ **Phase 1: Server-Side Schema & Logic**

### **1.1 Update ParkourRoomState.ts Schema**
**File:** `D:\_UNITY\parkour-server\src\schema\ParkourRoomState.ts`

**Current State:**
```typescript
export class ParkourRoomState extends Schema {
    @type({ map: PlayerState }) players = new MapSchema<PlayerState>();
    @type("float32") raceStartTime: number = 0;
    @type("boolean") raceStarted: boolean = false;
}
```

**Required Changes:**
- [x] Add `@type("string") gameState: string = "waiting"`
- [x] Add `@type("uint8") countdownValue: number = 0`
- [x] Add `@type("uint8") playerCount: number = 0`

**Expected Result:**
```typescript
export class ParkourRoomState extends Schema {
    @type({ map: PlayerState }) players = new MapSchema<PlayerState>();
    @type("float32") raceStartTime: number = 0;
    @type("boolean") raceStarted: boolean = false;
    @type("string") gameState: string = "waiting";
    @type("uint8") countdownValue: number = 0;
    @type("uint8") playerCount: number = 0;
}
```

---

### **1.2 Update ParkourRoom.ts Logic**
**File:** `D:\_UNITY\parkour-server\src\rooms\ParkourRoom.ts`

**Current Issues:**
- No game state management
- No countdown logic
- No min-player check

**Required Changes:**

#### **Add Constants:**
```typescript
export class ParkourRoom extends Room<ParkourRoomState> {
    maxClients = 8;
    private readonly MIN_PLAYERS = 2;
    private readonly COUNTDOWN_SECONDS = 3;
    private countdownInterval?: NodeJS.Timeout;
    // ... existing code
}
```

#### **Modify onCreate():**
```typescript
onCreate(options: any) {
    this.setState(new ParkourRoomState());
    this.state.gameState = "waiting"; // ADD THIS
    this.setPatchRate(33);
    // ... rest of existing onCreate
}
```

#### **Modify onJoin():**
```typescript
onJoin(client: Client, options: any) {
    console.log(client.sessionId, "joined!");

    const player = new PlayerState();
    player.id = client.sessionId;
    player.name = options.playerName || "Player";
    player.skinId = options.skinId || 0;

    const spawnPoint = this.spawnPoints[this.nextSpawnIndex % this.spawnPoints.length];
    this.nextSpawnIndex++;

    player.x = spawnPoint.x;
    player.y = spawnPoint.y;
    player.z = spawnPoint.z;

    console.log(`Player spawned at (${player.x}, ${player.y}, ${player.z}), skinId: ${player.skinId}`);

    this.state.players.set(client.sessionId, player);

    // ADD THIS:
    this.state.playerCount = this.state.players.size;
    this.checkGameStart();
}
```

#### **Modify onLeave():**
```typescript
onLeave(client: Client, consented: boolean) {
    console.log(client.sessionId, "left!");
    this.state.players.delete(client.sessionId);

    // ADD THIS:
    this.state.playerCount = this.state.players.size;

    if (this.state.gameState === "countdown" && this.state.playerCount < this.MIN_PLAYERS) {
        this.cancelCountdown();
    }
}
```

#### **Add New Methods:**
```typescript
private checkGameStart() {
    if (this.state.playerCount >= this.MIN_PLAYERS && this.state.gameState === "waiting") {
        this.startCountdown();
    }
}

private startCountdown() {
    this.state.gameState = "countdown";
    this.state.countdownValue = this.COUNTDOWN_SECONDS;
    console.log(`Countdown started: ${this.state.countdownValue} seconds`);

    this.countdownInterval = setInterval(() => {
        this.state.countdownValue--;
        console.log(`Countdown: ${this.state.countdownValue}`);

        if (this.state.countdownValue <= 0) {
            this.startGame();
        }
    }, 1000);
}

private cancelCountdown() {
    if (this.countdownInterval) {
        clearInterval(this.countdownInterval);
        this.countdownInterval = undefined;
    }
    this.state.gameState = "waiting";
    this.state.countdownValue = 0;
    console.log("Countdown cancelled - not enough players");
}

private startGame() {
    if (this.countdownInterval) {
        clearInterval(this.countdownInterval);
        this.countdownInterval = undefined;
    }

    this.state.gameState = "playing";
    this.state.countdownValue = 0;
    this.state.raceStarted = true;
    this.state.raceStartTime = Date.now();

    console.log("Game started!");
}
```

#### **Modify onDispose():**
```typescript
onDispose() {
    if (this.countdownInterval) {
        clearInterval(this.countdownInterval);
    }
    console.log("ParkourRoom disposed");
}
```

**Validation:**
- [x] Server compiles without errors (`npm run build`)
- [x] Test with console logs (2 players join → countdown → game starts)

---

### **1.3 Test Server Changes**
**Commands:**
```bash
cd "D:\_UNITY\parkour-server"
npm run build
npm run dev
```

**Test Checklist:**
- [x] Server starts without errors
- [x] First client joins → gameState = "waiting"
- [x] Second client joins → gameState = "countdown", countdownValue = 3
- [x] Countdown decrements → 3, 2, 1, 0
- [x] After countdown → gameState = "playing"
- [x] Player leaves during countdown → countdown cancels

---

## ✅ **Phase 2: Unity Schema Update**

### **2.1 Update ParkourRoomState.cs**
**File:** `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\Schema\ParkourRoomState.cs`

**Current State:**
```csharp
public class ParkourRoomState : Colyseus.Schema.Schema
{
    [Colyseus.Schema.Type(0, "map", typeof(Colyseus.Schema.MapSchema<PlayerState>))]
    public Colyseus.Schema.MapSchema<PlayerState> players = new Colyseus.Schema.MapSchema<PlayerState>();

    [Colyseus.Schema.Type(1, "float32")]
    public float raceStartTime = 0;

    [Colyseus.Schema.Type(2, "boolean")]
    public bool raceStarted = false;
}
```

**Required Changes:**
- [x] Add `[Colyseus.Schema.Type(3, "string")] public string gameState = "waiting"`
- [x] Add `[Colyseus.Schema.Type(4, "uint8")] public byte countdownValue = 0`
- [x] Add `[Colyseus.Schema.Type(5, "uint8")] public byte playerCount = 0`

**Expected Result:**
```csharp
public class ParkourRoomState : Colyseus.Schema.Schema
{
    [Colyseus.Schema.Type(0, "map", typeof(Colyseus.Schema.MapSchema<PlayerState>))]
    public Colyseus.Schema.MapSchema<PlayerState> players = new Colyseus.Schema.MapSchema<PlayerState>();

    [Colyseus.Schema.Type(1, "float32")]
    public float raceStartTime = 0;

    [Colyseus.Schema.Type(2, "boolean")]
    public bool raceStarted = false;

    [Colyseus.Schema.Type(3, "string")]
    public string gameState = "waiting";

    [Colyseus.Schema.Type(4, "uint8")]
    public byte countdownValue = 0;

    [Colyseus.Schema.Type(5, "uint8")]
    public byte playerCount = 0;
}
```

**Validation:**
- [x] Unity compiles without errors
- [x] No namespace conflicts

---

## ✅ **Phase 3: UI Scripts Creation**

### **3.1 Create Scripts/UI Directory**
**Path:** `D:\_UNITY\parkour legion demo\Assets\_0_Custom\Scripts\UI\`

**Action:**
- [x] Create `UI/` folder inside `Scripts/`

---

### **3.2 Create GameUIManager.cs**
**File:** `Scripts/UI/GameUIManager.cs`

**Purpose:** Central state manager for all UI transitions

**Key Features:**
- Singleton pattern
- GameState enum (Menu, Connecting, Waiting, Countdown, Playing)
- Manages MenuUI and LobbyUI visibility
- Public SetState() method for NetworkManager
- Handles cursor lock/unlock via CameraInputProvider

**Methods:**
- `void Awake()` - Singleton setup
- `void Start()` - Initialize to Menu state, unlock cursor
- `public void SetState(GameState newState)` - State transition handler
- `public void OnPlayButtonClicked()` - Trigger NetworkManager connection
- `private void ShowMenuUI()` - Show menu, hide lobby
- `private void ShowLobbyUI()` - Show lobby, hide menu
- `private void HideAllUI()` - Hide all UI panels
- `private void SetCursorState(bool locked)` - Manage cursor via CameraInputProvider

**References Needed:**
- MenuUI component
- LobbyUI component
- NetworkManager instance
- CameraInputProvider instance

---

### **3.3 Create MenuUI.cs**
**File:** `Scripts/UI/MenuUI.cs`

**Purpose:** Manages the main menu Play button

**Key Features:**
- Programmatically creates Canvas + Button in Awake()
- Button centered, styled (200x60, green background)
- OnClick → calls GameUIManager.OnPlayButtonClicked()

**UI Structure Created:**
```
MenuCanvas (Screen Space - Overlay)
└── Panel (semi-transparent dark background)
    └── PlayButton (centered)
        └── Text ("PLAY")
```

**Methods:**
- `void Awake()` - Create UI programmatically
- `public void Show()` - SetActive(true) on panel
- `public void Hide()` - SetActive(false) on panel
- `private void OnPlayButtonClicked()` - Forward to GameUIManager

---

### **3.4 Create LobbyUI.cs**
**File:** `Scripts/UI/LobbyUI.cs`

**Purpose:** Displays waiting/countdown status

**Key Features:**
- Programmatically creates Canvas + Text in Awake()
- Text centered, white, size 36
- Updates text dynamically

**UI Structure Created:**
```
LobbyCanvas (Screen Space - Overlay)
└── StatusText (centered, large)
```

**Methods:**
- `void Awake()` - Create UI programmatically
- `public void ShowWaiting()` - Display "Waiting for players..."
- `public void ShowCountdown(int seconds)` - Display "Game starts in {seconds}..."
- `public void Hide()` - SetActive(false) on canvas

---

### **3.5 UI Creation Details**

**MenuUI Canvas Setup:**
```csharp
GameObject canvasGO = new GameObject("MenuCanvas");
Canvas canvas = canvasGO.AddComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 10;
canvasGO.AddComponent<CanvasScaler>();
canvasGO.AddComponent<GraphicRaycaster>();

GameObject panelGO = new GameObject("Panel");
panelGO.transform.SetParent(canvasGO.transform, false);
Image panelImage = panelGO.AddComponent<Image>();
panelImage.color = new Color(0, 0, 0, 0.7f); // Semi-transparent dark

GameObject buttonGO = new GameObject("PlayButton");
buttonGO.transform.SetParent(panelGO.transform, false);
// ... add Button, Image, Text components
// ... configure RectTransform (centered, 200x60)
```

**LobbyUI Canvas Setup:**
```csharp
GameObject canvasGO = new GameObject("LobbyCanvas");
Canvas canvas = canvasGO.AddComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceOverlay;
canvas.sortingOrder = 10;
canvasGO.AddComponent<CanvasScaler>();

GameObject textGO = new GameObject("StatusText");
textGO.transform.SetParent(canvasGO.transform, false);
TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
text.fontSize = 36;
text.color = Color.white;
text.alignment = TextAlignmentOptions.Center;
// ... configure RectTransform (centered, full width)
```

**Validation:**
- [x] MenuUI creates Play button successfully
- [x] LobbyUI creates status text successfully
- [x] Button is clickable
- [x] Text updates correctly

---

## ✅ **Phase 4: PlayerController Movement Freeze**

### **4.1 Modify PlayerController.cs**
**File:** `Scripts/Player/PlayerController.cs`

**Current Issue:**
- Player immediately movable on spawn

**Required Changes:**

#### **Add Property:**
```csharp
private bool movementEnabled = false;

public bool MovementEnabled
{
    get => movementEnabled;
    set => movementEnabled = value;
}
```

#### **Modify Update():**
```csharp
private void Update()
{
    if (!movementEnabled) return; // ADD THIS LINE AT THE TOP

    inputHandler.Update();
    isGrounded = physics.CheckGrounded(characterController, transform);
    stateMachine.Update();
}
```

**Expected Behavior:**
- Player spawns frozen (movementEnabled = false)
- No input processing until enabled
- State machine doesn't update
- NetworkManager enables movement when game starts

**Validation:**
- [x] Player spawns and cannot move
- [x] Setting MovementEnabled = true allows movement
- [x] No errors in console

---

## ✅ **Phase 5: NetworkManager Integration**

### **5.1 Modify NetworkManager.cs**
**File:** `Scripts/Networking/NetworkManager.cs`

**Current Issues:**
- Auto-connects in Start()
- No game state listeners
- No movement freeze on spawn

**Required Changes:**

#### **5.1.1 Remove Auto-Connect**
**Current Start():**
```csharp
private async void Start()
{
    await ConnectToServer();
}
```

**New Start():**
```csharp
private void Start()
{
    // Don't auto-connect anymore
    // Connection triggered by GameUIManager.OnPlayButtonClicked()
}
```

#### **5.1.2 Add Public Connection Method**
```csharp
public async void ConnectAndJoin()
{
    if (UI.GameUIManager.Instance != null)
    {
        UI.GameUIManager.Instance.SetState(UI.GameState.Connecting);
    }

    await ConnectToServer();

    if (UI.GameUIManager.Instance != null)
    {
        UI.GameUIManager.Instance.SetState(UI.GameState.Waiting);
    }
}
```

#### **5.1.3 Add Game State Listeners**
**Modify SetupRoomHandlers():**
```csharp
private void SetupRoomHandlers()
{
    room.OnStateChange += (state, isFirstState) =>
    {
        if (isFirstState)
        {
            UpdateLocalPlayerPositionFromState(state);
            SpawnExistingPlayersFromState(state);

            var callbacks = Colyseus.Schema.Callbacks.Get(room);

            callbacks.OnAdd(s => s.players, (key, player) =>
            {
                if (key == room.SessionId) return;
                SpawnRemotePlayer(key, player);
            });

            callbacks.OnRemove(s => s.players, (key, player) =>
            {
                RemoveRemotePlayer(key);
            });

            // ADD THIS: Listen for gameState changes
            callbacks.OnChange(s => s.gameState, (value) =>
            {
                HandleGameStateChange(value);
            });

            // ADD THIS: Listen for countdownValue changes
            callbacks.OnChange(s => s.countdownValue, (value) =>
            {
                HandleCountdownUpdate(value);
            });
        }
    };

    room.OnLeave += (code) =>
    {
        Debug.Log($"Left room with code: {code}");
    };

    room.OnError += (code, message) =>
    {
        Debug.LogError($"Room error {code}: {message}");
    };
}
```

#### **5.1.4 Add State Change Handlers**
```csharp
private void HandleGameStateChange(string newState)
{
    Debug.Log($"Game state changed to: {newState}");

    if (UI.GameUIManager.Instance == null) return;

    switch (newState)
    {
        case "waiting":
            UI.GameUIManager.Instance.SetState(UI.GameState.Waiting);
            break;
        case "countdown":
            UI.GameUIManager.Instance.SetState(UI.GameState.Countdown);
            break;
        case "playing":
            UI.GameUIManager.Instance.SetState(UI.GameState.Playing);
            EnableLocalPlayerMovement();
            break;
    }
}

private void HandleCountdownUpdate(byte countdown)
{
    Debug.Log($"Countdown: {countdown}");

    if (UI.GameUIManager.Instance != null &&
        UI.GameUIManager.Instance.CurrentState == UI.GameState.Countdown)
    {
        var lobbyUI = FindObjectOfType<UI.LobbyUI>();
        lobbyUI?.ShowCountdown(countdown);
    }
}

private void EnableLocalPlayerMovement()
{
    if (localPlayer != null)
    {
        var controller = localPlayer.GetComponent<Player.PlayerController>();
        if (controller != null)
        {
            controller.MovementEnabled = true;
            Debug.Log("Local player movement enabled");
        }
    }
}
```

**Validation:**
- [x] Start() no longer auto-connects
- [x] ConnectAndJoin() can be called externally
- [x] Game state changes trigger UI updates
- [x] Countdown updates show in UI
- [x] Player movement enabled when game starts

---

## ✅ **Phase 6: CameraInputProvider Integration**

### **6.1 Modify CameraInputProvider.cs**
**File:** `Scripts/Camera/CameraInputProvider.cs`

**Current Issues:**
- Auto-locks cursor in Start()
- No public methods for external cursor control

**Required Changes:**

#### **6.1.1 Remove Auto-Lock**
**Current Start():**
```csharp
private void Start()
{
    if (cinemachineCamera == null)
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    if (cinemachineCamera != null)
    {
        orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        inputAxisController = cinemachineCamera.GetComponent<CinemachineInputAxisController>();

        if (orbitalFollow != null)
        {
            orbitalFollow.HorizontalAxis.Value = 0f;
            orbitalFollow.VerticalAxis.Value = 0f;
        }
    }

    LockCursor(); // REMOVE THIS LINE
}
```

**New Start():**
```csharp
private void Start()
{
    if (cinemachineCamera == null)
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    if (cinemachineCamera != null)
    {
        orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
        inputAxisController = cinemachineCamera.GetComponent<CinemachineInputAxisController>();

        if (orbitalFollow != null)
        {
            orbitalFollow.HorizontalAxis.Value = 0f;
            orbitalFollow.VerticalAxis.Value = 0f;
        }
    }

    // Don't auto-lock - GameUIManager will control cursor state
}
```

#### **6.1.2 Make Lock/Unlock Public**
**Current Methods:**
```csharp
private void LockCursor() { ... }
private void UnlockCursor() { ... }
```

**New Methods:**
```csharp
public void LockCursor()
{
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}

public void UnlockCursor()
{
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
}
```

**Validation:**
- [x] Cursor starts unlocked
- [x] Public LockCursor() works
- [x] Public UnlockCursor() works
- [x] ESC toggle still functional

---

## ✅ **Phase 7: GameUIManager Implementation Details**

### **7.1 Complete GameUIManager.cs**
**File:** `Scripts/UI/GameUIManager.cs`

**Full Implementation:**

```csharp
using UnityEngine;

namespace ParkourLegion.UI
{
    public enum GameState
    {
        Menu,
        Connecting,
        Waiting,
        Countdown,
        Playing
    }

    public class GameUIManager : MonoBehaviour
    {
        public static GameUIManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private MenuUI menuUI;
        [SerializeField] private LobbyUI lobbyUI;

        private GameState currentState = GameState.Menu;

        public GameState CurrentState => currentState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SetState(GameState.Menu);
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            Debug.Log($"GameUIManager: {currentState} → {newState}");
            currentState = newState;

            switch (currentState)
            {
                case GameState.Menu:
                    ShowMenuUI();
                    SetCursorState(false);
                    break;

                case GameState.Connecting:
                    HideAllUI();
                    SetCursorState(false);
                    break;

                case GameState.Waiting:
                    ShowLobbyUI();
                    if (lobbyUI != null) lobbyUI.ShowWaiting();
                    SetCursorState(false);
                    break;

                case GameState.Countdown:
                    ShowLobbyUI();
                    SetCursorState(false);
                    break;

                case GameState.Playing:
                    HideAllUI();
                    SetCursorState(true);
                    break;
            }
        }

        public void OnPlayButtonClicked()
        {
            Debug.Log("Play button clicked - connecting to server");

            var networkManager = Networking.NetworkManager.Instance;
            if (networkManager != null)
            {
                networkManager.ConnectAndJoin();
            }
            else
            {
                Debug.LogError("NetworkManager instance not found!");
            }
        }

        private void ShowMenuUI()
        {
            if (menuUI != null) menuUI.Show();
            if (lobbyUI != null) lobbyUI.Hide();
        }

        private void ShowLobbyUI()
        {
            if (menuUI != null) menuUI.Hide();
            if (lobbyUI != null) lobbyUI.gameObject.SetActive(true);
        }

        private void HideAllUI()
        {
            if (menuUI != null) menuUI.Hide();
            if (lobbyUI != null) lobbyUI.Hide();
        }

        private void SetCursorState(bool locked)
        {
            var cameraInput = FindObjectOfType<Camera.CameraInputProvider>();
            if (cameraInput != null)
            {
                if (locked)
                {
                    cameraInput.LockCursor();
                }
                else
                {
                    cameraInput.UnlockCursor();
                }
            }
        }
    }
}
```

**Validation:**
- [x] Singleton pattern works
- [x] State transitions log correctly
- [x] UI shows/hides appropriately
- [x] Cursor locks/unlocks correctly
- [x] Play button triggers connection

---

## ✅ **Phase 8: Full Integration Testing**

### **8.1 Single Player Test**
**Steps:**
1. Start Colyseus server (`npm run dev`)
2. Start Unity Play mode
3. Verify Menu shows with Play button
4. Verify cursor is unlocked
5. Click Play button
6. Verify "Waiting for players..." shows
7. Verify player spawns but is frozen
8. Verify cursor still unlocked

**Expected Behavior:**
- ✅ Menu → Waiting transition works
- ✅ Player frozen in waiting state
- ✅ Cursor unlocked

---

### **8.2 Multi-Player Test (Countdown)**
**Steps:**
1. Keep first client running
2. Start second client (ParrelSync or build)
3. Click Play on second client
4. Verify countdown starts: "Game starts in 3..."
5. Verify countdown decrements: 3 → 2 → 1
6. Verify game starts after 0
7. Verify UI disappears
8. Verify both players can move
9. Verify cursors lock

**Expected Behavior:**
- ✅ Countdown triggers with 2+ players
- ✅ Countdown counts down correctly
- ✅ Game starts after countdown
- ✅ Players become movable
- ✅ UI hides
- ✅ Cursors lock

---

### **8.3 Edge Case Test (Countdown Cancel)**
**Steps:**
1. Start 2 clients
2. Let countdown begin
3. Disconnect one client during countdown
4. Verify countdown cancels
5. Verify remaining client returns to "Waiting for players..."

**Expected Behavior:**
- ✅ Countdown cancels when players drop below 2
- ✅ gameState returns to "waiting"
- ✅ UI updates correctly

---

### **8.4 Connection Failure Test**
**Steps:**
1. Stop Colyseus server
2. Start Unity client
3. Click Play button
4. Verify error logged
5. Verify game doesn't crash

**Expected Behavior:**
- ✅ Connection error logged
- ✅ Client doesn't freeze
- ✅ (Optional) Return to menu with error message

---

## 📊 **Implementation Checklist**

### **Server-Side**
- [ ] ParkourRoomState.ts - Add gameState, countdownValue, playerCount
- [ ] ParkourRoom.ts - Add MIN_PLAYERS, COUNTDOWN_SECONDS constants
- [ ] ParkourRoom.ts - Modify onCreate() to set gameState = "waiting"
- [ ] ParkourRoom.ts - Modify onJoin() to update playerCount and checkGameStart()
- [ ] ParkourRoom.ts - Modify onLeave() to update playerCount and cancelCountdown()
- [ ] ParkourRoom.ts - Add checkGameStart() method
- [ ] ParkourRoom.ts - Add startCountdown() method
- [ ] ParkourRoom.ts - Add cancelCountdown() method
- [ ] ParkourRoom.ts - Add startGame() method
- [ ] ParkourRoom.ts - Modify onDispose() to clear countdown interval
- [ ] Test server with console logs

### **Unity Client - Schema**
- [ ] ParkourRoomState.cs - Add gameState field (Type 3)
- [ ] ParkourRoomState.cs - Add countdownValue field (Type 4)
- [ ] ParkourRoomState.cs - Add playerCount field (Type 5)

### **Unity Client - UI Scripts**
- [ ] Create Scripts/UI/ folder
- [ ] Create GameUIManager.cs (singleton, state enum, transitions)
- [ ] Create MenuUI.cs (programmatic Canvas + Button)
- [ ] Create LobbyUI.cs (programmatic Canvas + Text)
- [ ] Test UI creation in isolation

### **Unity Client - PlayerController**
- [ ] Add MovementEnabled property
- [ ] Modify Update() to check MovementEnabled
- [ ] Test freeze/unfreeze

### **Unity Client - NetworkManager**
- [ ] Remove auto-connect from Start()
- [ ] Add ConnectAndJoin() public method
- [ ] Add HandleGameStateChange() method
- [ ] Add HandleCountdownUpdate() method
- [ ] Add EnableLocalPlayerMovement() method
- [ ] Modify SetupRoomHandlers() to listen for gameState/countdownValue
- [ ] Add using ParkourLegion.UI namespace

### **Unity Client - CameraInputProvider**
- [ ] Remove LockCursor() call from Start()
- [ ] Make LockCursor() public
- [ ] Make UnlockCursor() public

### **Unity Scene Setup**
- [ ] Create GameUIManager GameObject in scene
- [ ] Attach GameUIManager.cs script
- [ ] Create MenuUI GameObject, attach MenuUI.cs
- [ ] Create LobbyUI GameObject, attach LobbyUI.cs
- [ ] Assign references in GameUIManager inspector

### **Testing**
- [ ] Single player - Menu → Waiting
- [ ] Multi-player - Countdown starts with 2 players
- [ ] Countdown counts down correctly
- [ ] Game starts after countdown
- [ ] Players become movable
- [ ] Cursor locks when playing
- [ ] Countdown cancels if player leaves
- [ ] Connection failure handling

---

## 🎯 **Success Criteria**

**Must Have:**
- ✅ Game starts with Menu UI visible
- ✅ Cursor unlocked in menu
- ✅ Play button triggers connection
- ✅ Player spawns frozen in Waiting state
- ✅ "Waiting for players..." shows when alone
- ✅ Countdown starts when 2+ players
- ✅ Countdown counts down: 3 → 2 → 1 → 0
- ✅ Game starts when countdown ends
- ✅ UI hides when playing
- ✅ Movement enabled when playing
- ✅ Cursor locks when playing

**Nice to Have:**
- ✅ Countdown cancels if players leave (< 2 players)
- ✅ Connection error handling
- ✅ Smooth state transitions
- ✅ Console logs for debugging

---

## 🐛 **Known Issues & Solutions**

### **Issue: UI doesn't show on start**
**Solution:** Ensure GameUIManager.Start() calls SetState(GameState.Menu)

### **Issue: Cursor doesn't unlock**
**Solution:** Check CameraInputProvider instance is found via FindObjectOfType

### **Issue: Player moves while frozen**
**Solution:** Ensure MovementEnabled = false by default in PlayerController

### **Issue: Countdown doesn't update**
**Solution:** Verify countdownValue listener is registered in SetupRoomHandlers()

### **Issue: Game doesn't start after countdown**
**Solution:** Check server startGame() sets gameState = "playing"

---

## 📝 **Notes**

- **Programmatic UI Creation:** All UI elements created via code in Awake()
- **State-Driven Design:** All behavior controlled by GameState enum
- **Server Authority:** Game state managed by server, clients react
- **Clean Separation:** UI, Network, Player systems remain decoupled
- **Testing Strategy:** Test each phase independently before integration

---

**Document Version:** 1.0
**Last Updated:** 2025-11-15
**Estimated Implementation Time:** 4-6 hours
