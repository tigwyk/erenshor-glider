# PRD: Erenshor Glider

## Introduction

Erenshor Glider is a full-featured automation bot for Erenshor, the single-player simulated MMO. Inspired by the legendary WoW-Glider, it provides complete AFK automation including combat, looting, grinding, leveling, and gathering. The bot features intelligent pathing, class-specific combat profiles, auto-mapping of resources, and a desktop GUI with 2D radar visualization.

Since Erenshor is a single-player game, this bot serves as a personal automation tool for players who want to experience the "bot life" or automate tedious grinding while focusing on other activities.

## Goals

- Provide full AFK automation for combat, looting, and grinding
- Support intelligent waypoint-based movement with auto-mapping capabilities
- Offer class-specific combat profiles with customizable rotations
- Include a desktop GUI with 2D radar, stats tracking, and controls
- Enable discovery and mapping of resource nodes, NPCs, and points of interest
- Allow community sharing of waypoint routes and combat profiles
- Research and implement appropriate technical approach for Erenshor integration

## User Stories

### US-001: Research Erenshor Technical Integration
**Description:** As a developer, I need to understand how to interface with Erenshor so I can choose the right technical approach.

**Acceptance Criteria:**
- [ ] Document Erenshor's technical architecture (Unity? Unreal? Custom?)
- [ ] Identify available integration methods (memory reading, mod support, API hooks)
- [ ] Determine how to read game state (player position, health, mana, target, nearby entities)
- [ ] Determine how to send inputs (movement, abilities, interactions)
- [ ] Document findings in `docs/technical-integration.md`

### US-002: Core Game State Reader
**Description:** As a bot, I need to read the current game state so I can make automation decisions.

**Acceptance Criteria:**
- [ ] Read player position (x, y, z coordinates)
- [ ] Read player health, mana/resource, level, XP
- [ ] Read current target information (if any)
- [ ] Read nearby entities (mobs, NPCs, players, resource nodes)
- [ ] Read player inventory state (bag space, items)
- [ ] Read player combat state (in combat, casting, dead)
- [ ] Read player buffs/debuffs
- [ ] State updates at minimum 10Hz
- [ ] Typecheck passes

### US-003: Input Controller
**Description:** As a bot, I need to send inputs to the game so I can control the character.

**Acceptance Criteria:**
- [ ] Send movement commands (forward, backward, strafe, turn)
- [ ] Send jump command
- [ ] Send ability/spell activation by keybind or ID
- [ ] Send target selection (nearest enemy, specific entity)
- [ ] Send interaction command (loot, talk to NPC, gather node)
- [ ] Send inventory management commands (use item, equip)
- [ ] Humanized input timing (configurable randomization)
- [ ] Typecheck passes

### US-004: Navigation System - Basic Movement
**Description:** As a bot, I need to move to specific coordinates so I can navigate the world.

**Acceptance Criteria:**
- [ ] Move to target coordinates using pathfinding
- [ ] Obstacle detection and avoidance
- [ ] Stuck detection and unstuck behavior
- [ ] Stop movement when destination reached (configurable radius)
- [ ] Face target direction
- [ ] Typecheck passes

### US-005: Waypoint System - Recording
**Description:** As a user, I want to record waypoints as I play so I can create patrol routes.

**Acceptance Criteria:**
- [ ] Start/stop waypoint recording via hotkey or GUI
- [ ] Automatically record position at configurable intervals
- [ ] Record waypoint types: normal, vendor, repair, mailbox, resource node
- [ ] Tag waypoints with metadata (node type, mob info)
- [ ] Save waypoint paths to file (JSON format)
- [ ] Visual feedback during recording (current waypoint count)
- [ ] Typecheck passes

### US-006: Waypoint System - Playback
**Description:** As a bot, I need to follow recorded waypoint paths so I can patrol and grind.

**Acceptance Criteria:**
- [ ] Load waypoint path from file
- [ ] Follow waypoints in sequence
- [ ] Loop path or reverse at end (configurable)
- [ ] Skip to nearest waypoint when starting mid-path
- [ ] Pause path following during combat
- [ ] Resume path after combat/looting complete
- [ ] Handle death: run back to corpse, resurrect, continue
- [ ] Typecheck passes

