# Mobile Detection & Canvas Visibility Design

**Date:** 2025-11-19
**Component:** Mobile Controls Visibility System
**Parent Design:** [joystick-integration-design.md](./joystick-integration-design.md)

---

## 📋 Overview

Implement automatic mobile controls canvas visibility management in **GameUIManager**. The mobile controls canvas (containing VariableJoystick and action buttons) should only be visible when running on mobile devices (phones/tablets via WebGL), and hidden on desktop browsers.

---

## 🎯 Design Goals

### Primary Goals
- ✅ Auto-detect mobile vs desktop browsers in WebGL builds
- ✅ Show mobile controls canvas ONLY on mobile devices
- ✅ Hide mobile controls canvas on desktop browsers
- ✅ Support editor testing (manual toggle or simulate mobile)
- ✅ Use reliable JavaScript plugin for detection (not Application.isMobilePlatform)

### Non-Goals
- ❌ Platform-specific builds (Android/iOS APK detection - not needed)
- ❌ Runtime user toggle (future enhancement)
- ❌ Per-device custom layouts

---

## 🔍 Research Summary: WebGL Mobile Detection

### Unity Built-in Methods (UNRELIABLE for WebGL)
Based on official Unity documentation and community research:

**❌ Application.isMobilePlatform**
- **Status:** UNRELIABLE for WebGL
- **Issue:** "Due to privacy and anonymization reasons, this property might not always report accurate information on all web browsers" (Unity Docs)
- **Behavior:** Returns inconsistent results across browsers
- **Verdict:** DO NOT USE for production

**❌ SystemInfo.deviceType**
- **Status:** UNRELIABLE for WebGL
- **Issue:** Often returns `DeviceType.Desktop` even on mobile devices
- **Verdict:** DO NOT USE

### Recommended Solution: JavaScript Plugin
**✅ Module.SystemInfo.mobile (Unity 2020.3+)**
- **Status:** RELIABLE
- **Method:** JavaScript plugin using `Module.SystemInfo.mobile`
- **Browser Support:** Works across all modern browsers
- **Implementation:** `.jslib` file + C# DllImport
- **Verdict:** RECOMMENDED

---

## 🏗️ Architecture Design

### Component Integration

```
GameUIManager.cs
  ├─ [SerializeField] mobileControlsCanvas (public reference)
  ├─ Start() → DetectMobileAndSetCanvas()
  └─ Uses MobileBrowserDetector.IsMobile()
       ↓
MobileBrowserDetector.cs (New Utility)
  ├─ IsMobile() → calls JavaScript plugin
  └─ Platform-aware (returns false in Editor)
       ↓
MobileBrowserDetector.jslib (New Plugin)
  └─ JavaScript: Module.SystemInfo.mobile
```

### Data Flow

```
Game Start
  ↓
GameUIManager.Start()
  ↓
Check Platform
  ├─ Editor → Use editorSimulateMobile bool
  ├─ WebGL → Call MobileBrowserDetector.IsMobile()
  └─ Other → Assume desktop (mobile controls hidden)
  ↓
Set Canvas Visibility
  ├─ Mobile Detected → mobileControlsCanvas.SetActive(true)
  └─ Desktop Detected → mobileControlsCanvas.SetActive(false)
```

---

## 📐 Component Design

### 1. JavaScript Plugin: MobileBrowserDetector.jslib

**File Path:** `Assets/Plugins/WebGL/MobileBrowserDetector.jslib`

**Purpose:** Access browser's SystemInfo to detect mobile devices

**Code:**
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

