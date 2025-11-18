# Joystick Integration - Implementation Plan

**Date:** 2025-11-19
**Component:** Player Input System Enhancement
**Design Docs:**
- [joystick-integration-design.md](./joystick-integration-design.md)
- [mobile-detection-design.md](./mobile-detection-design.md)

---

## 📋 Implementation Checklist

### Phase 1: Core Input Merging ⚡ CRITICAL PATH

#### Task 1.1: Modify PlayerInputHandler
**File:** `Scripts/Player/PlayerInputHandler.cs`
**Estimated Complexity:** Medium

- [ ] Add joystick reference field
  ```csharp
  private VariableJoystick variableJoystick;
  ```

- [ ] Add UI button state fields
  ```csharp
  private bool runButtonHeld = false;
  private bool jumpButtonPressed = false;
  private bool slideButtonPressed = false;
  ```

- [ ] Modify constructor to accept joystick parameter
  ```csharp
  public PlayerInputHandler(VariableJoystick joystick = null)
  {
      variableJoystick = joystick;
  }
  ```

- [ ] Modify Update() method - movement input merging
  ```csharp
  // Get keyboard input
  Vector2 keyboardInput = new Vector2(
      Input.GetAxis("Horizontal"),
      Input.GetAxis("Vertical")
  );

  // Get joystick input (if available)
  Vector2 joystickInput = Vector2.zero;
  if (variableJoystick != null)
  {
      joystickInput = new Vector2(
          variableJoystick.Horizontal,
          variableJoystick.Vertical
      );
  }

  // Merge: prioritize input with greater magnitude
  movementInput = (joystickInput.magnitude > keyboardInput.magnitude)
      ? joystickInput
      : keyboardInput;
  ```

- [ ] Modify Update() method - action input merging
  ```csharp
  isRunning = Input.GetKey(KeyCode.LeftShift) || runButtonHeld;

  jumpPressed = Input.GetKeyDown(KeyCode.Space) || jumpButtonPressed;
  if (jumpButtonPressed) jumpButtonPressed = false;

  slidePressed = Input.GetKeyDown(KeyCode.C) ||
                 Input.GetKeyDown(KeyCode.LeftControl) ||
                 slideButtonPressed;
  if (slideButtonPressed) slideButtonPressed = false;
  ```

- [ ] Add public methods for UI buttons
  ```csharp
  public void SetRunButton(bool held)
  {
      runButtonHeld = held;
  }

  public void PressJumpButton()
  {
      jumpButtonPressed = true;
  }

  public void PressSlideButton()
  {
      slideButtonPressed = true;
  }
  ```

**Testing:**
- [ ] Compile and verify no errors
- [ ] Test keyboard-only input (regression test)

---

#### Task 1.2: Modify PlayerController
**File:** `Scripts/Player/PlayerController.cs`
**Estimated Complexity:** Low

- [ ] Add joystick reference field
  ```csharp
  [Header("Input References")]
  [SerializeField] private VariableJoystick variableJoystick;
  ```

- [ ] Modify Awake() method
  ```csharp
  private void Awake()
  {
      characterController = GetComponent<CharacterController>();

      // Pass joystick to InputHandler constructor
      inputHandler = new PlayerInputHandler(variableJoystick);

      physics = new PlayerPhysics(gravity, groundCheckDistance, groundLayer);
      stateMachine = new PlayerStateMachine();

      InitializeStates();
  }
  ```

- [ ] Verify InputHandler property is public (already exists)
  ```csharp
  public PlayerInputHandler InputHandler => inputHandler;
  ```

**Testing:**
- [ ] Compile and verify no errors
- [ ] Test in Unity Editor (keyboard-only, no joystick assigned)
- [ ] Verify all states still work correctly

---

### Phase 2: UI Button Support 📱 MOBILE ESSENTIAL

#### Task 2.1: Create MobileInputButtons Component
**File:** `Scripts/UI/MobileInputButtons.cs` ➕ NEW
**Estimated Complexity:** Medium

- [ ] Create new script file in `Scripts/UI/` directory

