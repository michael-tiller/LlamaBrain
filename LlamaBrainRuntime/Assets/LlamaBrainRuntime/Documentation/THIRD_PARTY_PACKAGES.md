# Third-Party Packages

This document lists all third-party packages and assets used in the LlamaBrain Unity Runtime, along with installation instructions and usage information.

**Important:** Third-party assets are **not included** in this repository. "Free" does not mean "redistributable." You must install these packages separately. Only package manifest entries and attribution notices are committed to the repository.

**Reproducibility:** We commit `Packages/manifest.json` and `Packages/packages-lock.json` for deterministic package resolution.

---

## 📦 Core Runtime Required

These packages are required for the LlamaBrain Runtime to function.

### 1. UniTask

**Description:**  
UniTask is a high-performance async/await library for Unity that provides zero-allocation async operations. It's used throughout LlamaBrain for asynchronous operations, replacing traditional coroutines for better performance and code readability.

**Repository:**  
[https://github.com/Cysharp/UniTask](https://github.com/Cysharp/UniTask)

**License:** MIT License

**Tested with:** Unity 6000.0.58f2 LTS

**Installation:**

Via Unity Package Manager (UPM):
1. Open Unity's Package Manager (`Window > Package Manager`)
2. Click the "+" button and select "Add package from git URL..."
3. Enter the following URL:
   ```
   https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
   ```
4. Click "Add" to install the package

**⚠️ Installation Notes:**
- Users may encounter namespace resolution errors during UPM/Git installation. If this occurs, refer to the [UniTask repository's UPM path instructions](https://github.com/Cysharp/UniTask) or use the `.unitypackage` as an alternative installation method.

**Usage in LlamaBrain:**
- Used in `LlamaBrainAgent` for async dialogue operations (`SendPlayerInputAsync`, `SendWithSnapshotAsync`)
- Extension methods in `BrainAgentExtensions.cs` provide UniTask support for all BrainAgent operations
- Used in test suites for async test operations
- Provides `UniTaskVoid` for fire-and-forget operations

**Key Features:**
- Zero-allocation async/await
- PlayerLoop integration; supports common Unity platforms
- Awaitable Unity operations (AsyncOperation, coroutines)
- Task tracking and monitoring

---

### 2. TextMeshPro

**Description:**  
TextMeshPro (TMP) is Unity's advanced text rendering system that provides high-quality text rendering with rich formatting options. LlamaBrain uses TextMeshPro for all UI text components.

**Package:**  
`com.unity.textmeshpro` (installed via Package Manager)

**License:** Distributed by Unity as package `com.unity.textmeshpro` via Package Manager; install separately via Unity.

**Tested with:** Unity 6000.0.58f2 LTS

**Installation:**

Via Unity Package Manager:
1. Open Unity's Package Manager (`Window > Package Manager`)
2. Search for "TextMeshPro"
3. Click "Install"

**Note:** TextMeshPro version tracks with your Unity Editor version and package set. No specific version is required beyond what Unity provides.

**Usage in LlamaBrain:**
- All dialogue UI components use `TextMeshProUGUI` instead of legacy `Text` components
- Used in `ValidationGateOverlay.cs` for constraint and validation status display
- Used in `MemoryMutationOverlay.cs` for memory visualization
- Used in `RedRoomCanvas.cs` and other debug/development UI components
- Used in `DialogueMessage.cs` for message display
- Used in `NpcFollowerExample.cs` for prompt indicators and debug text

**Key Features:**
- High-quality text rendering
- Rich text formatting support
- Better performance than legacy Text components
- Advanced typography features

---

## 🎮 Samples Only

These assets are only required if you plan to use the sample scenes and demos.

### 3. Starter Assets – Third Person (URP version only)

**Description:**  
Unity's Starter Assets – Third Person character controller used in LlamaBrain sample scenes and demos for character movement and interaction. This asset provides the foundation for NPC movement and player interaction mechanics in example scenarios.

**⚠️ Unity 6 LTS Compatibility:**  
**Only the URP version is compatible with Unity 6 LTS.** The built-in render pipeline and HDRP variants are not supported.

**Asset Store:**  
[Starter Assets – Third Person](https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-updates-in-new-charactercontroller-pa-196526)

**License:** Non-standard Unity license (Standard Asset Store EULA). **Not redistributed** in this repository.

**Tested with:** Unity 6000.0.58f2 LTS (URP version only)

**Required Dependencies:**
- **URP Project**: Your Unity project must use the Universal Render Pipeline (URP)
- **New Input System**: The package requires Unity's New Input System (not the legacy Input Manager)
- **Cinemachine**: Cinemachine is a mandatory dependency for camera control

**Installation:**

1. Download from the Unity Asset Store
2. Install via Package Manager (My Assets) or Asset Store tooling, depending on Unity version
3. **Important**: Ensure you install the **URP version** (not built-in or HDRP variants)
4. Ensure your project is configured for URP (not built-in render pipeline or HDRP)
5. Enable the New Input System in Project Settings → Player → Active Input Handling
6. Install Cinemachine via Package Manager if not already installed
7. Ensure compatibility with Unity 6000.0.58f2 LTS or higher

**Usage in LlamaBrain:**
- Used in sample scenes for NPC movement and interaction
- Provides character controller functionality for demo NPCs
- Enables realistic character movement in example scenarios

**Note:**  
This asset is **not included** in the repository. You must download it separately from the Unity Asset Store if you want to use the sample scenes that depend on it. **Only the URP version is compatible with Unity 6 LTS.**

---

## 💾 Persistence (Feature 16)

This package is required for Feature 16 (Save/Load Game Integration) and Feature 14 (Deterministic Generation Seed).

### 4. SaveGameFree (Required for Persistence)

**Description:**  
SaveGameFree is a free Unity asset for saving and loading game data. It provides a complete solution for game state persistence with support for multiple serialization formats (JSON, XML, Binary), encryption, cross-platform support, and cloud storage options.

**Repository:**  
[https://github.com/BayatGames/SaveGameFree](https://github.com/BayatGames/SaveGameFree)

**License:** MIT License (GitHub repo)

**Latest GitHub Release:**  
[Version 2.5.0](https://github.com/BayatGames/SaveGameFree/releases/tag/2.5.0) (Apr 1, 2022)

**Tested with:** Unity 6000.0.58f2 LTS

**Note:** SaveGameFree is required for Feature 16 (Save/Load Game Integration) to persist game state and InteractionCount across sessions.

**Installation (recommended for this repo):**
- Download `Save.Game.Free.unitypackage` from the [GitHub Releases page](https://github.com/BayatGames/SaveGameFree/releases)
- Import the `.unitypackage` file into your Unity project:
  - In Unity, go to `Assets > Import Package > Custom Package...`
  - Select the downloaded `.unitypackage` file
  - Click "Import"

**⚠️ Important:** Do not use the Unity Asset Store download (different license terms - Standard Asset Store EULA). Use the GitHub release for MIT-licensed distribution.

**Transitive Dependencies:** Imports include FullSerializer; see Third-Party Notices for attribution.

**Key Features:**
- **Cross-Platform Support**: Works on all Unity-supported platforms
- **Multiple Serialization Formats**: JSON, XML, and Binary serialization
- **Encryption**: Built-in encryption support (Base64 format)
- **Cloud Storage**: Web and cloud storage options
- **Auto Save**: Automatic save functionality
- **File Management**: GetFiles and GetDirectories API methods
- **PlayerPrefs Option**: Optional PlayerPrefs-based storage
- **Event System**: OnSaving and OnLoading events
- **Ignored Files/Directories**: Configurable exclusion of files and directories from save operations

**Usage in LlamaBrain:**
- Used for persisting game state and NPC memory
- Saves conversation history and persona configurations
- Stores world state and episodic memories
- Provides encrypted storage for sensitive game data

**Configuration:**
- Default ignored files: "Player.log", "output_log.txt"
- Default ignored directories: "Analytics"
- Can be configured via `IgnoredFiles` and `IgnoredDirectories` properties

---

## 📋 Package Dependencies Summary

| Package | Version | Type | Required | Notes |
|---------|---------|------|----------|-------|
| UniTask | Tested | UPM/Git | ✅ Yes | May require namespace resolution troubleshooting |
| TextMeshPro | (Editor version) | Unity Package | ✅ Yes | Bundled with Unity |
| Starter Assets – Third Person | - | Asset Store | ⚠️ Samples Only | **URP version only** (requires URP, New Input System, Cinemachine) |
| SaveGameFree | 2.5.0+ | UnityPackage (manual import) | ✅ Yes | Use GitHub version, not Asset Store |

**Legend:**
- ✅ **Core Runtime**: Required for core functionality
- ⚠️ **Samples Only**: Required only for sample scenes and demos

---

## 🔧 Installation Checklist

Before using LlamaBrain Runtime, ensure you have:

**Core Runtime:**
- [ ] Unity 6000.0.58f2 LTS
- [ ] TextMeshPro package installed via Package Manager
- [ ] UniTask installed via Package Manager (Git URL)

**Samples (if using sample scenes):**
- [ ] Starter Assets – Third Person (URP version only) imported from Unity Asset Store
- [ ] Project configured for URP (not built-in or HDRP)
- [ ] New Input System enabled in Project Settings
- [ ] Cinemachine package installed via Package Manager

**Persistence (Feature 16):**
- [ ] SaveGameFree installed locally (not committed) - Required for save/load functionality

---

## 📚 Additional Resources

- [UniTask Documentation](https://github.com/Cysharp/UniTask)
- [TextMeshPro Documentation](https://docs.unity3d.com/Manual/com.unity.textmeshpro.html)
- [SaveGameFree GitHub Repository](https://github.com/BayatGames/SaveGameFree)
- [Starter Assets – Third Person (Asset Store)](https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-updates-in-new-charactercontroller-pa-196526)
- [Unity Package Manager Documentation](https://docs.unity3d.com/Manual/Packages.html)

---

## ⚠️ License Notes

Please review the license terms for each third-party package:

- **UniTask**: MIT License
- **TextMeshPro**: Distributed by Unity as package `com.unity.textmeshpro` via Package Manager
- **SaveGameFree**: MIT License (GitHub repo) - **Use GitHub version, not Asset Store version**
- **Starter Assets – Third Person**: Non-standard Unity license (Standard Asset Store EULA) - **Not redistributed**

**Redistribution Policy:**  
This repository does **not** include third-party assets. Only package manifest entries (for UPM packages) and attribution notices are committed. You must install all third-party packages separately. "Free" does not mean "redistributable" - always check license terms before including assets in your repository.

Ensure compliance with all third-party package licenses when distributing your project.
