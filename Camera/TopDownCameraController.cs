using Godot;

namespace Reroot.Camera;

public partial class TopDownCameraController : Node3D
{
    [Export]
    public NodePath CameraPath { get; set; } = new("Camera3D");

    [Export(PropertyHint.Range, "1.0, 250.0, 0.1")]
    public float OrbitDistance
    {
        get => _orbitDistance;
        set
        {
            _orbitDistance = Mathf.Clamp(value, MinimumOrbitDistance, MaximumOrbitDistance);
            ApplyCameraTransform();
        }
    }

    [Export(PropertyHint.Range, "1.0, 250.0, 0.1")]
    public float MinimumOrbitDistance { get; set; } = 4.0f;

    [Export(PropertyHint.Range, "1.0, 1000.0, 0.1")]
    public float MaximumOrbitDistance { get; set; } = 100.0f;

    [Export(PropertyHint.Range, "5.0, 89.0, 1.0")]
    public float PitchDegrees
    {
        get => Mathf.RadToDeg(_pitch);
        set
        {
            _pitch = Mathf.DegToRad(Mathf.Clamp(value, MinimumPitchDegrees, MaximumPitchDegrees));
            ApplyCameraTransform();
        }
    }

    [Export(PropertyHint.Range, "1.0, 89.0, 1.0")]
    public float MinimumPitchDegrees { get; set; } = 20.0f;

    [Export(PropertyHint.Range, "1.0, 89.0, 1.0")]
    public float MaximumPitchDegrees { get; set; } = 80.0f;

    [Export(PropertyHint.Range, "0.01, 1.0, 0.01")]
    public float OrbitSensitivity { get; set; } = 0.15f;

    [Export(PropertyHint.Range, "1.01, 4.0, 0.01")]
    public float ZoomMultiplier { get; set; } = 1.15f;

    private float _orbitDistance = 20.0f;
    private float _yaw;
    private float _pitch = Mathf.DegToRad(55.0f);
    private Camera3D? _camera;
    private bool _isOrbiting;
    private bool _isPanning;
    private Vector3 _panAnchor;

    public override void _Ready()
    {
        _camera = GetNodeOrNull<Camera3D>(CameraPath);
        if (_camera == null)
        {
            GD.PushError($"{nameof(TopDownCameraController)} could not find a Camera3D at '{CameraPath}'.");
            return;
        }

        _yaw = Rotation.Y;
        _pitch = Mathf.DegToRad(Mathf.Clamp(PitchDegrees, MinimumPitchDegrees, MaximumPitchDegrees));
        ApplyCameraTransform();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            HandleMouseMotion(mouseMotion);
            return;
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            HandleMouseButton(mouseButton);
        }
    }

    public void MoveFocusTo(Vector3 worldPosition)
    {
        GlobalPosition = worldPosition;
    }

    private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
    {
        if (_isOrbiting)
        {
            var sensitivity = Mathf.DegToRad(OrbitSensitivity);
            _yaw -= mouseMotion.Relative.X * sensitivity;
            _pitch = Mathf.Clamp(
                _pitch + mouseMotion.Relative.Y * sensitivity,
                Mathf.DegToRad(MinimumPitchDegrees),
                Mathf.DegToRad(MaximumPitchDegrees));
            ApplyCameraTransform();
        }

        if (_isPanning)
        {
            Pan(mouseMotion.Position);
        }

        if (_isOrbiting || _isPanning)
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        switch (mouseButton.ButtonIndex)
        {
            case MouseButton.Middle:
                _isPanning = mouseButton.Pressed &&
                    TryGetPanPlanePoint(mouseButton.Position, out _panAnchor);
                UpdateMouseMode();
                GetViewport().SetInputAsHandled();
                break;

            case MouseButton.Right:
                _isOrbiting = mouseButton.Pressed;
                UpdateMouseMode();
                GetViewport().SetInputAsHandled();
                break;

            case MouseButton.WheelUp when mouseButton.Pressed:
                OrbitDistance /= ZoomMultiplier;
                GetViewport().SetInputAsHandled();
                break;

            case MouseButton.WheelDown when mouseButton.Pressed:
                OrbitDistance *= ZoomMultiplier;
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private void Pan(Vector2 screenPosition)
    {
        if (TryGetPanPlanePoint(screenPosition, out var currentPoint))
        {
            GlobalPosition += _panAnchor - currentPoint;
        }
    }

    private bool TryGetPanPlanePoint(Vector2 screenPosition, out Vector3 worldPoint)
    {
        var rayOrigin = _camera!.ProjectRayOrigin(screenPosition);
        var rayDirection = _camera.ProjectRayNormal(screenPosition);
        if (Mathf.IsZeroApprox(rayDirection.Y))
        {
            worldPoint = default;
            return false;
        }

        var distance = (GlobalPosition.Y - rayOrigin.Y) / rayDirection.Y;
        if (distance < 0.0f)
        {
            worldPoint = default;
            return false;
        }

        worldPoint = rayOrigin + rayDirection * distance;
        return true;
    }

    private void ApplyCameraTransform()
    {
        if (_camera == null)
        {
            return;
        }

        Rotation = new Vector3(0.0f, _yaw, 0.0f);

        var height = Mathf.Sin(_pitch) * OrbitDistance;
        var depth = Mathf.Cos(_pitch) * OrbitDistance;
        _camera.Position = new Vector3(0.0f, height, depth);
        _camera.Rotation = new Vector3(-_pitch, 0.0f, 0.0f);
    }

    private void UpdateMouseMode()
    {
        Input.MouseMode = _isOrbiting
            ? Input.MouseModeEnum.Captured
            : Input.MouseModeEnum.Visible;
    }
}
