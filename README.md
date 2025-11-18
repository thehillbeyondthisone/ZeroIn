# ZeroIn
Find AFK players across Anarchy Online zones.

An AOSharp plugin that scans for nearby players while following a roamba path, with robust AFK detection and map overlay export capabilities.

## Features

- **Path-Based Scanning**: Follows your configured roamba path while scanning for players
- **Robust AFK Detection**: Multi-factor confidence scoring (0-100%) based on movement tracking
- **Visual Radar Overlay**: In-game visualization showing detection radius and player markers
- **Playfield Tracking**: Records zone information for cross-zone map overlay
- **Movement History**: Tracks total distance moved, stationary periods, and last movement time
- **Multiple Output Formats**: JSON, CSV, and summary text with full AFK analytics
- **Real-Time Display**: Live counts of detected vs AFK players in UI

## Installation

1. Build the project or download the compiled DLL
2. Place `ZeroIn.dll` in your AOSharp plugins folder
3. **IMPORTANT**: Do a CLEAN BUILD to ensure XML UI files are copied correctly
   - In Visual Studio: Build → Clean Solution, then Build → Rebuild Solution
4. Load the plugin in-game with `/plugin load ZeroIn`

## UI Guide - Every Button Explained

When you open the ZeroIn window in-game, you'll see these sections:

### Settings Section
- **Channel Id**: Text field for IPC (inter-process communication) channel number
  - Use same channel ID on multiple clients to coordinate scanning
  - Default: 1
- **Enable on inject**: Checkbox - auto-start scanning when plugin loads
- **Start/Stop**: Button - toggles the scanner on/off
  - Will show error if no scan path is configured

### Scan Path Section
- **Edit Scan Path**: Button - opens the roamba path editor
  - Click this FIRST to create your scanning route
  - Add waypoints by clicking on the map
  - The scanner will follow this path while detecting players

### Scanner Settings Section
- **Scan Spacing (m)**: Distance between scan waypoints (default: 40m)
- **Detection (m)**: Maximum range to detect players (default: 50m)
- **Only AFK Players**: Checkbox - filter output to only save AFK players
- **AFK Time (sec)**: Minimum stationary time to consider AFK (default: 30s)
- **Save to JSON**: Checkbox - export results as JSON file
- **Save to CSV**: Checkbox - export results as CSV file (includes AFK confidence %)
- **Log to Console**: Checkbox - print detections to chat window
- **Continuous Scanning**: Checkbox - loop the path indefinitely

### Scan Results Section
- **Detected: X**: Real-time count of all players detected
- **AFK: X**: Real-time count of likely AFK players
- **View Details**: Button - opens detailed results window with full player list
- **Save Config**: Button - saves all settings to config file

## Commands

- `/zeroin` - Opens the ZeroIn window
- `/radar` - Toggles visual radar overlay on/off
- `/zi` - Short alias for `/zeroin`

## How to Use

### Step-by-Step First Scan

1. **Create a Scan Path**:
   - Open ZeroIn window
   - Click "Edit Scan Path"
   - Add waypoints covering the area you want to scan
   - Close the path editor

2. **Configure Settings**:
   - Set "Detection (m)" to your preferred range (50m recommended)
   - Check "Save to CSV" for map overlay data
   - Optional: Enable "Continuous Scanning" to loop

3. **Start Scanning**:
   - Click the "Start" button
   - Your character will follow the path
   - Players are detected automatically within range

4. **Monitor Progress**:
   - Watch "Detected" and "AFK" counters update in real-time
   - Use `/radar` to see visual overlay of detection radius and player markers

