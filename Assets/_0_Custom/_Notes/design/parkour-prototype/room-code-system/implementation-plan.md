# Room Code System - Implementation Plan

**Date:** 2025-11-15
**Estimated Time:** 6-8 hours
**Complexity:** Medium

---

## 📋 Implementation Order

This plan follows the **backend-first** approach to ensure server changes are ready before client integration.

---

## Phase 1: Backend Schema & Room Logic (2-3 hours)

### Step 1.1: Update Server Schemas
**Files:** `src/schema/PlayerState.ts`, `src/schema/ParkourRoomState.ts`

**PlayerState.ts:**
```typescript
@type("boolean") isReady: boolean = false;
```

**ParkourRoomState.ts:**
```typescript
@type("string") roomCode: string = "";
```

**Testing:**
- Run TypeScript compiler: `npm run build`
- Verify no compilation errors

---

### Step 1.2: Generate Room Codes
**File:** `src/rooms/ParkourRoom.ts`

**Add method:**
```typescript
private generateRoomCode(): string {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; // Removed ambiguous chars
    let code = '';
    for (let i = 0; i < 4; i++) {
        code += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return code;
}
```

**Update `onCreate()`:**
```typescript
onCreate(options: any) {
    this.setState(new ParkourRoomState());
    this.state.roomCode = this.generateRoomCode();
    this.state.gameState = "waiting";
    this.setPatchRate(33);

    this.setMetadata({ roomCode: this.state.roomCode });

    console.log(`ParkourRoom created with code: ${this.state.roomCode}`);
    // ... rest of onCreate
}
```

**Testing:**
- Start server: `npm run dev`
- Create room, verify console shows room code
- Check metadata is set correctly

---

### Step 1.3: Update Max Players
**File:** `src/rooms/ParkourRoom.ts:6`

```typescript
maxClients = 4; // Changed from 8
```

---

### Step 1.4: Implement Ready State Logic
**File:** `src/rooms/ParkourRoom.ts`

**Add message handler in `onCreate()`:**
```typescript
this.onMessage("playerReady", (client, message) => {
    const player = this.state.players.get(client.sessionId);
    if (player) {
        player.isReady = message.isReady;
        console.log(`Player ${client.sessionId} ready state: ${player.isReady}`);
        this.checkReadyState();
    }
});
```

**Add ready state check method:**
```typescript
private checkReadyState() {
    const playerCount = this.state.players.size;

    if (playerCount < this.MIN_PLAYERS) {
        if (this.state.gameState === "countdown") {
            this.cancelCountdown();
        }
        return;
    }

    const allReady = Array.from(this.state.players.values())
        .every(p => p.isReady);

    if (allReady && this.state.gameState === "waiting") {
        console.log("All players ready - starting countdown");
        this.startCountdown();
    } else if (!allReady && this.state.gameState === "countdown") {
        console.log("Not all players ready - cancelling countdown");
        this.cancelCountdown();
    }
}
```

**Update `onJoin()` - remove auto-start:**
```typescript
onJoin(client: Client, options: any) {
    // ... existing spawn logic ...

    this.state.playerCount = this.state.players.size;
    // REMOVE: this.checkGameStart();
}
```

**Update `onLeave()` - add ready check:**
```typescript
onLeave(client: Client, consented: boolean) {
    console.log(client.sessionId, "left!");
    this.state.players.delete(client.sessionId);
    this.state.playerCount = this.state.players.size;

    // Check if we still have enough ready players
    this.checkReadyState();
}
```

**Testing:**
- Verify countdown does NOT start automatically when 2 players join
- Manually test ready message (use Postman/test client)

---

### Step 1.5: Add Room Code Filtering
**File:** `src/index.ts:22`

**Update room definition:**
```typescript
gameServer.define("parkour", ParkourRoom)
    .filterBy(['roomCode']);
```

**Add room lookup endpoint (optional but recommended):**
```typescript
// Add before gameServer.listen()
app.get("/api/find-room/:code", async (req, res) => {
    try {
        const roomCode = req.params.code.toUpperCase();
        const rooms = await gameServer.matchMaker.query({
            name: "parkour",
            private: false
        });

        const targetRoom = rooms.find(r => r.metadata?.roomCode === roomCode);

        if (targetRoom && targetRoom.clients < targetRoom.maxClients) {
            res.json({
                roomId: targetRoom.roomId,
                players: targetRoom.clients,
                maxPlayers: targetRoom.maxClients
            });
        } else if (targetRoom) {
            res.status(400).json({ error: "Room is full" });
        } else {
            res.status(404).json({ error: "Room not found" });
        }
    } catch (error) {
        res.status(500).json({ error: "Server error" });
    }
});
```

