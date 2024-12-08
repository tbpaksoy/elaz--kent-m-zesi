using Godot;
using Godot.Collections;
using System;

public partial class Visitor : CharacterBody3D
{
    [Export]
    public float Speed = 5.0f;
    [Export]
    private CanvasLayer layer;
    [Export]
    public string language = "tr";
    private RayCast3D rayCast;
    private Action inspectState, viewState;
    private Node target;
    private Node3D head, neck;
    private SpotLight3D light;
    private Color lightColor;

    public override void _PhysicsProcess(double delta)
    {
        MoveAndSlide();
        if (IsOnFloor() && viewState == View)
        {
            Vector3 x = head.GlobalTransform.Basis.X with { Y = 0 },
                    z = head.GlobalTransform.Basis.Z with { Y = 0 };
            if (Input.IsActionPressed("forward"))
            {
                MoveAndCollide(-z * Speed * (float)delta);
            }
            if (Input.IsActionPressed("backward"))
            {
                MoveAndCollide(z * Speed * (float)delta);
            }
            if (Input.IsActionPressed("left"))
            {
                MoveAndCollide(-x * Speed * (float)delta);
            }
            if (Input.IsActionPressed("right"))
            {
                MoveAndCollide(x * Speed * (float)delta);
            }
        }

        GodotObject obj = rayCast.GetCollider();
        if (obj is Node node && node != null)
        {
            target = node;
            inspectState = () => Inspect(node);
        }
        else
        {
            inspectState = UndoInspect;
        }
    }
    public override void _Process(double delta)
    {
        inspectState?.Invoke();

    }
    public override void _Ready()
    {
        viewState = View;

        Velocity = Vector3.Down;

        rayCast = GetNode<RayCast3D>("%ray_cast");
        head = GetNode<Node3D>("%camera");
        neck = GetNode<Node3D>("%neck");
        light = GetNode<SpotLight3D>("%light");
        lightColor = light.LightColor;

        Input.SetMouseMode(Input.MouseModeEnum.Captured);
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (viewState == View)
        {
            if (@event is InputEventMouseMotion motion)
            {
                neck.RotateY(-motion.Relative.X * 0.0075f);
                head.RotateX(-motion.Relative.Y * 0.0075f);
                if (head.RotationDegrees.X > 90)
                {
                    head.RotationDegrees = head.RotationDegrees with { X = 90 };
                }
                else if (head.RotationDegrees.X < -90)
                {
                    head.RotationDegrees = head.RotationDegrees with { X = -90 };
                }
            }
            if (Input.IsActionJustPressed("toggle_light"))
                light.LightColor = light.LightColor == lightColor ? Colors.Black : lightColor;
            if (Input.IsActionPressed("light_focus_inc"))
                light.SpotAngle = Mathf.Max(5, light.SpotAngle - 1);
            if (Input.IsActionPressed("light_focus_dec"))
                light.SpotAngle = Mathf.Min(30, light.SpotAngle + 1);

        }
        if (Input.IsActionJustPressed("interact"))
            viewState?.Invoke();

    }
    private void Inspect(Node node)
    {
        if (layer == null || !node.GetParent().HasMeta("about"))
        {
            GD.Print("Layer is null");
            return;
        }

        Dictionary about = node.GetParent().GetMeta("about").AsGodotDictionary();

        if (about == null || about.Count == 0)
        {
            GD.Print("No about data");
            return;
        }

        if (about.ContainsKey("name_" + language) && layer.HasNode("%name_label"))
        {
            Label label = layer.GetNode<Label>("%name_label");
            label.Text = about["name_" + language].ToString();
        }

        if (about.ContainsKey("description_" + language) && layer.HasNode("%description_text"))
        {
            RichTextLabel text = layer.GetNode<RichTextLabel>("%description_text");
            text.Text = about["description_" + language].ToString();
        }

        inspectState = null;
    }
    private void UndoInspect()
    {
        if (layer.HasNode("%name_label"))
        {
            Label label = layer.GetNode<Label>("%name_label");
            label.Text = "";
        }
        if (layer.HasNode("%description_text"))
        {
            RichTextLabel text = layer.GetNode<RichTextLabel>("%description_text");
            text.Text = "";
        }

        inspectState = null;
        target = null;
    }
    private void View()
    {
        if (target == null || !target.GetParent().HasMeta("cvc")) return;

        Camera3D camera = target.GetParent().GetNodeOrNull<Camera3D>(target.GetParent().GetMeta("cvc").AsNodePath());



        if (camera == null) return;

        camera.MakeCurrent();
        viewState = QuitView;
    }
    private void QuitView()
    {
        GetNode<Camera3D>("%camera").MakeCurrent();
        viewState = View;
    }

}
