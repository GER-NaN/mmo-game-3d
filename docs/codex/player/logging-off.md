---
aliases:
  - log-off modes
  - sheltered
  - unsheltered
---
# Logging off

**Status:** Decided, not built.

Logging off has three modes, from Rust with changes:

- **Unsheltered.** The character stays in the world, idle. It can be attacked.
- **Sheltered.** Set up camp, go to bed, go to a safe place; the character leaves the world. It needs a tent, a camper or a place where sheltering is allowed, but it is almost free, and it is the default.
- **Autonomous.** Logged off and unsheltered, with behaviours set: drones auto-defend, the house auto-repairs, a long hack keeps running. It costs a lot in batteries.

A character keeps travelling while logged off.
