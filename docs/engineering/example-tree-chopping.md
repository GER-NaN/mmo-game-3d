# Worked example: tree chopping

A new mechanic from end to end, to copy from. **This is not built**; it is the steps
someone would take. Walk up to a tree, press use, it falls (everyone in the zone sees
it), you get a log and Treework experience, and a stump stands there until the tree
grows back.

It follows the chest (`game/chests/Chest.cs`, `game/server/items/ServerChests.cs`), which does
nearly the same: a thing with a synced state, swapped models, a sound, a refill timer.
When a new mechanic looks like an existing one, open that one next to this.

## The shape of any mechanic

| Part | Where | Does |
| --- | --- | --- |
| The rules and numbers | `src/Rules/` | plain C#: the skill, the item, the XP; unit-tested |
| The thing in the world | a node class + scene in `game/<area>/` | what everyone sees; synced state; sounds and effects on the client |
| The server part | `game/server/<area>/Server<Thing>.cs` | what using it does: checks, state change, rewards, timers |
| The hook-up | `ServerInteractions.Use`, `ServerGame.Start`, `ServerGame`'s tick | routes a use to the part, builds it, ticks it |
| Placement | the zone scene, in the editor | where the things stand |
| Sound | `game/audio/sounds.json` | the catalog entry the client plays |
| Proof | a dev scenario | the feature, tested in seconds |

The server decides everything; a client only asks ("use Pine0") and draws what the
synced state says.

## 1. The rules (code)

**A skill.** `src/Rules/Skills/Skills.cs`:

- `SkillId`: add `Treework = 6` at the end. The number is saved in the database and sent
  on the wire; never reuse or renumber one.
- `SkillCatalog.All`: add `SkillId.Treework` (the skills panel lists this array, and the
  save loop walks it).
- `Name`: `case SkillId.Treework: return "Treework";`
- `HowEarned`: `case SkillId.Treework: return "Chopping trees.";`
- `SkillAwards`: `public const long TreeworkPerTree = 15;` (a placeholder).

No database change: `player_skills` stores any skill by its number.

**An item.** `src/Rules/Items/`:

- `ItemType.cs`: add `Log` at the end of the enum. Saved by name, sent by number.
- `ItemCatalog.cs`: a definition, or the game throws when it meets one:
  `{ ItemType.Log, new ItemDefinition(ItemType.Log, "Log", "A length of wood.", true) },`
  (`true`: logs stack).
- `Recycling.cs`: `{ ItemType.Log, 1 },` if the recycler should buy it (else it pays 0).

**A gesture** (the chopping motion). `src/Rules/Social/Gestures.cs`:
`public const string Chop = "chop";` and `new Gesture(Chop, "Chopping", 2, false),` in
`All`. "Chopping" is an animation in `Rig_Medium_Tools.glb`, which the player model
already loads; `false` means it is not a chat emote.

**The protocol.** `src/Rules/GameVersion.cs`: raise `Protocol` by one. A new synced
property, item and skill all change what the wire carries.

**A test**, if a rule has logic worth proving (here the rules are only entries; the
scenario in step 7 proves the feature).

## 2. The thing in the world (code)

`game/forest/ChoppableTree.cs`:

```csharp
namespace MmoGame3d.Forest;

using Godot;
using MmoGame3d.Interact;

/// <summary>
/// A tree you can chop down. Whether it stands is synced, so everyone sees it fall; what
/// chopping gives is decided on the server (ServerTrees), which also grows it back.
/// </summary>
public partial class ChoppableTree : Interactable
{
    [Export]
    public string TreeName { get; set; } = "pine";

    [Export]
    public bool Standing { get; set; } = true;

    public override string Prompt
    {
        get { return Standing ? "Chop the " + TreeName : ""; }
    }

    private bool _shown;
    private bool _shownStanding;

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            return;
        }

        GetNode<Node3D>("Tree").Visible = Standing;
        GetNode<Node3D>("Stump").Visible = !Standing;

        // Felled while in view: the sound, not only a swap of models.
        if (_shown && _shownStanding && !Standing)
        {
            Audio.AudioDirector.Current?.PlayAt("fx.tree_fall", GlobalPosition);
        }

        _shown = true;
        _shownStanding = Standing;
    }
}
```

Then **Build** (the hammer in the editor, or `dotnet build`), so the editor knows the class.

## 3. The scene (editor)

1. **Scene > New Scene**, root **Other Node > StaticBody3D**, named `ChoppableTree`.
2. Inspector > Script: load `game/forest/ChoppableTree.cs`.
3. Collision layer: 1 (World) is the default; leave it. The root is what blocks players.
4. Add a **CollisionShape3D** named `Collision`: a `CylinderShape3D`, radius 0.3,
   height 3, moved up 1.5 so it stands on the ground. A narrow trunk, so players walk
   under the branches.
5. Drag a tree prop in as a child, rename it `Tree` (for example
   `game/props/city_builder_bits/TreeA.tscn`). In its Inspector set **Collision > Layer**
   to nothing: the root already blocks, and a second body would too.
6. Add the stump as a child named `Stump`: a prop, or for now a `MeshInstance3D` with a
   short `CylinderMesh`. Untick **Visible**.
7. Add a **MultiplayerSynchronizer** named `Synchronizer`. With it selected, open the
   **Replication** panel at the bottom: **Add property to sync**, pick
   `ChoppableTree:Standing`, tick **Spawn**, set **Replicate** to **On Change**. In the
   Inspector: **Replication Interval** 1, **Visibility Update Mode** None (the zone's
   visibility gate decides who gets it, as for every interactable).
8. Save as `game/forest/ChoppableTree.tscn`.

## 4. The server part (code)

