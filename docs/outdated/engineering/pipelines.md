# Pipelines

How each kind of thing flows from a tool into the game. One section per component.

```
TOOL                         (reads)
  you:   developer action
  tool:  tool action
  out:   output, and where it lands
```

## Single vox model

`tree.vox` into the game.

```
MagicaVoxel
  you:   model the tree
  out:   tree.vox

           |  you: copy to art/models/
           v

Voxel Scene Maker            (art/mmo-game.project.json, art/models/)
  you:   place the model on a scene
  tool:  records the placement by model name
  out:   art/scenes/<scene>.scene.json

           |  you: dotnet build (build/MapPackage.targets runs the compiler)
           v

Compiler                     (art/mmo-game.project.json, art/scenes/, art/models/)
  tool:  validates every placement's model
         measures each used model -> footprint
         copies used models only
         stamps
  out:   Package/zones/<scene>.zone.json      placements + footprints
         Package/models/tree.vox              unchanged
         Package/package.json                 stamp

           |  you: rebuild server and client
           v

Server                       (Package/zones/, Package/package.json, Package/world.json)
  tool:  every compiled scene is a zone of its name -> ZoneLoader
         placement + footprint -> ZoneDressing (collision)
         never opens the .vox
  out:   collision in the world
         stamp checked on connect

Client                       (Package/zones/, Package/models/, Package/package.json -> Content/)
  tool:  ModelLibrary reads .vox -> VoxMesher -> GpuMesh, once per model
         ZoneScene draws once per placement
         placement properties -> sway, effect
  out:   the tree on screen
```

## Single sound

`fire-crackle.wav` into the game, as the sound a placement gives off.

```
Freesound, or any source
  you:   download the recording; note the source in audio/LICENSES.md
  out:   fire-crackle.wav

           |  you: copy to audio/sounds/
           |  you: python tools\wav-convert\wav_convert.py audio\sounds\fire-crackle.wav   (mono, 16-bit)
           v

audio/sounds.json            (hand-edited; the mixer, later)
  you:   add an entry: label -> file, level (0 a fly, 1 a waterfall)
  out:   audio/sounds.json                    "fire-crackle": { "file": "sounds/fire-crackle.wav", "level": 0.3 }

Voxel Scene Maker            (art/mmo-game.project.json, its props included)
  you:   set sound = <label> on a placement, its layer, or the scene
  tool:  stores the label as a free property
  out:   art/scenes/<scene>.scene.json

           |  you: dotnet build (build/MapPackage.targets runs the compiler)
           v

Compiler                     (art/, audio/sounds.json, audio/sounds/)
  tool:  resolves each placement's sound: placement, then layer, then scene
         checks the label is in the manifest             error SC1004
         checks the manifest's file exists               error SC1005
         level -> gain and reference (SoundLevels); an explicit gain or reference wins
         copies used recordings only
  out:   Package/sounds/fire-crackle.wav
         Package/sounds.json                  used labels, gain + reference resolved, paths relative to the package
         Package/zones/<scene>.zone.json      the label on the placement, as written

           |  you: rebuild server and client
           v

Server                       (Package/zones/)
  tool:  reads the label and ignores it

Client                       (Package/sounds/, Package/sounds.json -> Content/)
  tool:  SoundLibrary loads sounds.json and each .wav, once
         ZoneScene -> SoundEmitters: one source per placement with a known label
         every frame: distance from the player's body -> volume; the loudest 8 play
  out:   the crackle, louder as you walk up to the fire pit
```
