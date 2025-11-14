# Skin Selection UI Implementation Plan (Final)

**Date:** 2025-11-15 (Final Update)
**Feature:** Character Skin Selection in GameplayUI (Playing State Only)
**Status:** Ready for Implementation

---

## 🎯 **Requirements Summary**

### **User Flow:**
1. **Game reaches Playing state** (after countdown) → GameplayUI becomes visible
2. **Skin selection UI visible** in GameplayUI canvas
3. **Left/Right Buttons** → Cycle through character models (enable/disable children)
4. **Visual Preview** → Script handles model toggling via SetActive()
5. **Select Button** → Confirm skin and send to server

### **Key Features:**
- ✅ Skin selection in GameplayUI (NOT MenuUI)
- ✅ Shows ONLY when game is Playing (after countdown)
- ✅ Dynamic skin count via Transform.childCount
- ✅ Left/Right buttons handled by script (auto model toggling)
- ✅ GameUIManager has public skin selection variables
- ✅ Select button sends chosen skin to server

---

## 📊 **Current System Analysis**

### **✅ What's Already Implemented:**

1. **Server-Side:**
   - `PlayerState.skinId` field (uint8) ✅
   - `selectSkin` message handler ✅
   - Skin sync to all clients ✅

2. **Unity Client:**
   - `PlayerModelManager` script ✅
   - `SetModel(skinId)` method ✅
   - Network skin sync on join ✅
   - Random skin selection on connect ✅

3. **UI System:**
   - `GameUIManager` (Menu, Lobby, Playing states) ✅
   - `MenuUI` (Play button) ✅
   - Cursor control ✅

### **❌ What's Missing:**

1. **GameplayUI Canvas Integration:**
   - No Canvas reference in GameUIManager ❌
   - No show/hide logic in GameUIManager ❌
   - Doesn't show only after Playing state ❌

2. **Skin Selection in GameUIManager:**
   - No skin selection variables in GameUIManager ❌
   - No left/right button logic ❌
   - No model toggle logic (SetActive) ❌
   - No select button to send skin to server ❌

---

## 🏗️ **Architecture Design**

### **Component Structure:**

```
MenuUI (existing)
├── MenuCanvas
│   └── MenuPanel
│       ├── SkinSelectionUI (NEW)
│       │   ├── Model Preview Container (3D models placed here)
│       │   │   ├── character-a (skinId 0) - SetActive true/false
│       │   │   ├── character-b (skinId 1)
│       │   │   └── ... (all 18 models)
│       │   ├── Left Button (<)
│       │   ├── Right Button (>)
│       │   ├── Select Button
│       │   └── Skin Name Text (e.g., "Character A")
│       └── Play Button (existing)

GameplayUI (NEW)
└── GameplayCanvas
    ├── HUD elements (health, score, etc.)
    └── Other gameplay UI

GameUIManager (modified)
├── menuUI (existing)
├── lobbyUI (existing)
└── gameplayUI (NEW public field)
```

---

## 🎨 **UI Layout Specification**

### **Skin Selection UI Layout:**

```
┌────────────────────────────────────────┐
│          CHARACTER SELECTION           │
│                                        │
│    [<]   [3D Model Preview]   [>]     │
│                                        │
│          "Character Name"              │
│                                        │
│           [SELECT BUTTON]              │
│                                        │
│            [PLAY BUTTON]               │
└────────────────────────────────────────┘
```

**Layout Details:**
- **Model Preview:** Center of screen, 3D character model
- **Left Button:** Left side of model
- **Right Button:** Right side of model
- **Skin Name Text:** Below model (e.g., "Character A")
- **Select Button:** Below skin name
- **Play Button:** Bottom center (existing)

---

## 📋 **Implementation Plan**

### **Phase 1: GameplayUI Script & Integration**

#### **1.1 Create GameplayUI.cs**
**File:** `Scripts/UI/GameplayUI.cs`

**Purpose:** Simple script to show/hide gameplay UI

**Implementation:**
```csharp
using UnityEngine;

namespace ParkourLegion.UI
{
    public class GameplayUI : MonoBehaviour
    {
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5;

                gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            Hide();
        }

        public void Show()
        {
            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        public void Hide()
        {
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }
    }
}
```

**Note:** User will manually add UI elements as children of this canvas.

