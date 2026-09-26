namespace MmoGame3d.Gardening;

using Godot;
using MmoGame3d.Rules.Gardening;

/// <summary>
/// Builds a house plant from its design, the same way everywhere: at the potting table
/// while it is made, and wherever it stands after. Clients only; the server keeps the
/// design and never draws.
/// </summary>
public static class PlantBuilder
{
    private const string Folder = "res://assets/tinytreats/house_plants/";

    public static Node3D Build(PlantDesign design)
    {
        Node3D plant = new Node3D { Name = "Plant" };
        Node3D? pot = Model(design.Pot);

        if (pot != null)
        {
            plant.AddChild(pot);
        }

        foreach (PlantPiece piece in design.Pieces)
        {
            Node3D? model = Model(piece.Id);

            if (model != null)
            {
                Place(model, design.Pot, piece);
                plant.AddChild(model);
            }
        }

        return plant;
    }

    // A piece's model stands on the soil at its spot, turned, leaned out and sized.
    public static void Place(Node3D model, string pot, PlantPiece piece)
    {
        float radius = PlantParts.SoilRadius(pot);
        model.Position = new Vector3(piece.X * radius, PlantParts.SoilHeight(pot), piece.Z * radius);
        model.Basis = new Basis(Vector3.Up, piece.Yaw) * new Basis(Vector3.Right, piece.Tilt) * Basis.FromScale(Vector3.One * piece.Scale);
    }

    // Null when the art is missing on this machine (assets/README.md).
    public static Node3D? Model(string id)
    {
        string path = Folder + id + ".gltf";

        if (!ResourceLoader.Exists(path))
        {
            GD.PrintErr("House plant art missing at " + path + "; see assets/README.md");
            return null;
        }

        return GD.Load<PackedScene>(path).Instantiate<Node3D>();
    }
}
