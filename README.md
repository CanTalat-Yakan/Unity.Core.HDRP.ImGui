# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
  - Window → Package Manager
  - "+" → "Add package from git URL…"
  - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
  - Tools → Install & Update UnityEssentials
  - Install all or select individual modules; run again anytime to update

---

# HDRP ImGui

> Quick overview: Minimal Dear ImGui runtime rendering in HDRP via a Custom Pass.

A small runtime integration focused on clarity:
- Rendering is performed through an **HDRP Custom Pass**.
- Setup is **manual** (no auto-created volumes, no bootstraps).
- Input is forwarded from Unity's `Input` API into ImGui IO.

## Features
- Manual-only setup
  - No automatic scene objects; you wire up the host and Custom Pass yourself
- Minimal render path
  - Converts ImGui draw lists into a Unity mesh and draws it using a simple UI material
- Minimal input forwarding
  - Mouse + basic keyboard mapping for navigation and common shortcuts

## Requirements
- Unity 6000.0+
- HDRP (Custom Pass system)
- `ImGuiNET` + native `cimgui` binaries present in the project

## Usage

1) Add an `ImguiHost`
- Create an empty GameObject and add `UnityEssentials.ImguiHost`.
- Assign **Target Camera** (the same camera HDRP renders through).

2) Add a Custom Pass Volume
- Create a GameObject.
- Add `Custom Pass Volume` (HDRP).
- Set **Mode** to your preference (commonly *Global*).
- Add a custom pass of type `UnityEssentials.ImguiCustomPass`.
- In `ImguiCustomPass`, assign the **Host** reference to your `ImguiHost` component.

3) Enter Play Mode
- You should see the sample ImGui window.
- You should be able to hover and interact.

### How to add your own UI

`ImguiHost.Render(...)` is invoked by HDRP and is responsible for:
- updating IO (`DisplaySize`, `DeltaTime`, input events)
- calling `ImGui.NewFrame()` / building UI
- calling `ImGui.Render()` and submitting draw data

To add your own UI, replace the example window in `ImguiHost.Render` with your own ImGui calls.

## Native plugins (cimgui)

This project uses platform-specific native libraries. Unity must see **exactly one** editor-compatible `cimgui` per platform.

Recommended layout (example):
- Windows x64: `.../runtimes/win-x64/native/cimgui.dll`
- Linux x64: `.../runtimes/linux-x64/native/libcimgui.so`
- macOS x64/arm64: `.../runtimes/osx-*/native/libcimgui.dylib`

## Notes and Limitations
- This is a minimal renderer. It does not implement every feature of the official ImGui backends.
- The default material uses `UI/Default` (fallback `Unlit/Transparent`). If you need HDRP-specific blending or SRP batching behavior, provide a dedicated shader/material.
- Text input (IME, clipboard) and full key mapping are intentionally minimal; extend `ImguiInput` as needed.
- Multi-viewport / docking are not set up.

## Files in This Package
- `Runtime/ImguiHost.cs` – ImGui context owner and example window
- `Runtime/ImguiCustomPass.cs` – HDRP Custom Pass that forwards rendering to the host
- `Runtime/ImguiRenderer.cs` – Draw data → mesh conversion and draw calls
- `Runtime/ImguiInput.cs` – Unity Input → ImGui IO
- `Runtime/ImguiTextureRegistry.cs` – `Texture` ↔ `TextureId` mapping
- `Runtime/UnityEssentials.Imgui.asmdef` – Runtime assembly definition

## Troubleshooting
- **Nothing renders:**
  - Ensure the Custom Pass Volume is active for the camera and the pass is added.
  - Ensure `ImguiCustomPass.host` references an enabled `ImguiHost`.
  - Ensure `ImguiHost.Target Camera` matches the camera passed by HDRP.

- **Plugin load errors:**
  - Ensure exactly one native `cimgui` plugin is Editor-compatible for your OS.
  - Ensure the plugin CPU/OS settings match your Editor (Linux/Windows/macOS).

## Tags
unity, imgui, dear-imgui, hdrp, custom-pass, runtime, overlay, input