- [ ] Implement component structure
  ```csharp
  using UnityEngine;
  using UnityEngine.EventSystems;
  using UnityEngine.UI;

  namespace ParkourLegion.UI
  {
      public class MobileInputButtons : MonoBehaviour
      {
          [Header("References")]
          [SerializeField] private Button jumpButton;
          [SerializeField] private Button slideButton;
          [SerializeField] private Button runButton;

          private Player.PlayerInputHandler inputHandler;

          private void Start()
          {
              FindInputHandler();
              SetupButtons();
          }

          // Implementation here...
      }
  }
  ```

- [ ] Implement FindInputHandler() method
  ```csharp
  private void FindInputHandler()
  {
      var localPlayer = GameObject.Find("LocalPlayer");
      if (localPlayer != null)
      {
          var controller = localPlayer.GetComponent<Player.PlayerController>();
          if (controller != null)
          {
              inputHandler = controller.InputHandler;
              Debug.Log("MobileInputButtons: Found InputHandler");
          }
          else
          {
              Debug.LogWarning("MobileInputButtons: PlayerController not found on LocalPlayer");
          }
      }
      else
      {
          Debug.LogWarning("MobileInputButtons: LocalPlayer not found. Buttons will not work.");
      }
  }
  ```

- [ ] Implement SetupButtons() method
  ```csharp
  private void SetupButtons()
  {
      if (inputHandler == null)
      {
          Debug.LogWarning("MobileInputButtons: InputHandler is null, cannot setup buttons");
          return;
      }

      if (jumpButton != null)
      {
          AddEventTrigger(jumpButton.gameObject, EventTriggerType.PointerDown,
              (data) => inputHandler.PressJumpButton());
      }

      if (slideButton != null)
      {
          AddEventTrigger(slideButton.gameObject, EventTriggerType.PointerDown,
              (data) => inputHandler.PressSlideButton());
      }

      if (runButton != null)
      {
          AddEventTrigger(runButton.gameObject, EventTriggerType.PointerDown,
              (data) => inputHandler.SetRunButton(true));
          AddEventTrigger(runButton.gameObject, EventTriggerType.PointerUp,
              (data) => inputHandler.SetRunButton(false));
      }

      Debug.Log("MobileInputButtons: Buttons setup complete");
  }
  ```

- [ ] Implement AddEventTrigger() helper method
  ```csharp
  private void AddEventTrigger(GameObject target, EventTriggerType eventType,
      System.Action<BaseEventData> callback)
  {
      EventTrigger trigger = target.GetComponent<EventTrigger>();
      if (trigger == null)
      {
          trigger = target.AddComponent<EventTrigger>();
      }

      EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
      entry.callback.AddListener((data) => callback(data));
      trigger.triggers.Add(entry);
  }
  ```

**Testing:**
- [ ] Compile and verify no errors
- [ ] Script visible in Unity Inspector

---

### Phase 3: Unity Setup 🎨 INTEGRATION

#### Task 3.1: Setup VariableJoystick in Canvas
**Location:** Scene Canvas
**Estimated Time:** 10 minutes

- [ ] Open main scene (Scenes/main.unity)

- [ ] Locate or create Canvas for mobile controls
  - If no dedicated mobile canvas exists, create new Canvas: "MobileControlsCanvas"
  - Canvas settings: Screen Space - Overlay, Sort Order: 100

- [ ] Add VariableJoystick to Canvas
  - Navigate to `Assets/Joystick Pack/Prefabs/`
  - Drag "Variable Joystick" prefab into Canvas

- [ ] Position VariableJoystick
  - Anchor: Bottom-Left
  - Position: X = 150, Y = 150 (adjust as needed)
  - Size: 200x200 (adjust as needed)

- [ ] Configure VariableJoystick settings
  - Joystick Type: **Floating** (recommended) or Dynamic
  - Handle Range: 1
  - Dead Zone: 0.1
  - Axis Options: Both

- [ ] Test joystick in Play mode
  - [ ] Joystick appears in scene
  - [ ] Handle moves when dragged
  - [ ] Joystick resets on release

**Screenshot:** Take screenshot of final joystick positioning for reference

---

#### Task 3.2: Create Action Buttons UI
**Location:** Scene Canvas
**Estimated Time:** 20 minutes

- [ ] Create Jump Button
  - Create UI Button in Canvas: "JumpButton"
  - Position: Bottom-Right area (e.g., X = -150, Y = 150)
  - Size: 100x100
  - Text: "JUMP" or use icon sprite
  - Add EventTrigger component (will be added by script)