**Testing:**
- Test endpoint: `curl http://localhost:2567/api/find-room/TEST`
- Should return 404 for non-existent rooms

---

## Phase 2: Unity Schema Updates (30 min)

### Step 2.1: Update Unity Schemas
**Files:** `Scripts/Schema/PlayerState.cs`, `Scripts/Schema/ParkourRoomState.cs`

**PlayerState.cs:**
```csharp
[Colyseus.Schema.Type(10, "boolean")]
public bool isReady = false;
```

**ParkourRoomState.cs:**
```csharp
[Colyseus.Schema.Type(6, "string")]
public string roomCode = "";
```

**⚠️ IMPORTANT:** Schema type indices must match server exactly!

**Testing:**
- Compile Unity project
- Verify no compilation errors

---

## Phase 3: NetworkManager Updates (1-2 hours)

### Step 3.1: Add Room Join/Create Methods
**File:** `Scripts/Networking/NetworkManager.cs`

**Add new methods:**
```csharp
public async void CreateRoom(int skinId)
{
    if (UI.GameUIManager.Instance != null)
    {
        UI.GameUIManager.Instance.SetState(UI.GameState.Connecting);
    }

    await CreateRoomAsync(skinId);
}

public async void JoinRoomByCode(string roomCode, int skinId)
{
    if (UI.GameUIManager.Instance != null)
    {
        UI.GameUIManager.Instance.SetState(UI.GameState.Connecting);
    }

    await JoinRoomByCodeAsync(roomCode, skinId);
}

private async Task CreateRoomAsync(int skinId)
{
    client = new ColyseusClient(serverUrl);

    try
    {
        var options = new Dictionary<string, object>
        {
            { "skinId", skinId }
        };

        room = await client.Create<ParkourRoomState>("parkour", options);
        Debug.Log($"Created room with code: {room.State.roomCode}, Session: {room.SessionId}");

        SetupRoomHandlers();
        SpawnLocalPlayer();

        if (UI.GameUIManager.Instance != null)
        {
            UI.GameUIManager.Instance.SetState(UI.GameState.Waiting);
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Failed to create room: {e.Message}");
        if (UI.GameUIManager.Instance != null)
        {
            UI.GameUIManager.Instance.SetState(UI.GameState.Menu);
        }
    }
}

private async Task JoinRoomByCodeAsync(string roomCode, int skinId)
{
    client = new ColyseusClient(serverUrl);

    try
    {
        // First, find room by code
        string roomId = await GetRoomIdByCode(roomCode);

        if (string.IsNullOrEmpty(roomId))
        {
            throw new System.Exception("Room not found");
        }

        var options = new Dictionary<string, object>
        {
            { "skinId", skinId }
        };

        room = await client.JoinById<ParkourRoomState>(roomId, options);
        Debug.Log($"Joined room {roomCode}, Session: {room.SessionId}");

        SetupRoomHandlers();
        SpawnLocalPlayer();

        if (UI.GameUIManager.Instance != null)
        {
            UI.GameUIManager.Instance.SetState(UI.GameState.Waiting);
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"Failed to join room: {e.Message}");
        if (UI.GameUIManager.Instance != null)
        {
            UI.GameUIManager.Instance.SetState(UI.GameState.Menu);
        }
    }
}

private async Task<string> GetRoomIdByCode(string roomCode)
{
    using (UnityWebRequest request = UnityWebRequest.Get($"http://localhost:2567/api/find-room/{roomCode}"))
    {
        await request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<RoomIdResponse>(request.downloadHandler.text);
            return response.roomId;
        }

        return null;
    }
}

public void SetPlayerReady(bool ready)
{
    if (room != null)
    {
        room.Send("playerReady", new { isReady = ready });
        Debug.Log($"Sent ready state: {ready}");
    }
}
```

**Add helper class at bottom of file:**
```csharp
[System.Serializable]
public class RoomIdResponse
{
    public string roomId;
    public int players;
    public int maxPlayers;
}
```

**Add using statements at top:**
```csharp
using UnityEngine.Networking;
using System.Threading.Tasks;
```

