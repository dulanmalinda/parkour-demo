# Player Models & Animations Design

**Date:** 2025-11-14 (Updated: 2025-11-14)
**Component:** Player Visual Representation
**Feature:** Character models with synced skins and state-driven animations
**Architecture:** Pre-placed models with enable/disable switching (SetActive approach)

---

## 🎯 **Requirements**

1. **Multiple character model options** (18 skins: character-a through character-r)
2. **Random skin selection** when player joins
3. **Skin synced across network** (all clients see same skin)
4. **State-driven animations** (idle, walk, run, jump, fall, slide)
5. **Works for both local and remote players**

---

## 📊 **Architecture Overview**

### **Pre-Placed Models Approach (IMPLEMENTED)**

Both `LocalPlayer.prefab` and `RemotePlayer.prefab` contain:
- **GFXs** child GameObject containing all 18 character models
- Each character model has its own Animator with AnimatorController attached
- Models are pre-placed in prefab hierarchy (not runtime instantiation)

**Structure:**
```
LocalPlayer/RemotePlayer
├── (Capsule mesh - disabled, used for fallback)
├── CharacterController
├── Scripts (PlayerController, NetworkSync, etc.)
└── GFXs/
    ├── character-a (with Animator + Controller) ← skinId 0
    ├── character-b (with Animator + Controller) ← skinId 1
    ├── character-c (with Animator + Controller) ← skinId 2
    ├── ...
    └── character-r (with Animator + Controller) ← skinId 17
```

### **Data Flow:**

```
Player Joins
    ↓
Client picks random skin ID (0-17)
    ↓
Send skin ID to server
    ↓
Server stores in PlayerState.skinId
    ↓
Other clients receive PlayerState
    ↓
SetActive model at GFXs.children[skinId] = true (disable others)
    ↓
Cache active Animator reference
    ↓
Animator updates based on movementState changes
```

### **Why This Approach is Superior:**

| Advantage | Description |
|-----------|-------------|
| **Performance** | No runtime instantiation overhead, just SetActive() |
| **Simplicity** | Direct child access, no Resources.Load() or prefab references |
| **Editor-Friendly** | All models visible in prefab inspector for easy setup |
| **Reliability** | No missing prefab or path errors |
| **Debugging** | Easy to test individual models in editor |
| **Memory** | All models loaded once with prefab, minimal switching cost |

---

## 🗂️ **Schema Changes**

### **Server Schema (PlayerState.ts):**

```typescript
@type("uint8") skinId: number = 0;
```

**Added field:**
- `skinId` (0-255): Index of character model/skin

### **Unity Schema (PlayerState.cs):**

```csharp
[Type(9, "uint8")]
public byte skinId = 0;
```

---

## 🎨 **Implementation Design**

### **Phase 1: Schema & Server Updates**

**Server Changes:**
1. Add `skinId` to PlayerState schema
2. Server receives skin selection from client on join
3. Store in player state (synced to all clients)

**Unity Schema:**
1. Add `skinId` field to PlayerState.cs
2. Regenerate/update schema to match server

---

### **Phase 2: Prefab Hierarchy Setup (ALREADY COMPLETED)**

**✅ Current Setup:**
- Both `LocalPlayer.prefab` and `RemotePlayer.prefab` have `GFXs` child GameObject
- All 18 character models (character-a through character-r) are children of GFXs
- Each model has Animator component with AnimatorController attached

**Requirements for each model:**
- ✅ Humanoid rig configured (for animation retargeting)
- ✅ Animator component with AnimatorController attached
- All animations: Idle, Walk, Run, Jump, Fall, Slide (verify in controllers)
- Same scale/proportions (verify CharacterController fits all models)

---

### **Phase 3: Animator Controller Setup**

**Animator Parameters:**
```
Int: movementState (0-5)
```

**State Machine:**
```
Idle (state == 0)
Walk (state == 1)
Run (state == 2)
Jump (state == 3)
Fall (state == 4)
Slide (state == 5)
```

**Transitions:**
- Transitions triggered by `movementState` parameter changes
- No exit time (immediate transitions)
- All states can transition to any other state

---

### **Phase 4: Player Model Manager**

**New Script: `PlayerModelManager.cs`**

```csharp
namespace ParkourLegion.Player
{
    public class PlayerModelManager : MonoBehaviour
    {
        [SerializeField] private Transform gfxsContainer; // Reference to GFXs GameObject

        private GameObject activeModel;
        private Animator activeAnimator;
        private int currentSkinId = -1;

        public void SetModel(int skinId);
        public void UpdateAnimation(int movementState);
        private void DisableAllModels();
    }
}
```

**Responsibilities:**
- Enable/disable character models in GFXs container based on skinId
- Cache active Animator reference for performance
- Update animator parameter based on movementState
- Handle invalid skinId gracefully (fallback to skinId 0)

**Key Implementation Details:**
- **SetModel(skinId):**
  - Disable all models in gfxsContainer.children
  - Enable model at index skinId
  - Cache Animator component reference
  - Validate skinId range (0-17)