5. **View Results**:
   - Click "View Details" to see full list with AFK confidence %
   - CSV files saved to: `%LocalAppData%\AOSharp\AOSP\ZeroIn\ScanResults\`

## AFK Detection - How It Works

ZeroIn uses **4 criteria** to determine AFK status with 0-100% confidence:

1. **Stationary Count** (30 points max)
   - Number of times detected without movement
   - 10+ stationary detections = full points

2. **Time Without Movement** (30 points max)
   - Seconds since last observed movement
   - 120+ seconds stationary = full points

3. **Low Distance Per Second** (20 points max)
   - Total distance moved / time observed
   - <0.1m per second = full points

4. **Multiple Sightings** (20 points max)
   - Total number of times spotted
   - 5+ sightings = full points

**Examples**:
- Player standing still for 2 minutes: 85-100% AFK confidence
- Player walking slowly but continuously: 0-20% AFK confidence
- Player that teleported away: Removed from tracked list

## CSV Output Format

The enhanced CSV export includes these columns for map overlay integration:

```
CharId, InstanceId, Name, PlayfieldId, PlayfieldName, TimesSpotted,
FirstSeen, LastSeen, PosX, PosY, PosZ, Distance, Health,
TotalDistanceMoved, StationaryCount, LastMovementTime, IsAFK, AFKConfidence
```

**Key Columns**:
- `PlayfieldId` / `PlayfieldName`: Zone identification for cross-zone mapping
- `TotalDistanceMoved`: Total meters moved during observation
- `StationaryCount`: Times detected without moving
- `IsAFK`: Boolean true/false
- `AFKConfidence`: 0-100% confidence score

## Visual Radar

Enable with `/radar` command.

**What You'll See**:
- **Green Circle**: Your detection radius (size = Detection setting)
- **Small Markers**: Detected players
  - **Larger markers (3m)**: Likely AFK players
  - **Smaller markers (2m)**: Active players
- Marker names include AFK status for debugging

The radar updates every 500ms to prevent spam.

## Configuration File

Located at: `%LocalAppData%\AOSharp\AOSP\ZeroIn\<CharacterName>_config.json`

### Key Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `ScanSpacing` | 40m | Waypoint spacing in scan path |
| `PlayerDetectionRange` | 50m | Maximum detection range |
| `ContinuousScanning` | false | Loop path continuously |
| `OnlyAFK` | false | Only save AFK players to output |
| `AFKCheckTimeSeconds` | 30 | Min seconds for AFK detection |
| `LogToConsole` | true | Print detections to chat |
| `SaveToJson` | true | Export JSON files |
| `SaveToCsv` | true | Export CSV files |

## Output Files

All results saved to: `%LocalAppData%\AOSharp\AOSP\ZeroIn\ScanResults\`

### File Naming
```
scan_<PlayfieldName>_<Timestamp>.json
scan_<PlayfieldName>_<Timestamp>.csv
scan_<PlayfieldName>_<Timestamp>.txt
```

### JSON Format
Complete detection data with all properties (playfield, movement tracking, AFK status).

### CSV Format
Optimized for spreadsheet analysis and map overlay tools. Includes:
- Exact coordinates (X, Y, Z)
- Playfield identification
- Movement statistics
- AFK confidence percentage

### TXT Summary
Human-readable report:
```
ZeroIn Scan Results - Perpetual Wastelands
Scan Time: 2025-11-18 12:30:45
Characters Detected: 15 (8 likely AFK)

PlayerName1 [AFK 95%]
  Position: (1234.5, 6789.0, 10.0)
  Playfield: Perpetual Wastelands (587)
  Distance: 42.3m
  Times Spotted: 12 | Distance Moved: 0.5m
  Last Seen: 5s ago
```

## Troubleshooting

### "no LDBintern (700:9187774)" appearing on buttons

**Solution**: Your build didn't copy the updated XML files!

1. Close the game completely
2. In Visual Studio: Build → Clean Solution
3. **DELETE** your bin/Debug or bin/Release folder manually
4. Build → Rebuild Solution
5. Restart game and inject plugin

### "No scan path loaded" error on Start

**Solution**:
1. Click "Edit Scan Path" button
2. Create waypoints covering your scan area
3. Close path editor
4. Now click Start

### Scanner not detecting anyone

**Checklist**:
- Are there actually players nearby within Detection range?
- Is "Detection (m)" set to 50 or higher?
- Did you click Start to begin scanning?

### AFK count always shows 0

**Likely Cause**: Players are actually moving!
- AFK detection requires multiple observations without movement
- Try increasing "AFK Time (sec)" to 60+
- Check "View Details" to see individual confidence scores

### Visual radar not showing

**Solutions**:
- Type `/radar` to toggle it on
- Check that you have players detected (Detected count > 0)
- Radar only shows when scanner is actively running

## End Goal - Map Overlay

This plugin is designed to export data for creating map overlay interfaces showing chronically AFK players across all zones in Rubika/Shadowlands.

The CSV export provides:
- Exact coordinates per playfield
- Zone identification (PlayfieldId + PlayfieldName)
- AFK confidence scoring
- Movement history statistics

Import the CSV into your map visualization tool to display persistent AFK farmer locations.

## Credits

Built from Automaton.Roamba framework. Enhanced with robust AFK detection and map overlay capabilities.

## License

Free to use and modify. Created for the Anarchy Online community.