**Mark old method obsolete:**
```csharp
[System.Obsolete("Use CreateRoom() or JoinRoomByCode() instead")]
public async void ConnectAndJoin() { ... }
```

**Testing:**
- Will test after UI integration

---

## Phase 4: MenuUI Updates (1-2 hours)

### Step 4.1: Rebuild MenuUI with Room Code Input
**File:** `Scripts/UI/MenuUI.cs`

**Add fields:**
```csharp
private TMP_InputField roomCodeInput;
private Button joinRoomButton;
private Button createRoomButton;
```

**Update `CreateMenuUI()` method:**
```csharp
private void CreateMenuUI()
{
    // ... existing canvas setup ...

    // Title
    GameObject titleGO = new GameObject("Title");
    titleGO.transform.SetParent(menuPanel.transform, false);
    RectTransform titleRect = titleGO.AddComponent<RectTransform>();
    titleRect.anchorMin = new Vector2(0.5f, 0.7f);
    titleRect.anchorMax = new Vector2(0.5f, 0.7f);
    titleRect.sizeDelta = new Vector2(600, 80);
    titleRect.anchoredPosition = Vector2.zero;

    TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
    titleText.text = "PARKOUR LEGION";
    titleText.fontSize = 48;
    titleText.color = Color.white;
    titleText.alignment = TextAlignmentOptions.Center;
    titleText.fontStyle = FontStyles.Bold;

    // Room code input
    GameObject inputGO = new GameObject("RoomCodeInput");
    inputGO.transform.SetParent(menuPanel.transform, false);
    RectTransform inputRect = inputGO.AddComponent<RectTransform>();
    inputRect.anchorMin = new Vector2(0.5f, 0.5f);
    inputRect.anchorMax = new Vector2(0.5f, 0.5f);
    inputRect.sizeDelta = new Vector2(250, 50);
    inputRect.anchoredPosition = new Vector2(-80, 50);

    Image inputBg = inputGO.AddComponent<Image>();
    inputBg.color = new Color(0.2f, 0.2f, 0.2f);

    roomCodeInput = inputGO.AddComponent<TMP_InputField>();
    roomCodeInput.characterLimit = 4;
    roomCodeInput.placeholder = CreatePlaceholder(inputGO.transform, "Room Code");
    roomCodeInput.textComponent = CreateInputText(inputGO.transform);
    roomCodeInput.onValueChanged.AddListener(OnRoomCodeChanged);

    // Join button
    joinRoomButton = CreateButton("JoinRoomButton", new Vector2(90, 50), new Vector2(140, 50), "JOIN", menuPanel.transform);
    joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
    joinRoomButton.interactable = false;

    // Create button
    createRoomButton = CreateButton("CreateRoomButton", new Vector2(0, -20), new Vector2(200, 60), "CREATE ROOM", menuPanel.transform);
    createRoomButton.onClick.AddListener(OnCreateRoomClicked);

    Debug.Log("MenuUI created with room code system");
}

private Button CreateButton(string name, Vector2 position, Vector2 size, string text, Transform parent)
{
    GameObject buttonGO = new GameObject(name);
    buttonGO.transform.SetParent(parent, false);

    RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
    buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
    buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
    buttonRect.sizeDelta = size;
    buttonRect.anchoredPosition = position;

    Button button = buttonGO.AddComponent<Button>();
    Image buttonImage = buttonGO.AddComponent<Image>();
    buttonImage.color = new Color(0.18f, 0.8f, 0.44f);

    ColorBlock colors = button.colors;
    colors.normalColor = new Color(0.18f, 0.8f, 0.44f);
    colors.highlightedColor = new Color(0.32f, 0.85f, 0.53f);
    colors.pressedColor = new Color(0.15f, 0.7f, 0.38f);
    colors.disabledColor = new Color(0.3f, 0.3f, 0.3f);
    button.colors = colors;

    GameObject textGO = new GameObject("Text");
    textGO.transform.SetParent(buttonGO.transform, false);
    RectTransform textRect = textGO.AddComponent<RectTransform>();
    textRect.anchorMin = Vector2.zero;
    textRect.anchorMax = Vector2.one;
    textRect.sizeDelta = Vector2.zero;

    TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
    tmpText.text = text;
    tmpText.fontSize = 20;
    tmpText.color = Color.white;
    tmpText.alignment = TextAlignmentOptions.Center;
    tmpText.fontStyle = FontStyles.Bold;

    return button;
}

private TextMeshProUGUI CreatePlaceholder(Transform parent, string text)
{
    GameObject placeholderGO = new GameObject("Placeholder");
    placeholderGO.transform.SetParent(parent, false);
    RectTransform rect = placeholderGO.AddComponent<RectTransform>();
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.sizeDelta = new Vector2(-10, 0);
    rect.anchoredPosition = Vector2.zero;

    TextMeshProUGUI tmp = placeholderGO.AddComponent<TextMeshProUGUI>();
    tmp.text = text;
    tmp.fontSize = 16;
    tmp.color = new Color(0.5f, 0.5f, 0.5f);
    tmp.alignment = TextAlignmentOptions.Left;
    tmp.margin = new Vector4(10, 0, 0, 0);

    return tmp;
}

private TextMeshProUGUI CreateInputText(Transform parent)
{
    GameObject textGO = new GameObject("Text");
    textGO.transform.SetParent(parent, false);
    RectTransform rect = textGO.AddComponent<RectTransform>();
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.sizeDelta = new Vector2(-10, 0);
    rect.anchoredPosition = Vector2.zero;

    TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
    tmp.fontSize = 20;
    tmp.color = Color.white;
    tmp.alignment = TextAlignmentOptions.Left;
    tmp.margin = new Vector4(10, 0, 0, 0);

    return tmp;
}

private void OnRoomCodeChanged(string code)
{
    roomCodeInput.text = code.ToUpper();
    joinRoomButton.interactable = code.Length == 4;
}

private void OnJoinRoomClicked()
{
    string roomCode = roomCodeInput.text.ToUpper();
    if (roomCode.Length == 4 && GameUIManager.Instance != null)
    {
        GameUIManager.Instance.OnJoinRoomClicked(roomCode);
    }
}

private void OnCreateRoomClicked()
{
    if (GameUIManager.Instance != null)
    {
        GameUIManager.Instance.OnCreateRoomClicked();
    }
}
```

