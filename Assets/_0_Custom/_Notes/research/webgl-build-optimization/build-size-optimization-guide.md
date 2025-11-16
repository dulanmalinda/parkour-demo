# Unity WebGL Build Size Optimization Guide

**Date:** 2025-11-16
**Project:** Parkour Legion Demo
**Current Build Size:** ~15 MB
**Target:** Reduce build size significantly

---

## 📊 Current Project Analysis

### Installed Packages
From `D:\_UNITY\parkour legion demo\Packages\manifest.json`:

**High-Impact Packages (likely contributors to build size):**
- ✅ **URP (Universal Render Pipeline)** - `com.unity.render-pipelines.universal: 17.2.0`
  - Adds ~2.5-3 MB overhead
  - Includes post-processing assets, film grain textures, blue noise textures
- ✅ **New Input System** - `com.unity.inputsystem: 1.14.2`
  - Adds ~2.4 MB overhead
  - Old Input Manager is built-in and free
- ✅ **Cinemachine** - `com.unity.cinemachine: 3.1.5`
  - Camera system with additional overhead
- ✅ **Colyseus SDK** - `io.colyseus.sdk` (from GitHub)
  - WebSocket networking library
- ⚠️ **AI Navigation** - `com.unity.ai.navigation: 2.0.9`
  - Not used in project (no pathfinding in parkour game)
- ⚠️ **Multiplayer Center** - `com.unity.multiplayer.center: 1.0.0`
  - Not needed (using Colyseus, not Unity Netcode)
- ⚠️ **Multiplayer Playmode** - `com.unity.multiplayer.playmode: 1.6.1`
  - Not needed (using Colyseus, not Unity Netcode)
