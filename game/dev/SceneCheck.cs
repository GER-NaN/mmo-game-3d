namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// What an editor save would lose from each scene. The editor saves by packing the
/// edited tree, and packing keeps a change to an instanced scene's child only when that
/// instance has editable children; the game applies such a change either way, so a
/// hand-written scene can work in the game and still lose it on the first save. This
/// does the same pack and compares every saved property before and after.
/// </summary>
public static class SceneCheck
{
    private const int Shown = 80;

    // The number of scenes that would lose something.
    public static int Run(string root)
    {
        List<string> scenes = new List<string>();
        Collect(root, scenes);
        int losing = 0;

        foreach (string path in scenes)
        {
            List<string> losses = Check(path);

            foreach (string loss in losses)
            {
                GD.Print("Scene check: " + path + ": " + loss);
            }

            if (losses.Count > 0)
            {
                losing++;
            }
        }

        GD.Print("Scene check: " + scenes.Count + " scenes, " + losing + " would lose something on an editor save");
        return losing;
    }

    private static void Collect(string folder, List<string> scenes)
    {
        foreach (string file in DirAccess.GetFilesAt(folder))
        {
            if (file.EndsWith(".tscn"))
            {
                scenes.Add(folder + "/" + file);
            }
        }

        foreach (string sub in DirAccess.GetDirectoriesAt(folder))
        {
            Collect(folder + "/" + sub, scenes);
        }
    }

    private static List<string> Check(string path)
    {
        List<string> losses = new List<string>();
        PackedScene scene = GD.Load<PackedScene>(path);

        // Edit state, as the editor opens a scene: instances remember their scene and
        // whether their children are editable, which is what packing looks at.
        Node before = scene.Instantiate(PackedScene.GenEditState.Main);
        PackedScene repacked = new PackedScene();
        repacked.Pack(before);
        Node after = repacked.Instantiate(PackedScene.GenEditState.Main);

        Compare(before, after, before, after, losses);

        foreach (Node node in before.FindChildren("*", "", true, false))
        {
            // No owner: made by its parent (a scroll bar in a ScrollContainer), never saved.
            if (node.Owner == null)
            {
                continue;
            }

            NodePath at = before.GetPathTo(node);
            Node? kept = after.GetNodeOrNull(at);

            if (kept == null)
            {
                losses.Add(at + ": the node");
            }
            else
            {
                Compare(before, after, node, kept, losses);
            }
        }

        before.Free();
        after.Free();
        return losses;
    }

    private static void Compare(Node root, Node keptRoot, Node node, Node kept, List<string> losses)
    {
        NodePath at = root.GetPathTo(node);

        foreach (Godot.Collections.Dictionary property in node.GetPropertyList())
        {
            PropertyUsageFlags usage = (PropertyUsageFlags)(long)property["usage"];

            if ((usage & PropertyUsageFlags.Storage) == 0)
            {
                continue;
            }

            string name = (string)property["name"];
            string was = Describe(root, node.Get(name));
            string now = Describe(keptRoot, kept.Get(name));

            if (was != now)
            {
                int from = FirstDifference(was, now);
                losses.Add(at + ": " + name + ": " + Short(was, from) + " -> " + Short(now, from));
            }
        }
    }

    // Nodes by their path in the scene, and resources by file or by content, since the
    // two trees are different objects.
    private static string Describe(Node root, Variant value)
    {
        if (value.VariantType != Variant.Type.Object)
        {
            return GD.VarToStr(value);
        }

        GodotObject? thing = value.AsGodotObject();
        Node? node = thing as Node;
        Resource? resource = thing as Resource;

        if (thing == null)
        {
            return "null";
        }

        if (node != null)
        {
            return "node " + root.GetPathTo(node);
        }

        if (resource != null && resource.ResourcePath.Length > 0 && !resource.ResourcePath.Contains("::"))
        {
            return resource.ResourcePath;
        }

        return GD.VarToStr(value);
    }

    private static int FirstDifference(string a, string b)
    {
        int i = 0;

        while (i < a.Length && i < b.Length && a[i] == b[i])
        {
            i++;
        }

        return i;
    }

    // The part round the first difference, so a long resource shows what changed.
    private static string Short(string text, int from)
    {
        string line = text.Replace("\n", " ");
        int start = Math.Max(0, Math.Min(from - (Shown / 4), line.Length - Shown));
        string part = line.Substring(start, Math.Min(Shown, line.Length - start));
        return (start > 0 ? "..." : "") + part + (start + part.Length < line.Length ? "..." : "");
    }
}