- [ ] Create Run Button
  - Create UI Button in Canvas: "RunButton"
  - Position: Near Jump button (e.g., X = -280, Y = 150)
  - Size: 100x100
  - Text: "RUN" or use icon sprite
  - Add EventTrigger component (will be added by script)

- [ ] Create Slide Button
  - Create UI Button in Canvas: "SlideButton"
  - Position: Near Jump button (e.g., X = -150, Y = 280)
  - Size: 100x100
  - Text: "SLIDE" or use icon sprite
  - Add EventTrigger component (will be added by script)

- [ ] Create MobileControls container (optional but recommended)
  - Create Empty GameObject in Canvas: "MobileControls"
  - Move VariableJoystick and all buttons under MobileControls
  - This allows easy enable/disable of all mobile controls

- [ ] Style buttons (optional)
  - Adjust colors, add icons, set fonts
  - Add button press animations (Color Tint)

**Layout Reference:**
```
Canvas
├─ MobileControls
│  ├─ VariableJoystick (Bottom-Left)
│  ├─ RunButton (Bottom-Right, lower)
│  ├─ SlideButton (Bottom-Right, middle)
│  └─ JumpButton (Bottom-Right, upper)
```

---

#### Task 3.3: Setup MobileInputButtons Component
**Location:** Scene Canvas
**Estimated Time:** 5 minutes

- [ ] Add MobileInputButtons component to Canvas or MobileControls GameObject
  - Select Canvas (or MobileControls)
  - Add Component → Scripts → UI → MobileInputButtons

- [ ] Assign button references in Inspector
  - Jump Button: Drag JumpButton GameObject
  - Slide Button: Drag SlideButton GameObject
  - Run Button: Drag RunButton GameObject

- [ ] Test button references
  - [ ] All references assigned (not null)
  - [ ] No missing reference warnings

---

#### Task 3.4: Assign VariableJoystick to LocalPlayer Prefab
**Location:** Prefabs/LocalPlayer.prefab
**Estimated Time:** 5 minutes

- [ ] Open LocalPlayer prefab in Unity

- [ ] Select PlayerController component

- [ ] Assign VariableJoystick reference
  - **Method 1 (Scene Reference):**
    - In scene hierarchy, find VariableJoystick
    - Drag VariableJoystick to PlayerController's "Variable Joystick" field
    - **Note:** This creates a scene reference, not prefab reference
    - LocalPlayer must be instantiated after Canvas is created

  - **Method 2 (FindObjectOfType - Recommended for Runtime Spawn):**
    - Leave VariableJoystick field empty in prefab
    - Modify NetworkManager.SpawnLocalPlayer() to find and assign joystick:
    ```csharp
    localPlayer = Instantiate(localPlayerPrefab, spawnPosition, Quaternion.identity);

    // Find and assign joystick reference
    var joystick = FindObjectOfType<VariableJoystick>();
    var playerController = localPlayer.GetComponent<Player.PlayerController>();
    if (playerController != null && joystick != null)
    {
        // Use reflection or add SetJoystick() method
    }
    ```

  - **Method 3 (Public Setter - RECOMMENDED):**
    - Add public method to PlayerController:
    ```csharp
    public void SetJoystick(VariableJoystick joystick)
    {
        inputHandler = new PlayerInputHandler(joystick);
    }
    ```
    - Call from NetworkManager after spawning

- [ ] Choose implementation method and document decision

**Recommendation:** Use Method 3 (Public Setter) for clean separation and testability

---

#### Task 3.5: Integrate Joystick Assignment in NetworkManager
**File:** `Scripts/Networking/NetworkManager.cs`
**Estimated Complexity:** Low

**Only required if using Method 2 or Method 3 from Task 3.4**