**Remove old Play button code**

**Testing:**
- Visual check in Unity editor
- Test input validation (4 chars, uppercase conversion)

---

## Phase 5: LobbyUI Updates (1-2 hours)

### Step 5.1: Add Room Code Display & Ready Button
**File:** `Scripts/UI/LobbyUI.cs`

**Add fields:**
```csharp
private TextMeshProUGUI roomCodeText;
private TextMeshProUGUI playerListText;
private Button readyButton;
private TextMeshProUGUI readyButtonText;
private bool isReady = false;
```

**Update `CreateLobbyUI()`:**
```csharp
private void CreateLobbyUI()
{
    // ... existing canvas setup ...

    // Room code display (top-left)
    GameObject roomCodeGO = new GameObject("RoomCodeText");
    roomCodeGO.transform.SetParent(lobbyCanvas.transform, false);
    RectTransform roomCodeRect = roomCodeGO.AddComponent<RectTransform>();
    roomCodeRect.anchorMin = new Vector2(0, 1);
    roomCodeRect.anchorMax = new Vector2(0, 1);
    roomCodeRect.pivot = new Vector2(0, 1);
    roomCodeRect.sizeDelta = new Vector2(300, 60);
    roomCodeRect.anchoredPosition = new Vector2(20, -20);

    roomCodeText = roomCodeGO.AddComponent<TextMeshProUGUI>();
    roomCodeText.fontSize = 24;
    roomCodeText.color = Color.yellow;
    roomCodeText.alignment = TextAlignmentOptions.TopLeft;
    roomCodeText.fontStyle = FontStyles.Bold;
    roomCodeText.text = "Room Code: ----";

    // Player list
    GameObject playerListGO = new GameObject("PlayerListText");
    playerListGO.transform.SetParent(lobbyCanvas.transform, false);
    RectTransform playerListRect = playerListGO.AddComponent<RectTransform>();
    playerListRect.anchorMin = new Vector2(0.5f, 0.6f);
    playerListRect.anchorMax = new Vector2(0.5f, 0.6f);
    playerListRect.sizeDelta = new Vector2(400, 200);
    playerListRect.anchoredPosition = Vector2.zero;

    playerListText = playerListGO.AddComponent<TextMeshProUGUI>();
    playerListText.fontSize = 20;
    playerListText.color = Color.white;
    playerListText.alignment = TextAlignmentOptions.Top;
    playerListText.text = "";

    // Status text (existing, adjust position)
    RectTransform statusRect = statusText.GetComponent<RectTransform>();
    statusRect.anchoredPosition = new Vector2(0, -50);

    // Ready button
    GameObject readyButtonGO = new GameObject("ReadyButton");
    readyButtonGO.transform.SetParent(lobbyCanvas.transform, false);
    RectTransform readyButtonRect = readyButtonGO.AddComponent<RectTransform>();
    readyButtonRect.anchorMin = new Vector2(0.5f, 0.35f);
    readyButtonRect.anchorMax = new Vector2(0.5f, 0.35f);
    readyButtonRect.sizeDelta = new Vector2(200, 60);
    readyButtonRect.anchoredPosition = Vector2.zero;

    readyButton = readyButtonGO.AddComponent<Button>();
    Image readyButtonImage = readyButtonGO.AddComponent<Image>();
    readyButtonImage.color = new Color(0.18f, 0.8f, 0.44f);

    ColorBlock colors = readyButton.colors;
    colors.normalColor = new Color(0.18f, 0.8f, 0.44f);
    colors.highlightedColor = new Color(0.32f, 0.85f, 0.53f);
    colors.pressedColor = new Color(0.15f, 0.7f, 0.38f);
    readyButton.colors = colors;

    GameObject readyTextGO = new GameObject("Text");
    readyTextGO.transform.SetParent(readyButtonGO.transform, false);
    RectTransform readyTextRect = readyTextGO.AddComponent<RectTransform>();
    readyTextRect.anchorMin = Vector2.zero;
    readyTextRect.anchorMax = Vector2.one;
    readyTextRect.sizeDelta = Vector2.zero;

    readyButtonText = readyTextGO.AddComponent<TextMeshProUGUI>();
    readyButtonText.text = "READY";
    readyButtonText.fontSize = 24;
    readyButtonText.color = Color.white;
    readyButtonText.alignment = TextAlignmentOptions.Center;
    readyButtonText.fontStyle = FontStyles.Bold;

    readyButton.onClick.AddListener(OnReadyButtonClicked);

    lobbyCanvas.SetActive(false);
    Debug.Log("LobbyUI created with ready system");
}

public void UpdateRoomCode(string code)
{
    if (roomCodeText != null)
    {
        roomCodeText.text = $"Room Code: {code}";
    }
}

public void UpdatePlayerList(int playerCount, int maxPlayers, System.Collections.Generic.Dictionary<string, bool> playerReadyStates)
{
    if (playerListText != null)
    {
        string list = $"Players: {playerCount}/{maxPlayers}\n\n";
        int index = 1;
        foreach (var kvp in playerReadyStates)
        {
            string readyIcon = kvp.Value ? "✓" : "⏳";
            string readyText = kvp.Value ? "Ready" : "Not Ready";
            list += $"{index}. Player {readyIcon} ({readyText})\n";
            index++;
        }
        playerListText.text = list;
    }
}

private void OnReadyButtonClicked()
{
    isReady = !isReady;

    if (readyButtonText != null)
    {
        readyButtonText.text = isReady ? "✓ READY" : "READY";
    }

    var buttonImage = readyButton.GetComponent<Image>();
    if (buttonImage != null)
    {
        buttonImage.color = isReady ? new Color(0.8f, 0.8f, 0.18f) : new Color(0.18f, 0.8f, 0.44f);
    }

    if (GameUIManager.Instance != null)
    {
        GameUIManager.Instance.OnReadyButtonClicked(isReady);
    }
}

public void ResetReadyState()
{
    isReady = false;
    if (readyButtonText != null)
    {
        readyButtonText.text = "READY";
    }
    var buttonImage = readyButton.GetComponent<Image>();
    if (buttonImage != null)
    {
        buttonImage.color = new Color(0.18f, 0.8f, 0.44f);
    }
}
```

