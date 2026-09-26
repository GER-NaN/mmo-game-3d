namespace MmoGame3d.Ui;

using Godot;

/// <summary>
/// A small picture of a 3D model, framed to fit, for a button or a list. It renders
/// once into its own little world and then only shows that picture, so a tray of them
/// costs next to nothing after the first frame.
/// </summary>
public partial class ModelThumb : SubViewportContainer
{
    // The model to show; it is freed with the thumb.
    public Node3D? Model { get; set; }

    // Degrees round and down from the model's front.
    public float Turn { get; set; } = 35f;
    public float Look { get; set; } = 30f;

    public override void _Ready()
    {
        Stretch = true;
        MouseFilter = MouseFilterEnum.Ignore;

        Godot.Environment environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.ClearColor,
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = Colors.White,
            AmbientLightEnergy = 0.7f,
        };

        // The world is given before the viewport enters the tree, as at the potting table.
        SubViewport viewport = new SubViewport
        {
            OwnWorld3D = true,
            World3D = new World3D { Environment = environment },
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
        };
        AddChild(viewport);
        viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-50f, -35f, 0f) });

        if (Model == null)
        {
            return;
        }

        viewport.AddChild(Model);
        Aabb box = Bounds(Model, Transform3D.Identity);
        Vector3 middle = box.GetCenter();
        float reach = Mathf.Max(box.Size.Length(), 0.01f);
        Vector3 away = new Vector3(0f, 0f, 1f).Rotated(Vector3.Right, -Mathf.DegToRad(Look)).Rotated(Vector3.Up, Mathf.DegToRad(Turn));
        Camera3D camera = new Camera3D { Fov = 30f };
        viewport.AddChild(camera);
        camera.Position = middle + (away * reach * 1.7f);
        camera.LookAt(middle);
    }

    // The model's bounds in its own space, from every mesh under it.
    private static Aabb Bounds(Node node, Transform3D parent)
    {
        Transform3D here = parent;
        Aabb box = new Aabb();
        bool found = false;
        Node3D? spatial = node as Node3D;

        if (spatial != null)
        {
            here = parent * spatial.Transform;
        }

        VisualInstance3D? visual = node as VisualInstance3D;

        if (visual != null && !(node is Light3D))
        {
            box = here * visual.GetAabb();
            found = true;
        }

        foreach (Node child in node.GetChildren())
        {
            Aabb inner = Bounds(child, here);

            if (inner.Size != Vector3.Zero)
            {
                box = found ? box.Merge(inner) : inner;
                found = true;
            }
        }

        return box;
    }
}