- [ ] Modify SpawnLocalPlayer() method
  ```csharp
  private void SpawnLocalPlayer()
  {
      if (localPlayerPrefab == null)
      {
          Debug.LogError("Local player prefab not assigned!");
          return;
      }

      localPlayer = Instantiate(localPlayerPrefab, spawnPosition, Quaternion.identity);
      localPlayer.name = "LocalPlayer";

      // --- NEW: Assign joystick reference ---
      var joystick = FindObjectOfType<VariableJoystick>();
      var playerController = localPlayer.GetComponent<Player.PlayerController>();

      if (playerController != null && joystick != null)
      {
          playerController.SetJoystick(joystick);
          Debug.Log("NetworkManager: Assigned VariableJoystick to LocalPlayer");
      }
      else if (joystick == null)
      {
          Debug.LogWarning("NetworkManager: VariableJoystick not found in scene (keyboard-only mode)");
      }
      // --- END NEW ---

      LocalPlayerNetworkSync networkSync = localPlayer.GetComponent<LocalPlayerNetworkSync>();
      if (networkSync != null)
      {
          networkSync.Initialize(room);
      }
      else
      {
          Debug.LogWarning("LocalPlayerNetworkSync component not found on local player prefab");
      }

      SetupCameraTarget();
  }
  ```

- [ ] Add SetJoystick() method to PlayerController (if using Method 3)
  ```csharp
  public void SetJoystick(VariableJoystick joystick)
  {
      inputHandler = new PlayerInputHandler(joystick);
      Debug.Log($"PlayerController: Joystick assigned - {(joystick != null ? "Active" : "None")}");
  }
  ```

**Testing:**
- [ ] Spawn LocalPlayer in scene
- [ ] Verify joystick is assigned correctly
- [ ] Check debug logs for confirmation

---

### Phase 3.5: Mobile Detection & Canvas Visibility 📱 WEBGL ESSENTIAL

**Design Doc:** [mobile-detection-design.md](./mobile-detection-design.md)

#### Task 3.6: Create Folder Structure
**Estimated Time:** 2 minutes

- [ ] Create `Assets/Plugins/` folder (if not exists)
- [ ] Create `Assets/Plugins/WebGL/` folder
- [ ] Create `Assets/_0_Custom/Scripts/Utilities/` folder

---

#### Task 3.7: Create JavaScript Plugin
**File:** `Assets/Plugins/WebGL/MobileBrowserDetector.jslib` ➕ NEW
**Estimated Complexity:** Low

- [ ] Create new file: `MobileBrowserDetector.jslib`

- [ ] Implement JavaScript detection code
  ```javascript
  mergeInto(LibraryManager.library, {
      IsMobileBrowser: function() {
          // Use Unity's WebGL Module.SystemInfo.mobile (Unity 2020.3+)
          if (typeof Module !== 'undefined' &&
              typeof Module.SystemInfo !== 'undefined') {
              return Module.SystemInfo.mobile ? 1 : 0;
          }

          // Fallback: Manual user agent detection
          var userAgent = navigator.userAgent || navigator.vendor || window.opera;

          // Check for common mobile patterns
          var mobileRegex = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i;
          var isTablet = /ipad|android(?!.*mobile)|tablet/i.test(userAgent);
          var isMobile = mobileRegex.test(userAgent);

          return (isMobile || isTablet) ? 1 : 0;
      }
  });
  ```

- [ ] Save file in `Assets/Plugins/WebGL/` folder

**Testing:**
- [ ] Verify Unity recognizes plugin (no compile errors)
- [ ] Check Unity console for plugin compilation

---

#### Task 3.8: Create C# Wrapper Utility
**File:** `Assets/_0_Custom/Scripts/Utilities/MobileBrowserDetector.cs` ➕ NEW
**Estimated Complexity:** Low

- [ ] Create new file: `MobileBrowserDetector.cs`

- [ ] Implement static utility class
  ```csharp
  using UnityEngine;
  #if !UNITY_EDITOR && UNITY_WEBGL
  using System.Runtime.InteropServices;
  #endif

  namespace ParkourLegion.Utilities
  {
      public static class MobileBrowserDetector
      {
  #if !UNITY_EDITOR && UNITY_WEBGL
          [DllImport("__Internal")]
          private static extern int IsMobileBrowser();
  #endif

          public static bool IsMobile()
          {
  #if UNITY_EDITOR
              // In Editor: Return false by default
              return false;
  #elif UNITY_WEBGL
              // In WebGL build: Call JavaScript plugin
              try
              {
                  int result = IsMobileBrowser();
                  return result == 1;
              }
              catch (System.Exception e)
              {
                  Debug.LogWarning($"MobileBrowserDetector: Failed to detect mobile - {e.Message}");
                  return false; // Fallback to desktop
              }
  #else
              // Non-WebGL platforms
              return Application.isMobilePlatform;
  #endif
          }

          public static string GetDeviceInfo()
          {
  #if UNITY_EDITOR
              return "Editor (Desktop)";
  #elif UNITY_WEBGL
              bool isMobile = IsMobile();
              return isMobile ? "WebGL (Mobile Browser)" : "WebGL (Desktop Browser)";
  #else
              return $"{Application.platform}";
  #endif
          }
      }
  }
  ```

