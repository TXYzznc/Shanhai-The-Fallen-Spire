## 1. Optional package foundation

- [x] 1.1 Define the `sample.json` manifest: identity, version, entry scene, install root, and payload mappings.
- [x] 1.2 Create repository-root `Samples~/` and migrate Basic UI as a discoverable package with Unity `.meta` files preserved.
- [x] 1.3 Ignore only installed copies under `Assets/Sample/`; keep package source and documentation versioned.
- [x] 1.4 Validate manifests, normalize paths, verify payloads, and reject project-root escapes or destination conflicts.

## 2. Optional package manager

- [x] 2.1 Add the generic manager at `Tools > AI Friendly Frame > Samples`.
- [x] 2.2 Show package discovery, installation state, validation state, and an entry-scene action.
- [x] 2.3 Record installed files with SHA-256 hashes and refresh the AssetDatabase after package changes.
- [x] 2.4 Support validation, repair/reinstall, and safe uninstall that never silently removes modified files.
- [x] 2.5 Keep Launch, Build Settings, and non-sample assets untouched by package operations; permit only declared sample-scoped AppConfigs registrations with snapshot/restore protection.

## 3. Circuit Puzzle package

- [x] 3.1 Add `Samples~/CircuitPuzzle` with a manifest, scene, scripts, and package README.
- [x] 3.2 Generate reproducible 6×6 boards containing source, target, straight, corner, and T-junction nodes.
- [x] 3.3 Implement rotation, connectivity, moves, elapsed time, pause/resume, completion, reset, and next-level flow.
- [x] 3.4 Build all presentation procedurally with UGUI and generated glyphs; include no external art assets.
- [x] 3.5 Reuse transient energy-pulse UI objects and avoid allocations in the frame update path.

## 4. Framework capability demonstration

- [x] 4.1 Keep seed-driven sample configuration self-contained in the optional package and register it in AppConfigs only while installed.
- [x] 4.2 Provide Chinese/English HUD and runtime language switching with a safe local fallback.
- [x] 4.3 Publish sample events and persist best-move/highest-level values through GF services when available, otherwise PlayerPrefs.
- [x] 4.4 Report GF Resource availability without requiring a built resource package.
- [x] 4.5 Demonstrate safe SoundExtension calls for rotate/complete actions without shipping audio assets.
- [x] 4.6 Provide a Framework Status panel for seed, service availability, event count, pooling, resources, and sound.

## 5. Documentation and verification

- [x] 5.1 Document installation, operation, capability mapping, and removal in the package README and repository README.
- [x] 5.2 Verify discovery, install, validation, repair, conflict protection, and uninstall behavior through EditMode tests.
- [x] 5.3 Verify modified installed content is not silently removed.
- [x] 5.4 Verify the uninstalled framework compiles cleanly, and verify the Circuit Puzzle scene runs without console errors.
- [x] 5.5 Run strict OpenSpec validation, framework-purity audit, and Git whitespace validation.

## 6. Data-driven CircuitPuzzle integration

- [x] 6.1 Extend manifest and install records with declared GameData payloads and AppConfigs registration snapshots.
- [x] 6.2 Add conflict-safe install, validation, repair, and uninstall handling for AppConfigs integration.
- [x] 6.3 Add CircuitLevelTable, CircuitPuzzleConfig, and CircuitPuzzle language sources under the CircuitPuzzle package.
- [x] 6.4 Add a sample procedure that opens CircuitPuzzle after framework preload, while retaining direct-scene preview.
- [x] 6.5 Drive board generation, UI strings, and runtime status from the loaded table/config/localization data.

## 7. Transactional AppConfigs profiles

- [x] 7.1 Define exclusive full-AppConfigs profiles with a project-local, Git-ignored backup state directory.
- [x] 7.2 Add install, validation, repair and uninstall handling for full profile backup/restore.
- [x] 7.3 Detect interrupted profile switches and expose an explicit recovery action in Sample Manager.
- [x] 7.4 Update CircuitPuzzle to declare a complete sample AppConfigs profile.
- [ ] 7.5 Verify profile install, formal Launch, uninstall restoration, conflict protection and interrupted-state recovery.

## 8. Transactional Build Settings registration

- [x] 8.1 Allow manifests to explicitly request entry-scene Build Settings registration.
- [x] 8.2 Record, validate and restore Build Settings scene-list snapshots during install, repair and uninstall.
- [x] 8.3 Enable registration for Circuit Puzzle and document the formal-startup behavior.
- [ ] 8.4 Verify formal Launch, Build Settings restoration and modified-list protection.
- [ ] 6.6 Verify install → formal Launch → unload, direct preview, and uninstall → AppConfigs restoration.