**Notes:**
- Returns `1` for mobile, `0` for desktop (C# interprets as bool)
- Primary detection: `Module.SystemInfo.mobile`
- Fallback: User agent regex (for older Unity versions or edge cases)
- Detects phones AND tablets

---

### 2. C# Utility: MobileBrowserDetector.cs

**File Path:** `Assets/_0_Custom/Scripts/Utilities/MobileBrowserDetector.cs`

**Purpose:** C# wrapper for JavaScript plugin with platform-aware logic

**Code:**
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
            // In Editor: Return false by default (or use EditorPrefs for testing)
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
            // Non-WebGL platforms (Android, iOS, etc.)
            // These would use native builds, not WebGL
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

**Features:**
- ✅ Platform-aware compilation
- ✅ Editor returns false (desktop mode)
- ✅ WebGL calls JavaScript plugin
- ✅ Exception handling with fallback
- ✅ Debug helper method (GetDeviceInfo)

---

### 3. GameUIManager Integration

**File Path:** `Assets/_0_Custom/Scripts/UI/GameUIManager.cs`

**Modifications:**

**Add Fields:**
```csharp
[Header("Mobile Controls")]
[SerializeField] private Canvas mobileControlsCanvas;
[SerializeField] private bool editorSimulateMobile = false; // Inspector toggle for testing
```

**Modify Start() Method:**
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

**Add New Method:**
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
    isMobile = Utilities.MobileBrowserDetector.IsMobile();
    Debug.Log($"GameUIManager: Detected platform - {Utilities.MobileBrowserDetector.GetDeviceInfo()}");
#endif

    mobileControlsCanvas.gameObject.SetActive(isMobile);

    Debug.Log($"GameUIManager: Mobile Controls Canvas {(isMobile ? "ENABLED" : "DISABLED")}");
}
```

**Alternative: Public Method for Runtime Toggle (Optional)**
```csharp
public void SetMobileControlsVisibility(bool visible)
{
    if (mobileControlsCanvas != null)
    {
        mobileControlsCanvas.gameObject.SetActive(visible);
        Debug.Log($"GameUIManager: Mobile Controls manually set to {visible}");
    }
}
```

---

## 🎨 Unity Setup Requirements

### Folder Structure
```
Assets/
├─ Plugins/
│  └─ WebGL/                           # Create this folder
│     └─ MobileBrowserDetector.jslib   # JavaScript plugin
└─ _0_Custom/
   └─ Scripts/
      └─ Utilities/                    # Create this folder
         └─ MobileBrowserDetector.cs   # C# wrapper
```

### Canvas Hierarchy
```
Canvas (Main UI)
├─ MenuUI
├─ LobbyUI
└─ ... existing UI elements

MobileControlsCanvas (New - Separate Canvas)  ← Assign this to GameUIManager
├─ VariableJoystick
├─ JumpButton
├─ RunButton
└─ SlideButton
```

**Why Separate Canvas?**
- ✅ Easy enable/disable of all mobile controls at once
- ✅ Clean separation between main UI and mobile-specific UI
- ✅ No impact on existing UI hierarchy
- ✅ Can set different sorting order if needed

### GameUIManager Inspector Setup
1. Create new Canvas GameObject: "MobileControlsCanvas"
2. Move VariableJoystick and all action buttons under MobileControlsCanvas
3. Select GameUIManager GameObject
4. Assign MobileControlsCanvas to "Mobile Controls Canvas" field
5. Toggle "Editor Simulate Mobile" for testing in Unity Editor

---

## 🧪 Testing Strategy

### Test Scenarios

#### TS1: Desktop Browser (WebGL Build)
**Environment:** Chrome/Firefox/Edge on Windows/Mac
**Expected:**
- MobileBrowserDetector.IsMobile() returns `false`
- Mobile controls canvas is HIDDEN
- Keyboard/mouse controls work
- No joystick visible

**Test Steps:**
1. Build WebGL
2. Host on local server or web host
3. Open in desktop browser
4. Check mobile controls visibility
5. Test keyboard controls (WASD, Space, Shift, C)

**Status:** ⬜ Pass / ⬜ Fail

---

#### TS2: Mobile Browser (WebGL Build)
**Environment:** Chrome/Safari on iOS/Android phone/tablet
**Expected:**
- MobileBrowserDetector.IsMobile() returns `true`
- Mobile controls canvas is VISIBLE
- Joystick and buttons appear
- Touch controls work

**Test Steps:**
1. Build WebGL
2. Host on web server (https recommended)
3. Open in mobile browser (phone)
4. Check mobile controls visibility
5. Test joystick + button controls

**Status:** ⬜ Pass / ⬜ Fail

---

#### TS3: Unity Editor (Desktop)
**Environment:** Unity Editor
**Expected:**
- Default: Mobile controls HIDDEN (editorSimulateMobile = false)
- Toggle ON: Mobile controls VISIBLE (editorSimulateMobile = true)

**Test Steps:**
1. Play in Editor with editorSimulateMobile = false
2. Verify mobile controls hidden
3. Stop, set editorSimulateMobile = true
4. Play again
5. Verify mobile controls visible

**Status:** ⬜ Pass / ⬜ Fail

---

#### TS4: Tablet Browser (WebGL Build)
**Environment:** Safari on iPad or Chrome on Android tablet
**Expected:**
- MobileBrowserDetector.IsMobile() returns `true`
- Mobile controls canvas is VISIBLE

**Test Steps:**
1. Open WebGL build on tablet
2. Verify mobile controls visible
3. Test touch controls

**Status:** ⬜ Pass / ⬜ Fail

---

#### TS5: Console Logs
**Environment:** Any platform
**Expected:**
- Clear debug logs showing detection result
- "WebGL (Mobile Browser)" or "WebGL (Desktop Browser)"

**Test Steps:**
1. Open browser console (F12)
2. Load game
3. Check Unity logs for detection messages
4. Verify correct platform detected

**Status:** ⬜ Pass / ⬜ Fail

---

## 🚨 Edge Cases & Considerations

### Edge Case 1: JavaScript Plugin Missing
**Scenario:** `.jslib` file not in `Assets/Plugins/WebGL/`
**Handling:**
- WebGL build will fail to compile (Unity error)
- Prevent by verifying folder structure before build

**Resolution:**
- Ensure `Plugins/WebGL/` folder exists
- Verify `.jslib` file is present
- Check Unity console for plugin compilation errors

---

### Edge Case 2: Module.SystemInfo Unavailable
**Scenario:** Older Unity version or browser doesn't support Module.SystemInfo
**Handling:**
- JavaScript plugin has fallback user agent detection
- Checks for "android", "iphone", "ipad", etc. in user agent string

**Code Already Handles This:**
```javascript
// Primary detection
if (typeof Module !== 'undefined' && typeof Module.SystemInfo !== 'undefined') {
    return Module.SystemInfo.mobile ? 1 : 0;
}

