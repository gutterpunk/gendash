# Format System Documentation

## Overview

GenDash now supports multiple output formats for the puzzle database. The application internally uses XML for all operations, but can save and load the database in different formats for storage efficiency or compatibility.

## Command-Line Usage

### `-format` Option

```bash
GenDash.exe -format <format_name>
```

**Supported formats:**
- `xml` - Standard XML format (default)
- `binary`, `bin`, or `gdb` - Compact binary format
- `retro`, or `gdr` - Ultra-compact retro format for SGDK/older hardware

### Examples

```bash
# Use default XML format
GenDash.exe

# Explicitly use XML format
GenDash.exe -format xml

# Use binary format for smaller file size
GenDash.exe -format binary

# Use retro format for SGDK and older hardware
GenDash.exe -format retro

# Combine with other options
GenDash.exe -format retro -database MyPuzzles -tasks 8
```

## Binary Format Specification

The binary format is optimized for minimal size while maintaining all data integrity.

### File Structure

```
HEADER (16 bytes)
- Magic: "GENDASH\0" (8 bytes)
- Version: uint32 (4 bytes) = 1
- BoardCount: uint32 (4 bytes)
- RejectCount: uint32 (4 bytes)

BOARDS SECTION (variable)
- Board[] (BoardCount entries)

REJECTS SECTION (variable)
- Reject[] (RejectCount entries)
```

### Board Entry Format

```
- Hash: uint64 (8 bytes)
- Width: byte (1 byte)
- Height: byte (1 byte)
- Par: int32 (4 bytes)
- StartX: int32 (4 bytes)
- StartY: int32 (4 bytes)
- ExitX: int32 (4 bytes)
- ExitY: int32 (4 bytes)
- Idle: int32 (4 bytes)
- Data: LengthPrefixedString
- Solution: OptionalSolution
```

### Reject Entry Format

```
- Hash: uint64 (8 bytes)
- Reason: LengthPrefixedString
- HasDetails: bool (1 byte)
   - If true: Width, Height, StartX, StartY, ExitX, ExitY, Idle
- HasData: bool (1 byte)
   - If true: Data (LengthPrefixedString)
- HasSolution: bool (1 byte)
   - If true: Solution structure
```

### Solution Structure

```
- AvgDiff: int32 (4 bytes)
- AvgGoals: int32 (4 bytes)
- AvgBefore: int32 (4 bytes)
- AvgAfter: int32 (4 bytes)
- FallingDelta: int32 (4 bytes)
- Proximity: int32 (4 bytes)
- Score: int32 (4 bytes)
- FoldCount: uint16 (2 bytes)
- Fold[] (FoldCount entries)
   - Move: LengthPrefixedString
   - Data: LengthPrefixedString
```

### LengthPrefixedString Format

```
- Length: uint16 (2 bytes)
- UTF8Bytes: byte[] (Length bytes)
```

## Retro Format Specification
### File Structure

```
HEADER (5 bytes)
- Magic: "GD1" (3 bytes, includes version)
- BoardCount: uint16 (2 bytes)

BOARDS SECTION (variable)
- Board[] (BoardCount entries, only boards with solutions)
```

### Board Entry Format

Each board uses packed and compressed data:

```
DIMENSIONS (1-3 bytes):
- If width <= 15 AND height <= 15:
    PackedSize: byte ((width << 4) | height)
- Else:
    Marker: 0xFF
    Width: byte
    Height: byte

START POSITION (1-3 bytes):
- If startX < 16 AND startY < 16:
    PackedStart: byte ((startX << 4) | startY)
- Else:
    Marker: 0xFF
    StartX: byte
    StartY: byte

EXIT POSITION (1-3 bytes):
- If exitX < 16 AND exitY < 16:
    PackedExit: byte ((exitX << 4) | exitY)
- Else:
    Marker: 0xFF
    ExitX: byte
    ExitY: byte

PAR (1-3 bytes):
- If par < 256:
    Par: byte
- Else:
    Marker: 0xFF
    Par: uint16

IDLE: byte (1 byte)

BOARD DATA (variable):
- CompressedLength: uint16 (2 bytes)
- RLEData: byte[] (CompressedLength bytes)

SOLUTION:
- Score (1-3 bytes):
    If score < 256:
        Score: byte
    Else:
        Marker: 0xFF
        Score: uint16
- FoldCount: byte (1 byte)
- Folds[] (FoldCount entries):
    - Move: byte (single character: L/R/U/D/etc)
    - CompressedLength: uint16 (2 bytes)
    - RLEData: byte[] (CompressedLength bytes)
```

### RLE Compression Algorithm

```
Format: [count][char][count][char]...

- For each run of identical characters:
  - Write [count][char] where count is 1-255
- All counts are bytes (max 255 per run)
- Characters are ASCII (single byte each)
- Simple and consistent format - no special cases
```

**Example:**
- Input: `"****..#####abc"`
- Output: `[4]['*'][2]['.'][5]['#'][1]['a'][1]['b'][1]['c']`
- Bytes: `04 2A 02 2E 05 23 01 61 01 62 01 63`
- Size: 16 bytes for 14 chars (minimal overhead for short runs)

### Reading Retro Format in C (SGDK Example)

```c
typedef struct {
    u8 width;
    u8 height;
    u8 startX;
    u8 startY;
    u8 exitX;
    u8 exitY;
    u8 par;
    u8 idle;
    char* data;
    // ... solution data
} RetroBoard;

RetroBoard* loadRetroBoard(FILE* file) {
    RetroBoard* board = malloc(sizeof(RetroBoard));
    u8 packed;
    
    // Read dimensions
    packed = fgetc(file);
    if (packed == 0xFF) {
        board->width = fgetc(file);
        board->height = fgetc(file);
    } else {
        board->width = packed >> 4;
        board->height = packed & 0x0F;
    }
    
    // Read start position
    packed = fgetc(file);
    if (packed == 0xFF) {
        board->startX = fgetc(file);
        board->startY = fgetc(file);
    } else {
        board->startX = packed >> 4;
        board->startY = packed & 0x0F;
    }
    
    // ... continue reading other fields
    // ... decompress RLE data
    
    return board;
}
```

## Format Conversion

### Converting Existing XML to Binary

```bash
# Load XML database and save as binary
GenDash.exe -database MyDatabase -format binary

# The app will:
# 1. Load MyDatabase.xml
# 2. Process boards (if running generation)
# 3. Save as MyDatabase.gdb
```

### Converting Binary to XML

```bash
# Load binary database and save as XML
GenDash.exe -database MyDatabase -format xml

# The app will:
# 1. Load MyDatabase.gdb
# 2. Process boards (if running generation)
# 3. Save as MyDatabase.xml
```

