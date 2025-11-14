# Player Models & Animations Design

**Date:** 2025-11-14
**Component:** Player Visual Representation
**Feature:** Character models with synced skins and state-driven animations

---

## 🎯 **Requirements**

1. **Multiple character model options** (skins)
2. **Random skin selection** when player joins
3. **Skin synced across network** (all clients see same skin)
4. **State-driven animations** (idle, walk, run, jump, fall, slide)
5. **Works for both local and remote players**

---

## 📊 **Architecture Overview**

### **Data Flow:**

```
Player Joins
    ↓
Client picks random skin ID (0-N)
    ↓
Send skin ID to server
    ↓
Server stores in PlayerState.skinId
    ↓
Other clients receive PlayerState
    ↓
Instantiate correct model based on skinId
    ↓
Animator updates based on movementState
```

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

### **Phase 2: Character Model Setup**

**Model Structure:**
```
Resources/Characters/
├── Character_0/
│   ├── Model.prefab (with Animator)
│   └── AnimatorController.controller
├── Character_1/
│   ├── Model.prefab
│   └── AnimatorController.controller
└── Character_2/
    ├── Model.prefab
    └── AnimatorController.controller
```

**Requirements for each model:**
- Humanoid rig (for animation retargeting)
- Animator component with AnimatorController
- All animations: Idle, Walk, Run, Jump, Fall, Slide
- Same scale/proportions (or adjust CharacterController)

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
        [SerializeField] private GameObject[] characterModels; // Assign in Resources
        [SerializeField] private Transform modelParent;

        private GameObject currentModel;
        private Animator animator;

        public void SetModel(int skinId);
        public void UpdateAnimation(int movementState);
    }
}
```

**Responsibilities:**
- Load and instantiate character model based on skinId
- Update animator parameter based on movementState
- Replace capsule visual with character model

---

### **Phase 5: Local Player Integration**

**LocalPlayer Changes:**

1. **Add PlayerModelManager component**
2. **On spawn:**
   - Pick random skinId (0 to modelCount-1)
   - Send skinId to server
   - Instantiate model
3. **On state change:**
   - PlayerController updates state
   - PlayerModelManager.UpdateAnimation(movementState)

**Message to Server:**
```csharp
// In LocalPlayerNetworkSync.Initialize()
int randomSkin = Random.Range(0, modelCount);
room.Send("selectSkin", new { skinId = randomSkin });
```

---

### **Phase 6: Remote Player Integration**

**RemotePlayer Changes:**

1. **Add PlayerModelManager component**
2. **On spawn:**
   - Read skinId from PlayerState
   - Instantiate correct model
3. **On state update:**
   - RemotePlayerNetworkSync detects movementState change
   - Update animator parameter

**Note:** Remove/disable capsule renderer, keep CharacterController

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
- [ ] Add `skinId` to Unity PlayerState.cs
- [ ] Add "selectSkin" message handler on server
- [ ] Test server compiles

### **Phase 2: Character Models Preparation**
- [ ] Organize character models in Resources/Characters/
- [ ] Verify all models have Humanoid rig
- [ ] Create AnimatorController for each model
- [ ] Set up Animator parameters (movementState: Int)
- [ ] Create animation states (Idle, Walk, Run, Jump, Fall, Slide)
- [ ] Configure transitions based on movementState

### **Phase 3: PlayerModelManager Implementation**
- [ ] Create PlayerModelManager.cs script
- [ ] Implement SetModel(skinId) method
- [ ] Implement UpdateAnimation(movementState) method
- [ ] Add character model array reference
- [ ] Test model instantiation in editor

### **Phase 4: Local Player Integration**
- [ ] Add PlayerModelManager to LocalPlayer prefab
- [ ] Assign character models array
- [ ] Send random skinId to server on join
- [ ] Instantiate model based on skinId
- [ ] Update animator when PlayerController state changes
- [ ] Disable/remove capsule visual

### **Phase 5: Remote Player Integration**
- [ ] Add PlayerModelManager to RemotePlayer prefab
- [ ] Read skinId from PlayerState on spawn
- [ ] Instantiate correct model
- [ ] Update animator when movementState changes
- [ ] Remove color-based visual feedback (optional)

### **Phase 6: Testing**
- [ ] Test local player model appears
- [ ] Test animations play correctly
- [ ] Test remote player shows correct skin
- [ ] Test animations sync across network
- [ ] Test all 6 movement states
- [ ] Test with multiple different skins

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

### **Performance:**
- Models loaded from Resources (not Addressables for prototype)
- One Animator per player
- Animator updates only when state changes (not every frame)

### **Fallback:**
- If skinId invalid or model missing: use default (skinId 0)
- If animation missing: use Idle animation

---

## 🚀 **Optimization Ideas (Future)**

1. **Object Pooling:** Reuse model instances
2. **LOD:** Different detail levels based on distance
3. **Animation Culling:** Disable animator when off-screen
4. **Addressables:** Async model loading for large character libraries

---

## 📝 **Notes**

- Keep capsule MeshRenderer for fallback/debugging
- CharacterController stays the same (physics doesn't change)
- Camera target stays the same
- Only visual representation changes

---

## ✅ **Success Criteria**

- ✅ Each player spawns with random character model
- ✅ Same skin appears on all clients (synced)
- ✅ Animations play based on movement state
- ✅ Local and remote players use same system
- ✅ Smooth transitions between animation states
- ✅ No animation glitches or T-poses

---

## 🎯 **Next Step**

Ready to begin **Phase 1: Schema Updates**

Update server and Unity schemas to include `skinId` field!