- **UpdateAnimation(movementState):**
  - Set integer parameter on cached activeAnimator
  - Only update if animator reference is valid

---

### **Phase 5: Local Player Integration**

**LocalPlayer Changes:**

1. **Add PlayerModelManager component to LocalPlayer prefab**
2. **Assign gfxsContainer reference to GFXs GameObject**
3. **On spawn (in LocalPlayerNetworkSync):**
   - Pick random skinId (0 to 17)
   - Send skinId to server via "selectSkin" message
   - Call PlayerModelManager.SetModel(skinId)
4. **On state change (in PlayerController or new hook):**
   - When PlayerController.StateMachine changes state
   - Call PlayerModelManager.UpdateAnimation(movementState)

**Message to Server:**
```csharp
// In LocalPlayerNetworkSync.Start() or Initialize()
int randomSkin = Random.Range(0, 18); // 18 models (0-17)
room.Send("selectSkin", new { skinId = randomSkin });

// Get PlayerModelManager and set model
PlayerModelManager modelManager = GetComponent<PlayerModelManager>();
modelManager?.SetModel(randomSkin);
```

**Integration with State Changes:**
```csharp
// Option 1: In PlayerController state transitions
// Call modelManager.UpdateAnimation(GetMovementStateInt()) after state change

// Option 2: In PlayerStateMachine.ChangeState()
// Notify PlayerModelManager after successful state change
```

---

### **Phase 6: Remote Player Integration**

**RemotePlayer Changes:**

1. **Add PlayerModelManager component to RemotePlayer prefab**
2. **Assign gfxsContainer reference to GFXs GameObject**
3. **On spawn (in RemotePlayerNetworkSync.Initialize):**
   - Read skinId from PlayerState
   - Call PlayerModelManager.SetModel(playerState.skinId)
4. **On state update (in RemotePlayerNetworkSync.Update):**
   - Detect when movementState changes
   - Call PlayerModelManager.UpdateAnimation(playerState.movementState)

**Implementation in RemotePlayerNetworkSync:**
```csharp
private PlayerModelManager modelManager;
private int lastMovementState = -1;

public void Initialize(PlayerState state)
{
    playerState = state;
    modelManager = GetComponent<PlayerModelManager>();

    // Set initial model based on skinId
    modelManager?.SetModel(state.skinId);
}

void Update()
{
    // Existing position/rotation interpolation...

    // Check for movement state changes
    if (playerState.movementState != lastMovementState)
    {
        modelManager?.UpdateAnimation(playerState.movementState);
        lastMovementState = playerState.movementState;
    }
}
```

**Note:** Capsule renderer already disabled in prefabs, CharacterController remains active

---

### **Phase 7: Server Message Handler**

**ParkourRoom.ts:**

```typescript
this.onMessage("selectSkin", (client, message) => {
    const player = this.state.players.get(client.sessionId);
    if (player) {
        player.skinId = message.skinId;
        console.log(`Player ${client.sessionId} selected skin ${message.skinId}`);
    }
});
```

---

## 📋 **Implementation Checklist**

### **Phase 1: Schema Updates**
- [ ] Add `skinId` to server PlayerState.ts
- [ ] Add `skinId` to Unity PlayerState.cs (match index number!)
- [ ] Add "selectSkin" message handler on server
- [ ] Test server compiles and runs
- [ ] Verify schema field syncs properly

### **Phase 2: Prefab Hierarchy Verification**
- [✅] Verify LocalPlayer.prefab has GFXs GameObject with all models
- [✅] Verify RemotePlayer.prefab has GFXs GameObject with all models
- [ ] Verify all 18 models have Animator components
- [ ] Verify all Animators have AnimatorController assigned
- [ ] Verify all models have Humanoid rig configured
- [ ] Verify model scales and CharacterController compatibility

### **Phase 3: Animator Controller Setup**
- [ ] Create shared AnimatorController (or verify existing)
- [ ] Add `movementState` integer parameter (0-5)
- [ ] Create 6 animation states (Idle, Walk, Run, Jump, Fall, Slide)
- [ ] Configure transitions with movementState conditions
- [ ] Set transition settings (no exit time, 0.1-0.25s duration)
- [ ] Test in Animator window with manual parameter changes
- [ ] Assign controller to all 18 character model Animators

### **Phase 4: PlayerModelManager Implementation**
- [ ] Create PlayerModelManager.cs script in Scripts/Player/
- [ ] Implement SetModel(skinId) method (enable/disable logic)
- [ ] Implement UpdateAnimation(movementState) method
- [ ] Implement DisableAllModels() helper method
- [ ] Add skinId validation and fallback to 0
- [ ] Add null checks for animator reference

### **Phase 5: Local Player Integration**
- [ ] Add PlayerModelManager to LocalPlayer prefab
- [ ] Assign gfxsContainer reference to GFXs GameObject
- [ ] Modify LocalPlayerNetworkSync to pick random skinId
- [ ] Send "selectSkin" message to server on spawn
- [ ] Call SetModel(skinId) after sending message
- [ ] Hook UpdateAnimation() to PlayerController state changes
- [ ] Test with single player instance