- [ ] Save file in `Assets/_0_Custom/Scripts/Utilities/` folder

**Testing:**
- [ ] Compile and verify no errors
- [ ] Verify namespace: `ParkourLegion.Utilities`

---

#### Task 3.9: Setup Mobile Controls Canvas
**Location:** Scene hierarchy
**Estimated Time:** 10 minutes

- [ ] Create new Canvas GameObject in scene
  - Name: "MobileControlsCanvas"
  - Render Mode: Screen Space - Overlay
  - Sort Order: 100 (renders on top)

- [ ] Move mobile controls under MobileControlsCanvas
  - [ ] Move VariableJoystick
  - [ ] Move JumpButton
  - [ ] Move RunButton
  - [ ] Move SlideButton

- [ ] Verify hierarchy structure
  ```
  Canvas (Main UI)
  ├─ MenuUI
  ├─ LobbyUI
  └─ ... existing UI

  MobileControlsCanvas ← New separate canvas
  ├─ VariableJoystick
  ├─ JumpButton
  ├─ RunButton
  └─ SlideButton
  ```

- [ ] Save scene

**Testing:**
- [ ] All mobile controls visible in scene view
- [ ] Canvas renders correctly in play mode

---

#### Task 3.10: Modify GameUIManager for Canvas Visibility
**File:** `Scripts/UI/GameUIManager.cs` ✏️ MODIFY
**Estimated Complexity:** Medium

- [ ] Add using statement
  ```csharp
  using ParkourLegion.Utilities;
  ```

- [ ] Add fields to Mobile Controls header section
  ```csharp
  [Header("Mobile Controls")]
  [SerializeField] private Canvas mobileControlsCanvas;
  [SerializeField] private bool editorSimulateMobile = false;
  ```

- [ ] Add SetupMobileControlsVisibility() method
  ```csharp
  private void SetupMobileControlsVisibility()
  {
      if (mobileControlsCanvas == null)
      {
          Debug.LogWarning("GameUIManager: Mobile Controls Canvas not assigned!");
          return;
      }

      bool isMobile = false;

  #if UNITY_EDITOR
      // In Editor: Use inspector toggle for testing
      isMobile = editorSimulateMobile;
      Debug.Log($"GameUIManager: Editor mode - Simulate Mobile = {isMobile}");
  #else
      // In Build: Use actual detection
      isMobile = MobileBrowserDetector.IsMobile();
      Debug.Log($"GameUIManager: Detected platform - {MobileBrowserDetector.GetDeviceInfo()}");
  #endif

      mobileControlsCanvas.gameObject.SetActive(isMobile);

      Debug.Log($"GameUIManager: Mobile Controls Canvas {(isMobile ? "ENABLED" : "DISABLED")}");
  }
  ```

- [ ] Call method in Start() - add after InitializeSkinSelection()
  ```csharp
  private void Start()
  {
      menuUI = FindObjectOfType<MenuUI>();
      lobbyUI = FindObjectOfType<LobbyUI>();
      clickToResumeOverlay = FindObjectOfType<ClickToResumeOverlay>();

      // ... existing code ...

      InitializeSkinSelection();

      // NEW: Setup mobile controls visibility
      SetupMobileControlsVisibility();

      SetState(GameState.Menu);
  }
  ```

**Testing:**
- [ ] Compile and verify no errors
- [ ] Script visible in Unity Inspector

---

#### Task 3.11: Assign Canvas Reference in Inspector
**Location:** Unity Editor
**Estimated Time:** 2 minutes

- [ ] Select GameUIManager GameObject in scene
- [ ] Locate "Mobile Controls" section in Inspector
- [ ] Assign MobileControlsCanvas to "Mobile Controls Canvas" field
- [ ] Keep "Editor Simulate Mobile" unchecked (for desktop testing)
- [ ] Save scene

