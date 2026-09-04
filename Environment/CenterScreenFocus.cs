using Godot;

namespace Reroot.Environment;

public partial class CenterScreenFocus : Node
{
    [Export]
    public NodePath WorldEnvironmentPath { get; set; } = new("../../WorldEnvironment");

    [Export(PropertyHint.Layers3DPhysics)]
    public uint CollisionMask { get; set; } = uint.MaxValue;

    [Export(PropertyHint.Range, "-100.0, 100.0, 0.1")]
    public float FallbackPlaneHeight { get; set; }

    [Export(PropertyHint.Range, "0.0, 30.0, 0.1")]
    public float FocusSmoothing { get; set; } = 10.0f;

    [Export(PropertyHint.Range, "0.05, 1000.0, 0.05")]
    public float MinimumFocusDistance { get; set; } = 0.1f;

    private Camera3D? _camera;
    private CameraAttributesPhysical? _attributes;
    private float _focusDistance;

    public override void _Ready()
    {
        _camera = GetParentOrNull<Camera3D>();
        _attributes = ResolveWorldEnvironmentAttributes();
        if (_attributes != null)
        {
            _focusDistance = _attributes.FrustumFocusDistance;
        }

        if (_camera == null)
        {
            GD.PushError($"{nameof(CenterScreenFocus)} must be a child of a Camera3D.");
        }
        else if (_attributes == null)
        {
            GD.PushError(
                $"{nameof(CenterScreenFocus)} requires CameraAttributesPhysical on " +
                $"the WorldEnvironment at '{WorldEnvironmentPath}'.");
        }
    }

    public override void _Process(double delta)
    {
        if (_camera == null || _attributes == null)
        {
            return;
        }

        if (!TryGetFocusPoint(out Vector3 rayOrigin, out Vector3 focusPoint))
        {
            return;
        }

        var targetDistance = Mathf.Max(MinimumFocusDistance, rayOrigin.DistanceTo(focusPoint));
        var weight = FocusSmoothing <= 0.0f
            ? 1.0f
            : 1.0f - Mathf.Exp(-FocusSmoothing * (float)delta);

        _focusDistance = Mathf.Lerp(_focusDistance, targetDistance, weight);
        _attributes.FrustumFocusDistance = _focusDistance;
    }

    private CameraAttributesPhysical? ResolveWorldEnvironmentAttributes()
    {
        var worldEnvironment = GetNodeOrNull<WorldEnvironment>(WorldEnvironmentPath);
        return worldEnvironment?.CameraAttributes as CameraAttributesPhysical;
    }

    private bool TryGetFocusPoint(out Vector3 rayOrigin, out Vector3 focusPoint)
    {
        var viewportCenter = GetViewport().GetVisibleRect().Size * 0.5f;
        rayOrigin = _camera!.ProjectRayOrigin(viewportCenter);
        var rayDirection = _camera.ProjectRayNormal(viewportCenter);
        var rayEnd = rayOrigin + rayDirection * _camera.Far;

        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd, CollisionMask);
        var hit = _camera.GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.TryGetValue("position", out Variant position))
        {
            focusPoint = position.AsVector3();
            return true;
        }

        return TryIntersectPlane(rayOrigin, rayDirection, out focusPoint);
    }

    private bool TryIntersectPlane(Vector3 rayOrigin, Vector3 rayDirection, out Vector3 intersection)
    {
        if (Mathf.IsZeroApprox(rayDirection.Y))
        {
            intersection = default;
            return false;
        }

        var distance = (FallbackPlaneHeight - rayOrigin.Y) / rayDirection.Y;
        if (distance < 0.0f)
        {
            intersection = default;
            return false;
        }

        intersection = rayOrigin + rayDirection * distance;
        return true;
    }
}
