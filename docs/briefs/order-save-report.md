# Customer-order save migration report

`SaveStore` now writes version 2 and exposes `DirectoryPath` for the controller's reset flow. Version 1 files pass the original economy validation, then receive explicit empty order state, a level-zero shelf, zero reward counters, and a 15-second arrival countdown. Existing coins, day, upgrades, sales, completed IDs, inventory, and winding samples are kept.

Version 2 loading and saving validate shelf level and stock capacity; order count, unique canonical IDs, serial, recipe, and finite remaining patience; finite arrival countdown; nonnegative reward counters; and consistency between served orders, sales, tips, revenue, and satisfaction. Invalid files remain in place and disable saving until `ArchiveAndReset`. An invalid in-memory state is refused before it can replace a valid file. The load catch filter now includes `InvalidDataException`, which previously escaped on rejected envelopes.

`IntegrationChecks` now writes a V1-shaped fixture without the new fields, checks migration, saves and reloads orders on a nine-slot shelf, and tests malformed V2 rejection. These editor checks need the later Unity batch run when UI compilation is complete.

Local verification used a temporary Mono harness with a `JsonUtility` shim to exercise the real `SaveStore` and economy classes: 12 focused checks passed, including V1 preservation, V2 round trip, capacity and patience validation, serial validation, and corrupt-file protection. The editor check file also compiled against temporary Unity API stubs. The temporary harness and stubs are under ignored `Logs/` and are not part of the commit.
