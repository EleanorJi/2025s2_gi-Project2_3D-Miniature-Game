# Antventure Project Structure

## Complete Folder Architecture

```
Assets/
├── _Project/                           # Main project folder
│   ├── Scripts/                        # Script files
│   │   ├── Editor/                     # Editor-related scripts
│   │   │   ├── Tools/                  # Editor tools
│   │   │   ├── Inspectors/             # Custom inspectors
│   │   │   └── Windows/                # Editor windows
│   │   └── Runtime/                    # Runtime scripts
│   │       ├── Characters/             # Character-related scripts
│   │       │   ├── Player/             # Player controller
│   │       │   ├── Ants/               # Ant AI scripts
│   │       │   ├── NPCs/               # NPC scripts
│   │       │   └── Enemies/            # Enemy scripts
│   │       ├── Core/                   # Core systems
│   │       │   └── GameManager.cs     # Game manager
│   │       ├── Systems/                # Game systems
│   │       │   ├── Audio/              # Audio system
│   │       │   ├── Input/              # Input system
│   │       │   ├── Gameplay/           # Gameplay systems
│   │       │   └── Saving/             # Save system
│   │       ├── Camera/                 # Camera system
│   │       │   └── cameraFollow.cs     # Camera follow
│   │       ├── Interactables/          # Interactive objects
│   │       │   ├── Collectibles/       # Collectible items
│   │       │   ├── Mechanisms/         # Mechanisms and devices
│   │       │   └── Hazards/            # Hazardous objects
│   │       ├── UI/                     # User interface
│   │       │   ├── Menus/              # Menu interfaces
│   │       │   ├── HUD/                # Game interface
│   │       │   └── Common/             # Common UI components
│   │       └── Utilities/              # Utility classes
│   │           ├── Extensions/         # Extension methods
│   │           ├── Helpers/            # Helper tools
│   │           └── Constants/          # Constant definitions
│   │
│   ├── Art/                            # Art assets
│   │   ├── Animations/                 # Animation files
│   │   ├── Materials/                  # Material files
│   │   ├── Models/                     # 3D models
│   │   ├── Textures/                   # Texture files
│   │   ├── Environment/                # Environment art
│   │   ├── Characters/                 # Character art
│   │   ├── UI/                         # UI art assets
│   │   └── Effects/                    # Effects art
│   │
│   ├── Audio/                          # Audio assets
│   │   ├── Music/                      # Background music
│   │   ├── SFX/                        # Sound effect files
│   │   ├── Voice/                      # Voice files
│   │   └── Mixers/                     # Audio mixers
│   │
│   ├── Prefabs/                        # Prefabs
│   │   ├── Characters/                 # Character prefabs
│   │   ├── UI/                         # UI prefabs
│   │   ├── Environment/                # Environment prefabs
│   │   └── Effects/                    # Effect prefabs
│   │
│   ├── Scenes/                         # Scene files
│   │   ├── Level1_kitchen/             # Kitchen level assets
│   │   ├── Level1.unity                # Kitchen level scene
│   │   ├── Level1-2.unity              # Kitchen level variant
│   │   └── StartScene.unity            # Start scene
│   │
│   ├── ScriptableObjects/              # Scriptable objects
│   ├── Settings/                       # Project settings
│   └── Gizmos/                         # Gizmos icons
│
├── script/                             # Original scripts (to be migrated)
│   ├── cameraFollow.cs                 # Original camera script
│   └── PlayerController.cs             # Original player controller
│
├── materials/                          # Original materials folder
├── Level1_kitchen/                     # Original level assets
└── images/                             # Project image assets
```

## Folder Purpose Description

### Scripts
- **Editor/**: Unity editor extension scripts, only run in editor mode
- **Runtime/**: Game runtime scripts, will be packaged into the final game

#### Characters
- **Player/**: Player ant control scripts
- **Ants/**: AI assistant ant scripts
- **NPCs/**: Non-player character scripts
- **Enemies/**: Enemy and Boss scripts

#### Systems
- **Audio/**: Audio management and sound effect playback systems
- **Input/**: Input handling and key mapping systems
- **Gameplay/**: Summoning systems and game mechanics
- **Saving/**: Save loading and progress saving

#### Interactables
- **Collectibles/**: French fry crumbs, hidden items, etc.
- **Mechanisms/**: Switches, mechanisms, portals, etc.
- **Hazards/**: Traps, dangerous areas, etc.

### Art
Art assets categorized by function and type, facilitating artist collaboration

### 🔊 Audio
- **Music/**: Background music, supporting different levels
- **SFX/**: Sound effect files, categorized by type
- **Voice/**: Voice dialogue (if needed)
- **Mixers/**: Unity audio mixers

### 🔧 Prefabs
Reusable game object templates

### 🗺️ Scenes
Game levels and main scene files

## Usage Recommendations

### 1. Naming Conventions
- **File names**: PascalCase (e.g., `PlayerController.cs`)
- **Folder names**: PascalCase (e.g., `Characters/`)
- **Prefabs**: Descriptive names (e.g., `Ant_Worker_Prefab`)

### 2. Script Organization
- Each script contains only one main class
- Scripts with related functionality are placed in the same folder
- Use namespaces to avoid conflicts

### 3. Asset Management
- Art assets categorized by function
- Prefabs use consistent naming
- Regularly clean up unused assets

### 4. Version Control
- Ignore Unity-generated folders like `Library/`, `Temp/`, etc.
- Only commit source files, not build artifacts
- Use `.gitignore` file to exclude unnecessary files

## Migration Plan

### Phase 1: Script Organization
1. Migrate scripts from `script/` folder to new structure
2. Update script references and namespaces
3. Delete old script folders

### Phase 2: Asset Organization
1. Organize material files in `materials/`
2. Optimize model assets in `Level1_kitchen/`
3. Create standardized prefabs

### Phase 3: Scene Optimization
1. Clean up useless objects in existing scenes
2. Apply new prefab system
3. Set correct tags and hierarchies

## Quick Navigation

### Common File Locations
- **Game Manager**: `_Project/Scripts/Runtime/Core/GameManager.cs`
- **Player Controller**: `_Project/Scripts/Runtime/Characters/Player/PlayerController.cs`
- **Camera System**: `_Project/Scripts/Runtime/Camera/cameraFollow.cs`
- **Main Scene**: `_Project/Scenes/Level1.unity`

### When Adding New Features
1. Determine feature type (character/system/UI, etc.)
2. Create scripts in corresponding folder
3. Use appropriate namespaces
4. Create corresponding prefabs (if needed)

---

This structure provides a clear and scalable organization for the Antventure project, facilitating team collaboration and project maintenance. 