### US-007: Auto-Mapping System
**Description:** As a user, I want the bot to automatically discover and map resources as I play so I can build comprehensive route data.

**Acceptance Criteria:**
- [ ] Detect and record resource node locations (ore, herbs, etc.)
- [ ] Detect and record NPC locations (vendors, trainers, quest givers)
- [ ] Detect and record mob spawn points and types
- [ ] Store discoveries in local database with timestamps
- [ ] Avoid duplicate entries for same location
- [ ] Export map data to shareable format
- [ ] Typecheck passes

### US-008: Combat System - Target Selection
**Description:** As a bot, I need to intelligently select targets so I can efficiently grind.

**Acceptance Criteria:**
- [ ] Target nearest enemy within level range
- [ ] Prioritize enemies attacking the player
- [ ] Blacklist mobs by name/type (configurable)
- [ ] Avoid targeting tapped/tagged mobs (if applicable in Erenshor)
- [ ] Skip enemies that are too far from waypoint path
- [ ] Target priority list support (prefer certain mob types)
- [ ] Typecheck passes

### US-009: Combat System - Basic Combat Loop
**Description:** As a bot, I need to execute combat so I can kill enemies.

**Acceptance Criteria:**
- [ ] Engage target (move into range, face target)
- [ ] Execute combat rotation until target dead
- [ ] Handle target flee/evade behavior
- [ ] Disengage if combat takes too long (stuck detection)
- [ ] Wait for health/mana regeneration between fights (configurable threshold)
- [ ] Typecheck passes

### US-010: Combat Profiles - Rotation System
**Description:** As a user, I want to define combat rotations so the bot fights optimally for my class.

**Acceptance Criteria:**
- [ ] Define ability priority list with conditions
- [ ] Conditions: health %, mana %, buff present/absent, target health %, cooldown ready
- [ ] Support opener vs. combat vs. finisher phases
- [ ] Execute highest priority available ability
- [ ] Respect global cooldown
- [ ] Save/load profiles from file (JSON or YAML)
- [ ] Typecheck passes

### US-011: Combat Profiles - Class Templates
**Description:** As a user, I want pre-built class profiles so I can start botting immediately.

**Acceptance Criteria:**
- [ ] Create baseline profile for each Erenshor class
- [ ] Profiles handle basic combat effectively
- [ ] Profiles include self-healing where applicable
- [ ] Profiles include buff maintenance
- [ ] Profiles are editable and serve as templates
- [ ] Typecheck passes

### US-012: Looting System
**Description:** As a bot, I need to loot corpses so I can collect items and gold.

**Acceptance Criteria:**
- [ ] Detect lootable corpses nearby
- [ ] Move to corpse and loot
- [ ] Loot all items or filter by quality (configurable)
- [ ] Skip looting if bags full
- [ ] Return to previous activity after looting
- [ ] Typecheck passes

### US-013: Inventory Management
**Description:** As a bot, I need to manage inventory so I don't run out of bag space.

**Acceptance Criteria:**
- [ ] Track bag space usage
- [ ] Destroy junk items when bags full (configurable quality threshold)
- [ ] Trigger vendor run when bags full (if vendor waypoint defined)
- [ ] Sell junk to vendor
- [ ] Repair at vendor if durability low
- [ ] Restock consumables from vendor (configurable list)
- [ ] Typecheck passes

### US-014: Rest and Recovery
**Description:** As a bot, I need to manage health and mana so I survive and fight efficiently.

**Acceptance Criteria:**
- [ ] Eat food when health below threshold (out of combat)
- [ ] Drink water when mana below threshold (out of combat)
- [ ] Use health potions in emergency (in combat, configurable threshold)
- [ ] Use mana potions when needed (configurable)
- [ ] Wait for regen if no consumables available
- [ ] Typecheck passes

### US-015: Death Handling
**Description:** As a bot, I need to handle death gracefully so I can continue grinding.