**Testing:**
- Visual check in Unity
- Test ready button toggle

---

## Phase 6: GameUIManager Integration (1 hour)

### Step 6.1: Add Room Code Methods
**File:** `Scripts/UI/GameUIManager.cs`

**Add methods:**
```csharp
public void OnCreateRoomClicked()
{
    Debug.Log("Create room button clicked");

    int skinId = Random.Range(0, totalSkins);

    var networkManager = Networking.NetworkManager.Instance;
    if (networkManager != null)
    {
        networkManager.CreateRoom(skinId);
    }
    else
    {
        Debug.LogError("NetworkManager instance not found!");
    }
}

public void OnJoinRoomClicked(string roomCode)
{
    Debug.Log($"Join room button clicked: {roomCode}");

    int skinId = Random.Range(0, totalSkins);

    var networkManager = Networking.NetworkManager.Instance;
    if (networkManager != null)
    {
        networkManager.JoinRoomByCode(roomCode, skinId);
    }
    else
    {
        Debug.LogError("NetworkManager instance not found!");
    }
}

public void OnReadyButtonClicked(bool ready)
{
    Debug.Log($"Ready button clicked: {ready}");

    var networkManager = Networking.NetworkManager.Instance;
    if (networkManager != null)
    {
        networkManager.SetPlayerReady(ready);
    }
}
```

