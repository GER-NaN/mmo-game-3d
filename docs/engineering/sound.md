# Sound

Built 2026-09-26 from the sound plan. Every choice and level is a placeholder, picked by
file name and length and never heard by whoever picked it: change them by ear.

## Files

- The sound library is `C:\game-sound` on the author's machine: music, ambience, effects,
  interface and voice packs.
- `game/audio/sounds.json` is the catalog: every sound the game plays, by id
  (`ui.click`, `music.shop`, `step.grass`, `voice.karen.greeting`), with its files in the
  library (one is picked at random each time), its bus, its level in dB, whether it
  loops, and for sounds in the world how far it carries.
- `python tools/audio-subset/copy.py` copies exactly the files the catalog names into
  `assets/audio/` (not in git), with each pack's licence; then Godot imports them once.
  A sound whose file is missing is skipped with a line in the log, so a machine without
  the library still runs.
- `assets/README.md` lists the packs, their licences and the credits owed. Helton Yan,
  Nathan Gibson and Dillon Becker (CC BY 4.0) must be credited; the main menu's Credits
  does it. Bleeoop's files must not be passed on raw.

## Buses and settings

`default_bus_layout.tres`: Master, Music, Ambience, Effects, Interface, Voice. Settings
has a slider for each but Voice, saved in `settings.cfg` under `[audio]`.

## The director

`game/audio/AudioDirector.cs`, client only (not in headless clients, not on the server).

- `Play(id)`: a one-off, flat (interface, notices).
- `PlayAt(id, position)`: a one-off from a spot in the world (the EMP, a zap, a chest,
  footsteps, greetings).
- `Attach(id, node)`: a loop that comes from a thing and moves with it; the caller frees
  it (a drone's hum, a broken fixable's crackle, a lit lamp's buzz, a terminal's hum).
- `Music(id)`, `Ambience(id)`: one track and one bed at a time, faded across.
- Every button clicks: the terminal's bleeps inside the terminal, a warm click elsewhere.

## Where sounds come from

- Music and ambience: `ClientGame.UpdateSoundscape`, by the zone, the hour
  (`DayNight.IsNight`) and being online (a drone replaces the music).
- Notices: `ClientGame.NoticeSound` picks by the notice's text (achievement, level up,
  phone push, pick up, drones down, else the plain one).
- Footsteps: `Player.Step`, every 0.8 m walked on the ground, by the zone's `Surface`
  (rock, grass, tile, dirt, or none).
- Voices: an interactable's `Voice` (a person) says a greeting when used
  (`ClientGame.Greet`). Which actor is whose is a placeholder.
- The rest in place: going online and offline, code guesses, Agent Defense presses,
  messages, money coming in, doors and car doors, the EMP, zaps, chests.

## Open

From the sound plan, waiting on the author: the music's tone (the only music pack is
cosy fantasy folk), a voice per player character (players have no voice yet), whether
others' sounds are heard, how much music plays, terminal typing sounds.
