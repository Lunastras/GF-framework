using UnityEngine;
using System.Collections;

public class RoundShadowScript : MonoBehaviour
{

    [Header("Settings")]
    public Transform _parent;
    public Vector3 _parentOffset = new Vector3(0f, 0.01f, 0f);
    public LayerMask _layerMask;

    void Update()
    {
        Ray ray = new Ray(transform.position, -Vector3.up);
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo, 100f, _layerMask))
        {
            // Position
            _parent.position = hitInfo.point + _parentOffset;

            // Rotate to same angle as ground
            _parent.up = hitInfo.normal;
        }
        else
        {
            // If raycast not hitting (air beneath feet), position it far away
            _parent.position = new Vector3(0f, 1000f, 0f);
        }
    }

}