// Fallback detection
var mobileRegex = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i;
// ...
```

---

### Edge Case 3: Canvas Reference Not Assigned
**Scenario:** User forgets to assign mobileControlsCanvas in Inspector
**Handling:**
- Log warning: "Mobile Controls Canvas not assigned!"
- Method returns early (no crash)
- Game continues with default visibility (whatever was set in scene)

**User Fix:**
- Assign canvas reference in GameUIManager Inspector

---

### Edge Case 4: False Detection (Desktop Detected as Mobile)
**Scenario:** Browser user agent spoofing or unusual device
**Handling:**
- Accept as edge case (rare)
- User can manually toggle if needed (future enhancement: settings UI)

**Mitigation:**
- Use dual detection (Module.SystemInfo + user agent regex)
- Increases accuracy

---

### Edge Case 5: Privacy-Focused Browsers
**Scenario:** Brave, Tor, or other privacy browsers may block SystemInfo
**Handling:**
- Fallback to user agent detection
- If both fail, default to desktop (mobile controls hidden)

**Rationale:**
- Better to hide mobile controls on desktop than show on desktop
- Desktop users can still use keyboard (fully functional)
- Mobile users with strict privacy settings may need to allow JavaScript

---

## 📊 Performance Considerations

### Detection Timing
- **When:** Once at game start (Start() method)
- **Cost:** Single JavaScript call (~1ms)
- **Impact:** Negligible

### Canvas Enable/Disable
- **When:** Once at game start
- **Cost:** SetActive() on single Canvas (~<1ms)
- **Impact:** Negligible

### Runtime Overhead
- **Zero:** Detection happens once, no per-frame checks
- **Memory:** No additional allocations

**Verdict:** Performance impact is negligible.

---

## 🔄 Migration Plan (Updating Existing Design)

### Changes to Original Joystick Integration Design

**Original Plan:**
- Mobile controls always present in scene
- Optional platform-specific visibility via `#if UNITY_STANDALONE`

**Updated Plan:**
- Mobile controls in separate canvas
- Automatic runtime detection via JavaScript plugin
- Clean separation, better UX

### Updated Implementation Phases

**Phase 1: Core Input Merging** (No Changes)
- PlayerInputHandler modifications
- PlayerController modifications

**Phase 2: UI Button Support** (No Changes)
- MobileInputButtons component creation

**Phase 3: Unity Setup** (UPDATED)
- ✏️ Create MobileControlsCanvas (separate from main Canvas)
- ✏️ Add VariableJoystick to MobileControlsCanvas
- ✏️ Create action buttons under MobileControlsCanvas
- ✏️ Setup MobileInputButtons component

**Phase 3.5: Mobile Detection** (NEW)
- ➕ Create `Plugins/WebGL/` folder
- ➕ Create `MobileBrowserDetector.jslib`
- ➕ Create `Scripts/Utilities/` folder
- ➕ Create `MobileBrowserDetector.cs`
- ✏️ Modify GameUIManager (add canvas reference + visibility logic)