**Acceptance Criteria:**
- [ ] Detect player death
- [ ] Run back to corpse (if applicable in Erenshor)
- [ ] Resurrect at corpse or graveyard
- [ ] Rebuff after resurrection
- [ ] Resume previous activity
- [ ] Stop after configurable death count (safety feature)
- [ ] Typecheck passes

### US-016: Desktop GUI - Main Window
**Description:** As a user, I want a desktop application so I can control and monitor the bot.

**Acceptance Criteria:**
- [ ] Start/Stop/Pause bot controls
- [ ] Current status display (state, target, location)
- [ ] Health/Mana bars visualization
- [ ] Session stats (runtime, kills, XP gained, gold gained, items looted)
- [ ] XP/hour, kills/hour, gold/hour calculations
- [ ] Log window showing bot actions
- [ ] Minimize to system tray
- [ ] Typecheck passes

### US-017: Desktop GUI - 2D Radar
**Description:** As a user, I want a 2D radar view so I can see what's around my character.

**Acceptance Criteria:**
- [ ] Top-down 2D view centered on player
- [ ] Show player position and facing direction
- [ ] Show nearby mobs (color-coded: hostile red, neutral yellow, friendly green)
- [ ] Show resource nodes (distinct icons by type)
- [ ] Show NPCs (distinct icons by type)
- [ ] Show waypoint path overlay
- [ ] Zoom in/out controls
- [ ] Configurable radar range
- [ ] Typecheck passes

### US-018: Desktop GUI - Profile Editor
**Description:** As a user, I want to edit combat profiles in the GUI so I don't have to edit files manually.

**Acceptance Criteria:**
- [ ] Load/save combat profiles
- [ ] Add/remove/reorder abilities in rotation
- [ ] Edit conditions for each ability
- [ ] Test ability conditions against current game state
- [ ] Duplicate existing profiles as templates
- [ ] Typecheck passes

### US-019: Desktop GUI - Waypoint Editor
**Description:** As a user, I want to edit waypoints in the GUI so I can refine my routes.

**Acceptance Criteria:**
- [ ] Load/save waypoint paths
- [ ] Visual display of waypoints on 2D map
- [ ] Add/remove/move waypoints
- [ ] Edit waypoint type and metadata
- [ ] Reverse path direction
- [ ] Merge multiple paths
- [ ] Typecheck passes

### US-020: Configuration System
**Description:** As a user, I want to configure bot behavior so I can customize it to my needs.

**Acceptance Criteria:**
- [ ] Configuration file (JSON/YAML) for all settings
- [ ] GUI settings panel for common options
- [ ] Per-character configuration profiles
- [ ] Import/export configuration
- [ ] Sensible defaults for all settings
- [ ] Typecheck passes

### US-021: Gathering Mode
**Description:** As a user, I want a gathering-focused mode so I can farm resources efficiently.

**Acceptance Criteria:**
- [ ] Prioritize resource nodes over combat
- [ ] Only fight if attacked (or configurable aggro radius)
- [ ] Follow route optimized for node locations
- [ ] Track node respawn timers
- [ ] Statistics for nodes gathered by type
- [ ] Typecheck passes

### US-022: Grinding Mode
**Description:** As a user, I want a grinding mode so I can level and farm mobs efficiently.

**Acceptance Criteria:**
- [ ] Prioritize finding and killing mobs
- [ ] Stay within defined grinding area
- [ ] Loot all kills
- [ ] Return to vendor when bags full
- [ ] Optimize for XP/hour or drops (configurable)
- [ ] Typecheck passes

### US-023: Community Sharing - Export/Import
**Description:** As a user, I want to share waypoints and profiles so the community can benefit.

**Acceptance Criteria:**
- [ ] Export waypoint paths to shareable file
- [ ] Export combat profiles to shareable file
- [ ] Export mapped resource data
- [ ] Import shared files from other users
- [ ] Validate imported files for safety
- [ ] Typecheck passes

### US-024: Safety Features
**Description:** As a user, I want safety features so the bot doesn't do something catastrophic.

