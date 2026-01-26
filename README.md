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

> Quick overview: Minimal ImGui.NET integration for HDRP that renders through a Custom Pass in Edit Mode and Play Mode, providing a minimalist immediate mode GUI library based on Dear ImGui and powered by the ImGui.NET runtime.

This package is a small, practical bridge between **ImGui.NET** (Dear ImGui) and **HDRP**:
- Rendering is performed through an **HDRP Custom Pass**.
- Setup stays **manual and explicit** (no auto-created volumes, no bootstrap objects).
- Input is forwarded from Unity's `Input` API into ImGui IO.

## Features
- HDRP-native overlay rendering
  - Renders into the HDRP camera color buffer via Custom Pass
- Edit Mode + Play Mode
  - Useful for tooling/debug UI without entering Play Mode
- Minimal integration surface
  - A single host (`ImGuiHost`) and a single custom pass (`ImGuiCustomPass`)

## Requirements
- Unity 6000.0+
- HDRP (Custom Pass system)
- `ImGuiNET` + native `cimgui` binaries present in the project

## Usage

1) Add an `ImGuiHost`
- Create an empty GameObject and add `UnityEssentials.ImGuiHost`.

2) Add a Custom Pass Volume + `ImGuiCustomPass`
- Create a GameObject.
- Add `Custom Pass Volume` (HDRP).
- Set **Mode** to your preference (commonly *Global*).
- Set the **Injection Point** to **After Post Process**.
  - This is very important for correct colors.
- Add a custom pass of type `UnityEssentials.ImGuiCustomPass`.

That’s it.
- It renders directly in Edit Mode and works in Play Mode too, as long as the volume is active for the camera.

## How it works

`ImGuiHost.Render(...)` is invoked by HDRP every frame and is responsible for:
- updating IO (`DisplaySize`, `DeltaTime`, input events)
- calling `ImGui.NewFrame()`
- executing registered UI callbacks
- calling `ImGui.Render()` and submitting draw data

### Sample

All ImGui UI is drawn by registering a draw callback on the global `ImGuiHost`.
Any number of scripts can register; they all contribute windows to the same frame.

```csharp
using ImGuiNET;
using UnityEngine;
using UnityEssentials;

public sealed class ImGuiExampleUi : MonoBehaviour
{
    private void OnEnable() => ImGuiHost.Register(Draw);
    private void OnDisable() => ImGuiHost.Unregister(Draw);

    private static void Draw()
    {
        // Main tool window
        ImGui.Begin("Main Tool");
        ImGui.Text("This is the primary ImGui window.");
        ImGui.End();

        // Secondary window
        ImGui.Begin("Stats");
        var fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        ImGui.Text($"FPS: {fps:0}");
        ImGui.End();

        // Any number of additional windows can be added here
        ImGui.ShowDemoWindow();
    }
}
```

Every script that calls `ImGuiHost.Register` simply adds more UI to the same ImGui frame. No special root object or manual scene wiring is required.

### Rules of thumb
- Do not call `ImGui.NewFrame()` / `ImGui.Render()` in your callbacks (the host owns the frame).
- Keep callbacks fast (they run every rendered frame).
- If you need ordering, register/unregister in a controlled sequence (or introduce your own dispatcher).

## Notes and Limitations
- This is a minimal renderer. It does not implement every feature of the official ImGui backends.
- Text input (IME, clipboard) and full key mapping are intentionally minimal; extend `ImGuiInput` as needed.
- Docking is not set up.

## Files in This Package
- `Runtime/ImGuiHost.cs` – ImGui context owner and frame driver
- `Runtime/ImGuiCustomPass.cs` – HDRP Custom Pass that forwards rendering to the host
- `Runtime/ImGuiRenderer.cs` – Draw data → mesh conversion and draw calls
- `Runtime/ImGuiInput.cs` – Unity Input → ImGui IO
- `Runtime/ImGuiTextureRegistry.cs` – `Texture` ↔ `TextureId` mapping
- `Runtime/UnityEssentials.HDRPImGui.asmdef` – Runtime assembly definition

## Troubleshooting
- **Nothing renders:**
  - Ensure the Custom Pass Volume is active for the camera and the pass is added.

- **Wrong colors / looks washed out:**
  - Ensure the Custom Pass injection point is **After Post Process**.

## Tags
unity, imgui, dear-imgui, hdrp, custom-pass, runtime, overlay, tooling