---

#### **1.2 Modify GameUIManager.cs - Start() Method**

**Update Start() to initialize everything:**

```csharp
private void Start()
{
    menuUI = FindObjectOfType<MenuUI>();
    lobbyUI = FindObjectOfType<LobbyUI>();

    if (menuUI == null)
    {
        Debug.LogWarning("MenuUI not found in scene.");
    }

    if (lobbyUI == null)
    {
        Debug.LogWarning("LobbyUI not found in scene.");
    }

    // ===== GAMEPLAY UI INITIALIZATION =====
    if (gameplayCanvas != null)
    {
        gameplayCanvas.enabled = false; // Hide initially
    }
    else
    {
        Debug.LogWarning("Gameplay Canvas not assigned to GameUIManager!");
    }

    // ===== SKIN SELECTION INITIALIZATION =====
    InitializeSkinSelection();

    SetState(GameState.Menu);
}

// ===== SKIN SELECTION INITIALIZATION =====
private void InitializeSkinSelection()
{
    if (skinModelsContainer == null)
    {
        Debug.LogWarning("Skin models container not assigned");
        return;
    }

    totalSkins = skinModelsContainer.childCount;

    if (totalSkins == 0)
    {
        Debug.LogWarning("Skin models container has no children!");
        return;
    }

    if (skinLeftButton != null)
    {
        skinLeftButton.onClick.AddListener(OnSkinLeftClicked);
    }

    if (skinRightButton != null)
    {
        skinRightButton.onClick.AddListener(OnSkinRightClicked);
    }

    if (skinSelectButton != null)
    {
        skinSelectButton.onClick.AddListener(OnSkinSelectClicked);
    }

    ShowSkinModel(currentSkinIndex);
    Debug.Log($"Skin selection initialized with {totalSkins} skins");
}
```

#### **1.3 Modify GameUIManager.cs - SetState() Method**

**Update SetState() to show/hide GameplayUI Canvas:**

```csharp
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
            HideGameplayUI(); // HIDE
            break;

        case GameState.Connecting:
            HideAllUI();
            SetCursorState(false);
            HideGameplayUI(); // HIDE
            break;

        case GameState.Waiting:
            ShowLobbyUI();
            if (lobbyUI != null) lobbyUI.ShowWaiting();
            SetCursorState(false);
            HideGameplayUI(); // HIDE
            break;

        case GameState.Countdown:
            ShowLobbyUI();
            SetCursorState(false);
            HideGameplayUI(); // HIDE
            break;

        case GameState.Playing:
            HideAllUI();
            SetCursorState(true);
            ShowGameplayUI(); // SHOW ONLY HERE
            break;
    }
}

// ===== GAMEPLAY UI SHOW/HIDE METHODS =====
private void ShowGameplayUI()
{
    if (gameplayCanvas != null)
    {
        gameplayCanvas.enabled = true;
        Debug.Log("Gameplay UI shown");
    }
}

private void HideGameplayUI()
{
    if (gameplayCanvas != null)
    {
        gameplayCanvas.enabled = false;
    }
}
```

---

#### **1.4 Modify GameUIManager.cs - Skin Button Methods**

**Add skin navigation and selection methods (clear section):**

```csharp
// ===== SKIN SELECTION BUTTON HANDLERS =====
private void OnSkinLeftClicked()
{
    currentSkinIndex--;
    if (currentSkinIndex < 0)
    {
        currentSkinIndex = totalSkins - 1; // Wrap to end
    }

    ShowSkinModel(currentSkinIndex);
    Debug.Log($"Skin changed: {currentSkinIndex}");
}

private void OnSkinRightClicked()
{
    currentSkinIndex++;
    if (currentSkinIndex >= totalSkins)
    {
        currentSkinIndex = 0; // Wrap to start
    }

    ShowSkinModel(currentSkinIndex);
    Debug.Log($"Skin changed: {currentSkinIndex}");
}

private void OnSkinSelectClicked()
{
    Debug.Log($"Skin selected: {currentSkinIndex}");
    SendSkinChangeToServer(currentSkinIndex);
}

// ===== SKIN MODEL TOGGLE =====
private void ShowSkinModel(int skinIndex)
{
    if (skinModelsContainer == null) return;

    // Disable all, enable only the selected one
    for (int i = 0; i < skinModelsContainer.childCount; i++)
    {
        skinModelsContainer.GetChild(i).gameObject.SetActive(i == skinIndex);
    }
}

// ===== SEND SKIN TO SERVER =====
private void SendSkinChangeToServer(int skinId)
{
    var networkManager = Networking.NetworkManager.Instance;
    if (networkManager != null && networkManager.Room != null)
    {
        networkManager.Room.Send("selectSkin", new { skinId = skinId });
        Debug.Log($"Sent skin change to server: {skinId}");
    }
    else
    {
        Debug.LogWarning("Cannot send skin change - not connected to server");
    }
}
```

