

using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

namespace GfPathFindingNamespace
{

    //[ExecuteInEditMode]
    public class GfPathfinding : MonoBehaviour
    {
        [SerializeField]
        private Vector3 m_distanceBetweenNodes = new(0.5f, 0.5f, 0.5f);

        [SerializeField]
        private CollisionType m_collisionType = CollisionType.BOX;

        [SerializeField]
        private float m_collisionRadius = 1.0f;

        [SerializeField]
        private LayerMask m_layerMask = 0;


        /*
        [SerializeField]
        private Transform m_start = null;

        [SerializeField]
        private Transform m_end = null;*/

#if UNITY_EDITOR

        [SerializeField]
        private bool m_visualizePoints = false;

        [SerializeField]
        private bool m_visualiseCollisionRadius = false;


#endif //UNITY_EDITOR

        [System.Serializable]
        public struct NodePathSaveData
        {
            public int3 gridSize;
            public float3 startPoint;
            public int gridLength;
            public float3 scale;

            public PathNode[] pathNodeArray;

            public CollisionType collisionType;
        }

        public enum CollisionType
        {
            SPHERE,
            BOX
        }

        private int3 m_gridSize;
        private int m_gridLength = 0;
        private float3 m_startPoint;

        private float3 m_scale;

        private float3 m_invDistanceBetweenNodes = new(0.5f, 0.5f, 0.5f);

        private NativeArray<PathNode> m_pathNodes;

        private Transform m_transform;

        private static NativeArray<int3> NeighboursOffsetArray;

        private static NativeArray<int3> AllNeighboursOffsetArray;


        private static readonly int3[] NEIGHBOUR_OFFSETS =
        {
      //new(1, 0, 1)
     new(1, 0, 0)
    //, new(1, 0, -1)

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

    //, new(-1, 0, 1)
    , new(-1, 0, 0)
    //, new(-1, 0, -1)

    //, new(-1, -1, 1)
    //, new(-1, -1, 0)
    //, new(-1, -1, -1)
    };

        private static readonly int3[] ALL_NEIGHBOUR_OFFSETS =
        {
      new(1, 0, 1)
    , new(1, 0, 0)
    , new(1, 0, -1)

    , new(1, 1, 1)
    , new(1, 1, 0)
    , new(1, 1, -1)


    , new(1, -1, 1)
    , new(1, -1, 0)
    , new(1, -1, -1)

    //////////////////

    , new(0, 1, 1)
    , new(0, 1, 0)
    , new(0, 1, -1)

    , new(0, 0, 1)
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
    , new(-1, -1, -1)
    };

        public void SetNodePathData(NodePathSaveData data)
        {
            if (m_pathNodes.IsCreated) m_pathNodes.Dispose();
            m_gridSize = data.gridSize;
            m_gridLength = data.gridLength;
            m_startPoint = data.startPoint;
            m_scale = data.scale;
            var pathNodeArray = data.pathNodeArray;
            Debug.Log("The nodepath length is: " + m_gridLength);
            m_pathNodes = new(pathNodeArray, Allocator.Persistent);

            m_invDistanceBetweenNodes.x = 1.0f / m_distanceBetweenNodes.x;
            m_invDistanceBetweenNodes.y = 1.0f / m_distanceBetweenNodes.y;
            m_invDistanceBetweenNodes.z = 1.0f / m_distanceBetweenNodes.z;
        }

        public NodePathSaveData GenerateNodePathData()
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

                        bool isWalkable;
                        if (CollisionType.SPHERE == m_collisionType)
                            isWalkable = 0 == Physics.OverlapSphereNonAlloc(position, m_collisionRadius, buffer, m_layerMask, QueryTriggerInteraction.Ignore);
                        else
                            isWalkable = 0 == Physics.OverlapBoxNonAlloc(position, new Vector3(m_collisionRadius, m_collisionRadius, m_collisionRadius), buffer, Quaternion.identity, m_layerMask, QueryTriggerInteraction.Ignore);

                        PathNode node = new PathNode
                        {
                            position = position,
                            positionIndex = int3(x, y, z),
                            isWalkable = isWalkable
                        };

