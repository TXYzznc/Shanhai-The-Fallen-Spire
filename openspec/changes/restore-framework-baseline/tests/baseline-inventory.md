# Framework Baseline Inventory

The pre-cleanup inventory was used only to delimit the destructive cleanup.
Historical product names, content identifiers, file hashes and removed artifact
paths are intentionally not retained in the framework tree.

## Retained categories

- Unity framework runtime and editor code
- Framework-facing extensions and configuration primitives
- Required packages, plugins and project settings
- Domain-neutral engineering, asset-production, quality and delivery skills
- Parameterized governance, build, validation and synchronization tools
- Current framework OpenSpec specifications

## Removed categories

- Product-domain agents and skills
- Gameplay-domain guidance and balancing workflows
- Product scenes, resources, catalogs, generated assemblies and reports
- Sample, example and demonstration artifacts
- Historical product specifications and archived changes
- One-off scripts coupled to removed content

## Decision rule

A retained file must be required by the framework, referenced by a retained
framework file, or define a reusable capability without assuming a product
domain. Everything else is outside the baseline.
