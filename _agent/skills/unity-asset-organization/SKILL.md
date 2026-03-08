---
name: unity-asset-organization
description: "Asset folder structure, naming conventions, and prefab organization best practices"
---

# Unity Asset Organization

## Overview
Standards for organizing assets, naming conventions, and folder structure to maintain clean, scalable Unity projects.

## Folder Structure

```
Assets/
├── Project/              # Project-specific assets (underscore keeps at top)
│   ├── Data/
│   │   ├── Materials/
│   │   ├── Textures/
│   │   ├── Shaders/
│   │   ├── Models/
│   │   ├── VFX/
│   │   ├── Audio/
│   │   │	├── Music/
│   │   │	└── SFX/
│   │   └── Animations/
│   ├── Prefabs/
│   │   ├── UI/
│   │   └── VFX/
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Gameplay/
│   │   ├── UI/
│   │   └── Utils/
│   └── ScriptableObjects/
├── Plugins/               # Third-party plugins, jslib
├── Resources/             # Runtime-loaded assets (use sparingly)
├── StreamingAssets/       # Files copied as-is to build
└── ThirdParty/            # External packages
```

## Naming Conventions

### General Rules
- Use **PascalCase** for all asset names
- Be descriptive: `PlayerHealthBar` not `HealthBar1`
- Include type suffix where helpful: `PlayerController`, `EnemyPrefab`

### Prefixes by Type
| Type | Prefix | Example |
|------|--------|---------|
| Prefab | - | `Player.prefab` |
| Material | M_ | `M_Metal.mat` |
| Texture | T_ | `T_Wood_Diffuse.png` |
| UI Sprite | UI_ | `UI_Button_Normal.png` |
| Animation | Anim_ | `Anim_Run.anim` |
| Animator | AC_ | `AC_Player.controller` |
| ScriptableObject | SO_ | `SO_PlayerStats.asset` |
| Audio Clip | - | `SFX_Jump.wav` |

### Texture Suffixes
| Suffix | Purpose |
|--------|---------|
| _Diffuse / _D | Albedo/Base color |
| _Normal / _N | Normal map |
| _Metallic / _M | Metallic map |
| _Roughness / _R | Roughness map |
| _AO | Ambient occlusion |
| _Emission / _E | Emission map |

## Prefab Best Practices

### Organization
- Group by feature/system, not by Unity type
- One prefab per file
- Keep prefab hierarchies shallow (max 3-4 levels)

### Naming
```
Player.prefab           # Main character
Enemy_Goblin.prefab     # Enemy variant
UI_HealthBar.prefab     # UI element
VFX_Explosion.prefab    # Visual effect
```

### Prefab Variants
```
Enemy_Base.prefab       # Base prefab
├── Enemy_Goblin.prefab    # Variant
├── Enemy_Skeleton.prefab  # Variant
└── Enemy_Boss.prefab      # Variant
```

## UI Organization
```
Prefabs/UI/
├── Common/
│   ├── UI_Button.prefab
│   ├── UI_Panel.prefab
│   └── UI_Text.prefab
├── Screens/
│   ├── UI_MainMenu.prefab
│   ├── UI_Settings.prefab
│   └── UI_GameOver.prefab
└── HUD/
    ├── UI_HealthBar.prefab
    └── UI_Minimap.prefab
```

## Resources Folder
Use sparingly - everything in Resources is included in build.

```csharp
// Only for truly dynamic loading
var prefab = Resources.Load<GameObject>("Prefabs/DynamicItem");
```

**Prefer**: Addressables or direct references for most assets.

## Best Practices
- ✅ Use consistent naming across entire project
- ✅ Group by feature, not by asset type
- ✅ Keep Resources folder minimal
- ✅ Use Addressables for large projects
- ✅ Document custom conventions in README
- ❌ **NEVER** use spaces in asset names
- ❌ **NEVER** use special characters (except underscore)
- ❌ **NEVER** scatter related assets across folders
