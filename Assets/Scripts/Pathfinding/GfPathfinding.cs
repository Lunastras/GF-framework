using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;


using static Unity.Mathematics.math;

[ExecuteInEditMode]
public class GfPathfinding : MonoBehaviour
{
    [SerializeField]
    private Vector3 m_distanceBetweenNodes = new(0.5f, 0.5f, 0.5f);

    [SerializeField]
    private float m_collisionRadius = 1.0f;

    [SerializeField]
    private LayerMask m_layerMask = 0;


    [SerializeField]
    private Transform m_start = null;

    [SerializeField]
    private Transform m_end = null;

#if UNITY_EDITOR

    [SerializeField]
    private bool m_visualizePoints = false;

    [SerializeField]
    private bool m_visualiseCollisionRadius = false;

    [SerializeField]
    private float m_visualiseDrawDistanceSquared = 100;

    private Transform m_camera;

    private const float NEGATIVE_HALF = -0.5f;

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
    private float3 m_startPosition;

    NativeArray<PathNode> m_pathNodeArray;
    NativeArray<int3> m_neighbourOffsetArray;

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

        float3 positionOffset = new(
              scale.x % m_distanceBetweenNodes.x
            , scale.y % m_distanceBetweenNodes.y
            , scale.z % m_distanceBetweenNodes.z
        );

        positionOffset *= 0.5f;

        int length = gridSize.x * gridSize.y * gridSize.z;
        PathNode[] pathNodes = new PathNode[length];
        float3 startPosition = NEGATIVE_HALF * scale + transform.position;
        startPosition += positionOffset;

        Collider[] buffer = new Collider[1];
        for (int z = 0; z < gridSize.z; ++z)
        {
            for (int y = 0; y < gridSize.y; ++y)
            {
                for (int x = 0; x < gridSize.x; ++x)
                {
                    float3 position = startPosition + float3(
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
        data.startPoint = startPosition;

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
            m_startPosition = data.startPoint;
            PathNode[] pathNodes = data.pathNodeArray;

            m_pathNodeArray = new(m_gridLength, Allocator.Persistent);

            for (int i = 0; i < m_gridLength; ++i)
            {
                m_pathNodeArray[i] = pathNodes[i];
            }
        }

        if (m_neighbourOffsetArray.IsCreated == false)
            m_neighbourOffsetArray = new(NEIGHBOUR_OFFSETS, Allocator.Persistent);
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
        if (m_pathNodeArray.IsCreated)
        {
            NativeList<float3> path = new(Allocator.TempJob);

            FindPathJob pathJob = new FindPathJob
            {
                startPoint = end,
                endPoint = start,
                pathNodeArray = m_pathNodeArray,
                gridSize = m_gridSize,
                neighbourOffsetArray = m_neighbourOffsetArray,
                gridLength = m_gridLength,
                distanceBetweenNodes = m_distanceBetweenNodes,
                startPosition = m_startPosition,
                path = path
            };

            pathJob.Run();
            //JobHandle handle = pathJob.Schedule();
            //handle.Complete();

            if (path.Length > 0)
            {
                Debug.Log("Found path!");
                for (int i = 0; i < path.Length - 1; ++i)
                {
                    Debug.DrawRay(path[i], path[i + 1] - path[i], Color.green, 0.1f);
                }
            }
            else
            {
                Debug.Log("Doodoo fart, no path...");
            }

            path.Dispose();

        }
    }

    [BurstCompile]
    private struct FindPathJob : IJob
    {
        public float3 startPoint;
        public float3 endPoint;

        public NativeList<float3> path;

        public NativeArray<PathNode> pathNodeArray;

        public NativeArray<int3> neighbourOffsetArray;

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

                node.gCost = int.MaxValue;
                // node.hCost = CalculateDistanceCost(node.position, endPoint);
                // node.CalculateFCost();

                node.blue = false;
                node.openListIndex = -1;
                node.inClosedList = false;
                pathNodeArray[i] = node;
            }

            PathNode startNode = pathNodeArray[CalculateIndex(startPoint)];
            startNode.hCost = CalculateDistanceCost(startNode.position, endPoint);
            startNode.gCost = 0;
            startNode.blue = true;
            startNode.CalculateFCost();
            startNode.openListIndex = 0;
            startNode.cameFromNodeIndex = -1;
            pathNodeArray[startNode.index] = startNode;

            int endNodeIndex = CalculateIndex(endPoint);
            PathNode endNode = pathNodeArray[endNodeIndex];
            endNode.cameFromNodeIndex = -1;
            endNode.blue = true;
            pathNodeArray[endNodeIndex] = endNode;

            NativeList<int> openList = new(16, Allocator.Temp);

            openList.Add(startNode.index);

            while (openList.Length > 0)
            {
                int currentNodeIndex = GetLowestFNodeIndex(openList);
                PathNode currentNode = pathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex) break;//reached destination

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

                currentNode.inClosedList = true;
                pathNodeArray[currentNodeIndex] = currentNode;

                for (int i = 0; i < neighbourOffsetArray.Length; ++i)
                {
                    int3 indexPosition = currentNode.indexPosition + neighbourOffsetArray[i];
                    if (!IsInBounds(indexPosition)) continue; //node not valid

                    int neighbourIndex = indexPosition.x + indexPosition.y * gridSize.x + indexPosition.z * gridSize.x * gridSize.y;
                    PathNode neighbourNode = pathNodeArray[neighbourIndex];
                    if (neighbourNode.inClosedList || !neighbourNode.isWalkable) continue; //already visited node or it isn't walkable

                    float tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode.position, neighbourNode.position);

                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.hCost = CalculateDistanceCost(neighbourNode.position, endPoint);
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.CalculateFCost();

                        if (-1 == neighbourNode.openListIndex)
                        {
                            neighbourNode.openListIndex = openList.Length;
                            openList.Add(neighbourIndex);
                        }

                        pathNodeArray[neighbourIndex] = neighbourNode;
                    }
                }
            }

            path = CalculatePath(pathNodeArray[endNodeIndex]);
            openList.Dispose();
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
            PathNode lowestCostPathNode = pathNodeArray[openList[0]];
            for (int i = 1; i < openList.Length; ++i)
            {
                PathNode testPathNode = pathNodeArray[openList[i]];
                if (testPathNode.fCost < lowestCostPathNode.fCost)
                {
                    lowestCostPathNode = testPathNode;
                }
            }

            return lowestCostPathNode.index;
        }
    }

    private void Update()
    {
        if (m_start && m_end)
        {
            double time = Time.realtimeSinceStartupAsDouble;
            FindPath(m_start.position, m_end.position);

            double elapsed = Time.realtimeSinceStartupAsDouble - time;

            Debug.Log("The time it took for the path to be calculated is: " + elapsed);
        }

    }


    private void OnDestroy()
    {
        if (m_pathNodeArray.IsCreated) m_pathNodeArray.Dispose();
        if (m_neighbourOffsetArray.IsCreated) m_neighbourOffsetArray.Dispose();
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
            inClosedList = false;
            cameFromNodeIndex = -1;
            this.indexPosition = indexPosition;
            blue = false;
            openListIndex = -1;
        }



        public float3 position;
        public int3 indexPosition;
        public int index;

        public float gCost;
        public float hCost; //squared distance from end point
        public float fCost;

        public bool blue;

        public bool inClosedList;
        public int openListIndex;

        public bool isWalkable;

        public int cameFromNodeIndex;

        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }
    }
}



