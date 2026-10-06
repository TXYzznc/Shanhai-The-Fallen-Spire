## ADDED Requirements

### Requirement: Sample package discovery
The framework SHALL discover optional Sample packages under the repository-level `Samples~/` directory without requiring Unity to import those source files.

#### Scenario: Package is listed
- **WHEN** a valid package directory contains a manifest with an id, version, display name, entry scene, and payload mappings
- **THEN** Sample Manager lists the package and its installation state.

### Requirement: Manifest-scoped installation
The framework SHALL install a Sample only to paths explicitly declared by that package manifest and SHALL preserve Unity `.meta` files during the operation.

#### Scenario: Clean installation
- **WHEN** a user installs a package whose declared targets do not exist
- **THEN** Sample Manager copies its declared payloads, writes an installation marker and file manifest, refreshes AssetDatabase, and reports the installed version.

#### Scenario: Target conflict
- **WHEN** a declared installation target already contains files not owned by the package
- **THEN** Sample Manager refuses to overwrite the target and reports the conflicting path.

### Requirement: Safe removal and repair
The framework SHALL remove only files recorded in the installed package manifest and SHALL not automatically delete an installation whose recorded files were modified.

#### Scenario: Clean removal
- **WHEN** a user removes an unmodified installed package
- **THEN** Sample Manager deletes only its recorded files and markers, refreshes AssetDatabase, and reports that the package is not installed.

#### Scenario: Modified installation
- **WHEN** a user attempts to remove a package whose installed files no longer match the recorded manifest
- **THEN** Sample Manager stops removal and presents repair, manual backup, or cancel actions.

### Requirement: Default framework isolation
The framework SHALL NOT install Sample payloads, modify the default Launch procedure chain, modify AppConfigs default load lists, or modify Build Settings merely because the repository is opened. A package MAY explicitly request its installed entry scene to be registered in Build Settings.

#### Scenario: Fresh clone
- **WHEN** a user opens a fresh clone without installing a Sample
- **THEN** no Sample assets are active under `Assets/` and the default framework startup behavior remains unchanged.

### Requirement: Transactional Build Settings scene registration
A Sample MAY declare that its entry scene must be included in Build Settings. The manager SHALL append a missing declared scene during installation, record the complete scene-list snapshot before and after the operation, and restore the previous list only when the current list still matches the recorded installed state.

#### Scenario: Install a formal-startup sample
- **WHEN** a package explicitly requests Build Settings registration and its installed entry scene exists
- **THEN** the manager appends the enabled entry scene to Build Settings and records the resulting scene list with the package installation.

#### Scenario: Uninstall a formal-startup sample
- **WHEN** the managed Build Settings list still matches the recorded installed state
- **THEN** the manager restores the pre-install scene list before removing the Sample payload.

#### Scenario: Build Settings changed after installation
- **WHEN** the Build Settings scene list differs from the state recorded after installation
- **THEN** the manager refuses automatic repair or uninstall and reports the shared-project-settings conflict.

### Requirement: Sample Manager access
The framework SHALL provide Sample Manager through `Tools > AI Friendly Frame > Samples` with install, open, validate, repair, and uninstall actions.

#### Scenario: Open an installed sample
- **WHEN** a user invokes Open for an installed package
- **THEN** Sample Manager opens the package manifest's entry scene without changing the default build scene list.

### Requirement: Controlled sample AppConfigs integration
An installed Sample MAY declare sample-namespaced DataTable, Config, Language, and Procedure registrations. The manager SHALL snapshot `AppConfigs` before applying those entries and SHALL restore that snapshot only when the current asset still matches the recorded installed state.

#### Scenario: Install data-driven sample
- **WHEN** a package declares `Sample/` data registrations and the target AppConfigs asset has not changed during installation
- **THEN** the manager records the previous AppConfigs state, registers only the declared sample entries, and records the resulting state for validation.

#### Scenario: Uninstall data-driven sample
- **WHEN** the installed sample and the managed AppConfigs state both validate
- **THEN** the manager restores the recorded AppConfigs state and removes only the Sample-owned payload files.

#### Scenario: AppConfigs changed after installation
- **WHEN** AppConfigs no longer matches the recorded installed state
- **THEN** the manager refuses automatic uninstall or repair and reports the shared-configuration conflict.

### Requirement: Transactional full AppConfigs profiles
A Sample that requires a complete startup configuration MAY declare a full AppConfigs profile instead of incremental registrations. The manager SHALL back up the complete `AppConfigs.asset` before activation, verify the backup and active asset by hash, and restore the exact asset before deleting the profile's installed payloads.

#### Scenario: Activate a full profile
- **WHEN** a user installs a valid Sample with a full AppConfigs profile and no other installed Sample manages AppConfigs
- **THEN** the manager persists a local backup and recovery record, replaces every AppConfigs load list declared by the profile, and records the resulting active state before completing installation.

#### Scenario: Restore a full profile
- **WHEN** a user uninstalls a full-profile Sample whose managed AppConfigs asset and backup still validate
- **THEN** the manager restores the exact pre-install AppConfigs asset, removes the local backup state, and removes only the Sample-owned payloads.

#### Scenario: Recover an interrupted profile activation
- **WHEN** profile backup state exists but the Sample installation marker does not
- **THEN** Sample Manager offers a recovery operation that restores the backed-up AppConfigs asset and removes the incomplete local state.