### **Phase 6: Remote Player Integration**
- [ ] Add PlayerModelManager to RemotePlayer prefab
- [ ] Assign gfxsContainer reference to GFXs GameObject
- [ ] Modify RemotePlayerNetworkSync.Initialize() to read skinId
- [ ] Call SetModel(skinId) in Initialize()
- [ ] Add movementState change detection in Update()
- [ ] Call UpdateAnimation() when state changes
- [ ] Test with multi-client setup

### **Phase 7: Testing**
- [ ] Test local player model appears (random skin)
- [ ] Test animations play correctly for local player
- [ ] Test remote player shows correct synced skin
- [ ] Test animations sync across network
- [ ] Test all 6 movement states (Idle, Walk, Run, Jump, Fall, Slide)
- [ ] Test with 2+ clients with different skins
- [ ] Test skinId 0-17 all work correctly
- [ ] Test invalid skinId defaults to skinId 0
- [ ] Test disconnect/reconnect maintains skin consistency
- [ ] Test rapid state changes don't break animations

---

## 🎮 **Animation State Mapping**

| Movement State | State ID | Animation Clip |
|---------------|----------|----------------|
| IdleState | 0 | Idle |
| WalkState | 1 | Walk |
| RunState | 2 | Run |
| JumpState | 3 | Jump |
| FallState | 4 | Fall |
| SlideState | 5 | Slide |

---

## 🔧 **Technical Considerations**

### **Model Requirements:**
- **Rig Type:** Humanoid (for animation retargeting)
- **Scale:** Consistent across all models (or adjust CharacterController per model)
- **Pivot:** At feet (Y=0) for proper ground alignment
- **Animations:** All 6 states must exist for each model
- **Count:** 18 models total (character-a through character-r)

### **Performance:**
- **Memory:** All 18 models loaded with prefab (acceptable for prototype)
- **Only one model active per player:** Other 17 are disabled (SetActive false)
- **One Animator per player:** Only active model's animator updates
- **Animator updates:** Only when state changes (not every frame)
- **No instantiation overhead:** Just SetActive() toggle

### **Fallback:**
- If skinId invalid (< 0 or > 17): use default (skinId 0)
- If GFXs container missing: log error, keep capsule visible
- If animation missing: use Idle animation (animator handles this)
- If Animator missing on model: log warning, skip animation updates

---

## 🚀 **Optimization Ideas (Future)**

1. **Object Pooling:** Reuse model instances
2. **LOD:** Different detail levels based on distance
3. **Animation Culling:** Disable animator when off-screen
4. **Addressables:** Async model loading for large character libraries

---

## 📝 **Notes**

- **Capsule MeshRenderer:** Already disabled in both prefabs, keep for fallback/debugging
- **CharacterController:** Stays the same (physics doesn't change)
- **Camera target:** Stays the same (no changes needed)
- **Only visual representation changes:** Physics and controls remain identical
- **GFXs organization:** All 18 models pre-placed in hierarchy for easy switching
- **Shared AnimatorController:** Can use same controller for all models (Humanoid retargeting)
- **skinId range:** 0-17 (18 total models)

---

## ✅ **Success Criteria**

- ✅ Each player spawns with random character model
- ✅ Same skin appears on all clients (synced)
- ✅ Animations play based on movement state
- ✅ Local and remote players use same system
- ✅ Smooth transitions between animation states
- ✅ No animation glitches or T-poses

---

## 🎯 **Next Steps**

Ready to begin implementation in this order:

1. **Phase 1:** Schema Updates (server + Unity)
2. **Phase 3:** Animator Controller Setup (see [animator-controller-setup-guide.md](./animator-controller-setup-guide.md))
3. **Phase 4:** PlayerModelManager script implementation
4. **Phase 5:** Local Player integration
5. **Phase 6:** Remote Player integration
6. **Phase 7:** Testing and validation

---

## 📚 **Summary**

**Architecture Decision:** Pre-placed models with SetActive() switching (not runtime instantiation)

**Key Components:**
- **18 character models** (character-a through character-r)
- **GFXs GameObject** in both LocalPlayer and RemotePlayer prefabs
- **PlayerModelManager** script to handle model switching and animation updates
- **Shared AnimatorController** with movementState integer parameter (0-5)
- **Network sync** via skinId field in PlayerState schema

**Implementation Flow:**
1. Server stores skinId → syncs to all clients
2. Client enables model at GFXs.children[skinId]
3. Caches active Animator reference
4. Updates Animator.SetInteger("movementState", value) on state changes

**Benefits of This Approach:**
- ✅ Simple and straightforward
- ✅ No runtime instantiation overhead
- ✅ Editor-friendly prefab setup
- ✅ Easy to test and debug
- ✅ Reliable and performant

---

**Related Documentation:**
- [animator-controller-setup-guide.md](./animator-controller-setup-guide.md) - Detailed Animator setup
- [../player-controller/player-controller-design.md](../player-controller/player-controller-design.md) - State machine reference
- [../multiplayer/implementation-plan.md](../multiplayer/implementation-plan.md) - Network integration context