- ⚠️ **Visual Scripting** - `com.unity.visualscripting: 1.9.7`
  - Not used (project uses C# scripts only)
- ⚠️ **Timeline** - `com.unity.timeline: 1.8.9`
  - Not used (no cinematic sequences in project)

**Potentially Removable:**
- AI Navigation (~0.5-1 MB)
- Multiplayer Center (~0.3 MB)
- Multiplayer Playmode (~0.5 MB)
- Visual Scripting (~1-2 MB)
- Timeline (~0.5 MB)
- Test Framework (~0.5 MB if not building with tests)

**Estimated Removal Savings:** ~3-5 MB

---

## 🎯 High-Impact Optimization Strategies

### 1. **Code Stripping & IL2CPP Settings** ⚠️ CRITICAL ISSUE

**Problem:** Colyseus SDK breaks with Medium/High code stripping!

**From Research:**
> Unity's managed code stripping at Medium or High settings will strip default constructors from `Colyseus.Schema.Schema` derived types, causing `MissingMethodException` during runtime handshaking.

**Recommended Setting:**
- **Managed Stripping Level:** LOW (or Disabled)
- **Cannot use Medium/High due to Colyseus compatibility**

**Alternative Optimization:**
- **IL2CPP Code Generation:** Optimize Size (Faster smaller builds)
  - Location: `Edit > Project Settings > Player > Other Settings > IL2CPP Code Generation`
  - Set to: **Optimize Size**
- **Strip Engine Code:** Enable
  - Location: `Edit > Project Settings > Player > Other Settings > Strip Engine Code`
  - Removes unused Unity engine classes

**Estimated Impact:** 1-2 MB savings (with Low stripping + Optimize Size)

---

### 2. **Compression Settings** ⚠️ HIGHEST IMPACT

**Current Setting:** Unknown (needs verification)

**Recommended Setting:**
- **Compression Format:** Brotli
  - Location: `Edit > Project Settings > Player > Publishing Settings > Compression Format`
  - Brotli achieves 15-30% smaller size than Gzip
  - Brotli Level 11 is best for production

**Alternative (Advanced):**
- Build with **Disabled** compression
- Use external compression tools with max settings:
  - `--brotli-quality 11`
  - `--gzip-level 9`
  - Provides multiple format fallbacks

**Server Configuration Required:**
- Must set `Content-Encoding: br` header for Brotli
- Must serve files over HTTPS (Chrome/Firefox requirement)

**Estimated Impact:** 60-80% reduction in download size (3-5 MB final download)

---

### 3. **URP Post-Processing Removal** ⚠️ HIGH IMPACT

**Problem:** URP automatically includes post-processing assets even if unused.

**Automatically Included Assets:**
- Film grain textures: 10 textures × 256KB = **2.7 MB**
- Blue noise textures: 7 textures × 64KB = **0.4 MB**
- Debug fonts: 80KB + 16KB = **0.1 MB**
- Anti-aliasing textures
- Post-processing shaders

**How to Remove:**
1. Open all **Universal Renderer Data** assets
2. Uncheck **Post-Processing > Enabled**
3. Remove references to **Post Process Data** scriptable objects
4. Verify no post-processing volumes in scenes

**Estimated Impact:** 2-3 MB savings

---

### 4. **Remove Unused Packages**

**Action Items:**
```
Remove from manifest.json:
- com.unity.ai.navigation (not used)
- com.unity.multiplayer.center (not used, using Colyseus)
- com.unity.multiplayer.playmode (not used)
- com.unity.visualscripting (not used)
- com.unity.timeline (not used)
- com.unity.test-framework (not needed in build)
```

**How to Remove:**
1. Open `Window > Package Manager`
2. Find each package
3. Click **Remove**
4. Or manually edit `Packages/manifest.json`

**Estimated Impact:** 3-5 MB savings

---

### 5. **Replace New Input System with Old Input Manager** (OPTIONAL)

**Current:** Using New Input System (`com.unity.inputsystem: 1.14.2`)
**Alternative:** Built-in Input Manager (free)

**Trade-off:**
- **Savings:** ~2.4 MB
- **Cost:** Need to rewrite input handling code
- **Effort:** 2-4 hours of refactoring

**Current Usage:**
- `Scripts/Player/PlayerInputHandler.cs` - WASD, Space, Shift, C
- `Scripts/Camera/CameraInputProvider.cs` - Mouse input

**Recommendation:** Low priority - refactoring effort not worth 2.4 MB unless critical

**Estimated Impact:** 2.4 MB savings (if refactored)

---

### 6. **Quality Settings Optimization**

**Action Items:**
1. Remove unused quality levels
   - Location: `Edit > Project Settings > Quality`
   - Keep only 1-2 levels (e.g., WebGL optimized)
2. Disable unnecessary features per level:
   - Anti-aliasing: 0 (off)
   - Soft Particles: Off
   - Realtime Reflection Probes: Off
   - Shadows: Disable or Low resolution

**Current:** Multiple quality levels detected (Mobile, etc.)

**Estimated Impact:** 0.5-1 MB savings

---

### 7. **API Compatibility Level**

**Recommended Setting:**
- **.NET Standard 2.1**
  - Location: `Edit > Project Settings > Player > Other Settings > API Compatibility Level`
  - Produces smaller builds
  - Cross-platform support

**Estimated Impact:** 0.5-1 MB savings

---

### 8. **WebGL-Specific Player Settings**

**Code Optimization:**
- Location: `Edit > Project Settings > Player > Publishing Settings > Code Optimization`
- Set to: **Disk Size (with LTO)**
- Warning: Slower build times, but smallest output

**Exception Support:**
- Location: `Edit > Project Settings > Player > Publishing Settings > Exception Support`
- Set to: **Explicitly Thrown Exceptions Only**
- Reduces bundle size and improves performance

**Development Build:**
- Ensure **Development Build** is UNCHECKED
- Enables Gzip compression and minification

**Estimated Impact:** 1-2 MB savings

---

### 9. **Disable Unity Splash Screen** (Unity 6+)

**Action:**
- Location: `Edit > Project Settings > Player > Splash Image`
- Uncheck **Show Unity Logo**
- Uncheck **Show Unity Splash Screen**

**Note:** Free license may have restrictions

**Estimated Impact:** 0.2-0.5 MB savings

---

### 10. **Audio Optimization**

**Settings:**
- Background music: Compressed in memory, low quality
- Sound effects: Decompress on load
- Location: Select audio file > Inspector > Import Settings

**Current Project:** Minimal audio usage

**Estimated Impact:** 0.5 MB savings (if audio added later)

---

## 📋 Implementation Checklist

### Phase 1: Package Removal (Immediate, Low Risk)
- [ ] Remove `com.unity.ai.navigation`
- [ ] Remove `com.unity.multiplayer.center`
- [ ] Remove `com.unity.multiplayer.playmode`
- [ ] Remove `com.unity.visualscripting`
- [ ] Remove `com.unity.timeline`
- [ ] Remove `com.unity.test-framework`
- **Expected Savings:** 3-5 MB

### Phase 2: Build Settings Optimization (Immediate, Low Risk)
- [ ] Set Compression Format to **Brotli**
- [ ] Set Code Optimization to **Disk Size (with LTO)**
- [ ] Set Exception Support to **Explicitly Thrown Exceptions Only**
- [ ] Enable **Strip Engine Code**
- [ ] Set IL2CPP Code Generation to **Optimize Size**
- [ ] Set Managed Stripping Level to **LOW** (Colyseus requirement)
- [ ] Set API Compatibility Level to **.NET Standard 2.1**
- [ ] Ensure **Development Build** is unchecked
- [ ] Disable Unity Splash Screen
- **Expected Savings:** 2-3 MB + 60-80% compression

### Phase 3: URP Optimization (Immediate, Medium Risk)
- [ ] Open all Universal Renderer Data assets
- [ ] Uncheck **Post-Processing > Enabled**
- [ ] Remove Post Process Data references
- [ ] Verify no post-processing volumes in scenes
- [ ] Test rendering to ensure no visual regressions
- **Expected Savings:** 2-3 MB

### Phase 4: Quality Settings (Immediate, Low Risk)
- [ ] Remove unused quality levels (keep 1-2)
- [ ] Disable anti-aliasing
- [ ] Disable soft particles
- [ ] Disable realtime reflection probes
- [ ] Set shadows to Low or Off
- **Expected Savings:** 0.5-1 MB

### Phase 5: Server Configuration (Deployment)
- [ ] Configure server to send `Content-Encoding: br` header
- [ ] Ensure HTTPS deployment (required for Brotli)
- [ ] Test browser decompression
- **Expected Savings:** N/A (enables Brotli compression)

---

## 🎯 Expected Results

### Before Optimization
- **Current Build:** ~15 MB (uncompressed or Gzip)

### After Phase 1-4 Optimization
- **Uncompressed Build:** ~7-9 MB
- **With Brotli Compression:** ~2-3 MB (download size)

### Best Case Scenario
- **Final Download Size:** 2-3 MB (Brotli compressed)
- **Total Reduction:** 80-85%

---

## ⚠️ Critical Constraints

### Colyseus SDK Limitation
- **Cannot use Medium/High Code Stripping**
- Must keep Managed Stripping Level at **LOW**
- This limits code optimization potential
- Trade-off: Multiplayer functionality vs. build size

### URP Dependency
- Using URP adds ~2.5 MB overhead vs. Built-in RP
- Trade-off: Modern rendering features vs. build size
- Consider switching to Built-in RP if URP features not essential
- **Recommendation:** Keep URP, disable post-processing only

---

## 🔍 Build Size Analysis Tools

**After implementing optimizations, analyze build:**

1. **Build Report:**
   - `File > Build Settings > Build > Build Report`
   - Shows asset contributions to build size

2. **Unity Build Report Inspector:**
   - Free tool on Asset Store
   - Detailed breakdown of build contents

3. **Memory Profiler Package:**
   - `Window > Package Manager > Memory Profiler`
   - Analyze memory and asset usage

---

## 📊 Comparison: Empty Unity Project Build Sizes

**Reference data (2025):**
- Default empty Unity project: 7-8 MB
- Optimized empty project: 2 MB
- With URP: +2.5 MB
- With New Input System: +2.4 MB

**Your Project (15 MB) Breakdown Estimate:**
- Unity core: ~7 MB
- URP: ~2.5 MB
- New Input System: ~2.4 MB
- Cinemachine: ~1 MB
- Colyseus SDK: ~0.5 MB
- Other packages: ~1-2 MB
- Project scripts/assets: ~0.5 MB

---

## 🚀 Recommended Action Plan

### Step 1: Quick Wins (30 minutes)
1. Remove unused packages (Phase 1)
2. Change build settings (Phase 2)
3. Disable URP post-processing (Phase 3)
4. Clean up quality settings (Phase 4)

### Step 2: Build & Test (15 minutes)
1. Create WebGL build
2. Verify functionality (multiplayer, camera, movement)
3. Check build report for size breakdown

### Step 3: Measure Results (5 minutes)
1. Compare before/after build sizes
2. Test compressed download size
3. Document final results

### Step 4: Server Deployment (if needed)
1. Configure Brotli headers on hosting server
2. Ensure HTTPS enabled
3. Test browser decompression

**Total Time Investment:** ~1 hour
**Expected Reduction:** 10-12 MB (80-85%)

---

## 📖 References

- [Unity Manual: Reducing File Size](https://docs.unity3d.com/Manual/ReducingFilesize.html)
- [Unity Manual: WebGL Distribution Size](https://docs.unity3d.com/Manual/webgl-distributionsize-codestripping.html)
- [Unity WebGL Compression Guide](https://miltoncandelero.github.io/unity-webgl-compression)
- [Colyseus Unity SDK: Code Stripping Issue #135](https://github.com/colyseus/colyseus-unity-sdk/issues/135)
- [Unity WebGL Loading Test (GitHub)](https://github.com/JohannesDeml/UnityWebGL-LoadingTest)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-16
**Author:** Research Mode Analysis