                        int index = x + y * gridSize.x + z * gridSize.x * gridSize.y;
                        pathNodes[index] = node;
                    }
                }
            }

            NodePathSaveData data = new();
            data.gridSize = gridSize;
            data.pathNodeArray = pathNodes;
            data.gridLength = length;
            data.collisionType = m_collisionType;
            data.startPoint = startPoint;
            data.scale = scale;

            Debug.Log("Node path created!");

            return data;
        }

        public void GenerateNodePath()
        {
            NodePathSaveData data = GenerateNodePathData();
            LevelManager.SetNodePathData(this, data);
            SetNodePathData(data);
            Start();
        }

        private void Start()
        {
            m_transform = transform;

            if (NeighboursOffsetArray.IsCreated == false)
                NeighboursOffsetArray = new(NEIGHBOUR_OFFSETS, Allocator.Persistent);


            if (AllNeighboursOffsetArray.IsCreated == false)
                AllNeighboursOffsetArray = new(ALL_NEIGHBOUR_OFFSETS, Allocator.Persistent);

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
                    col = Color.red;
                    if (m_pathNodes[i].isWalkable) col = Color.green;
                    Gizmos.color = col;

                    if (m_visualiseCollisionRadius)
                        if (CollisionType.SPHERE == m_collisionType)
                            Gizmos.DrawWireSphere(m_pathNodes[i].position, m_collisionRadius);
                        else
                        {
                            float size = m_collisionRadius * 2.0f;
                            Gizmos.DrawWireCube(m_pathNodes[i].position, new Vector3(size, size, size));
                        }

                    if (m_visualizePoints)
                        Gizmos.DrawSphere(m_pathNodes[i].position, 0.1f);
                }
            }
        }