**Acceptance Criteria:**
- [ ] Emergency stop hotkey (global, works even when game focused)
- [ ] Auto-stop after configurable runtime
- [ ] Auto-stop after configurable death count
- [ ] Auto-stop if character stuck for too long
- [ ] Whisper/chat detection with configurable response (stop, alert, ignore)
- [ ] Log all actions for review
- [ ] Typecheck passes

### US-025: Statistics and Logging
**Description:** As a user, I want detailed statistics so I can analyze my botting sessions.

**Acceptance Criteria:**
- [ ] Per-session statistics (kills, XP, gold, items, deaths, runtime)
- [ ] Historical session log
- [ ] XP/hour graphs over time
- [ ] Loot tables and drop rates
- [ ] Export statistics to CSV
- [ ] Typecheck passes

## Functional Requirements

- FR-1: The system must read game state from Erenshor at minimum 10Hz refresh rate
- FR-2: The system must send inputs to control character movement, combat, and interactions
- FR-3: The system must support waypoint recording with position capture at configurable intervals
- FR-4: The system must follow waypoint paths with obstacle avoidance and stuck detection
- FR-5: The system must auto-discover and map resource nodes, NPCs, and mob spawns
- FR-6: The system must intelligently select targets based on configurable priority rules
- FR-7: The system must execute combat rotations based on ability priority and conditions
- FR-8: The system must provide pre-built combat profiles for all Erenshor classes
- FR-9: The system must automatically loot corpses with configurable filtering
- FR-10: The system must manage inventory including vendor runs and item destruction
- FR-11: The system must handle consumables for health/mana recovery
- FR-12: The system must handle death, resurrection, and recovery automatically
- FR-13: The system must provide a desktop GUI with start/stop controls and session statistics
- FR-14: The system must display a 2D radar showing nearby entities and waypoints
- FR-15: The system must provide GUI editors for combat profiles and waypoint paths
- FR-16: The system must support configuration via file and GUI
- FR-17: The system must support distinct gathering and grinding operation modes
- FR-18: The system must support export/import of waypoints, profiles, and map data
- FR-19: The system must include safety features (emergency stop, auto-stop conditions, logging)
- FR-20: The system must track and display detailed session statistics

## Non-Goals

- No multiplayer server interaction (Erenshor is single-player)
- No anti-detection or obfuscation features (not needed for single-player)
- No account management or multi-boxing
- No auction house automation
- No quest automation (grinding/gathering focus only for v1)
- No 3D map rendering (2D radar only for v1)
- No cloud sync or online profile sharing platform (local file sharing only)
- No mobile companion app

## Technical Considerations

- **Integration Research Required:** First priority is determining how to interface with Erenshor (memory reading, mod API, etc.)
- **Technology Stack (Tentative):**
  - Desktop app: Electron, Tauri, or native (depending on integration needs)
  - If memory reading: May need native code (Rust, C++)
  - If mod-based: Match Erenshor's modding language
- **Data Storage:** SQLite for map data and session history, JSON for configs and profiles
- **Performance:** State reader must be efficient to avoid impacting game performance
- **Cross-platform:** Windows priority; Mac/Linux if feasible with chosen approach

## Success Metrics

- Bot can run unattended for 4+ hours without intervention
- Grinding efficiency within 70% of optimal manual play
- Less than 1 death per hour in appropriate-level content
- Resource gathering finds 90%+ of nodes along route
- GUI provides clear visibility into bot state at all times
- Users can create new waypoint routes in under 5 minutes
- Combat profiles can be customized without code knowledge

## Open Questions

1. What is Erenshor's technical architecture? (Unity/Unreal/Custom)
2. Does Erenshor have official mod/plugin support?
3. What anti-cheat (if any) does Erenshor employ? (likely none for single-player)
4. What classes exist in Erenshor and what are their combat mechanics?
5. How does death/resurrection work in Erenshor?
6. Are there instanced areas that require special handling?
7. What resource node types exist in Erenshor?
8. Is there a vendor/repair system similar to WoW?
