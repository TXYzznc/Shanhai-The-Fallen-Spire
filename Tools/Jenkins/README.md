# GF_X Jenkins integration

`JenkinsBuilder` reads the two JSON files in this directory when invoked from a
CI job. All defaults are project-relative and deliberately contain no server,
repository, or machine-specific values.

Provide your own Jenkins controller, job configuration, credentials, and source
checkout step. This framework intentionally does not ship a destructive Git
reset/pull script or a Jenkins binary.
