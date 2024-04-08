using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GravityReference
{
    public Transform SphericalParent;
    public Vector3 UpVec;

    public GravityReference(Vector3 upVec, Transform sphericalParent = null)
    {
        UpVec = upVec;
        SphericalParent = sphericalParent;
    }

    public GravityReference(Transform sphericalParent = null)
    {
        UpVec = Vector3.up;
        SphericalParent = sphericalParent;
    }
}
