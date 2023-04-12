using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeCreator : MonoBehaviour
{
    public GameObject[] m_worldObjects;
    public int m_nodeMinSize = 5;

    Octree m_octree;
    // Start is called before the first frame update
    void Start()
    {
        m_octree = new(m_worldObjects, m_nodeMinSize);

    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            m_octree.m_rootNode.Draw();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }


}

internal class Octree
{
    public OctreeNode m_rootNode;
    public Octree(GameObject[] worldObjects, float minNodeSize)
    {
        Bounds bounds = new Bounds();

        foreach (GameObject go in worldObjects)
        {
            bounds.Encapsulate(go.GetComponent<Collider>().bounds);
        }

        float maxSize = 0.5f * Mathf.Max(Mathf.Max(bounds.size.z, bounds.size.x), bounds.size.y);
        Vector3 sizeVector = new Vector3(maxSize, maxSize, maxSize);
        bounds.SetMinMax(bounds.center - sizeVector, bounds.center + sizeVector);
        m_rootNode = new OctreeNode(bounds, minNodeSize);

        AddObjects(worldObjects);
    }

    public void AddObjects(GameObject[] worldObjects)
    {
        foreach (GameObject go in worldObjects)
        {
            m_rootNode.AddObject(go);
        }
    }

}
internal class OctreeNode
{
    Bounds nodeBounds;
    float minSize;
    Bounds[] childBounds;
    OctreeNode[] children = null;

    public OctreeNode(Bounds b, float minNodeSize)
    {
        nodeBounds = b;
        minSize = minNodeSize;

        float quarter = nodeBounds.size.y / 4.0f;
        float childLength = nodeBounds.size.y / 2;

        Vector3 childSize = new Vector3(childLength, childLength, childLength);
        childBounds = new Bounds[8];
        childBounds[0] = new Bounds(nodeBounds.center + new Vector3(-quarter, quarter, -quarter), childSize);
        childBounds[1] = new Bounds(nodeBounds.center + new Vector3(-quarter, quarter, quarter), childSize);
        childBounds[2] = new Bounds(nodeBounds.center + new Vector3(quarter, quarter, -quarter), childSize);
        childBounds[3] = new Bounds(nodeBounds.center + new Vector3(quarter, quarter, quarter), childSize);

        childBounds[4] = new Bounds(nodeBounds.center + new Vector3(-quarter, -quarter, -quarter), childSize);
        childBounds[5] = new Bounds(nodeBounds.center + new Vector3(-quarter, -quarter, quarter), childSize);
        childBounds[6] = new Bounds(nodeBounds.center + new Vector3(quarter, -quarter, -quarter), childSize);
        childBounds[7] = new Bounds(nodeBounds.center + new Vector3(quarter, -quarter, quarter), childSize);
    }

    public void Draw()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(nodeBounds.center, nodeBounds.size);

        if (null != children)
        {
            for (int i = 0; i < 8; ++i)
            {
                if (null != children[i])
                {
                    children[i].Draw();
                }
            }
        }

    }

    public void AddObject(GameObject go)
    {
        DivideAndAdd(go);
    }

    public void DivideAndAdd(GameObject go)
    {
        if (nodeBounds.size.y > minSize)
        {
            if (null == children) children = new OctreeNode[8];

            bool dividing = false;
            for (int i = 0; i < 8; i++)
            {
                if (children[i] == null)
                {
                    children[i] = new(childBounds[i], minSize);
                }

                if (childBounds[i].Intersects(go.GetComponent<Collider>().bounds))
                {
                    dividing = true;
                    children[i].DivideAndAdd(go);
                }
            }

            if (false == dividing) children = null;
        }
    }
}