---

### **Phase 2: Server-Side - Add Skin Change Handler**

The server already has `selectSkin` message handler, but we need to verify it updates the player's model:

**Verify ParkourRoom.ts has:**
```typescript
this.onMessage("selectSkin", (client, message) => {
    const player = this.state.players.get(client.sessionId);
    if (player) {
        player.skinId = message.skinId;
        console.log(`Player ${client.sessionId} selected skin ${message.skinId}`);
    }
});
```

✅ **Already implemented** - No changes needed!

---

---

### **Phase 3: Unity Manual Setup (User)**

#### **3.1 GameplayUI Canvas Setup**

**User creates their GameplayUI:**
```
GameplayUI (Canvas GameObject)
├── User's gameplay elements
│   ├── Health Bar
│   ├── Score
│   └── etc.
└── Skin Selection Panel
    ├── Model Container (Transform)
    │   ├── character-0
    │   ├── character-1
    │   └── ... (dynamic count)
    ├── Left Button
    ├── Right Button
    └── Select Button
```

---

#### **3.2 GameUIManager Inspector Assignments**

**Select GameUIManager GameObject in hierarchy, then assign in Inspector:**

**📌 Gameplay UI Section:**
- **Gameplay Canvas** → Drag your manually created Canvas

**📌 Skin Selection Section:**
- **Skin Models Container** → Drag Transform with model children
- **Skin Left Button** → Drag left button
- **Skin Right Button** → Drag right button
- **Skin Select Button** → Drag select button

**✅ That's it!** GameUIManager handles:
- Show/hide canvas (only in Playing state)
- Model toggling (SetActive)
- Button click handlers
- Server communication

---

### **Phase 4: Testing Plan**

#### **4.1 Skin Selection UI Test**
- [ ] Menu shows with skin preview
- [ ] Click Left button → currentSkinId decreases, wraps to max
- [ ] Click Right button → currentSkinId increases, wraps to 0
- [ ] OnSkinChanged event fires with correct skinId
- [ ] User's model toggle method receives correct skinId
- [ ] Click Select button → logs selected skinId
- [ ] GetSelectedSkinId() returns correct value

#### **4.2 GameplayUI Test**
- [ ] GameplayUI hidden in Menu state
- [ ] GameplayUI hidden in Waiting state
- [ ] GameplayUI hidden in Countdown state
- [ ] GameplayUI shows ONLY in Playing state (after countdown)
- [ ] User's manual UI elements visible when playing

#### **4.3 Integration Test**
- [ ] Select a skin (e.g., skinId 5)
- [ ] Click Play button
- [ ] Connect to server
- [ ] Verify local player spawns with selected skin (not random)
- [ ] Verify server logs "selected skin 5"
- [ ] Second player joins with different skin
- [ ] Both players see correct skins

---

## 📊 **Data Flow Diagram**

```
User Opens Game
    ↓
Menu Shows (GameState.Menu)
    ↓
[SkinSelectionUI active, showing character-a]
    ↓
User clicks Right Button (3 times)
    ↓
Preview shows character-d (skinId 3)
    ↓
User clicks Select Button
    ↓
selectedSkinId = 3 stored in SkinSelectionUI
    ↓
User clicks Play Button
    ↓
NetworkManager.ConnectAndJoin()
    ↓
MenuUI.GetSelectedSkinId() → returns 3
    ↓
Send to server: { skinId: 3 }
    ↓
Server stores in PlayerState.skinId = 3
    ↓
LocalPlayer spawns
    ↓
PlayerModelManager.SetModel(3) → enables character-d
    ↓
Countdown: 3... 2... 1...
    ↓
GameState.Playing
    ↓
GameplayUI shows (cursor locks, menu hidden)
```

