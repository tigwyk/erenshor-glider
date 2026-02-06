# Erenshor Technical Integration Research

## Overview

Erenshor is a **Unity-based** single-player MMO simulation game available on Steam. The game has an active modding community using **BepInEx** as the primary modding framework.

## Game Engine

- **Engine**: Unity (Mono runtime)
- **Platform**: Windows (Steam)
- **Executable**: `Erenshor.exe`

## Modding Framework

### BepInEx
The game uses **BepInEx 5.x** (Mono) as the standard modding framework:
- Framework location: `steamapps/common/Erenshor/BepInEx/`
- Config location: `BepInEx/config/`
- Plugins location: `BepInEx/plugins/`

### Harmony Patching
BepInEx mods use **Harmony** for runtime method patching:
- Prefix patches (run before original method)
- Postfix patches (run after original method)
- Can intercept and modify game state

## Key Game Classes & APIs

Based on analysis of existing BepInEx mods, the following game classes and singletons are available:

### GameData (Static Singleton)
Central access point for game state:

```csharp
// Player-related
GameData.PlayerControl          // Player controller component
GameData.PlayerControl.transform.position  // Player world position (Vector3)
GameData.PlayerControl.Myself   // Player's Character component
GameData.PlayerControl.CurrentTarget  // Currently targeted Character
GameData.PlayerCombat           // Player combat controller
GameData.PlayerInv              // Player inventory
GameData.PlayerInv.ALLSLOTS[]   // All inventory slots
GameData.PlayerInv.Empty        // Empty item reference
GameData.PlayerAud              // Player audio source
GameData.PlayerTyping           // Is player typing in chat
GameData.CurrentCharacterSlot   // Current character save slot
GameData.CurrentCharacterSlot.index  // Character slot index

// UI and Systems
GameData.GM                     // GameManager singleton
GameData.GM.HKManager           // Hotkey manager
GameData.GM.HKManager.AllHotkeys  // List of all hotkey slots
GameData.GM.AutoEngageAttackOnSkill  // Auto-attack setting
GameData.GM.DemoBuild           // Is demo build flag
GameData.TextInput              // Chat/command text input
GameData.TextInput.typed.text   // Current typed text
GameData.BankUI                 // Bank UI controller
GameData.AHUI                   // Auction House UI
GameData.Smithing               // Blacksmithing/Forge UI
GameData.ItemOnCursor           // Item currently on cursor
GameData.InCharSelect           // Is in character select screen
GameData.AuctionWindowOpen      // Is auction window open
GameData.SFXVol                 // Sound effects volume

// Databases
GameData.SpellDatabase          // Spell database
GameData.SpellDatabase.GetSpellByID(id)  // Get spell by ID
GameData.SkillDatabase          // Skill database
GameData.SkillDatabase.GetSkillByID(id)  // Get skill by ID
GameData.Misc                   // Miscellaneous utilities
```

### Character Class
Represents any character (player, NPC, mob, SimPlayer):

```csharp
character.MyStats               // CharacterStats component
character.MyFaction             // Faction enum (Player, Enemy, Neutral, etc.)
character.MySkills              // Character skills component
character.MySkills.GetAscensionRank(id)  // Get ascension rank
```

### Character.Faction Enum
```csharp
Character.Faction.Player
Character.Faction.Enemy
// ... other factions
```

### PlayerCombat Class
```csharp
GameData.PlayerCombat.ToggleAttack()  // Toggle auto-attack
GameData.PlayerCombat.ForceAttackOn() // Force enable auto-attack
```

### CastSpell Component
Attached to characters that can cast spells:
```csharp
castSpell.KnownSpells           // List of known spells
castSpell.isPlayer              // Is this the player
castSpell.MyChar                // Parent Character reference
```

### Spell Class
```csharp
spell.Id                        // Unique spell ID string
spell.SelfOnly                  // Is self-cast only
spell.Type                      // SpellType enum
spell.Cooldown                  // Cooldown in seconds
spell.AutomateAttack            // Should trigger auto-attack
```

### Spell.SpellType Enum
```csharp
Spell.SpellType.Misc
// ... other types
```

### Skill Class
```csharp
skill.Id                        // Unique skill ID string
skill.SkillName                 // Display name
skill.Cooldown                  // Cooldown duration
```

### Hotkeys Class
Represents a hotbar slot:
```csharp
hotkey.AssignedSpell            // Assigned Spell (or null)
hotkey.AssignedSkill            // Assigned Skill (or null)
hotkey.AssignedItem             // Assigned InventorySlot (or null)
hotkey.thisHK                   // HKType enum
hotkey.Cooldown                 // Current cooldown remaining
hotkey.InvSlotIndex             // Inventory slot index if item
hotkey.PlayerSpells             // CastSpell component
hotkey.PlayerSkills             // Skills component
hotkey.MyImage                  // UI Image component
hotkey.ClearMe()                // Clear this hotkey
hotkey.AssignSpellFromBook(spell)    // Assign a spell
hotkey.AssignSkillFromBook(skill)    // Assign a skill
hotkey.AssignItemFrominv(slot)       // Assign an item
```

### Hotkeys.HKType Enum
```csharp
Hotkeys.HKType.Spell
Hotkeys.HKType.Skill
Hotkeys.HKType.Item
```

### NPC Class
```csharp
NPC.AggroOn()                   // Called when NPC aggros player
```

