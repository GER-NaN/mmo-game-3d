namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Rules.World;

// Friends, with who is online and where, and the ignored. Buttons never take focus, so
// walking goes on after a click.
public partial class SocialPanel : PanelContainer
{
    private static readonly Color Offline = new Color(1f, 1f, 1f, 0.5f);

    // (player id) for both Remove and Unignore: either takes them off their list.
    public event Action<string>? RemovePressed;

    public void ShowContacts(string[] friendIds, string[] friendNames, string[] friendZones, string[] ignoredIds, string[] ignoredNames)
    {
        VBoxContainer friends = GetNode<VBoxContainer>("%Friends");
        VBoxContainer ignored = GetNode<VBoxContainer>("%Ignored");
        Clear(friends);
        Clear(ignored);

        for (int i = 0; i < friendIds.Length; i++)
        {
            bool online = friendZones[i].Length > 0;
            AddRow(friends, friendIds[i], friendNames[i], online ? ZoneIds.SceneOf(friendZones[i]) : "offline", online, "Remove");
        }

        for (int i = 0; i < ignoredIds.Length; i++)
        {
            AddRow(ignored, ignoredIds[i], ignoredNames[i], "", true, "Unignore");
        }

        if (friendIds.Length == 0)
        {
            friends.AddChild(new Label { Text = "No friends yet.", Modulate = Offline });
        }

        if (ignoredIds.Length == 0)
        {
            ignored.AddChild(new Label { Text = "Nobody.", Modulate = Offline });
        }
    }

    private void AddRow(VBoxContainer list, string id, string name, string where, bool bright, string action)
    {
        HBoxContainer row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        row.AddChild(new Label { Text = name, SizeFlagsHorizontal = SizeFlags.ExpandFill, Modulate = bright ? Colors.White : Offline });
        row.AddChild(new Label { Text = where, Modulate = bright ? Colors.White : Offline });
        Button button = new Button { Text = action, FocusMode = FocusModeEnum.None };
        button.Pressed += () => RemovePressed?.Invoke(id);
        row.AddChild(button);
        list.AddChild(row);
    }

    private static void Clear(Node list)
    {
        foreach (Node child in list.GetChildren())
        {
            list.RemoveChild(child);
            child.QueueFree();
        }
    }
}