**Update `SetState()` for Waiting:**
```csharp
case GameState.Waiting:
    ShowLobbyUI();
    if (lobbyUI != null)
    {
        lobbyUI.ShowWaiting();
        lobbyUI.ResetReadyState();
        UpdateLobbyDisplay();
    }
    SetCursorState(false);
    HideGameplayUI();
    break;
```

**Add lobby update method:**
```csharp
private void Update()
{
    if (currentState == GameState.Waiting)
    {
        UpdateLobbyDisplay();
    }
}

private void UpdateLobbyDisplay()
{
    var networkManager = Networking.NetworkManager.Instance;
    if (networkManager != null && networkManager.Room != null && lobbyUI != null)
    {
        string roomCode = networkManager.Room.State.roomCode;
        lobbyUI.UpdateRoomCode(roomCode);

        var playerStates = new System.Collections.Generic.Dictionary<string, bool>();
        networkManager.Room.State.players.ForEach((sessionId, playerState) =>
        {
            playerStates[sessionId] = playerState.isReady;
        });

        int playerCount = networkManager.Room.State.playerCount;
        lobbyUI.UpdatePlayerList(playerCount, 4, playerStates);
    }
}
```

**Testing:**
- Integration test with full flow

---

## Phase 7: Testing & Bug Fixes (1-2 hours)

### Test Cases

**Room Creation:**
- [ ] Click "Create Room" → Room created with 4-char code
- [ ] Room code visible in lobby (top-left)
- [ ] Server logs show room code

**Room Joining:**
- [ ] Enter 4-char code → "Join Room" button enabled
- [ ] Enter 3-char code → Button disabled
- [ ] Join valid room → Success
- [ ] Join invalid room → Error message
- [ ] Join full room → Error message

**Ready States:**
- [ ] Single player clicks Ready → Button shows "✓ READY", color changes
- [ ] Player unreadies → Button resets
- [ ] 2 players both ready → Countdown starts
- [ ] Player unreadies during countdown → Countdown cancels
- [ ] Player disconnects → Ready count updates

**Edge Cases:**
- [ ] Player joins during countdown → Countdown continues (or resets?)
- [ ] Last player leaves → Room destroyed
- [ ] Player rejoins same room → State preserved

---

## 📊 Time Breakdown

| Phase | Task | Estimated Time |
|-------|------|----------------|
| 1 | Backend Schema & Logic | 2-3 hours |
| 2 | Unity Schema | 30 min |
| 3 | NetworkManager | 1-2 hours |
| 4 | MenuUI | 1-2 hours |
| 5 | LobbyUI | 1-2 hours |
| 6 | GameUIManager | 1 hour |
| 7 | Testing & Fixes | 1-2 hours |
| **Total** | | **6-8 hours** |

---

## ⚠️ Potential Blockers

1. **Schema Type Indices Mismatch**
   - Server and Unity schema type indices MUST match exactly
   - Solution: Carefully verify indices when adding new fields

2. **Room Lookup Timing**
   - HTTP request to find room might timeout
   - Solution: Add proper async/await handling, show loading state

3. **Ready State Race Conditions**
   - Multiple players clicking ready simultaneously
   - Solution: Server is authoritative, state will sync correctly

4. **UI Not Updating**
   - Room state changes might not trigger UI updates
   - Solution: Add Update() loop check or use Colyseus callbacks

---

## 🚀 Deployment Checklist

- [ ] Server changes deployed
- [ ] Unity schemas regenerated
- [ ] Client build tested
- [ ] Error handling verified
- [ ] Edge cases covered
- [ ] Documentation updated

---

**Plan Version:** 1.0
**Last Updated:** 2025-11-15