#endif //UNITY_EDITOR

        public bool GetPathfindingJob(float3 start, float3 end, NativeList<float3> path, out JobHandle handle)
        {
            bool hasJob = false;
            if (m_pathNodes.IsCreated && m_pathNodes.Length > 0)
            {
                hasJob = true;

                FindPathJob pathJob = new FindPathJob
                {
                    startPoint = start,
                    endPoint = end,
                    gridSize = m_gridSize,
                    pathNodeArray = m_pathNodes.AsReadOnly(),
                    neighboursOffsetArray = NeighboursOffsetArray.AsReadOnly(),
                    allNeighboursOffsetArray = AllNeighboursOffsetArray.AsReadOnly(),
                    gridLength = m_gridLength,
                    invDistanceBetweenNodes = m_invDistanceBetweenNodes,
                    boundsStart = m_startPoint,
                    path = path,
                    scale = m_scale

                };

                handle = pathJob.Schedule();
            }
            else
            {
                handle = default;
            }

            return hasJob;
        }

        [BurstCompile]
        private struct FindPathJob : IJob
        {
            public float3 startPoint;
            public float3 endPoint;

            public NativeList<float3> path;

            public NativeArray<PathNode>.ReadOnly pathNodeArray;

            public NativeArray<int3>.ReadOnly neighboursOffsetArray;

            public NativeArray<int3>.ReadOnly allNeighboursOffsetArray;

            public int3 gridSize;

            public int gridLength;

            public float3 invDistanceBetweenNodes;

            public float3 boundsStart;

            public float3 scale;

            public void Execute()
            {
                NativeArray<PathNodeData> nodesData = new(gridLength, Allocator.Temp);
                NativeList<int> openList = new(32, Allocator.Temp);
                int zOffset = gridSize.x * gridSize.y;

                int endNodeIndex = CalculateSafeIndex(endPoint, zOffset);
                endPoint = pathNodeArray[endNodeIndex].position;
                PathNodeData endNode = new PathNodeData
                {
                    gCost = float.MaxValue,
                    openListIndex = -1,
                    cameFromNodeIndex = -1,
                    initialised = true,
                };

                nodesData[endNodeIndex] = endNode;

                bool endValid = pathNodeArray[endNodeIndex].isWalkable;

                endValid = endValid && (PositionInBounds(startPoint) || PositionInBounds(endPoint)); //check if at least one of the positions is in bounds

                int startNodeIndex = CalculateSafeIndex(startPoint, zOffset);
                PathNodeData startNode = new PathNodeData
                {
                    gCost = 0,
                    openListIndex = 0,
                    initialised = true,
                    cameFromNodeIndex = -1,
                    fCost = CalculateDistanceCost(pathNodeArray[startNodeIndex].position, endPoint)
                };

                nodesData[startNodeIndex] = startNode;

                openList.Add(startNodeIndex);

                int currentNodeIndex = startNodeIndex;
                PathNode currentNode;
                PathNodeData currentNodeData;


                while (openList.Length > 0 && currentNodeIndex != endNodeIndex && endValid)
                {
                    currentNodeIndex = GetLowestFNodeIndex(openList, nodesData);
                    currentNode = pathNodeArray[currentNodeIndex];
                    currentNodeData = nodesData[currentNodeIndex];

                    if (-1 != currentNodeData.openListIndex) //is in openlist
                    {
                        int indexToRemove = currentNodeData.openListIndex;
                        openList.RemoveAtSwapBack(indexToRemove);
                        currentNodeData.openListIndex = -1;

                        if (openList.Length != indexToRemove)
                        {
                            int lastNodeIndex = openList[indexToRemove];
                            PathNodeData lastNodeData = nodesData[lastNodeIndex];
                            lastNodeData.openListIndex = indexToRemove;
                            nodesData[lastNodeIndex] = lastNodeData;
                        }
                    }

                    currentNodeData.inClosedList = true; //visited node, not walkable anymore
                    nodesData[currentNodeIndex] = currentNodeData;

                    for (int i = 0; i < neighboursOffsetArray.Length; ++i)
                    {
                        int3 neighbourPositionIndex = currentNode.positionIndex + neighboursOffsetArray[i];
                        if (!Index3InBounds(neighbourPositionIndex)) continue; //node not valid

                        int neighbourIndex = neighbourPositionIndex.x + neighbourPositionIndex.y * gridSize.x + neighbourPositionIndex.z * zOffset;
                        PathNode neighbourNode = pathNodeArray[neighbourIndex];
                        PathNodeData neighbourNodeData = nodesData[neighbourIndex];

                        if (neighbourNodeData.inClosedList || !neighbourNode.isWalkable) continue; //already visited node or it isn't walkable

                        if (!neighbourNodeData.initialised)
                        {
                            neighbourNodeData = new PathNodeData
                            {
                                gCost = float.MaxValue,
                                openListIndex = -1,
                                cameFromNodeIndex = -1,
                                initialised = true,
                            };

                            nodesData[neighbourIndex] = neighbourNodeData;
                        }

                        float tentativeGCost = currentNodeData.gCost + CalculateDistanceCost(currentNode.position, neighbourNode.position);
                        if (tentativeGCost < neighbourNodeData.gCost)
                        {
                            neighbourNodeData.cameFromNodeIndex = currentNodeIndex;
                            neighbourNodeData.gCost = tentativeGCost;
                            neighbourNodeData.fCost = tentativeGCost + CalculateDistanceCost(neighbourNode.position, endPoint);

                            if (-1 == neighbourNodeData.openListIndex)
                            {
                                neighbourNodeData.openListIndex = openList.Length;
                                openList.Add(neighbourIndex);
                            }

                            nodesData[neighbourIndex] = neighbourNodeData;
                        }
                    }
                }

                path = CalculatePath(nodesData[endNodeIndex], pathNodeArray[endNodeIndex], nodesData);
                nodesData.Dispose();
                openList.Dispose();
            }

            private NativeList<float3> CalculatePath(PathNodeData endNodeData, PathNode endNode, NativeArray<PathNodeData> nodesData)
            {
                if (endNodeData.cameFromNodeIndex != -1) //found path
                {
                    while (endNodeData.cameFromNodeIndex != -1)
                    {
                        PathNode pathNode = pathNodeArray[endNodeData.cameFromNodeIndex];
                        PathNodeData pathNodeData = nodesData[endNodeData.cameFromNodeIndex];

                        path.Add(endNode.position);
                        endNode = pathNode;
                        endNodeData = pathNodeData;
                    }

                    path.Add(endNode.position); //add first node
                }

                return path;
            }

            private float CalculateDistanceCost(float3 a, float3 b)
            {
                return lengthsq(a - b);
            }

            private bool Index3InBounds(int3 indexPosition)
            {
                return indexPosition.x >= 0 && indexPosition.x < gridSize.x
                    && indexPosition.y >= 0 && indexPosition.y < gridSize.y
                    && indexPosition.z >= 0 && indexPosition.z < gridSize.z;
            }

            private bool PositionInBounds(float3 position)
            {
                position -= boundsStart;
                return position.x >= 0 && position.x < scale.x
                    && position.y >= 0 && position.y < scale.y
                    && position.z >= 0 && position.z < scale.z;
            }

            private int CalculateIndex(float3 position)
            {
                position -= boundsStart;
                int x = (int)round(position.x * invDistanceBetweenNodes.x);
                int y = (int)round(position.y * invDistanceBetweenNodes.y);
                int z = (int)round(position.z * invDistanceBetweenNodes.z);

                //make sure the value is in bounds
                x = clamp(x, 0, gridSize.x - 1);
                y = clamp(y, 0, gridSize.y - 1);
                z = clamp(z, 0, gridSize.z - 1);

                return x + y * gridSize.x + z * gridSize.x * gridSize.y;
            }

            public int CalculateSafeIndex(float3 position, int zOffset)
            {
                int index = CalculateIndex(position);
                int3 positionIndex = pathNodeArray[CalculateIndex(position)].positionIndex;

                int currentIndex = index;
                PathNode node = pathNodeArray[currentIndex];
                int3 neighbourPositionIndex;
                for (int i = 0; i < allNeighboursOffsetArray.Length && !node.isWalkable; ++i)
                {
                    neighbourPositionIndex = positionIndex + allNeighboursOffsetArray[i];
                    if (Index3InBounds(neighbourPositionIndex))
                    {
                        currentIndex = neighbourPositionIndex.x + neighbourPositionIndex.y * gridSize.x + neighbourPositionIndex.z * zOffset; ;
                        node = pathNodeArray[currentIndex];
                    }
                }

                return currentIndex;
            }

            private int GetLowestFNodeIndex(NativeList<int> openList, NativeArray<PathNodeData> nodeDatas)
            {
                int lowestFCostIndex = openList[0];
                float lowestFCost = nodeDatas[lowestFCostIndex].fCost;
                for (int i = 1; i < openList.Length; ++i)
                {
                    PathNodeData testPathNode = nodeDatas[openList[i]];
                    if (testPathNode.fCost < lowestFCost)
                    {
                        lowestFCost = testPathNode.fCost;
                        lowestFCostIndex = openList[i];
                    }
                }

                return lowestFCostIndex;
            }
        }

        /*

        const int TEST_COUNT = 128;
        List<NativeList<float3>> m_paths = new(TEST_COUNT);

        private void Update()
        {
            if (m_start && m_end)
            {
                double time = Time.realtimeSinceStartupAsDouble;
                NativeArray<JobHandle> handles = new(TEST_COUNT, Allocator.Temp);

                for (int i = 0; i < TEST_COUNT; ++i)
                {
                    m_paths.Add(new(Allocator.TempJob));
                    GetPathfindingJob(m_start.position, m_end.position, m_paths[i], out JobHandle handle);
                    handles[i] = handle;
                }

                JobHandle.CompleteAll(handles);

                double elapsed = Time.realtimeSinceStartupAsDouble - time;

                var path = m_paths[0];
                if (path.Length > 0)
                {
                    Debug.Log("yeeee found it");
                    for (int i = 0; i < path.Length - 1; ++i)
                    {
                        Color colour = i == (path.Length - 2) ? Color.red : Color.green;
                        Debug.DrawRay(path[i], path[i + 1] - path[i], colour, 0.1f);
                    }
                }
                else
                {
                    Debug.Log("No path, baddyy");
                }

                for (int i = 0; i < TEST_COUNT; ++i)
                    m_paths[i].Dispose();

                m_paths.Clear();

                Debug.Log("The time it took for the path to be calculated is: " + elapsed);
            }

        }
        */

        private void OnDestroy()
        {
            if (m_pathNodes.IsCreated) m_pathNodes.Dispose();
            if (NeighboursOffsetArray.IsCreated) NeighboursOffsetArray.Dispose();
            if (AllNeighboursOffsetArray.IsCreated) AllNeighboursOffsetArray.Dispose();
        }

        [System.Serializable]
        public struct PathNode
        {
            public float3 position;

            public int3 positionIndex;
            public bool isWalkable;
        }

        public struct PathNodeData
        {
            public PathNodeData(float gCost = float.MaxValue, float fCost = 0, int cameFromNodeIndex = -1, int openListIndex = -1)
            {
                this.gCost = gCost;
                this.fCost = fCost;
                this.cameFromNodeIndex = cameFromNodeIndex;
                this.openListIndex = openListIndex;
                inClosedList = false;
                initialised = false;
            }

            public bool inClosedList;
            public float gCost;
            public float fCost;

            public int openListIndex;

            public int cameFromNodeIndex;

            public bool initialised;
        }
    }



}
