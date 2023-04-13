using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Unity.Collections;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.Serialization.Formatters.Binary;


using static Unity.Mathematics.math;

[ExecuteInEditMode]
public class GfPathfinding : MonoBehaviour
{
    [SerializeField]
    private Vector3 m_distanceBetweenNodes = new(0.5f, 0.5f, 0.5f);

    [SerializeField]
    private float m_collisionRadius = 1.0f;

    [SerializeField]
    private LayerMask m_layerMask;


    [SerializeField]
    private Transform m_start;

    [SerializeField]
    private Transform m_end;

#if UNITY_EDITOR

    [SerializeField]
    private bool m_visualizePoints;

    [SerializeField]
    private bool m_visualiseCollisionRadius;

    [SerializeField]
    private float m_visualiseDrawDistanceSquared = 100;

    private Transform m_camera;

#endif //UNITY_EDITOR

    [System.Serializable]
    private struct NodePathData
    {
        public int3 gridSize;
        public float3 startPoint;
        public int gridLength;

        public PathNode[] pathNodeArray;
    }

    private int3 m_gridSize;
    private int m_gridLength = 0;
    private float3 m_startPoint;

    NativeArray<PathNode> m_pathNodeArray;
    NativeArray<int3> m_neighbourOffsetArray;

    private static readonly int3[] NEIGHBOUR_OFFSETS =
    {
      new(1, 1, 1)
    , new(1, 1, 0)
    , new(1, 1, -1)

    , new(1, 0, 1)
    , new(1, 0, 0)
    , new(1, 0, -1)

    , new(1, -1, 1)
    , new(1, -1, 0)
    , new(1, -1, -1)

    //////////////////

    , new(0, 1, 1)
    , new(0, 1, 0)
    , new(0, 1, -1)

    , new(0, 0, 1)
    , new(0, 0, 0)
    , new(0, 0, -1)

    , new(0, -1, 1)
    , new(0, -1, 0)
    , new(0, -1, -1)

    //////////////////

    , new(-1, 1, 1)
    , new(-1, 1, 0)
    , new(-1, 1, -1)

    , new(-1, 0, 1)
    , new(-1, 0, 0)
    , new(-1, 0, -1)

    , new(-1, -1, 1)
    , new(-1, -1, 0)
    , new(-1, -1, -1)};

    public void GenerateNodePath()
    {
        Debug.Log("Generating node path, might take a few seconds");

        Vector3 scale = transform.localScale;
        int3 gridSize = new((int)(scale.x / m_distanceBetweenNodes.x) + 1
        , (int)(scale.y / m_distanceBetweenNodes.y) + 1
        , (int)(scale.z / m_distanceBetweenNodes.z) + 1);

        float3 offset = new(
              scale.x % m_distanceBetweenNodes.x
            , scale.y % m_distanceBetweenNodes.y
            , scale.z % m_distanceBetweenNodes.z
        );

        offset *= 0.5f;

        int length = gridSize.x * gridSize.y * gridSize.z;
        PathNode[] pathNodes = new PathNode[length];
        float3 startPoint = (-0.5f) * scale + transform.position;
        startPoint += offset;

        Collider[] buffer = new Collider[1];
        for (int z = 0; z < gridSize.z; ++z)
        {
            for (int y = 0; y < gridSize.y; ++y)
            {
                for (int x = 0; x < gridSize.x; ++x)
                {
                    float3 position = startPoint + float3(
                              x * m_distanceBetweenNodes.x
                            , y * m_distanceBetweenNodes.y
                            , z * m_distanceBetweenNodes.z);

                    bool isWalkable = 0 == Physics.OverlapSphereNonAlloc(position, m_collisionRadius, buffer, m_layerMask, QueryTriggerInteraction.Ignore);

                    PathNode node = new(position, int3(x, y, z), isWalkable);
                    node.index = x + y * gridSize.x + z * gridSize.x * gridSize.y;
                    pathNodes[node.index] = node;
                }
            }
        }

        //Save data

        NodePathData data = new();
        data.gridSize = gridSize;
        data.pathNodeArray = pathNodes;
        data.gridLength = length;
        data.startPoint = startPoint;

        //if (!File.Exists(Application.persistentDataPath + "/LevelsData/TestLevel.dat"))
        //   File.Create(Application.persistentDataPath + "/LevelsData/TestLevel.dat").Dispose();

        File.WriteAllText(Application.persistentDataPath + "/TestLevel.dat", JsonUtility.ToJson(data));

        Debug.Log("Node path created!");
        Start();
    }

