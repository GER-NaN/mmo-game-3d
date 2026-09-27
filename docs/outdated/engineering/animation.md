# Animation

What moves in the world and how, as agreed on 2026-09-19. The character rig is built
and proven on the prototype models; the rest is the plan those models will be replaced
under. Nothing here is a promise about the pack we ship, only about the method.

## Three kinds of movement

1. **Wind** is a vertex shader offset: the top of a model sways, the base stays put,
   driven by a free property `sway` on a placement or its layer. Built. Right for
   trees, bushes, leaves, flags, anything that bends as a whole. Wrong for anything
   with a joint.
2. **Parts turning about pivots** is the rig below. Right for characters, animals,
   wheels, doors, wings, a rope as a chain. It is a skeleton in the sense voxel games
   use the word: rigid parts and a hierarchy, never a skinned mesh, because a bent
   voxel looks broken.
3. **Whole models following a path** is a placement with a path, which the dressing
   already does. Right for a train on a rail or a car on a road, with turning wheels
   from kind 2 layered on top.

## The character rig, as built

`CharacterParts.Split` cuts a model into head, torso, two arms and two legs by a rule
in fractions of its height: the bottom fifth is legs split down the middle, the top
two fifths are the head, and the outermost column on each side of the body band is an
arm. Every part keeps the model's size and palette, so the mesher lays them out in one
frame and they meet only where the cut was. `CharacterRig` puts each part on the GPU
with its pivot (neck, shoulder, hip) and draws a `CharacterPose` as six draws, each
turned about its pivot before the body's own world matrix. `CharacterAnimator` turns a
body's speed into a pose: the stride advances by distance, stopping eases the limbs
back, a body left standing glances about. `WorldScreen` keeps one animator per player
and feeds it the smoothed position's speed, so a body walks when it is drawn moving.

The rule is a stand-in for data. It fits the chibi prototype models and nothing else,
and that is fine: the models are placeholders too. What is meant to last is everything
downstream of the cut: the rig, the pose, the animator, the draw.

## The rule for final art

**Build anything that should move as separate named objects in MagicaVoxel, with each
object's origin where its joint is.** A character is objects named `head`, `torso`,
`arm_l`, `arm_r`, `leg_l`, `leg_r`; a dog adds `tail`; a car has `body` and four
`wheel_*`; a door is `frame` and `leaf`. The file's scene graph (nTRN and nSHP chunks,
which `VoxFile` skips today) carries the names and positions, so the cut and the pivots
come free with the art. Reading those chunks is the one real piece of work in the
switch; the proportional rule stays as the fallback for a single-object model.

Two additions to the rig follow from that:

- **A hierarchy.** A part may have a parent, and its world matrix is its own turn
  about its pivot, then its parent's. A hand under a forearm under an upper arm; a
  rope as links each hanging off the one above.
- **Clips as data.** A walk, a wave, a pickup reach or a hit reaction is a list of
  rotations per named part over time, in a small file beside the model, and the
  animator plays and blends clips instead of computing them. Procedural stays for what
  it is good at, such as a head following something.

## What the method covers

| Thing | Parts and pivots | Driver |
|---|---|---|
| Person, cat, dog | legs, head, tail at the hip, neck, tail base | speed of the body |
| Bird | two wings at the shoulder; body on a path | speed, or a clip |
| Car, train | wheels on their axles; body on a path | distance over wheel radius |
| Door, window, drawbridge | one leaf on its hinge | an event: open, close |
| Rope, chain, sign on a bracket | links each hanging off the one above | a clip, or a little physics |
| Leaves, grass, flags | none: this is wind | `sway` |

## Small effects: fire, fountains, smoke

Embers rising from a fire pit, water arcing out of a fountain and falling, smoke
drifting off a chimney are a particle system, and the shape of one is standard:

- **An emitter** at a point, with a rate (particles per second), a lifetime with
  jitter, an initial velocity with a spread (a cone for a fountain, nearly straight up
  for a fire), gravity or not, a little drag, and a colour ramp over the particle's
  life (fire: yellow to orange to red to gone, with glow so bloom picks it up; water:
  pale blue, no glow, dies when it reaches the ground).
- **Particles** are a pool of small structs updated on the CPU each frame: position,
  velocity, age. A few thousand is nothing. Gravity plus velocity gives the fountain's
  arc for free; fire is the same code with no gravity and a fade.
- **Drawing** is one draw call for the lot: each particle is a single voxel cube, all
  of them written into one dynamic vertex buffer each frame (or instanced), through the
  voxel shader that already exists, so they are lit and fogged like everything else
  and a glowing ember blooms like a lamp.

Where an effect sits and what it is comes from the art and the scene, the same way
everything else does: the effect is data (a name, in a small file of effect
definitions with the numbers above), a placement says `effect = fire` as a free
property, and the point comes from the model, either a named object in the vox file
(`emit`) or, until then, the top centre of the model's footprint. Colour comes from
the ramp, not from sampling the model, because the ember's colour is the fire's, not
the pit's. Randomness is jitter on lifetime, velocity and the emit point, which is
what keeps two fountains from looking like one.

Not built yet. The first version is one emitter type with the numbers above, a
`ParticleSystem` beside the renderer, and the fire pit and the fountain in the
starter zone as its two test cases.

## Clips as data, as built

Built 2026-09-21 with the phone. `AnimationClip` (in `Voxels.Rendering`) is a clip as
data: for each named part (`head`, `torso`, `arm_l`, `arm_r`, `leg_l`, `leg_r`) a list of
keys (time, pitch, yaw, roll), a loop flag, and events at moments. JSON in and out is
the file form; the built-in clips in `Clips.cs` are the same class made in code.
`ClipPlayer` plays one forward or back, loops or holds the end; `CharacterAnimator`
lays the clip's parts over the procedural walk and leaves the rest to it; the rig
turns a part as the clip says and can hand back a part's world matrix and its far
end, which is where a held thing sits. The first clip is hand from pocket to face,
half a second, held while online through the phone and played back to put it away.

Not built: the vox scene-graph reader (names and pivots from the file) and the
hierarchy. The proportional cut still supplies the six part names, and every clip is
written against those names, so the art can replace the cut without touching a clip.
