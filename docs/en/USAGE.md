# Curves, profiles, and uploads

[Home](../../README.en.md) · **English** · [Português (Brasil)](../USO.md)

The apps support Portuguese, English, and Spanish. Examples below retain Portuguese button names; select Português (Brasil) in the language selector to match them.

## Understand the day

Each profile has **five channels and 48 points**, every 30 minutes from 00:00 to 23:30. Points store integer percentages from 0 to 100. The preview interpolates between points, including from 23:30 to 00:00.

Names and colors identify channels without changing physical CH1-CH5 wiring order. Rename them to match your fixture. Example intensities are illustrative, not lighting recommendations for particular species.

## Edit

On **Windows**, select the profile and **Editar curva** for a channel. Without **Desenhar curva** enabled, clicking only selects a preview time. Enable drawing to change values with the mouse. **Isolar canal selecionado** and **Ampliar curva** help focus on one curve. Sliders edit the indicated half-hour point; press Enter to confirm table values. A preview time may be interpolated, so check the selected point before editing.

On **Android**, use **Curvas** and tap the chart to choose a point. Arrows move to adjacent points. Select a channel to highlight it, then edit sliders, numeric inputs, or individual cells in the full table.

**Criar ciclo** sets start/end times, ramp duration, and five peak intensities. The interval must end later on the same day and fit both ramps. It replaces all 48 points in the current profile; **Desfazer** restores the previous edit.

## Profiles and files

Create or duplicate profiles to experiment. The local library holds up to 100 profiles; a batch upload supports up to 16 controller slots. Both apps share Lume's JSON model.

| Format | Support |
|---|---|
| Lume `.json` | Whole-library backup/export. Import adds profiles. |
| Pled V2.2 `.lmf` | One 48-point, five-channel profile. Both apps import; Windows exports. |
| Pled `.lcm` | Project referencing local LMF files. Windows imports it if referenced files exist; Android does not. |

Autosave is local. Internal Android backups do not survive uninstalling or clearing app data. Export JSON outside the app. You can transfer libraries between Windows and Android by exporting JSON and importing it on the other platform.

## Upload profiles

Find/select the controller first. The review shows local names and short device aliases: at most seven ASCII characters, with suffixes for collisions. Local names are preserved.

**One profile:** choose the individual-profile option and a slot from 1 to 16. It replaces that slot without full-memory erasure. Review the slot before writing.

**All profiles:** choose full-memory replacement and confirm. The controller receives one erase command followed by local profiles in order, starting at slot 1. Erasing profiles does not remove the five physical channels; intensities are defined within each profile.

Lume validates/copies the whole plan and backs up the local library before the first write. **It does not read schedules already on the device**; this backup cannot restore programs that existed only in controller memory.

Keep the connection active and the app open. Commands require acknowledgment. Current Android Smart Timer recovery allows up to three identical attempts only after timeout without a valid device reply: the same indexed point, or the initial header before any points of that profile. It does not restart a partial profile, repeat erasure, or retry credentials, clock commands, or finalizers. An unexpected valid status stops immediately.

Since 1.0.5, confirmed Smart erasure is followed by a cancelable 2.1-second wait. This is a conservative operating margin, not a measured firmware erase time. If recovery fails, the app stops and reports the phase/completed profiles. There is no device rollback.

Use **Conexão → Diagnóstico da última comunicação → Copiar diagnóstico** after a failure. It distinguishes silence from rejected replies without raw packets or passwords. A matching preview curve is not physical readback. Smart uploads include both per-profile and whole-batch finalizers from the working vendor APK; check the active profile on the controller after uploading.

## Clock, resume, and preview

- **Sincronizar horário:** sends local system date/time; verify your PC/phone clock and time zone first.
- **Retomar programa:** exits manual mode; it is not a profile selector.
- **Aplicar este instante aos LEDs:** Windows USB only; sends real intensities after confirmation.
- **Recife / local daily preview:** sends no commands without the live link. Coral/fish animation does not prove the fixture's physical state.

See [compatibility and hardware evidence](COMPATIBILITY.md) for the limits of each function's validation.

## Reef on the real light

Local preview remains hardware-free. In Android 1.2.0, Link light fixture explicitly enables Smart Timer Wi-Fi preview: timeline scrubbing and day playback send the same five intensities to the light. It does not rewrite profiles or change the device clock. Pausing playback holds the current moment until you end live preview. [Operation, recovery, and limits](LIVE-PREVIEW.md).
