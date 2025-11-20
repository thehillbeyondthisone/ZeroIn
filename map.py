from PIL import Image
import io
import os

# Configuration
txt_file = 'PlanetMapIndexNormal.txt'
bin_file = 'PlanetMapGfxNormal.bin'
output_image = 'FullMap_Fixed.png'

def extract_and_stitch():
    # 1. Parse the text file for the High Res layer
    tile_offsets = []
    with open(txt_file, 'r') as f:
        lines = f.readlines()

    # Logic to find the 1024x1536 block
    capturing = False
    for line in lines:
        if "Size 1024 1536" in line:
            capturing = True
            continue
        if capturing and "Size" in line and "1024 1536" not in line:
            break # Stop if we hit a new block
        
        if capturing and line.strip().startswith("FilePos"):
            tile_offsets.append(int(line.split()[1]))

    # Add EOF as the end of the last tile
    tile_offsets.append(os.path.getsize(bin_file))

    # Map Params
    width = 1024
    height = 1536
    cols = 8
    rows = 12
    tile_size = 128

    full_map = Image.new('RGB', (width, height))

    with open(bin_file, 'rb') as bf:
        tile_idx = 0
        
        # --- THE FIX: COLUMN-MAJOR LOOP ---
        # We iterate X (Cols) first, then Y (Rows). 
        # This reads down the strips instead of across the rows.
        for x in range(cols):
            for y in range(rows):
                if tile_idx >= len(tile_offsets) - 1:
                    break

                # Calculate byte range
                start = tile_offsets[tile_idx]
                end = tile_offsets[tile_idx+1]
                length = end - start

                bf.seek(start)
                tile_data = bf.read(length)

                try:
                    tile = Image.open(io.BytesIO(tile_data))
                    
                    # Calculate placement
                    px = x * tile_size
                    py = y * tile_size
                    
                    full_map.paste(tile, (px, py))
                    print(f"Placed Tile {tile_idx} at {px},{py}")
                except Exception as e:
                    print(f"Error on tile {tile_idx}: {e}")

                tile_idx += 1

    full_map.save(output_image)
    print(f"Done! Fixed map saved to {output_image}")

if __name__ == "__main__":
    extract_and_stitch()