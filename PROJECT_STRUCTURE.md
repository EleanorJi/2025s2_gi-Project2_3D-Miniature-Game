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
└── UniversalRenderPipelineGlobalSettings.asset         # URP global settings
```

## Descriptions

- Art/
  - Primary library of visual assets used across all scenes.
  - Notable subfolders:
    - Animations/: Player, helper ant, and pigeon animation clips and controllers.
    - Audio/: Level music/SFX organization plus mixers.
    - Materials/: URP materials, skybox, and physics materials.
    - Models/: Level kits and props grouped by level (kitchen, street, dock, tutorial env).
    - Prefabs/: Reusable VFX/UI/gameplay prefabs (e.g., AntMinion, CookieIcon, TelegraphRing).
    - Shader/: Custom shaders (e.g., CloudFresnel, water/sink foam effects).
    - Sprite/: 2D sprites including DeathImage set.
    - Textures/: Shared texture sets (e.g., drySoil, sand).
    - UI/: Cursor textures and home page assets.

- Resources/
  - Holds assets loaded at runtime via `Resources.Load` (e.g., `FontManager.asset`).
  - Usage notes:
    - Keep `Resources/` small and intentional; consider Addressables for scalable content.
    - Paths used in `Resources.Load` are case-sensitive and must match folder structure.

- Scenes/
  - Contains gameplay and boss scenes. Current scenes:
    - Level0_Tutorial.unity: Tutorial/intro level.
    - Level1_kitchen.unity: Kitchen map.
    - Level2_Street.unity: Street map.
    - Level3_boss.unity: Boss encounter.
  - Top-level `StartScene.unity` serves as entry/bootstrap scene.

- Scripts/
  - C# source code organized by domain:
    - Editor/: Custom inspectors, tools, and editor windows.
    - Runtime/:
      - Attack/: Projectiles and telegraphing.
      - Camera/: Camera follow and intro animations per level.
      - Characters/: Enemy and minion logic (boss pigeon, health, particles).
      - checkPoint/: Checkpoints, level finish triggers, and tutorial buttons.
      - Core/: Core scene ordering/bootstrap logic.
      - Interactables/: Collectibles, hazards, and mechanisms.
      - Level2/: Level-specific logic for Level 2.
      - pigeon/: Shared pigeon behavior scripts.
      - Player/: Player controller, combat, health, HUD bindings.
      - Systems/: Audio, Gameplay, Input, Saving sub-systems.
      - UI/: UI managers, menus, HUD, cursor controllers, font manager.
      - Utilities/: Constants, Extensions, Helpers used across systems.

- TextMesh Pro/
  - TMP built-in resources (fonts, shaders, examples & extras).
  - Best practices:
    - Avoid modifying assets under TMP package folders; duplicate into project folders if customization is needed.
    - Centralize font/material settings in TMP Settings and project-level font managers.

- StartScene.unity
  - Startup scene. Configure in `File → Build Settings` as the first scene to load. Use it to bootstrap managers and transition to gameplay scenes.

- DefaultVolumeProfile.asset
  - Global Volume profile for URP post-processing. Tweak effects here (Bloom, Vignette, etc.) or create per-scene volumes for overrides.

- UniversalRenderPipelineGlobalSettings.asset
  - URP global settings asset that controls defaults such as layer names for rendering and other pipeline-level options.

## Conventions

- Naming
  - Use PascalCase for scripts (e.g., `PlayerController.cs`) and descriptive names for assets/prefabs (e.g., `Ant_Worker.prefab`).
  - Keep class names and filenames in sync to avoid Unity reference issues.

- Organization
  - Group scripts and assets by feature/level. Mirror folder names between `Scripts/` and `Art/` where possible.
  - Place level-specific logic in a level folder (e.g., `Scripts/Runtime/Level2/`).
  - Keep prefabs next to their visual assets when they are tightly coupled.

- Version Control
  - Do not commit generated folders like `Library/`, `Temp/`, `Obj/`, or `Build/`. Keep `.meta` files under version control.
  - Exclude OS metadata files from version control.

- Rendering
  - Ensure the intended URP assets are selected in `Project Settings → Graphics` and `Quality`. Keep renderer features consistent across renderers if multiple are used.
  - Tune `DefaultVolumeProfile.asset` for global post-processing; use local Volumes for per-scene overrides.

## Notes

- If Addressables are adopted later, prefer placing runtime-loadable assets under Addressables groups instead of `Resources/`.
- Consider moving `StartScene.unity` into `Assets/Scenes/` for consistency if desired; update build settings accordingly.
- When adding new levels, create `Scenes/LevelX_*` scenes and mirror asset/script structure under `Art/Models` and `Scripts/Runtime/LevelX/` for clarity.
