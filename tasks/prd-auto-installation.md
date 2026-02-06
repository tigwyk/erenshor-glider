# PRD: Automated BepInEx Installation and Plugin Management

## Introduction

This feature eliminates the need for users to manually install and configure BepInEx, the plugin loading framework required for Erenshor Glider. Currently, users must download BepInEx, extract it to their Erenshor folder, and manually copy plugin DLLs. This automation will handle the entire installation process through the GUI, making the bot accessible to users without technical knowledge of mod loaders or game file manipulation.

## Goals

- Eliminate manual BepInEx installation steps for end users
- Provide automatic detection of Erenshor installation directory via Steam
- Offer manual directory selection as fallback
- Enable one-click plugin installation and updates
- Provide integrated game launching through the GUI
- Reduce support burden related to installation issues
- Make the bot usable by non-technical players

## User Stories

### US-001: Download BepInEx automatically
**Description:** As a user, I want the GUI to automatically download BepInEx so that I don't need to find and download it manually.

**Acceptance Criteria:**
- [ ] GUI downloads BepInEx x64 from official GitHub releases
- [ ] Download progress is displayed to the user
- [ ] Downloaded files are cached locally for future use
- [ ] Handles download failures with clear error messages
- [ ] Validates download integrity (checksum verification)

### US-002: Auto-detect Erenshor installation via Steam
**Description:** As a user, I want the GUI to automatically find my Erenshor installation so I don't need to browse for it.

**Acceptance Criteria:**
- [ ] GUI queries Steam registry for installed games
- [ ] Correctly identifies Erenshor installation path on Windows
- [ ] Falls back to manual selection if auto-detection fails
- [ ] Displays detected path to user for confirmation
- [ ] Validates the detected path contains Erenshor.exe

### US-003: Manual directory selection fallback
**Description:** As a user, I want to manually browse and select my Erenshor folder if auto-detection fails.

**Acceptance Criteria:**
- [ ] Folder browser dialog opens for manual selection
- [ ] User can navigate to any directory
- [ ] Selected path is validated as containing Erenshor.exe
- [ ] Clear error message if invalid directory selected
- [ ] Selected path is persisted for future sessions

### US-004: Extract and install BepInEx to Erenshor folder
**Description:** As a user, I want BepInEx automatically extracted and installed in the correct location.

**Acceptance Criteria:**
- [ ] BepInEx files are extracted to Erenshor root directory
- [ ] Correct BepInEx folder structure is created (plugins, core, etc.)
- [ ] Installation progress is displayed
- [ ] Backup of existing doorstop_config.dll if present
- [ ] Installation verification confirms BepInEx is properly installed

### US-005: One-click plugin installation
**Description:** As a user, I want a single button to install the ErenshorGlider plugin DLL to the BepInEx plugins folder.

**Acceptance Criteria:**
- [ ] "Install Plugin" button in GUI main window
- [ ] Copies ErenshorGlider.dll to BepInEx/plugins folder
- [ ] Overwrites existing plugin if already present
- [ ] Shows confirmation message on success
- [ ] Handles file locked errors (game running) with helpful message
- [ ] Creates plugins folder if it doesn't exist

### US-006: Check for plugin updates
**Description:** As a user, I want the GUI to notify me when a new version of the plugin is available.

**Acceptance Criteria:**
- [ ] GUI queries GitHub releases for latest version on startup
- [ ] Compares local version against latest release
- [ ] Shows notification when update is available
- [ ] Displays changelog/release notes for new version
- [ ] Handles offline mode gracefully (no error)

### US-007: Download and install plugin updates
**Description:** As a user, I want to update the plugin with one click when a new version is available.

**Acceptance Criteria:**
- [ ] "Update Now" button appears when update is available
- [ ] Downloads new plugin DLL from GitHub releases
- [ ] Backs up existing DLL before update
- [ ] Installs new DLL to BepInEx/plugins folder
- [ ] Shows download and installation progress
- [ ] Confirms successful update with version number

### US-008: Launch game through GUI
**Description:** As a user, I want to launch Erenshor directly from the GUI with BepInex loaded.

**Acceptance Criteria:**
- [ ] "Launch Game" button in main window
- [ ] Starts Erenshor.exe with BepInex injection active
- [ ] Button is disabled if game is already running
- [ ] Optionally minimizes GUI when game launches
- [ ] Detects when game process exits
- [ ] Shows game running status in GUI

### US-009: Installation status display
**Description:** As a user, I want to see the current installation status of BepInEx and the plugin at a glance.

**Acceptance Criteria:**
- [ ] Status panel shows BepInEx installation state (Not Installed / Installed / Update Available)
- [ ] Status panel shows plugin installation state (Not Installed / Installed / Update Available)
- [ ] Shows installed versions for both BepInEx and plugin
- [ ] Visual indicators (colors/icons) for status
- [ ] Clicking status opens installation details

### US-010: First-run setup wizard
**Description:** As a new user, I want a guided setup wizard that walks me through the initial installation.

**Acceptance Criteria:**
- [x] Wizard launches on first run if no installation detected
- [x] Step-by-step flow: Welcome → Detect Erenshor → Install BepInEx → Install Plugin → Complete
- [x] Each step has clear instructions and visual indicators
- [x] User can go back to previous steps
- [x] Wizard can be skipped and accessed later from menu
- [x] Shows success message with "Launch Game" option at completion

