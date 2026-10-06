## ADDED Requirements

### Requirement: Independent procedural circuit puzzle
The CircuitPuzzle Sample SHALL provide an independently openable 2D circuit-rotation puzzle that uses only procedural or Unity built-in visual primitives for its gameplay presentation.

#### Scenario: Start a generated puzzle
- **WHEN** the user opens the installed CircuitPuzzle entry scene and starts play mode
- **THEN** the game creates a playable 6×6 default board containing a start, an end, and rotatable circuit nodes without loading external art assets.

### Requirement: Seeded board generation and completion
The CircuitPuzzle Sample SHALL generate boards from a visible seed and SHALL complete a level when a continuous path connects the start node to the end node.

#### Scenario: Recreate a board
- **WHEN** the user chooses regenerate without changing the seed
- **THEN** the same node layout is recreated.

#### Scenario: Complete a path
- **WHEN** node rotations establish a valid connection from start to end
- **THEN** the game enters the cleared state, records steps and elapsed time, and exposes a next-level action.

### Requirement: Observable framework capability integration
The CircuitPuzzle Sample SHALL expose observable use of configuration/data, localization, settings, events, object reuse, resource access, and safe sound playback.

#### Scenario: Inspect framework status
- **WHEN** the user opens the Framework Status panel during play
- **THEN** the panel shows the active language, settings values, board seed, event counts, reuse metrics, and current resource/audio capability status.

#### Scenario: Missing optional audio asset
- **WHEN** the sample requests an optional sound asset that is not installed
- **THEN** `SoundExtension` reports a diagnostic and gameplay continues without an unhandled exception.

### Requirement: Sample containment
The CircuitPuzzle Sample SHALL NOT alter the framework's default Launch behavior and SHALL be removable through Sample Manager without leaving Sample-owned assets in active Unity paths.

#### Scenario: Remove CircuitPuzzle
- **WHEN** the user removes an unmodified installed CircuitPuzzle package
- **THEN** its declared installed assets are removed and opening the default Launch scene retains its prior behavior.

### Requirement: Data-driven framework startup
The CircuitPuzzle Sample SHALL provide package-owned DataTable, Config, and Language inputs, register them through AppConfigs on installation, and use them after PreloadProcedure completes.

#### Scenario: Launch through framework procedures
- **WHEN** CircuitPuzzle is installed and the project starts from the normal Launch scene
- **THEN** PreloadProcedure loads the sample registrations and the sample procedure opens CircuitPuzzle using the active table/config/localization data.

#### Scenario: Direct scene preview
- **WHEN** the user opens the installed CircuitPuzzle entry scene directly
- **THEN** the scene remains playable and clearly reports that it is using a preview fallback when framework-preloaded data is unavailable.
