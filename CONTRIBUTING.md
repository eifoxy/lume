# Contributing

**English** · [Português (Brasil)](CONTRIBUTING.pt-BR.md) · [Home](README.en.md)

Lume is a free personal project, built with vibe coding and substantial AI assistance to solve a real aquarium-controller problem, with no profit motive. Fixes, accessibility improvements, tests, and compatibility feedback are welcome. See [Build and test](docs/en/DEVELOPMENT.md); discuss protocol changes or new controller support in an issue before implementation.

For bug reports, include app version, OS, controller model/firmware, connection type, reproduction steps, and expected/observed behavior. For hardware validation, distinguish an app acknowledgment from a result checked on the device. Packet acknowledgments and complete memory readback are different evidence.

Do not publish passwords, actual SSIDs, public IPs, signing keys, tokens, personal libraries, private screenshots, or proprietary vendor binaries. Replace private data with examples.

Run relevant checks. Preserve validation before mutation, explicit confirmation before erasure, local backups, and bounded response deadlines. Smart Timer retries are limited to three identical attempts on timeout without a valid device reply: the same indexed point or initial header before that profile's points begin. Erasure is sent once, followed by a cancelable wait. Do not extend retries to erasure, credentials, clock, or finalizers, or restart partial profiles. Simulation must not affect hardware. Update guides and the compatibility matrix with behavior changes.

Keep the main README in Brazilian Portuguese and provide English documentation with reciprocal language links. AI assistance is welcome; describe verification and limitations honestly. The project's free, noncommercial purpose does not add restrictions to its MIT license.