`game/server/world/ServerTrees.cs`:

```csharp
namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using MmoGame3d.Forest;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.Social;

/// <summary>
/// Chopping trees: a log and Treework for whoever fells one, and the tree grows back
/// after a while. Trees are not saved: a server start finds every tree standing.
/// </summary>
public class ServerTrees
{
    // Placeholder.
    private static readonly TimeSpan RegrowAfter = TimeSpan.FromMinutes(3);

    private readonly Network _session;
    private readonly ServerProgress _progress;
    private readonly Action<Session> _bagChanged;
    private readonly Dictionary<ChoppableTree, DateTime> _felledAt = new Dictionary<ChoppableTree, DateTime>();

    public ServerTrees(Network session, ServerProgress progress, Action<Session> bagChanged)
    {
        _session = session;
        _progress = progress;
        _bagChanged = bagChanged;
    }

    public void Chop(Session session, ChoppableTree tree)
    {
        if (!tree.Standing)
        {
            _session.SendNotice(session.PeerId, "Only a stump is left. It grows back in a while.");
            return;
        }

        tree.Standing = false;
        _felledAt[tree] = DateTime.UtcNow;
        session.Body?.Show(Gestures.Chop);
        session.Inventory!.Add(ItemType.Log, ItemTier.Standard, 1);
        _bagChanged(session);
        _progress.Award(session, SkillId.Treework, SkillAwards.TreeworkPerTree);
        _session.SendNotice(session.PeerId, "You chopped the " + tree.TreeName + " and got a log.");
    }

    public void Tick()
    {
        DateTime now = DateTime.UtcNow;
        List<ChoppableTree> regrown = new List<ChoppableTree>();

        foreach (KeyValuePair<ChoppableTree, DateTime> entry in _felledAt)
        {
            if (now - entry.Value >= RegrowAfter)
            {
                entry.Key.Standing = true;
                regrown.Add(entry.Key);
            }
        }

        foreach (ChoppableTree tree in regrown)
        {
            _felledAt.Remove(tree);
        }
    }
}
```

What the server part must do, whatever the mechanic:

- **Check first.** Reach is already checked by `ServerInteractions`; check the state
  (is it standing?), and anything else (a tool equipped, a skill level, money).
- **Change state only here.** Setting `tree.Standing` is all it takes for every client
  in the zone to see it: the synchronizer sends it.
- **Tell the player** with a notice; the level-up notice comes from
  `ServerProgress.Award`.
- **Send the bag** after changing it (`_bagChanged`, which is `ServerGame.SendInventory`).
  The bag is saved with the player; nothing else to do.
- Something that spends money or items takes an intent id (making-changes.md).

## 5. The hook-up (code)

- `game/server/core/ServerInteractions.cs`: a property
  `public ServerTrees? Trees { get; set; }`, and in `Use`'s switch:
  `case MmoGame3d.Forest.ChoppableTree tree: Trees?.Chop(session, tree); break;`
- `game/server/core/ServerGame.cs`, in `Start` next to the chests:
  `_trees = new ServerTrees(network, _progress, SendInventory);` and
  `_interactions.Trees = _trees;` (with a field `private ServerTrees _trees = null!;`).
- `ServerGame`'s tick, next to `_chests.Tick();`: `_trees.Tick();`

Nothing else: `ServerGame` already hands every interactable's `Synchronizer` to the
visibility gate, so only players in that zone are sent the tree's state.

## 6. Place the trees (editor)

1. Open the zone, for example `game/zones/outskirts/outskirts.tscn`.
2. Drag `game/forest/ChoppableTree.tscn` onto the zone's **Interactables** node.
3. Name each one uniquely (`Pine0`, `Pine1`, ...): a client asks to use a thing by its
   name. Set `TreeName` and, if needed, `Reach` in the Inspector.
4. Save, then restart the server (it reads zone scenes at start).

## 7. Sound (catalog)

In `game/audio/sounds.json`, next to `fx.chest`:

```json
"fx.tree_fall": {
  "files": [ "sfx/<pack>/<a creak and crash>.wav" ],
  "bus": "Effects",
  "level": -8,
  "range": 25
},
```

Then `python tools/audio-subset/copy.py`, and open the editor once to import the new
file. Missing files are skipped with a log line, so the game runs without it.

## 8. Prove it (dev scenario)

- `game/dev/scenarios/ServerScenarios.cs`, in `Apply`'s switch: set the player up beside a tree:
  `case "chop": StandBy(record, ZoneIds.Outskirts, "Pine0", new Vector3(0f, 0f, 1.3f)); break;`
- `game/dev/scenarios/ScenarioDriver.cs`, in its switch: use it and expect the notice:
  `case "chop": Use("Chop the pine"); Expect("it chopped", () => Noticed("You chopped the pine and got a log.")); break;`
- `scripts/scenario-test.ps1`: add `"chop"` to the `$Scenarios` list.
- Run `.\scripts\scenario-test.ps1 -Scenarios chop`.

## Variations

- **Drop the log on the ground** instead of into the bag: `GroundItems.DropAt(zone,
  spot, ItemType.Log, ItemTier.Standard, 1)` (hand the part `_groundItems`).
- **Only with an axe equipped**: check the player's equipment in `Chop`, refuse with a
  notice. An axe is a new `ItemType` that is not stackable, like the EMP emitter.
- **Chopping takes time**: the client sends a start, the server remembers when and
  checks the time on the finish request (as Agent Defense does), rather than awarding on
  the first press.
- **Remember felled trees across restarts**: a migration and a store, as the garden's
  display plants do (making-changes.md, a database change).
- **An achievement for the first tree**: making-changes.md, achievements.
