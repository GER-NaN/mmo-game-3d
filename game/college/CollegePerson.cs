namespace MmoGame3d.College;

using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Players;

/// <summary>
/// Someone at the college: the registrar (the Class, enrolling, changing career) or a
/// professor (rank-ups). Talking to them opens their panel; what they do is decided on
/// the server (ServerCollege).
/// </summary>
public partial class CollegePerson : Interactable
{
    public const string Registrar = "registrar";
    public const string Professor = "professor";

    [Export]
    public string Role { get; set; } = Registrar;

    [Export]
    public string PersonName { get; set; } = "Registrar";

    [Export]
    public string ModelPath { get; set; } = "res://assets/kaykit/characters/Protagonist_A.glb";

    public override string Prompt
    {
        get { return "Talk to " + PersonName; }
    }

    public override void _Ready()
    {
        GetNode<Label3D>("NameLabel").Text = PersonName;

        if (!Multiplayer.IsServer())
        {
            AddChild(new CharacterModel { Name = "Model", ModelPath = ModelPath });
        }
    }
}
