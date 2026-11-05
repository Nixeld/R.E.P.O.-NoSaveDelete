# Changelog

## v1.0.0

This is a hard fork of [Pxntxrez's No Save Delete](https://github.com/Pxntxrez/NoSaveDelete). It carries over every feature of the original mod and fixes its interaction with Defibro.

### Fixed
- **Defibro can now save the run.** After all players die, the mod no longer restarts the level immediately. It waits for a Defibro to attempt a rescue:
  - If the Defibro revives a player, the auto-restart is canceled and the run continues as normal.
  - If no Defibro is present, or it is too far from the bodies to attempt a rescue, the mod restarts the save on its original schedule instead of letting the level run out to the disposal arena.
  - An active rescue is given up to 15 seconds to finish before the mod restarts anyway. A Defibro rescue has been measured live at about 8 seconds after death, so this leaves plenty of time for the Defibro to revive you.
- When several players die at the same instant, the save now restarts exactly once instead of racing multiple restarts.