**Phase 4: Testing & Validation** (UPDATED)
- ✅ Add mobile detection tests (TS1-TS5)
- ✅ Test desktop browser
- ✅ Test mobile browser
- ✅ Test tablet browser
- ✅ Test Editor simulation

**Phase 5: Documentation & Cleanup** (No Changes)

---

## 📝 Code Changes Summary

### Files to Create
1. **MobileBrowserDetector.jslib** ➕ NEW
   - Location: `Assets/Plugins/WebGL/`
   - Purpose: JavaScript plugin for mobile detection

2. **MobileBrowserDetector.cs** ➕ NEW
   - Location: `Assets/_0_Custom/Scripts/Utilities/`
   - Purpose: C# wrapper for JavaScript plugin

### Files to Modify
3. **GameUIManager.cs** ✏️ MODIFY
   - Add mobileControlsCanvas field
   - Add editorSimulateMobile field
   - Add SetupMobileControlsVisibility() method

### Unity Scene Changes
4. **Canvas Hierarchy** ✏️ MODIFY
   - Create MobileControlsCanvas (separate Canvas)
   - Move mobile controls under MobileControlsCanvas
   - Assign to GameUIManager

### Files Unchanged
- ✅ PlayerInputHandler.cs (handles input regardless of visibility)
- ✅ PlayerController.cs (handles input regardless of visibility)
- ✅ MobileInputButtons.cs (works regardless of canvas visibility)
- ✅ All other scripts

---

## 🎯 Implementation Checklist

### Step 1: Create Folders
- [ ] Create `Assets/Plugins/` folder (if not exists)
- [ ] Create `Assets/Plugins/WebGL/` folder
- [ ] Create `Assets/_0_Custom/Scripts/Utilities/` folder

### Step 2: Create JavaScript Plugin
- [ ] Create `MobileBrowserDetector.jslib` file
- [ ] Copy JavaScript code (see Component Design section)
- [ ] Place in `Assets/Plugins/WebGL/`
- [ ] Verify Unity recognizes plugin (check Inspector)

### Step 3: Create C# Wrapper
- [ ] Create `MobileBrowserDetector.cs` file
- [ ] Copy C# code (see Component Design section)
- [ ] Place in `Assets/_0_Custom/Scripts/Utilities/`
- [ ] Verify namespace: `ParkourLegion.Utilities`
- [ ] Compile and check for errors

### Step 4: Modify GameUIManager
- [ ] Add `using ParkourLegion.Utilities;` at top
- [ ] Add mobileControlsCanvas field
- [ ] Add editorSimulateMobile field
- [ ] Add SetupMobileControlsVisibility() method
- [ ] Call method in Start()
- [ ] Compile and check for errors

### Step 5: Setup Unity Scene
- [ ] Create new Canvas GameObject: "MobileControlsCanvas"
- [ ] Configure Canvas settings (Screen Space - Overlay, Sort Order: 100)
- [ ] Move VariableJoystick to MobileControlsCanvas
- [ ] Move all action buttons to MobileControlsCanvas
- [ ] Select GameUIManager
- [ ] Assign MobileControlsCanvas reference
- [ ] Save scene

### Step 6: Test in Editor
- [ ] Play with editorSimulateMobile = false → controls hidden
- [ ] Play with editorSimulateMobile = true → controls visible
- [ ] Check console logs for detection messages

### Step 7: Test WebGL Build
- [ ] Build WebGL
- [ ] Test on desktop browser → controls hidden
- [ ] Test on mobile browser → controls visible
- [ ] Check browser console for logs

---

## 📚 Related Documentation

### Design Documents
- [Joystick Integration Design](./joystick-integration-design.md) - Parent design
- [Joystick Implementation Plan](./implementation-plan.md) - Full implementation checklist

### Unity Documentation
- [WebGL Browser Compatibility](https://docs.unity3d.com/Manual/webgl-browsercompatibility.html)
- [Application.isMobilePlatform](https://docs.unity3d.com/ScriptReference/Application-isMobilePlatform.html)
- [Creating WebGL Plugins](https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html)

### External Resources
- [Stack Overflow: Unity WebGL Mobile Detection](https://stackoverflow.com/questions/60806966/unity-webgl-check-if-mobile)
- [GitHub: CheckIfMobileForUnityWebGL](https://github.com/gigacee/CheckIfMobileForUnityWebGL)

---

**Document Version:** 1.0
**Status:** Design Complete - Ready for Implementation
**Parent Design:** [joystick-integration-design.md](./joystick-integration-design.md)
**Author:** Cody (Research Mode)
**Last Updated:** 2025-11-19
