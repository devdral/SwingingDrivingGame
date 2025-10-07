using Godot;

namespace SwingingDrivingGame.Utility;

public static class World3DExtensions
{
    /// <summary>
    /// Helper method to easily raycast using World3D (with type-safety!)
    /// WARNING: This still incurs dictionary access costs because it interacts with
    /// the underlying Godot.Collections.Dictionary (untyped) API.
    /// </summary>
    /// <param name="query">The query to query the physics server about. Basically just a ray in 3D space.</param>
    /// <returns></returns>
    public static RayCastResult3D? CastRay(this World3D world3D, PhysicsRayQueryParameters3D query)
    {
        var dss = world3D.DirectSpaceState;
        var dict = dss.IntersectRay(query);
        if (dict.Count > 0)
        {
            return new RayCastResult3D(
                (Vector3)dict["position"],
                (Vector3)dict["normal"],
                (GodotObject)dict["collider"],
                (Rid)dict["rid"],
                (int)dict["shape"],
                dict["metadata"]
                );
        }

        return null;
    }

    public static bool DoesRayIntersect(this World3D world3D, PhysicsRayQueryParameters3D query)
    {
        var dss = world3D.DirectSpaceState;
        var dict = dss.IntersectRay(query);
        return dict.Count > 0;
    }
}

public record RayCastResult3D(
    Vector3 Position,
    Vector3 Normal,
    GodotObject Collider,
    Rid PhysServerColliderObject,
    int ShapeIndex,
    Variant Metadata);