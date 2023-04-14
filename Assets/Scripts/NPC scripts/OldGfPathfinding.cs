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
public class OldGfPathfinding : MonoBehaviour
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

    private PathNode[] m_pathNodes;

    NativeArray<int> m_neighbourOffsetArray;

    private static readonly int3[] NEIGHBOUR_OFFSETS =
    {
      new(1, 0, 1)
    , new(1, 0, 0)
    , new(1, 0, -1)

    //, new(1, 1, 1)
    //, new(1, 1, 0)
    //, new(1, 1, -1)


    //, new(1, -1, 1)
    //, new(1, -1, 0)
    //, new(1, -1, -1)

    //////////////////

    //, new(0, 1, 1)
    , new(0, 1, 0)
    //, new(0, 1, -1)

    , new(0, 0, 1)
    , new(0, 0, -1)

    //, new(0, -1, 1)
    , new(0, -1, 0)
    //, new(0, -1, -1)

    //////////////////

    //, new(-1, 1, 1)
    //, new(-1, 1, 0)
    //, new(-1, 1, -1)

    , new(-1, 0, 1)
    , new(-1, 0, 0)
    , new(-1, 0, -1)

    //, new(-1, -1, 1)
    //, new(-1, -1, 0)
    //, new(-1, -1, -1)
    };

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
                    int index = x + y * gridSize.x + z * gridSize.x * gridSize.y;
                    pathNodes[index] = node;
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

        m_gridLength = 0;
        m_gridSize = int3(0, 0, 0);

        if (File.Exists(Application.persistentDataPath + "/TestLevel.dat"))
        {
            NodePathData data = JsonUtility.FromJson<NodePathData>(File.ReadAllText(Application.persistentDataPath + "/TestLevel.dat"));
            m_gridSize = data.gridSize;
            m_gridLength = data.gridLength;
            m_startPoint = data.startPoint;
            m_pathNodes = data.pathNodeArray;
        }

        if (m_neighbourOffsetArray.IsCreated == false)
            m_neighbourOffsetArray = new(NEIGHBOUR_OFFSETS.Length, Allocator.Persistent);

        for (int i = 0; i < NEIGHBOUR_OFFSETS.Length; ++i)
        {
            m_neighbourOffsetArray[i] = NEIGHBOUR_OFFSETS[i].x + NEIGHBOUR_OFFSETS[i].y * m_gridSize.x + NEIGHBOUR_OFFSETS[i].z * m_gridSize.x * m_gridSize.y;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        if (m_pathNodes.Length > 0)
        {
            Color col;

            for (int i = 0; i < m_gridLength; ++i)
            {
                float3 dirToCamera = m_camera.position;
                dirToCamera -= m_pathNodes[i].position;

                if (lengthsq(dirToCamera) <= m_visualiseDrawDistanceSquared)
                {
                    col = Color.red;
                    if (m_pathNodes[i].isWalkable) col = Color.green;
                    Gizmos.color = col;

                    if (m_visualiseCollisionRadius)
                        Gizmos.DrawWireSphere(m_pathNodes[i].position, m_collisionRadius);

                    if (m_visualizePoints)
                        Gizmos.DrawSphere(m_pathNodes[i].position, 0.1f);
                }
            }
        }
    }