**Testing:**
- [ ] Reference assigned (not null)
- [ ] No missing reference warnings

---

#### Task 3.12: Test Mobile Detection in Editor
**Environment:** Unity Editor
**Estimated Time:** 5 minutes

- [ ] **Test 1: Desktop mode (default)**
  - editorSimulateMobile = false
  - Play scene
  - Expected: Mobile controls canvas HIDDEN
  - Check console: "Editor mode - Simulate Mobile = False"
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **Test 2: Mobile simulation**
  - editorSimulateMobile = true
  - Play scene
  - Expected: Mobile controls canvas VISIBLE
  - Check console: "Editor mode - Simulate Mobile = True"
  - Verify joystick and buttons visible
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **Test 3: Canvas reference missing**
  - Remove canvas reference
  - Play scene
  - Expected: Warning logged, no crash
  - Restore reference
  - Status: ⬜ Pass / ⬜ Fail

---

### Phase 4: Testing & Validation ✅ QUALITY ASSURANCE

#### Task 4.1: Regression Testing (Keyboard-Only)
**Environment:** Unity Editor, Desktop build
**Goal:** Ensure existing functionality is unaffected

- [ ] **TC1.1: Movement with WASD**
  - Press W/A/S/D keys
  - Expected: Character moves in respective directions
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC1.2: Running with Shift**
  - Hold Shift + WASD
  - Expected: Character runs at faster speed
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC1.3: Jumping with Space**
  - Press Space while grounded
  - Expected: Character jumps
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC1.4: Sliding with C**
  - Press C while moving
  - Expected: Character slides
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC1.5: State transitions**
  - Move between Idle → Walk → Run → Jump → Fall states
  - Expected: Smooth transitions, no stuck states
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC1.6: Multiplayer sync (keyboard)**
  - Join room with 2 players, both using keyboard
  - Expected: Remote player movement syncs correctly
  - Status: ⬜ Pass / ⬜ Fail

**If any fail:** STOP and debug before proceeding

---

#### Task 4.2: Joystick-Only Testing
**Environment:** Unity Editor (simulate touch) or mobile build
**Goal:** Verify joystick input works correctly

- [ ] **TC2.1: Movement with joystick**
  - Drag joystick in all directions (up/down/left/right/diagonals)
  - Expected: Character moves in joystick direction
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC2.2: Movement magnitude**
  - Move joystick to center (slight drag)
  - Move joystick to edge (full drag)
  - Expected: Character speed varies with joystick distance from center
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC2.3: Joystick dead zone**
  - Barely touch joystick (within dead zone)
  - Expected: Character remains idle (no movement)
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC2.4: Jump button**
  - Press Jump UI button while grounded
  - Expected: Character jumps
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC2.5: Run button (hold)**
  - Hold Run button + move joystick
  - Release Run button while moving
  - Expected: Character runs while held, walks when released
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC2.6: Slide button**
  - Press Slide button while moving with joystick
  - Expected: Character slides
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC2.7: Combined actions**
  - Move joystick + press Jump button → Jump while moving
  - Hold Run button + move joystick + press Jump → Run-jump
  - Expected: All action combinations work
  - Status: ⬜ Pass / ⬜ Fail

**If any fail:** Debug joystick integration

---

#### Task 4.3: Hybrid Input Testing (Keyboard + Joystick)
**Environment:** Unity Editor (both inputs available)
**Goal:** Verify input priority and seamless switching

- [ ] **TC3.1: Keyboard → Joystick switch**
  - Move with WASD (character moving)
  - While moving, drag joystick
  - Expected: Character smoothly follows joystick direction
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC3.2: Joystick → Keyboard switch**
  - Move with joystick (character moving)
  - While moving, press WASD
  - Expected: Character continues with keyboard input (if keyboard magnitude > joystick)
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC3.3: Joystick priority (magnitude test)**
  - Lightly press W key (small input)
  - Fully drag joystick (large input)
  - Expected: Character follows joystick (larger magnitude)
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC3.4: Action button combinations**
  - Press keyboard Space AND Jump button simultaneously → Jump once
  - Hold keyboard Shift AND Run button simultaneously → Run state
  - Expected: No double-actions, clean OR logic
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC3.5: Simultaneous opposite inputs**
  - Press W + move joystick backward
  - Expected: Larger magnitude input wins, no cancellation
  - Status: ⬜ Pass / ⬜ Fail

