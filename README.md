# ZeroIn
Find that pesky someone.

An AOSharp plugin that systematically scans play areas using grid search patterns to detect and map AFK characters in Anarchy Online.

## Features

- **Grid Search Pattern**: Efficient lawnmower-pattern scanning with configurable spacing
- **4-Corner Area Definition**: Define irregular zone shapes using corner coordinates
- **Character Detection**: Automatically detects nearby players within 50m range
- **AFK Detection**: Tracks character movement to identify truly AFK players
- **Multiple Output Formats**: Saves results as JSON, CSV, and summary text files
- **Continuous Scanning**: Optional loop mode for ongoing surveillance
- **Configurable Filters**: Filter by AFK status, ignore specific names

## Installation

1. Build the project or download the compiled DLL
2. Place `ZeroIn.dll` in your AOSharp plugins folder
3. Load the plugin in-game with `/plugin load ZeroIn`

## Quick Start

1. **Add a new scan area:**
   ```
   /zeroin addarea "Perpetual Wastelands"
   ```

2. **Set the four corner coordinates:**
   - Move to the first corner of your scan area
   - `/zeroin setcorner 1`
   - Repeat for corners 2, 3, and 4

3. **Save your configuration:**
   ```
   /zeroin save
   ```

4. **Start scanning:**
   ```
   /zeroin start
   ```

5. **View results:**
   ```
   /zeroin status
   ```

## Commands

### Scan Control
- `/zeroin start` - Start scanning the current area
- `/zeroin stop` - Stop the current scan
- `/zeroin status` - Show scan status and detected characters

### Area Management
- `/zeroin area` - List all configured areas
- `/zeroin area <name|index>` - Switch to a different area
- `/zeroin addarea <name>` - Create a new scan area
- `/zeroin setcorner <1-4>` - Set corner N to your current position

### Configuration
- `/zeroin config` - Display current settings
- `/zeroin save` - Save configuration to file
- `/zeroin reload` - Reload configuration from file

### Help
- `/zeroin help` - Show all commands
- `/zi` - Short alias for `/zeroin`

## Configuration File

The plugin creates a configuration file at:
```
%LocalAppData%\AOSharp\AOSP\ZeroIn\<CharacterName>_config.json
```

### Key Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `ScanSpacing` | 40m | Distance between scan lines (40m with 50m detection = 10m overlap) |
| `PlayerDetectionRange` | 50m | Maximum range to detect players |
| `ContinuousScanning` | false | Loop the scan continuously |
| `OnlyAFK` | false | Only save characters that haven't moved |
| `AFKCheckTimeSeconds` | 30 | Seconds to observe before marking as AFK |
| `LogToConsole` | true | Print detections to console |
| `SaveToJson` | true | Save results as JSON |
| `SaveToCsv` | true | Save results as CSV |

## Output Files

Scan results are saved to:
```
%LocalAppData%\AOSharp\AOSP\ZeroIn\ScanResults\
```

### File Formats

**JSON** - Complete detection data with full character information:
```json
[
  {
    "Name": "PlayerName",
    "Position": { "X": 1234.5, "Y": 6789.0, "Z": 10.0 },
    "Level": 220,
    "Profession": "Engineer",
    "HasMoved": false
  }
]
```

**CSV** - Spreadsheet-compatible format:
```
Name,CharId,Level,Profession,Breed,Faction,X,Y,Z,FirstSeen,LastSeen,HasMoved,TimesSpotted
```

**Summary Text** - Human-readable report with statistics and character list

## How It Works

1. **Grid Generation**: Creates a lawnmower pattern covering the defined 4-corner area
2. **Movement**: Navigates through waypoints using AOSharp's pathfinding
3. **Scanning**: Continuously checks for nearby players (every 100ms)
4. **Tracking**: Records each character's position and monitors movement
5. **Output**: Saves results when scan completes or is stopped

### Search Pattern

The grid uses a lawnmower pattern with alternating row directions for efficiency:

```
1 → → → → 2
          ↓
4 ← ← ← ← 3
↓
5 → → → → 6
```

With 40m spacing and 50m detection range, there's a 10m overlap ensuring no character is missed.

## Tips

- **Corner Order**: Corners can be in any order, the plugin calculates bounding box automatically
- **Irregular Zones**: Works with non-rectangular zones by using min/max bounds
- **Multiple Areas**: Configure multiple zones and switch between them
- **AFK Detection**: Characters must be observed for at least `AFKCheckTimeSeconds` to be marked as AFK
- **Z-Axis**: Plugin uses average Z height, handles different elevations

## Example Use Cases

- **Finding AFK farmers** in popular grinding zones
- **Mapping player distribution** across playfields
- **Monitoring enemy positions** in PvP areas
- **Locating quest NPCs** when coordinates are unknown
- **Player census** for zone population data

## Troubleshooting

**"No valid area configured"**
- Make sure all 4 corners are set (use `/zeroin setcorner 1-4`)
- Check area validity with `/zeroin config`

**"Scan not detecting anyone"**
- Verify `PlayerDetectionRange` is 50m or higher
- Check that players are within the scan area bounds
- Ensure `IgnoreSelf` isn't filtering everyone

**"Character not saving to output"**
- If `OnlyAFK` is true, only stationary characters are saved
- Check `AFKCheckTimeSeconds` - may need longer observation time

## Credits

Based on the roamba movement pattern from the PetPersonas plugin.

## License

Free to use and modify. Created for the Anarchy Online community.