#endif //UNITY_EDITOR

    private NativeList<float3> FindPath(float3 start, float3 end)
    {
        NativeList<float3> path = new(Allocator.TempJob);

        if (m_pathNodes.Length > 0)
        {
            NativeArray<PathNode> pathNodeArray = new(m_pathNodes, Allocator.TempJob);

            FindPathJob pathJob = new FindPathJob
            {
                startPoint = start,
                endPoint = end,
                pathNodeArray = pathNodeArray,
                gridSize = m_gridSize,
                neighbourOffsetArray = m_neighbourOffsetArray,
                gridLength = m_gridLength,
                distanceBetweenNodes = m_distanceBetweenNodes,
                startPosition = m_startPoint,
                path = path
            };

            pathJob.Run();
            pathNodeArray.Dispose();
            //JobHandle handle = pathJob.Schedule();
            //handle.Complete();
        }

        return path;
    }

    [BurstCompile]
    private struct FindPathJob : IJob
    {
        public float3 startPoint;
        public float3 endPoint;

        public NativeList<float3> path;

        public NativeArray<PathNode> pathNodeArray;

        public NativeArray<int> neighbourOffsetArray;

        public int3 gridSize;

        public float gridLength;

        public float3 distanceBetweenNodes;

        public float3 startPosition;

        public void Execute()
        {

            for (int i = 0; i < gridLength; ++i)
            {
                var node = pathNodeArray[i];
                node.cameFromNodeIndex = -1;
                node.gCost = float.MaxValue;
                node.openListIndex = -1;
                // node.fCost = node.gCost + CalculateDistanceCost(node.position, endPoint);
                //node.blue = false;
                pathNodeArray[i] = node;
            }

            int endNodeIndex = CalculateIndex(endPoint);
            PathNode endNode = pathNodeArray[endNodeIndex];
            endNode.cameFromNodeIndex = -1;
            endNode.gCost = float.MaxValue;
            endNode.fCost = float.MaxValue;
            endPoint = endNode.position;
            pathNodeArray[endNodeIndex] = endNode;

            NativeList<int> openList = new(Allocator.Temp);

            int startNodeIndex = CalculateIndex(startPoint);
            PathNode startNode = pathNodeArray[CalculateIndex(startPoint)];
            startNode.gCost = 0;
            startNode.openListIndex = 0;
            startNode.fCost = CalculateDistanceCost(startNode.position, endPoint);
            startNode.cameFromNodeIndex = -1;
            pathNodeArray[startNodeIndex] = startNode;

            openList.Add(startNodeIndex);

            while (openList.Length > 0)
            {
                int currentNodeIndex = GetLowestFNodeIndex(openList);
                PathNode currentNode = pathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex) break;

                if (-1 != currentNode.openListIndex)
                {
                    int indexToRemove = currentNode.openListIndex;
                    openList.RemoveAtSwapBack(indexToRemove);
                    currentNode.openListIndex = -1;

                    if (openList.Length != indexToRemove)
                    {
                        int lastNodeIndex = openList[indexToRemove];
                        PathNode lastNode = pathNodeArray[lastNodeIndex];
                        lastNode.openListIndex = indexToRemove;
                        pathNodeArray[lastNodeIndex] = lastNode;
                    }
                }

                currentNode.isWalkable = false; //visited node, not walkable anymore
                pathNodeArray[currentNodeIndex] = currentNode;

                for (int i = 0; i < neighbourOffsetArray.Length; ++i)
                {
                    int neighbourIndex = currentNodeIndex + neighbourOffsetArray[i];
                    if (0 > neighbourIndex || pathNodeArray.Length <= neighbourIndex) continue; //node not valid

                    PathNode neighbourNode = pathNodeArray[neighbourIndex];
                    if (!neighbourNode.isWalkable) continue; //already visited node or it isn't walkable

                    float tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode.position, neighbourNode.position);
                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.fCost = tentativeGCost + CalculateDistanceCost(neighbourNode.position, endPoint);

                        if (-1 == neighbourNode.openListIndex)
                        {
                            neighbourNode.openListIndex = openList.Length;
                            openList.Add(neighbourIndex);
                        }

                        pathNodeArray[neighbourIndex] = neighbourNode;
                    }
                }
            }

            openList.Dispose();
            path = CalculatePath(pathNodeArray[endNodeIndex]);
        }

        private NativeList<float3> CalculatePath(PathNode endNode)
        {
            if (endNode.cameFromNodeIndex != -1) //found path
            {
                path.Add(endNode.position);
                while (endNode.cameFromNodeIndex != -1)
                {
                    PathNode pathNode = pathNodeArray[endNode.cameFromNodeIndex];
                    path.Add(endNode.position);
                    endNode = pathNode;
                }

                path.Add(endNode.position);
            }

            return path;
        }

        private float CalculateDistanceCost(float3 a, float3 b)
        {
            return lengthsq(a - b);
        }

        private bool IsInBounds(int3 indexPosition)
        {
            return indexPosition.x >= 0 && indexPosition.x < gridSize.x
                && indexPosition.y >= 0 && indexPosition.y < gridSize.y
                && indexPosition.z >= 0 && indexPosition.z < gridSize.z;
        }

        private int CalculateIndex(float3 position)
        {
            position -= startPosition;
            int x = (int)round(position.x / distanceBetweenNodes.x);
            int y = (int)round(position.y / distanceBetweenNodes.y);
            int z = (int)round(position.z / distanceBetweenNodes.z);

            //make sure the value is in bounds
            x = clamp(x, 0, gridSize.x - 1);
            y = clamp(y, 0, gridSize.y - 1);
            z = clamp(z, 0, gridSize.z - 1);

            return x + y * gridSize.x + z * gridSize.x * gridSize.y;
        }

        private int GetLowestFNodeIndex(NativeList<int> openList)
        {
            int lowestFCostIndex = openList[0];
            float lowestFCost = pathNodeArray[lowestFCostIndex].fCost;
            for (int i = 1; i < openList.Length; ++i)
            {
                PathNode testPathNode = pathNodeArray[openList[i]];
                if (testPathNode.fCost < lowestFCost)
                {
                    lowestFCost = testPathNode.fCost;
                    lowestFCostIndex = openList[i];
                }
            }

            return lowestFCostIndex;
        }
    }

    private void Update()
    {
        if (m_start && m_end)
        {
            double time = Time.realtimeSinceStartupAsDouble;
            var path = FindPath(m_start.position, m_end.position);

            double elapsed = Time.realtimeSinceStartupAsDouble - time;

            if (path.Length > 0)
            {
                Debug.Log("yeeee found it");
                for (int i = 0; i < path.Length - 1; ++i)
                {
                    Debug.DrawRay(path[i], path[i + 1] - path[i], Color.green, 0.1f);
                }
            }
            else
            {
                Debug.Log("No path, baddyy");
            }

            path.Dispose();

            Debug.Log("The time it took for the path to be calculated is: " + elapsed);
        }

    }

    private void OnDestroy()
    {
        if (m_neighbourOffsetArray.IsCreated) m_neighbourOffsetArray.Dispose();
    }

    [System.Serializable]
    private struct PathNode
    {
        public PathNode(float3 position, int3 indexPosition, bool isWalkable = true)
        {
            this.position = position;
            gCost = float.MaxValue;
            fCost = gCost;
            this.isWalkable = isWalkable;
            cameFromNodeIndex = -1;
            openListIndex = -1;
        }

        public float3 position;

        public float gCost;
        public float fCost;

        public int openListIndex;

        public bool isWalkable;

        public int cameFromNodeIndex;
    }

    private struct PathNodeData
    {
        public PathNodeData(float gCost = float.MaxValue, float fCost = float.MaxValue, int cameFromNodeIndex = -1, int openListIndex = -1)
        {
            this.gCost = gCost;
            this.fCost = fCost;
            this.cameFromNodeIndex = cameFromNodeIndex;
            this.openListIndex = openListIndex;
        }

        public float gCost;
        public float fCost;

        public int openListIndex;

        public int cameFromNodeIndex;
    }
}