**If any fail:** Review magnitude priority logic

---

#### Task 4.4: WebGL Mobile Detection Testing
**Environment:** WebGL build (desktop + mobile browsers)
**Goal:** Verify mobile detection and canvas visibility

- [ ] **TC4.1: Desktop browser detection**
  - Build WebGL and open in Chrome/Firefox/Edge (desktop)
  - Expected: Mobile controls canvas HIDDEN, keyboard works
  - Check console: "WebGL (Desktop Browser)"
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC4.2: Mobile browser detection (phone)**
  - Open WebGL build in mobile browser (iOS Safari / Android Chrome)
  - Expected: Mobile controls canvas VISIBLE, touch controls work
  - Check console: "WebGL (Mobile Browser)"
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC4.3: Tablet browser detection**
  - Open WebGL build on iPad or Android tablet
  - Expected: Mobile controls canvas VISIBLE
  - Check console: "WebGL (Mobile Browser)"
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC4.4: JavaScript plugin working**
  - Check browser console for Unity logs
  - Verify IsMobileBrowser() executed successfully
  - Expected: No JavaScript errors
  - Status: ⬜ Pass / ⬜ Fail

**If any fail:** Debug JavaScript plugin or C# integration

---

#### Task 4.5: Mobile Platform Testing
**Environment:** WebGL mobile browser
**Goal:** Verify mobile-specific behavior

- [ ] **TC5.1: Touch input responsiveness**
  - Drag joystick with touch
  - Tap action buttons with touch
  - Expected: No lag, responsive controls
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC5.2: Multi-touch support**
  - Hold joystick + tap Jump button simultaneously
  - Hold Run button + drag joystick simultaneously
  - Expected: Both touches register correctly
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC5.3: UI visibility**
  - Check mobile controls visibility on different screen sizes
  - Expected: Controls visible and accessible on all devices
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC5.4: Performance**
  - Monitor FPS while using joystick controls
  - Expected: No performance degradation vs keyboard input
  - Status: ⬜ Pass / ⬜ Fail

**If any fail:** Optimize mobile controls or touch handling

---

#### Task 4.6: Network Sync Testing
**Environment:** Multiplayer session (2+ players)
**Goal:** Verify input method doesn't affect network synchronization

- [ ] **TC6.1: Cross-platform sync**
  - Player 1: Keyboard input
  - Player 2: Joystick input
  - Expected: Both players see each other's movements correctly
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC6.2: Action sync**
  - Player 1: Jump with keyboard Space
  - Player 2: Jump with UI button
  - Expected: Both jumps visible to all players
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC6.3: State sync**
  - Trigger all states (Idle/Walk/Run/Jump/Fall/Slide) with joystick
  - Expected: Remote player shows correct state
  - Status: ⬜ Pass / ⬜ Fail

**If any fail:** Check LocalPlayerNetworkSync (should be unaffected)

---

#### Task 4.7: Edge Case Testing
**Environment:** Various scenarios
**Goal:** Verify robustness and error handling

- [ ] **TC7.1: Missing joystick reference**
  - Remove VariableJoystick assignment from PlayerController
  - Test gameplay
  - Expected: Falls back to keyboard-only, no errors
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC7.2: Missing button references**
  - Remove button assignments from MobileInputButtons
  - Test gameplay
  - Expected: No errors, buttons simply don't work
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC7.3: LocalPlayer spawned before Canvas**
  - Modify spawn order (LocalPlayer before Canvas exists)
  - Expected: Joystick reference null, keyboard works, no crash
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC7.4: Rapid button spam**
  - Rapidly tap Jump/Slide buttons (10+ times/second)
  - Expected: Actions trigger correctly, no state machine errors
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC7.5: Joystick during countdown**
  - Use joystick input during lobby countdown (movement disabled)
  - Expected: No movement, no errors
  - Status: ⬜ Pass / ⬜ Fail

- [ ] **TC7.6: Canvas reference missing (mobile detection)**
  - Remove mobileControlsCanvas assignment in GameUIManager
  - Play scene
  - Expected: Warning logged, no crash, game continues
  - Status: ⬜ Pass / ⬜ Fail

**If any fail:** Improve error handling or add null checks

---