---

## 🎯 **Implementation Checklist**

### **Phase 1: GameplayUI Integration**
- [ ] Create `Scripts/UI/GameplayUI.cs`
- [ ] Add `gameplayUI` field to GameUIManager.cs
- [ ] Modify `GameUIManager.Start()` to hide GameplayUI initially
- [ ] Modify `GameUIManager.SetState()` to show GameplayUI only in Playing state
- [ ] Test GameplayUI visibility in different states

### **Phase 2: Skin Selection UI**
- [ ] Create `Scripts/UI/SkinSelectionUI.cs`
- [ ] Implement `OnLeftButtonClicked()` (cycle backward)
- [ ] Implement `OnRightButtonClicked()` (cycle forward)
- [ ] Implement `OnSelectButtonClicked()` (store selection)
- [ ] Implement `ShowPreviewModel(skinId)` (enable/disable models)
- [ ] Implement `GetSelectedSkinId()` public method
- [ ] Test left/right navigation
- [ ] Test select button

### **Phase 3: MenuUI Integration**
- [ ] Add `SkinSelectionUI` field to MenuUI.cs
- [ ] Add `GetSelectedSkinId()` method to MenuUI.cs
- [ ] Test MenuUI returns correct selected skinId

### **Phase 4: NetworkManager Integration**
- [ ] Modify `ConnectToServer()` to get selected skin from MenuUI
- [ ] Remove random skin selection
- [ ] Test selected skin sent to server
- [ ] Test local player spawns with selected skin

### **Phase 5: Unity Manual Setup (User)**
- [ ] Add SkinSelectionUI component to user's existing skin selection panel
- [ ] Assign Skin Container (Transform with model children)
- [ ] Assign Left/Right/Select buttons
- [ ] Add listener to OnSkinChanged event (user's model toggle method)
- [ ] Create GameplayUI GameObject
- [ ] Add GameplayUI.cs component
- [ ] Assign GameplayUI to GameUIManager inspector
- [ ] Add user's gameplay UI elements under GameplayUI canvas

### **Phase 6: Full Testing**
- [ ] Test skin selection carousel (left/right)
- [ ] Test select button confirmation
- [ ] Test Play button with selected skin
- [ ] Test GameplayUI visibility (only after Playing)
- [ ] Test multi-client with different skins
- [ ] Test all 18 skins work correctly

---

## 💡 **Key Design Decisions**

### **1. Why Store Selected Skin in SkinSelectionUI?**
- Decouples selection UI from network logic
- User can preview without committing
- Select button confirms choice
- Easy to reset/change before connecting

### **2. Why GameplayUI Shows Only After Playing?**
- Menu/Lobby UI needs to be hidden first
- Countdown should have minimal UI (just countdown text)
- Gameplay HUD distracting during menu/countdown
- Clean state separation

### **3. Why Dynamic Skin Count (childCount)?**
- Flexible - user can add/remove models without code changes
- No hardcoded skin count (18)
- Automatic detection from Transform hierarchy
- Easy to scale

### **4. Why UnityEvent for Model Toggle?**
- User already handles model visibility
- Decouples our logic from user's implementation
- User can wire up via Inspector (no code needed)
- Flexible - user can use any method they want

---

## 🚀 **Next Steps**

1. **Modify GameUIManager.cs** (add all skin selection logic)
2. **User assigns references in Inspector** (GameplayUI, skin container, buttons)
3. **Testing** (verify skin selection works in Playing state)

---

## ✅ **Success Criteria**

- ✅ GameplayUI (user's canvas) shows ONLY in Playing state
- ✅ Skin selection UI is part of GameplayUI
- ✅ Dynamic skin count via Transform.childCount
- ✅ Left/Right buttons cycle through models (SetActive toggling)
- ✅ Only one model visible at a time
- ✅ Select button sends "selectSkin" to server
- ✅ Server updates player's skinId
- ✅ All clients see skin change
- ✅ All logic in GameUIManager (no separate SkinSelectionUI script)

---

**Document Version:** 1.0
**Ready for Implementation:** Yes
**Estimated Time:** 2-3 hours (code) + user manual setup time
