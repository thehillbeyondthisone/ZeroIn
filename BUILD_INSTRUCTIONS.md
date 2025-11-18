# ZeroIn Build Instructions

## The XML File Problem - Why You're Still Seeing "no LDBintern" Text

### What's Happening

1. **Source XML files** are in `UI/Views/` folder in the repository ✓ (These are CORRECT)
2. During build, MSBuild **copies** these XML files to `bin/Debug/` or `bin/Release/` folder
3. **The game loads XML from bin/Debug**, NOT from the source folder
4. If you don't delete `bin/Debug` before rebuilding, **the old buggy XML files stay there**
5. Visual Studio's "Clean" command doesn't always delete everything

### The Solution - Manual Clean Build

```bash
# 1. Close the game completely (very important!)

# 2. In Visual Studio:
Build → Clean Solution

# 3. MANUALLY delete the output folder:
# Navigate to your ZeroIn project folder and DELETE:
bin/Debug/
# or
bin/Release/

# 4. Now rebuild:
Build → Rebuild Solution

# 5. The fresh XML files are now copied to bin/Debug

# 6. Start game and inject ZeroIn.dll
```

### How to Verify It Worked

After injecting the plugin, open the ZeroIn window and check:
- Settings section should have "Channel Id" label (not "no LDBintern")
- Scanner Settings should have "Scan Spacing (m)" and "Detection (m)" labels
- All text should be readable and meaningful

If you STILL see "no LDBintern" numbers, you didn't fully delete the bin folder.

## Project Structure

```
ZeroIn/
├── UI/
│   ├── Views/
│   │   ├── BuddyCoreView.xml       ← Source XML (correct)
│   │   └── ScanSettingsView.xml    ← Source XML (correct)
│   └── Windows/
│       └── MainWindow.xml           ← Source XML (correct)
├── bin/
│   └── Debug/                       ← BUILD OUTPUT (delete this for clean build!)
│       ├── ZeroIn.dll
│       ├── UI/
│       │   ├── Views/
│       │   │   ├── BuddyCoreView.xml    ← Copied during build
│       │   │   └── ScanSettingsView.xml ← Copied during build
│       │   └── Windows/
│       │       └── MainWindow.xml       ← Copied during build
```

## MSBuild Copy Process

The `.csproj` file contains these lines (or similar):

```xml
<ItemGroup>
  <Content Include="UI\**\*.xml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

**PreserveNewest** means:
- If file doesn't exist in output → copy it
- If file exists and source is NEWER → copy it
- If file exists and source is OLDER or SAME → **DON'T COPY IT**

This is why you must manually delete the output folder!

## Common Build Mistakes

### ❌ Wrong: Just clicking "Build"
```
Build → Build Solution
```
This won't update XML files if they already exist in bin/Debug.

### ❌ Wrong: Just clicking "Clean"
```
Build → Clean Solution
```
Visual Studio's clean is incomplete - some files remain.

### ✅ Correct: Manual clean + rebuild
```
1. Build → Clean Solution
2. Manually delete bin/Debug or bin/Release folder
3. Build → Rebuild Solution
```

## Build Configurations

Make sure you're building and testing the same configuration:

- **Debug**: Builds to `bin/Debug/` - use this for development
- **Release**: Builds to `bin/Release/` - use this for distribution

If you build in Debug but copy from Release folder (or vice versa), you'll use the wrong DLL!

## File Copying Debug

To see if XML files are being copied during build:

1. In Visual Studio: Tools → Options → Projects and Solutions → Build and Run
2. Set "MSBuild project build output verbosity" to **Detailed**
3. Build the project
4. Check the Output window for lines like:
   ```
   Copying file from "UI\Views\BuddyCoreView.xml" to "bin\Debug\UI\Views\BuddyCoreView.xml"
   ```

If you DON'T see these copy messages, the files aren't being updated!

## Testing Checklist

Before testing in-game:

- [ ] Closed game completely
- [ ] Build → Clean Solution
- [ ] Manually deleted bin/Debug folder
- [ ] Build → Rebuild Solution
- [ ] Verified XML files exist in bin/Debug/UI/Views/
- [ ] Opened one XML file in bin/Debug to verify it has NO `label=` attribute on HLayoutGroup
- [ ] Started game
- [ ] Injected ZeroIn.dll from bin/Debug folder
- [ ] Opened ZeroIn window and verified labels are correct

## If It STILL Doesn't Work

1. **Check which DLL you're loading**:
   - Make sure you're loading from `bin/Debug/ZeroIn.dll`
   - Not from some old backup folder

2. **Verify the source XML is correct**:
   ```bash
   grep "HLayoutGroup.*label=" UI/Views/*.xml
   ```
   Should return NO results. If it finds matches, the source XML still has bugs.

3. **Check .csproj file** for XML file includes:
   - Open ZeroIn.csproj in a text editor
   - Look for `<Content Include="UI\**\*.xml">`
   - Verify `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`

4. **Nuclear option - Rebuild everything**:
   ```bash
   # Delete ALL build artifacts
   rm -rf bin/ obj/

   # Rebuild
   Build → Rebuild Solution
   ```

## Current Branch Status

Branch: `claude/clean-working-01XvDD8XKA8YU4Lz8JqHYwd5`

This branch contains:
- ✅ Fixed BuddyCoreView.xml (no label on HLayoutGroup)
- ✅ Fixed ScanSettingsView.xml (no label on HLayoutGroup)
- ✅ Robust AFK detection system
- ✅ Visual radar overlay
- ✅ Movement tracking
- ✅ Playfield data for map overlay
- ✅ Enhanced CSV/JSON/TXT exports

All fixes are in the source code. You just need to get them into your bin/Debug folder!
