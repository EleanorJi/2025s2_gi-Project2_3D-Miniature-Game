# Project Structure (Assets)

This document reflects the current structure of the `Assets` folder in this Unity project, along with brief descriptions of the contents and guidance for adding new content.

## Folder Tree

```
Assets/
├── Art/                                   # Art assets (materials, textures, models, prefabs, VFX)
│   ├── Animations/                        # Animation clips & controllers
│   │   ├── HelpAnt/
│   │   ├── pigeon/                        # Level1, Level3
│   │   └── Player/
│   ├── Audio/                             # Audio library
│   │   ├── Level2/
│   │   ├── Mixers/
│   │   ├── Music/
│   │   ├── SFX/
│   │   └── Voice/
│   ├── Characters/                        # Character UI sprites (HP icons, etc.)
│   ├── Effects/                           # VFX prefabs (e.g., BoomSmoke)
│   ├── Environment/
│   ├── Materials/                         # Materials and physic materials
│   │   └── skybox/
│   ├── Models/                            # Level kits and models
│   │   ├── Level1_kitchen/
│   │   ├── Level2_Street/
│   │   ├── Level3_dock/
│   │   └── tut-environment/
│   ├── Prefabs/                           # Reusable prefabs (e.g., AntMinion, UI icons)
│   ├── Shader/                            # Custom shaders (e.g., CloudFresnel)
│   ├── Sprite/                            # 2D sprites
│   │   └── DeathImage/
│   ├── Textures/                          # Generic textures
│   │   └── drySoil/
│   └── UI/
│       ├── Cursor/
│       └── HomePageResource/
├── Resources/                             # Assets loaded via Resources API at runtime
│   └── FontManager.asset
├── Scenes/                                # Scene files for gameplay and menus
│   ├── Level0_Tutorial.unity
│   ├── Level1_kitchen.unity
│   ├── Level2_Street.unity
│   └── Level3_boss.unity
├── Scripts/                               # Gameplay and utility C# scripts
│   ├── Editor/                            # Editor tooling (inspectors, windows, tools)
│   └── Runtime/                           # Runtime code organized by domain/feature
│       ├── Attack/
│       ├── Camera/
│       ├── Characters/                    # Enemies, minions, interactions
│       ├── checkPoint/
│       ├── Core/
│       ├── Interactables/                 # Collectibles, Hazards, Mechanisms
│       ├── Level2/
│       ├── pigeon/
│       ├── Player/
│       ├── Systems/                       # Audio, Gameplay, Input, Saving
│       ├── UI/
│       └── Utilities/                     # Constants, Extensions, Helpers
├── TextMesh Pro/                          # TMP package assets (fonts, shaders, examples)
├── StartScene.unity                       # Entry/start scene
├── DefaultVolumeProfile.asset             # Global Volume profile (URP)
├── New Toon Render Pipeline Asset.asset   # Custom URP render pipeline asset (Toon)
├── New Universal Render Pipeline Asset_Renderer.asset  # URP renderer asset
└── UniversalRenderPipelineGlobalSettings.asset         # URP global settings
```

## Descriptions

- Art/
  - Primary location for visual assets included with the project. Contains numerous subfolders for materials, textures, sprites, prefabs, VFX, and other art resources used across scenes.

- Resources/
  - Holds assets that are loaded at runtime via `Resources.Load`. Use sparingly and prefer Addressables for large projects. Ensure paths are stable to avoid runtime load errors.

- Scenes/
  - Contains Unity scene files (`.unity`). Use subfolders for level-specific content if the number of scenes grows. The project also includes a top-level `StartScene.unity` in `Assets/` which acts as an entry point.

- Scripts/
  - C# source code for gameplay logic, UI, utilities, and systems. Organize by feature or domain (e.g., `Gameplay/`, `Systems/`, `UI/`). Keep filenames and class names in sync.

- TextMesh Pro/
  - TextMesh Pro package resources (fonts, shaders, examples). Avoid modifying package-provided assets unless duplicating them into your own folders.

- StartScene.unity
  - Startup scene. Configure in `File → Build Settings` as the first scene to load. Use it to bootstrap managers and transition to gameplay scenes.

- DefaultVolumeProfile.asset
  - Global Volume profile for URP post-processing. Tweak effects here (Bloom, Vignette, etc.) or create per-scene volumes for overrides.

- New Toon Render Pipeline Asset.asset
  - Custom URP Render Pipeline Asset using a toon/cel-shaded configuration. Assign via `Project Settings → Graphics` to apply globally.

- New Universal Render Pipeline Asset_Renderer.asset
  - URP Renderer Asset used by the pipeline. Configure renderer features (e.g., SSAO, Render Objects, 2D Renderer) here.

- UniversalRenderPipelineGlobalSettings.asset
  - URP global settings asset that controls defaults such as layer names for rendering and other pipeline-level options.

## Conventions

- Naming
  - Use PascalCase for scripts (e.g., `PlayerController.cs`) and descriptive names for assets and prefabs (e.g., `Ant_Worker.prefab`).

- Organization
  - Group scripts and assets by feature. Avoid large, catch-all folders. Mirror folder names between `Scripts/` and `Art/` when features are tightly coupled.

- Version Control
  - Do not commit generated folders like `Library/`, `Temp/`, `Obj/`, or `Build/`. Keep `.meta` files under version control. `.DS_Store` can be ignored or deleted.

- Rendering
  - Ensure the intended URP assets are selected in `Project Settings → Graphics` and `Quality`. Keep renderer features consistent across renderers if multiple are used.

## Notes

- If Addressables are adopted later, prefer placing runtime-loadable assets under Addressables groups instead of `Resources/`.
- Consider moving `StartScene.unity` into `Assets/Scenes/` for consistency if desired; update build settings accordingly.