    private void Start()
    {

#if UNITY_EDITOR
        m_camera = UnityEditor.SceneView.lastActiveSceneView.camera.transform;
#endif

        if (m_pathNodeArray.IsCreated) m_pathNodeArray.Dispose();
        m_gridLength = 0;
        m_gridSize = int3(0, 0, 0);

        if (File.Exists(Application.persistentDataPath + "/TestLevel.dat"))
        {
            NodePathData data = JsonUtility.FromJson<NodePathData>(File.ReadAllText(Application.persistentDataPath + "/TestLevel.dat"));
            m_gridSize = data.gridSize;
            m_gridLength = data.gridLength;
            m_startPoint = data.startPoint;
            PathNode[] pathNodes = data.pathNodeArray;

            m_pathNodeArray = new(m_gridLength, Allocator.Persistent);

            for (int i = 0; i < m_gridLength; ++i)
            {
                m_pathNodeArray[i] = pathNodes[i];
            }
        }

        if (m_neighbourOffsetArray.IsCreated == false)
            m_neighbourOffsetArray = new(NEIGHBOUR_OFFSETS, Allocator.Persistent);

        if (m_start && m_end)
            FindPath(m_start.position, m_end.position);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        if (m_pathNodeArray.IsCreated)
        {
            Color col;

            for (int i = 0; i < m_gridLength; ++i)
            {
                float3 dirToCamera = m_camera.position;
                dirToCamera -= m_pathNodeArray[i].position;

                if (lengthsq(dirToCamera) <= m_visualiseDrawDistanceSquared)
                {
                    col = Color.red;
                    if (m_pathNodeArray[i].isWalkable) col = Color.green;
                    Gizmos.color = col;

                    if (m_pathNodeArray[i].blue) Gizmos.color = Color.yellow;

                    if (m_visualiseCollisionRadius)
                        Gizmos.DrawWireSphere(m_pathNodeArray[i].position, m_collisionRadius);

                    if (m_visualizePoints)
                        Gizmos.DrawSphere(m_pathNodeArray[i].position, m_pathNodeArray[i].blue ? 0.5f : 0.1f);
                }
            }
        }
    }
#endif //UNITY_EDITOR

    private void FindPath(float3 start, float3 end)
    {
        for (int i = 0; i < m_gridLength; ++i)
        {
            var node = m_pathNodeArray[i];
            node.cameFromNodeIndex = -1;
            node.gCost = int.MaxValue;
            node.hCost = CalculateDistanceCost(node.position, end);
            node.CalculateFCost();
        }

        PathNode startNode = m_pathNodeArray[CalculateIndex(start)];
        startNode.hCost = CalculateDistanceCost(startNode.position, end);
        startNode.gCost = 0;
        startNode.blue = true;
        startNode.CalculateFCost();
        startNode.cameFromNodeIndex = -1;
        m_pathNodeArray[startNode.index] = startNode;

        int endNodeIndex = CalculateIndex(end);
        PathNode endNode = m_pathNodeArray[endNodeIndex];
        endNode.cameFromNodeIndex = -1;
        endNode.blue = true;
        m_pathNodeArray[endNodeIndex] = endNode;

        NativeList<int> openList = new(Allocator.Temp);
        NativeList<int> closedList = new(Allocator.Temp);

        openList.Add(startNode.index);

        while (openList.Length > 0)
        {
            int currentNodeIndex = GetLowestFNodeIndex(openList);
            PathNode currentNode = m_pathNodeArray[currentNodeIndex];

            if (currentNodeIndex == endNodeIndex)
            {
                //reached destination
                break;
            }

            for (int i = 0; i < openList.Length; ++i)
            {
                if (openList[i] == currentNodeIndex)
                {
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }

            closedList.Add(currentNodeIndex);

            for (int i = 0; i < m_neighbourOffsetArray.Length; ++i)
            {
                int3 indexPosition = currentNode.indexPosition + m_neighbourOffsetArray[i];
                if (!IsInBounds(indexPosition)) continue; //node not valid

                int neighbourIndex = indexPosition.x + indexPosition.y * m_gridSize.x + indexPosition.z * m_gridSize.x * m_gridSize.y;
                PathNode neighbourNode = m_pathNodeArray[neighbourIndex];
                if (closedList.Contains(neighbourIndex) || !neighbourNode.isWalkable) continue; //already visited node or it isn't walkable

                float tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode.position, neighbourNode.position);
                if (tentativeGCost < neighbourNode.gCost)
                {
                    neighbourNode.cameFromNodeIndex = currentNodeIndex;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.CalculateFCost();
                    m_pathNodeArray[neighbourIndex] = neighbourNode;

                    if (!openList.Contains(neighbourIndex))
                    {
                        openList.Add(neighbourIndex);
                    }
                }
            }
        }

        NativeList<float3> path = CalculatePath(m_pathNodeArray[endNodeIndex]);
        if (path.Length > 0)
        {
            Debug.Log("Found path!");
            for (int i = 0; i < path.Length - 1; ++i)
            {
                Debug.DrawRay(path[i], path[i] - path[i + 1], Color.red, 100000);
            }
        }
        else
        {
            Debug.Log("Doodoo fart, no path...");
        }

        openList.Dispose();
        path.Dispose();
        closedList.Dispose();

    }

