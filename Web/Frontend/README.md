# ZeroIn Command v5.2 - Web Frontend

An advanced real-time radar and map interface for Anarchy Online, featuring intelligent player detection, faction tracking, and sophisticated map calibration tools.

## Features

### Real-Time Player Detection
- **Faction-Based Tracking**: Automatically detects and categorizes players by faction (Clan, Omni, Neutral)
- **AFK Detection**: Advanced algorithms identify stationary players with confidence ratings
- **Distance Calculation**: Real-time distance tracking from your position
- **Smart Filtering**: Toggle visibility of players by faction and AFK status

### Interactive Map System
- **Dual-Layer Mapping**:
  - Global radar map (FullMap_Fixed.png) covering all major zones
  - Local map overlays (e.g., ICC_Map.jpg) for detailed area navigation
- **Leaflet.js Integration**: Smooth pan, zoom, and marker interactions
- **Calibration Tools**: Precise alignment system for both global and local maps

### Advanced Calibration

#### Global Radar Calibration ("Drag Radar")
1. Click "Drag Radar" button
2. Drag your player marker to the correct position on the map
3. System automatically calculates and saves zone offset coordinates
4. Each playfield remembers its calibration

#### Local Map Calibration ("Drag Minimap")
1. Click "Drag Minimap" button
2. Use the blue center handle to move the overlay
3. Use the white corner handle to resize (maintains aspect ratio)
4. Adjust opacity with slider
5. Use Flip X/Y toggles to match map orientation
6. "Snap To Player" centers overlay on your position

### User Interface

#### Statistics Panel
- Total players detected
- Breakdown by faction (Clan, Omni, Neutral)
- AFK player count
- Real-time updates every 500ms

#### Player List
- Sortable by distance
- Click to fly to player on map
- Shows player status and location
- Color-coded by faction

#### Connection Status
- Visual indicator (green dot = connected, red = disconnected)
- Real-time status updates
- Automatic reconnection handling

### Keyboard Shortcuts
- `H` - Toggle help modal
- `R` - Reset view to player position
- `C` - Toggle Clan filter
- `O` - Toggle Omni filter
- `N` - Toggle Neutral filter
- `A` - Toggle AFK filter

## Technical Details

### API Endpoints
The frontend communicates with the C# plugin backend via HTTP API:

- `GET /api/position` - Your current position and playfield
- `GET /api/players` - All detected players with metadata
- `GET /api/mapinfo` - Current playfield information and zone offsets
- `GET /api/status` - Plugin status and configuration

### Data Persistence
- All calibration data saved to browser localStorage
- Zone offsets persist across sessions
- Local map configurations stored per-layer
- Export/import via JSON

### Coordinate System
The system uses a sophisticated coordinate transformation:
1. Game coordinates (X, Y, Z) from Anarchy Online
2. Zone offset correction per playfield
3. Map coordinate scaling (divisor: 50.0)
4. Percentage-based positioning on image
5. Optional X/Z axis inversion for map alignment
6. Final pixel coordinates for rendering

### Performance
- 500ms refresh rate for player updates
- 5-second interval for map info updates
- Efficient marker update system (only changes rendered)
- Stale player cleanup after 10 seconds

## Installation

### Files Required
Place these files in your ZeroIn plugin directory under `Web/Frontend/`:
- `index.html` - Main application
- `FullMap_Fixed.png` - Global radar map (1024×1536px)
- `ICC_Map.jpg` - ICC zone local map
- Additional local maps as needed

### C# Plugin Integration
The HttpMapServer automatically serves these files:
- Root path `/` → `index.html`
- Static files → Direct file serving (`.jpg`, `.png`, `.gif`)
- API paths → JSON responses

## Usage

1. Start the ZeroIn plugin in Anarchy Online
2. Open http://localhost:8080 in your web browser
3. The map will auto-connect and display your position
4. Calibrate maps as needed for your current zone
5. Monitor detected players in real-time

## Browser Compatibility
- Chrome/Edge (recommended)
- Firefox
- Safari
- Any modern browser with ES6+ support

## Tips for Best Results

### First-Time Setup
1. Start in a known location (e.g., ICC Grid entrance)
2. Use "Drag Radar" to align your position
3. Add local map overlay if available
4. Use "Drag Minimap" to align local details
5. Export configuration for backup

### AFK Detection
Players are marked AFK when:
- Stationary for 60+ seconds (configurable)
- Observed multiple times without movement
- Confidence rating shows reliability (0-100%)

### Filtering Strategy
- Toggle faction filters to reduce clutter
- Enable AFK filter to focus on active threats
- Use player list to quickly locate specific targets

## Troubleshooting

### Map Not Loading
- Ensure image files are in `Web/Frontend/` directory
- Check browser console for 404 errors
- Verify HttpMapServer is running (port 8080)

### Players Not Appearing
- Check that scanning is enabled in plugin
- Verify detection range settings
- Ensure you're within range of other players

### Calibration Issues
- Use "Reset" to clear saved calibration
- Ensure you're in the correct playfield
- Try the "Snap To Player" function first

### Connection Problems
- Check firewall settings for localhost:8080
- Restart the plugin if needed
- Clear browser cache and reload

## Development

### Adding New Local Maps
1. Place image file in `Web/Frontend/`
2. Add to `localMaps` object in JavaScript:
   ```javascript
   "MapName": {
       file: "YourMap.jpg",
       bounds: [[minLat, minLng], [maxLat, maxLng]],
       minZoom: 2,
       opacity: 1.0,
       aspectRatio: 1.0
   }
   ```
3. Calibrate using "Drag Minimap" mode

### Extending the API
Add new endpoints in `HttpMapServer.cs`:
1. Add case in `HandleRequest()` switch
2. Create corresponding `Serve...()` method
3. Use `SendJson()` for JSON responses

## Credits

Built for ZeroIn - Anarchy Online automation plugin
- Leaflet.js for mapping library
- AOSharp SDK for game integration
- Original radar concept by the AO community

## Version History

### v5.2 (Current)
- Advanced faction-based filtering
- Statistics panel with real-time metrics
- Keyboard shortcuts
- Help modal with documentation
- Connection status indicators
- Enhanced UX with animations
- Improved calibration workflow

---

**For more information, press `H` in the web interface or visit the main ZeroIn documentation.**
