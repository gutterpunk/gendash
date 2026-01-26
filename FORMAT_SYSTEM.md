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

### Examples

```bash
# Use default XML format
GenDash.exe

# Explicitly use XML format
GenDash.exe -format xml

# Use binary format for smaller file size
GenDash.exe -format binary

# Combine with other options
GenDash.exe -format binary -database MyPuzzles -tasks 8
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

## Future Formats

Potential future format additions:
- **JSON**: For web/API integration
- **SQLite**: For query capabilities
- **Compressed**: For maximum space savings
- **MessagePack**: For network efficiency