    private NativeList<float3> CalculatePath(PathNode endNode)
    {
        NativeList<float3> path = new(Allocator.Temp);
        if (endNode.cameFromNodeIndex != -1) //found path
        {
            path.Add(endNode.position);
            while (endNode.cameFromNodeIndex != -1)
            {
                PathNode pathNode = m_pathNodeArray[endNode.cameFromNodeIndex];
                path.Add(endNode.cameFromNodeIndex);
                endNode = pathNode;
            }
        }

        return path;
    }

    private float CalculateDistanceCost(float3 a, float3 b)
    {
        return lengthsq(a - b);
    }

    private bool IsInBounds(int3 indexPosition)
    {
        return indexPosition.x >= 0 && indexPosition.x < m_gridSize.x
            && indexPosition.y >= 0 && indexPosition.y < m_gridSize.y
            && indexPosition.z >= 0 && indexPosition.z < m_gridSize.z;
    }

    private void OnDestroy()
    {
        if (m_pathNodeArray.IsCreated) m_pathNodeArray.Dispose();
        if (m_neighbourOffsetArray.IsCreated) m_neighbourOffsetArray.Dispose();
    }

    private int CalculateIndex(float3 position)
    {
        position -= m_startPoint;
        int x = (int)(position.x / m_distanceBetweenNodes.x);
        int y = (int)(position.y / m_distanceBetweenNodes.y);
        int z = (int)(position.z / m_distanceBetweenNodes.z);

        //make sure the value is in bounds
        x = clamp(x, 0, m_gridSize.x - 1);
        y = clamp(y, 0, m_gridSize.y - 1);
        z = clamp(z, 0, m_gridSize.z - 1);

        return x + y * m_gridSize.x + z * m_gridSize.x * m_gridSize.y;
    }

    private int GetLowestFNodeIndex(NativeList<int> openList)
    {
        PathNode lowestCostPathNode = m_pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; ++i)
        {
            PathNode testPathNode = m_pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost)
            {
                lowestCostPathNode = testPathNode;
            }
        }

        return lowestCostPathNode.index;
    }

    [System.Serializable]
    private struct PathNode
    {
        public PathNode(float3 position, int3 indexPosition, bool isWalkable = true)
        {
            this.position = position;
            index = 0;
            gCost = float.MaxValue;
            hCost = 0;
            fCost = gCost + hCost;
            this.isWalkable = isWalkable;
            cameFromNodeIndex = -1;
            this.indexPosition = indexPosition;
            blue = false;
        }

        public PathNode(float3 position, float3 endPoint, int3 indexPosition, bool isWalkable = true)
        {
            this.position = position;
            index = 0;
            gCost = float.MaxValue;
            hCost = lengthsq(position - endPoint);
            fCost = gCost + hCost;
            this.isWalkable = true;
            cameFromNodeIndex = -1;
            this.indexPosition = indexPosition;
            blue = false;
        }

        public float3 position;
        public int3 indexPosition;
        public int index;

        public float gCost;
        public float hCost; //squared distance from end point
        public float fCost;

        public bool blue;


        public bool isWalkable;

        public int cameFromNodeIndex;

        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }
    }
}


