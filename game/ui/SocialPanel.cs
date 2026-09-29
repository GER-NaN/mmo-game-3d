namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Rules.World;

// Friends, with who is online and where. A friend's "..." opens their own view (message,
// remove); the ignored are one layer down, behind the Ignored button, so they are out of
// sight. Buttons never take focus, so walking goes on after a click.
public partial class SocialPanel : PanelContainer
{
    private static readonly PackedScene RowScene = GD.Load<PackedScene>("res://game/ui/FriendRow.tscn");
    private static readonly Color Offline = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color OnlineDot = new Color(0.35f, 0.85f, 0.4f);
    private static readonly Color OfflineDot = new Color(0.5f, 0.5f, 0.5f);

    // (player id) for both Remove and Unignore: either takes them off their list.
    public event Action<string>? RemovePressed;

    // (player id, name): opens a private conversation in the chat.
    public event Action<string, string>? MessagePressed;

    public event Action? Closed;

    private string[] _friendIds = new string[0];
    private string[] _friendNames = new string[0];
    private string[] _friendZones = new string[0];

    // The friend whose view is open; null on the list or the ignored.
    private string? _friendId;

    public override void _Ready()
    {
        GetNode<Button>("%Close").Pressed += () => Closed?.Invoke();
        GetNode<Button>("%Back").Pressed += ShowFriendsView;
        GetNode<Button>("%ShowIgnored").Pressed += ShowIgnoredView;
        GetNode<Button>("%FriendMessage").Pressed += () =>
        {
            int i = Array.IndexOf(_friendIds, _friendId);

            if (i >= 0)
            {
                MessagePressed?.Invoke(_friendIds[i], _friendNames[i]);
            }
        };
        GetNode<Button>("%FriendRemove").Pressed += () =>
        {
            if (_friendId != null)
            {
                RemovePressed?.Invoke(_friendId);
            }
        };
    }

    public void ShowContacts(string[] friendIds, string[] friendNames, string[] friendZones, string[] ignoredIds, string[] ignoredNames)
    {
        _friendIds = friendIds;
        _friendNames = friendNames;
        _friendZones = friendZones;
        VBoxContainer friends = GetNode<VBoxContainer>("%Friends");
        VBoxContainer ignored = GetNode<VBoxContainer>("%Ignored");
        Clear(friends);
        Clear(ignored);

        for (int i = 0; i < friendIds.Length; i++)
        {
            AddFriendRow(friends, friendIds[i], friendNames[i], friendZones[i]);
        }

        for (int i = 0; i < ignoredIds.Length; i++)
        {
            AddIgnoredRow(ignored, ignoredIds[i], ignoredNames[i]);
        }

        if (friendIds.Length == 0)
        {
            friends.AddChild(new Label { Text = "No friends yet.", Modulate = Offline });
        }

        if (ignoredIds.Length == 0)
        {
            ignored.AddChild(new Label { Text = "Nobody.", Modulate = Offline });
        }

        GetNode<Button>("%ShowIgnored").Text = "Ignored (" + ignoredIds.Length + ")";

        // A friend's view follows them going on or offline, and closes when they are removed.
        if (_friendId != null)
        {
            if (Array.IndexOf(_friendIds, _friendId) >= 0)
            {
                ShowFriendView(_friendId);
            }
            else
            {
                ShowFriendsView();
            }
        }
    }

    private void AddFriendRow(VBoxContainer list, string id, string name, string zone)
    {
        bool online = zone.Length > 0;

        // Rows are named for who they hold, so they can be found by it (bots.md T2).
        HBoxContainer row = RowScene.Instantiate<HBoxContainer>();
        row.Name = "Friend_" + id;
        row.GetNode<Panel>("Dot").SelfModulate = online ? OnlineDot : OfflineDot;
        row.GetNode<Panel>("Dot").TooltipText = online ? "Online" : "Offline";
        Label nameLabel = row.GetNode<Label>("Name");
        nameLabel.Text = name;
        nameLabel.Modulate = online ? Colors.White : Offline;
        Label where = row.GetNode<Label>("Where");
        where.Text = online ? ZoneIds.DisplayName(zone) : "offline";
        where.Modulate = online ? Colors.White : Offline;

        Button mail = row.GetNode<Button>("Mail");
        mail.Disabled = !online;
        mail.TooltipText = online ? "Message " + name : name + " is offline";
        mail.Pressed += () => MessagePressed?.Invoke(id, name);
        row.GetNode<Button>("More").Pressed += () => ShowFriendView(id);
        list.AddChild(row);
    }

    private void AddIgnoredRow(VBoxContainer list, string id, string name)
    {
        HBoxContainer row = new HBoxContainer { Name = "Ignored_" + id };
        row.AddThemeConstantOverride("separation", 12);
        row.AddChild(new Label { Text = name, SizeFlagsHorizontal = SizeFlags.ExpandFill });
        Button button = new Button { Name = "Unignore", Text = "Unignore", FocusMode = FocusModeEnum.None };
        button.Pressed += () => RemovePressed?.Invoke(id);
        row.AddChild(button);
        list.AddChild(row);
    }

    private void ShowFriendsView()
    {
        _friendId = null;
        ShowView("Friends", "%FriendsView");
    }

    private void ShowIgnoredView()
    {
        _friendId = null;
        ShowView("Ignored", "%IgnoredView");
    }

    private void ShowFriendView(string id)
    {
        int i = Array.IndexOf(_friendIds, id);

        if (i < 0)
        {
            return;
        }

        _friendId = id;
        bool online = _friendZones[i].Length > 0;
        GetNode<Label>("%FriendStatus").Text = online ? "Online, in " + ZoneIds.DisplayName(_friendZones[i]) : "Offline";
        GetNode<Button>("%FriendMessage").Disabled = !online;
        ShowView(_friendNames[i], "%FriendView");
    }

    private void ShowView(string title, string view)
    {
        GetNode<Label>("%Title").Text = title;
        GetNode<Button>("%Back").Visible = view != "%FriendsView";

        foreach (string each in new string[] { "%FriendsView", "%IgnoredView", "%FriendView" })
        {
            GetNode<Control>(each).Visible = each == view;
        }

        // Shrink to the new view, not stay as tall as the last one.
        ResetSize();
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
