using System;
using MakeStack.Manager;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MakeStack.Map
{
    public struct NativeStack<T> where T : unmanaged
    {
        public NativeStack(Allocator allocator)
        {
            _list = new NativeList<T>(allocator);
        }

        public void Push(T item) => _list.Add(item);

        public T Pop()
        {
            var item = _list[_list.Length - 1];
            _list.RemoveAt(_list.Length - 1);
            return item;
        }
        public T Peek() => _list[_list.Length - 1];
        public int Count => _list.Length;
        public void Dispose() => _list.Dispose();

        private NativeList<T> _list;
    }

    public class MapGenerator : MonoBehaviour
    {
        [Header("Maze Settings")]
        public int width = 20;
        public int height = 20;
        public float cellSize = 2f;
        public int randomSeed = 0;
        public int totalStages = 3;

        [Header("Prefabs")]
        public GameObject floorPrefab;
        public GameObject wallPrefab;
        public GameObject winPrefab;
        public GameObject linePrefab;
        public GameObject brickPrefab;
        public GameObject winPosPrefab;
        public GameObject unbrick;

        [Header("Settings")]
        [SerializeField] private float winPrefabHeight;
        [SerializeField] private float runwayHeight;

        public int BrickNeedToPass { get; set; }
        private NativeArray<int> maze;
        private List<GameObject> spawnedObjects = new List<GameObject>();
        private Dictionary<string, Transform> prefabParents = new();

        [ContextMenu("Generate All Stages")]
        public void GenerateFromInspector() => GenerateAllStages();

        [ContextMenu("Clear Map")]
        public void ClearFromInspector() => ClearMap();

        void Start()
        {
            GenerateAllStages();
        }

        public void GenerateAllStages()
        {
            ClearMap();
            
            PoolManager.Instance.CreatePool(floorPrefab, 50, 500);
            PoolManager.Instance.CreatePool(wallPrefab, 50, 500);
            PoolManager.Instance.CreatePool(winPrefab, 10, 50);
            PoolManager.Instance.CreatePool(linePrefab, 20, 100);
            PoolManager.Instance.CreatePool(brickPrefab, 50, 500);
            PoolManager.Instance.CreatePool(winPosPrefab, 1, 5);
            PoolManager.Instance.CreatePool(unbrick, 20, 100);

            Vector3 offset = Vector3.zero;

            for (int stage = 1; stage <= totalStages; stage++)
            {
                GenerateStage(stage, offset, out int blocksInStage, out GameObject endObjBorder, out GameObject startObjBorder);
                
                // tag start / end
                if (startObjBorder != null) startObjBorder.tag = "StartPoint";
                if (endObjBorder != null) endObjBorder.tag = "EndPoint";

                // place fence immediately after stage
                float fenceZ = offset.z + height * cellSize;
                GenerateFence(new Vector3(offset.x, offset.y, fenceZ));

                // next stage starts right after fence (fence occupies 1 row)
                if (stage < totalStages)
                {
                    offset.z = fenceZ + cellSize;
                }
                else
                {
                    // stage cuối: runway begins right after fence
                    float runwayStartZ = fenceZ + cellSize;
                    GenerateRunwayAtPosition(runwayStartZ, endObjBorder, blocksInStage);
                }
                
                BrickNeedToPass = (int)Math.Round(blocksInStage * 0.4);

                Debug.Log($"[Stage {stage}] Generated with {blocksInStage} floor blocks");
            }
        }

        /// <summary>
        /// Generate một stage ở vị trí offset (origin), trả về:
        /// blockCount, endObjBorder (topmost cell object), startObjBorder (bottommost cell object).
        /// Start đặt ở hàng 0, End đặt ở hàng height-1.
        /// </summary>
        private void GenerateStage(int stageIndex, Vector3 offset, out int blockCount, out GameObject endObjBorder, out GameObject startObjBorder)
        {
            blockCount = 0;
            endObjBorder = null;
            startObjBorder = null;

            uint seed = (randomSeed == 0) ? (uint)UnityEngine.Random.Range(1, int.MaxValue) 
                                          : (uint)(randomSeed + stageIndex * 100);

            maze = new NativeArray<int>(width * height, Allocator.TempJob);

            int midX = (width % 2 == 0) ? width / 2 - 1 : width / 2;
            int startY = (stageIndex == 1) ? 1 : 0;
            int2 startPos = new int2(midX, startY);
            int2 desiredEnd = new int2(midX, height - 1);

            var job = new DFSMazeJob
            {
                width = width,
                height = height,
                maze = maze,
                startPos = startPos,
                endPos = new int2(midX, Mathf.Clamp(height - 2, 0, height - 1)),
                seed = seed
            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            // Đảm bảo top mở
            int topIndex = (height - 1) * width + midX;
            if (topIndex >= 0 && topIndex < maze.Length)
                maze[topIndex] = 1;

            // Đảm bảo start mở
            int startIndex = startY * width + midX;
            if (startIndex >= 0 && startIndex < maze.Length)
                maze[startIndex] = 1;

            // Spawn cell
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 pos = offset + new Vector3(x * cellSize, 0, y * cellSize);
                    GameObject obj = null;

                    if (maze[y * width + x] == 1)
                    {
                        obj = SpawnFromPool(floorPrefab, pos, Quaternion.identity);
                        blockCount++;
                    }
                    else
                    {
                        obj = SpawnFromPool(wallPrefab, pos + Vector3.up * 0.1f, Quaternion.identity);
                    }

                    if (x == midX && y == height - 1 && obj != null) endObjBorder = obj;
                    if (x == midX && y == startY && obj != null) startObjBorder = obj;
                }
            }

            maze.Dispose();
        }


        private void GenerateFence(Vector3 offset)
        {
            int fenceWidth = width + 2;

            for (int i = 0; i < fenceWidth; i++)
            {
                Vector3 pos = offset + new Vector3((i - 1) * cellSize, 0, 0);
                GameObject obj = null;
                if (i == 0 || i == fenceWidth - 1)
                {
                    Vector3 posHigh = pos + Vector3.up * winPrefabHeight;
                    obj = SpawnFromPool(winPrefab, posHigh, Quaternion.identity);
                }
                else
                {
                    obj = SpawnFromPool(unbrick, pos, Quaternion.Euler(-90, 0, 0));
                }
            }
        }
        
        /// <summary>
        /// Runway starts at given Z (world), centered at X of endObjBorder.
        /// </summary>
        private void GenerateRunwayAtPosition(float runwayStartZ, GameObject endObjBorder, int length)
        {
            if (endObjBorder == null)
            {
                Debug.LogWarning("[MapGenerator] endObjBorder is null, cannot center runway. Aborting runway.");
                return;
            }

            float centerX = endObjBorder.transform.position.x;

            for (int i = 0; i < length; i++)
            {
                Vector3 pos = new Vector3(centerX, runwayHeight, runwayStartZ + i * cellSize);
                SpawnFromPool(linePrefab, pos, Quaternion.Euler(-90, 0, 0));
                SpawnFromPool(brickPrefab, pos + Vector3.up * 0.1f, Quaternion.identity);
            }

            Vector3 winPos = new Vector3(centerX, runwayHeight, runwayStartZ + length * cellSize);
            SpawnFromPool(winPosPrefab, winPos, Quaternion.identity);
        }

        private GameObject SpawnFromPool(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            GameObject obj = PoolManager.Instance.GetObject(prefab, pos, rot);
            if (obj != null)
            {
                obj.transform.SetParent(GetParentForPrefab(prefab));
                spawnedObjects.Add(obj);
            }
            return obj;
        }

        private Transform GetParentForPrefab(GameObject prefab)
        {
            string key = prefab.name;
            if (!prefabParents.ContainsKey(key))
            {
                GameObject parentObj = new GameObject(key + "_Container");
                prefabParents[key] = parentObj.transform;
            }
            return prefabParents[key];
        }

        public void ClearMap()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                    PoolManager.Instance.ReturnObject(obj);
            }
            spawnedObjects.Clear();
            
            foreach (var kvp in prefabParents)
            {
                if (kvp.Value != null)
                {
                    for (int i = kvp.Value.childCount - 1; i >= 0; i--)
                    {
                        var child = kvp.Value.GetChild(i).gameObject;
                        PoolManager.Instance.ReturnObject(child);
                    }
                }
            }
        }

        [BurstCompile]
        struct DFSMazeJob : IJob
        {
            public uint seed;
            public int width;
            public int height;
            public NativeArray<int> maze;
            public int2 startPos;
            public int2 endPos;

            public void Execute()
            {
                for (int i = 0; i < maze.Length; i++) maze[i] = 0;

                NativeStack<int2> stack = new NativeStack<int2>(Allocator.Temp);
                stack.Push(startPos);
                maze[startPos.y * width + startPos.x] = 1;

                Unity.Mathematics.Random rng = new Unity.Mathematics.Random(seed);

                // allow carving to edges by checking >=0 and <width/height
                while (stack.Count > 0)
                {
                    bool carved = false;
                    int2 current = stack.Peek();

                    int2[] dirs = new int2[]
                    {
                        new int2(0, 2),
                        new int2(2, 0),
                        new int2(0, -2),
                        new int2(-2, 0)
                    };

                    // shuffle
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        int swap = rng.NextInt(0, dirs.Length);
                        (dirs[i], dirs[swap]) = (dirs[swap], dirs[i]);
                    }

                    foreach (var dir in dirs)
                    {
                        int2 next = current + dir;

                        if (next.x >= 0 && next.y >= 0 &&
                            next.x < width && next.y < height)
                        {
                            if (maze[next.y * width + next.x] == 0)
                            {
                                int2 wall = current + dir / 2;
                                // guard wall index
                                if (wall.x >= 0 && wall.y >= 0 && wall.x < width && wall.y < height)
                                    maze[wall.y * width + wall.x] = 1;

                                maze[next.y * width + next.x] = 1;
                                stack.Push(next);
                                carved = true;
                                break;
                            }
                        }
                    }

                    if (!carved) stack.Pop();
                }

                stack.Dispose();
            }
        }
    }
}