**Status:** ✅ Complete

### US-011: Repair installation function
**Description:** As a user, I want to repair my installation if something goes wrong.

**Acceptance Criteria:**
- [x] "Repair Installation" button in settings/help menu
- [x] Reinstalls BepInEx files without changing user configuration
- [x] Reinstalls plugin DLL
- [x] Validates all files are correct
- [x] Shows detailed repair log
- [x] Reports what was fixed or what problems remain

**Status:** ✅ Complete

### US-012: Uninstallation support
**Description:** As a user, I want to cleanly remove BepInEx and the plugin if I stop using the bot.

**Acceptance Criteria:**
- [x] "Uninstall" button in settings
- [x] Shows warning dialog with confirmation step
- [x] Removes BepInEx files and folders
- [x] Removes plugin DLL
- [x] Optionally backs up saves/config before uninstall
- [x] Confirms successful uninstallation

**Status:** ✅ Complete

## Functional Requirements

- FR-1: System shall download BepInEx x64 from https://github.com/BepInEx/BepInEx/releases
- FR-2: System shall detect Erenshor installation path via Steam registry key (HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2879460)
- FR-3: System shall provide folder browser dialog for manual Erenshor directory selection
- FR-4: System shall validate selected directory contains Erenshor.exe before proceeding
- FR-5: System shall extract BepInEx archive contents to Erenshor root directory
- FR-6: System shall copy ErenshorGlider.dll to BepInEx/plugins directory
- FR-7: System shall query GitHub API for latest ErenshorGlider release version
- FR-8: System shall compare local assembly version against latest release
- FR-9: System shall download and install plugin updates from GitHub releases
- FR-10: System shall launch Erenshor.exe process from detected installation path
- FR-11: System shall detect if Erenshor.exe process is already running
- FR-12: System shall persist Erenshor installation path in application settings
- FR-13: System shall cache downloaded BepInEx archive locally for reinstallations
- FR-14: System shall provide first-run setup wizard for new installations
- FR-15: System shall validate BepInEx installation by checking for core DLL files
- FR-16: System shall provide repair function to fix broken installations
- FR-17: System shall provide uninstall function to remove BepInEx and plugin files

## Non-Goals (Out of Scope)

- Automatic installation of Erenshor game itself
- Management of other BepInEx plugins besides ErenshorGlider
- BepInEx configuration editing (advanced settings like logging levels)
- Linux/Proton support (Windows only initially)
- Multiple Erenshor installation profiles
- Automatic creation/management of combat profiles or game configuration
- Cloud save integration for bot configurations
- In-game overlay or UI

## Design Considerations

### UI/UX Requirements
- Clean, non-technical language throughout installation process
- Progress bars for all download and extraction operations
- Clear visual feedback for each installation step
- Status icons: ✓ (installed), ⚠ (update available), ✗ (not installed/error)
- First-run wizard should be friendly and approachable for non-technical users
- Error messages should be actionable (e.g., "Close Erenshor and try again" vs "File access denied")

### Mockups Needed
- First-run setup wizard screens
- Main window status panel design
- Settings/Installation management dialog
- Update available notification design

### Existing Components to Reuse
- Current MainWindow.cs as base for installation UI
- ConfigManager for persisting installation path
- Existing GitHub release checking logic if any

## Technical Considerations

### Dependencies
- Steam Registry access (Microsoft.Win32.Registry)
- HTTP client for downloads (System.Net.Http)
- ZIP extraction (System.IO.Compression)
- GitHub API for version checking (optional: can use HTML scraping)
- Process management for launching/monitoring Erenshor.exe

### Known Constraints
- Requires administrator privileges if Erenshor is installed in Program Files
- BepInEx version compatibility (must use correct BepInEx version for Unity backend)
- File locking issues if game is running during installation
- GitHub API rate limiting for version checks

### Integration Points
- Existing ErenshorGlider.GUI application
- GitHub releases (BepInEx and ErenshorGlider)
- Steam installation registry
- Local file system

### Performance Requirements
- BepInEx download (~3MB) should complete in under 10 seconds on typical broadband
- Extraction should complete in under 5 seconds
- Startup version check should not block GUI initialization (async)
- Installation wizard should complete in under 2 minutes total

### Security Considerations
- Validate downloaded files via checksum/SHA256
- Use HTTPS for all downloads
- Warn user if running as administrator
- Don't expose sensitive paths in error messages

## Success Metrics

- Reduce time to first bot run from ~15 minutes (manual installation) to under 2 minutes
- Reduce installation-related support requests by 80%
- 95% of new users complete installation without visiting documentation
- Average support ticket resolution time for installation issues reduced by 60%
- User survey shows 4+ star rating for installation experience

## Open Questions

1. Should we bundle BepInEx with the GUI installer instead of downloading at runtime?
   - Pro: Faster installation, works offline
   - Con: Larger download size, harder to update BepInEx version

2. Should we support Erenshor installations outside of Steam (e.g., Game Pass, standalone)?
   - Would require additional detection methods

3. How should we handle BepInEx configuration file conflicts on updates?
   - Preserve user changes vs. reset to defaults

4. Should the GUI automatically close when game launches?
   - Some users want to monitor bot while playing

5. Minimum BepInEx version required? (Need to verify game compatibility)

6. Should we support beta/development builds of the plugin?
   - Opt-in to prereleases via settings?