### Key Methods for Patching

| Method | Purpose | Patch Type |
|--------|---------|------------|
| `TypeText.CheckCommands()` | Command input parsing | Prefix |
| `PlayerCombat.ToggleAttack()` | Auto-attack toggle | Postfix |
| `NPC.AggroOn()` | NPC aggro trigger | Postfix |
| `Character.DoDeath()` | Character death event | Postfix |
| `ItemDatabase.Start()` | Item database initialization | Postfix |
| `AuctionHouseUI.OpenListItem()` | Auction item listing | Postfix |
| `CharSelectManager.LoadHotkeys()` | Character hotkey loading | Pre/Postfix |
| `Hotkeys.Update()` | Hotkey frame update | Pre/Postfix |
| `Hotkeys.DoHotkeyTask()` | Hotkey activation | Prefix |
| `SpellVessel.EndSpell()` | Spell cast completion | Prefix |

### Zone/Scene Management
```csharp
ZoneAtlas.Atlas                 // List of zone atlas entries
zoneAtlasEntry.ZoneName         // Zone name string
SceneManager.GetActiveScene()   // Current Unity scene
SceneManager.sceneLoaded        // Scene loaded event
```

### UI and Logging
```csharp
UpdateSocialLog.LogAdd(message, color)     // Add to social/chat log
UpdateSocialLog.CombatLogAdd(message, color)  // Add to combat log
```

## Reading Game State

### Player Position
```csharp
Vector3 position = GameData.PlayerControl.transform.position;
float x = position.x;
float y = position.y;
float z = position.z;
```

### Player Health/Mana (Inferred)
Based on code patterns, likely accessed via:
```csharp
GameData.PlayerControl.Myself.MyStats.CurrentHP  // Current health
GameData.PlayerControl.Myself.MyStats.MaxHP      // Max health
GameData.PlayerControl.Myself.MyStats.CurrentMP  // Current mana
GameData.PlayerControl.Myself.MyStats.MaxMP      // Max mana
```

### Current Target
```csharp
Character target = GameData.PlayerControl.CurrentTarget;
if (target != null) {
    CharacterStats stats = target.MyStats;
    // Access target info
}
```

### Combat State (Inferred)
Combat state likely available via:
- `GameData.PlayerCombat` properties
- Character state flags
- Need to investigate further with decompiler

### Nearby Entities
No direct API found - likely need to use Unity's physics or iterate game objects:
```csharp
// Possible approaches:
Physics.OverlapSphere(position, radius)
FindObjectsOfType<NPC>()
FindObjectsOfType<Character>()
```

## Sending Inputs

### Targeting
```csharp
// Target cycling appears to use Tab key
// Specific targeting would need further investigation
```

### Auto-Attack
```csharp
GameData.PlayerCombat.ToggleAttack();
GameData.PlayerCombat.ForceAttackOn();
```

### Ability Activation
Abilities are activated via hotkey simulation or direct method calls:
```csharp
// Via hotkey
hotkey.DoHotkeyTask();  // Private, needs reflection

// Via spell system
castSpell.StartSpell(spell, targetStats);
```

### Movement
Movement likely controlled via:
- Direct `transform.position` modification (may cause issues)
- Input simulation (keyboard keys)
- Character controller manipulation

## Recommended Approach

### For the Glider Bot
1. **Create a BepInEx plugin** - This is the standard and safest approach
2. **Use Harmony patches** to hook into game events (death, combat, etc.)
3. **Access GameData singleton** for reading game state
4. **Use reflection** for private fields/methods when needed
5. **Input simulation** for movement and ability use (safest approach)

### Technology Stack
- **Language**: C#
- **Framework**: BepInEx 5.x (Mono)
- **Build**: .NET Framework 4.7.2 or .NET Standard 2.0
- **IDE**: Visual Studio or Rider

### Project Structure
```
ErenshorGlider/
  ErenshorGlider.sln
  ErenshorGlider/
    ErenshorGlider.csproj
    Plugin.cs           # Main BepInEx plugin
    GameReader.cs       # Game state reading
    InputController.cs  # Input simulation
    Navigation.cs       # Movement logic
    Combat.cs           # Combat logic
    ...
```

## Character Classes in Erenshor

Six playable classes:

1. **Arcanist** - Cloth-wearing mage with various magic types
2. **Druid** - Nature magic, life and death forces
3. **Paladin** - Heavy armor, Solunarian magic (day/night)
4. **Reaver** - Combat stances, heavy damage dealer/tank
5. **Stormcaller** - Bow specialist with lightning magic
6. **Windblade** - Light armor offensive combat, lifesteal

## References

- [Thunderstore Erenshor Mods](https://thunderstore.io/c/erenshor/)
- [Steam Community Modding Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3485536525)
- [ErenshorQoL Source (GitHub)](https://github.com/Brumdail/ErenshorQoL)
- [Erenshor-ExtendedHotbars Source (GitHub)](https://github.com/MizukiBelhi/Erenshor-ExtendedHotbars)
- [Official Erenshor Wiki](https://erenshor.wiki.gg/)
- [BepInEx Documentation](https://docs.bepinex.dev/)

## Next Steps

1. Set up BepInEx development environment
2. Create decompiler project to explore game assemblies further
3. Document additional APIs as discovered
4. Implement core game state readers
5. Test input simulation methods
