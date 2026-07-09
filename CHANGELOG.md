# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/), and this project adheres
to [Semantic Versioning](https://semver.org/).

---

## v1.0.0

### Added

- **Ban Mods system** — server-side mod banning via configurable GUID list (`ban_mods`)
- **Require THM** — optional server setting (`require_thm`) to kick players who don't have Tick Happy Mod
- **Multiplayer mod detection protocol** — clients with THM report their full mod list to the server on join
    - Custom network message (msgId `33993`) registered via direct injection into `SERVER_MESSAGE_HANDLERS`
    - Server checks client mods against `BanModsList` and kicks if any match
- **Kick reason notification** — uses `Server_DoAlertSingle` (for THM clients) and built-in KrokoshaMP alert msgId
  `30006` (for non-THM clients) before kicking
- **Timeout mechanism** — `report_timeout` (default 15s) kicks players who never send a mod report
- **Single-player self-check** — quits the game if banned mods are detected locally
- **KrokoshaCasualtiesMP integration** — `[BepInDependency("KrokoshaCasualtiesMP", SoftDependency)]`
    - Subscribes to `NetPlayer.OnPlayerJoined` for join detection
    - Harmony-free server handler registration via reflection into `Net.SERVER_MESSAGE_HANDLERS`
- **Multi-delimiter split** — `BanModsList` supports `,` `;` `|` `、` `，` space tab newline

### Changed

- Target framework upgraded from `net472` to `net48` for KrokoshaMP compatibility

### Technical Notes

- All KrokoshaMP version differences handled at runtime via reflection
- Harmony patching of `Net.InvokeServerMessage` skipped (method is DMD-wrapped in KrokoshaMP v4.0.1)