### Phase 5: Documentation & Cleanup 📚 FINAL

#### Task 5.1: Update Project Documentation

- [ ] Update project-overview.md
  - Add joystick integration to features list
  - Update input controls section
  - Add mobile support mention

- [ ] Update player-controller design docs
  - Reference joystick integration design
  - Update input handling architecture diagrams

- [ ] Create joystick setup guide (if needed)
  - Quick start for adding joystick to new scenes
  - Troubleshooting common issues

---

#### Task 5.2: Code Cleanup

- [ ] Remove debug logs (if any added during development)
- [ ] Add XML documentation comments to new methods
- [ ] Verify code follows project style guide (CLAUDE.md)
- [ ] Check for unused using statements
- [ ] Verify namespace consistency

---

#### Task 5.3: Final Verification

- [ ] Run all test cases one final time
- [ ] Create build (WebGL or Android)
- [ ] Test build on actual device (if possible)
- [ ] Document any known limitations or future enhancements

---

## ⚠️ Known Limitations & Future Enhancements

### Current Limitations
- ❌ No camera control on mobile (CameraInputProvider is mouse-only)
- ❌ No gyroscope/accelerometer support
- ❌ No haptic feedback on button press
- ❌ No on-screen button customization (fixed positions)

### Future Enhancements
- 🔮 Add second joystick for camera rotation (right side)
- 🔮 Add swipe gestures for camera control
- 🔮 Add button position customization UI
- 🔮 Add platform-specific auto-show/hide (desktop vs mobile)
- 🔮 Add haptic feedback support for mobile
- 🔮 Add button size scaling for different screen sizes
- 🔮 Add gyroscope-based camera control option

---

## 🎯 Success Criteria

### Must Have (Phase 1-3)
- ✅ Keyboard input works exactly as before (zero regression)
- ✅ Joystick input provides full movement control
- ✅ UI buttons provide all actions (jump/run/slide)
- ✅ Hybrid input works with magnitude-based priority
- ✅ Network sync unaffected by input method
- ✅ No runtime errors or null references

### Should Have (Phase 4)
- ✅ All test cases pass (TC1-TC6)
- ✅ Mobile build tested and verified
- ✅ Edge cases handled gracefully
- ✅ Performance is acceptable on mobile

### Nice to Have (Phase 5)
- ✅ Documentation updated
- ✅ Code cleaned and polished
- ✅ Setup guide created

---

## 📊 Estimated Timeline

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| Phase 1: Core Input Merging | 1.1 - 1.2 | 1-2 hours |
| Phase 2: UI Button Support | 2.1 | 1 hour |
| Phase 3: Unity Setup | 3.1 - 3.5 | 1-2 hours |
| Phase 3.5: Mobile Detection | 3.6 - 3.12 | 1-1.5 hours |
| Phase 4: Testing & Validation | 4.1 - 4.7 | 3-4 hours |
| Phase 5: Documentation & Cleanup | 5.1 - 5.3 | 1 hour |
| **Total** | | **8-11.5 hours** |

**Note:** Timeline assumes familiarity with Unity and the existing codebase. Add buffer time for debugging and iteration.

**Key Additions:**
- Phase 3.5 adds mobile detection system (JavaScript plugin + C# wrapper + GameUIManager integration)
- Phase 4 expanded with WebGL-specific testing (desktop browser, mobile browser, tablet)

---

## 🚀 Quick Start Checklist (TL;DR)

For developers who just need the essentials:

1. ✅ Modify `PlayerInputHandler.cs` - merge keyboard + joystick inputs
2. ✅ Modify `PlayerController.cs` - add joystick reference field
3. ✅ Create `MobileInputButtons.cs` - handle UI button events
4. ✅ Add VariableJoystick to Canvas (from Joystick Pack prefabs)
5. ✅ Create Jump/Run/Slide UI buttons
6. ✅ Assign joystick to PlayerController (via NetworkManager or scene reference)
7. ✅ Test keyboard-only (regression)
8. ✅ Test joystick-only (new functionality)
9. ✅ Test hybrid input (both at same time)
10. ✅ Test in multiplayer session

---

**Document Version:** 1.0
**Status:** Ready for Implementation
**Design Doc:** [joystick-integration-design.md](./joystick-integration-design.md)
**Last Updated:** 2025-11-19
