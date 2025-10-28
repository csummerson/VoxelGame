using Godot;
using System;

public partial class OrbitalObject : Node3D
{
    [Export] Node3D primaryBody;

    [Export] float rotationDamp = 0.3f;

    Vector3 targetUp = Vector3.Up;

    public override void _Process(double delta)
    {
        if (primaryBody != null && GetParent() != null)
        {
            RotateTowardPrimary(GetParent() as Node3D, (float)delta);
        }
    }

    private void RotateTowardPrimary(Node3D parent, float delta)
    {

        Vector3 currentRotation = parent.GlobalTransform.Basis.Y;
        targetUp = -(primaryBody.GlobalTransform.Origin - parent.GlobalTransform.Origin).Normalized();

        Quaternion q = new Quaternion(currentRotation, targetUp);
        Quaternion targetRotation = q * parent.GlobalTransform.Basis.GetRotationQuaternion();

        Quaternion smoothed = parent.GlobalTransform.Basis.GetRotationQuaternion().Slerp(targetRotation.Normalized(), rotationDamp * delta * 60);

        Basis newBasis = new Basis(smoothed);
        parent.GlobalTransform = new Transform3D(newBasis, parent.GlobalTransform.Origin);
    }

}
