using System;
using MakeStack.Manager;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MakeStack.Map
{
    public struct NativeStack<T> where T : unmanaged
    {
        private NativeList<T> _list;
        
        public NativeStack(Allocator allocator)
        {
            _list = new NativeList<T>(allocator);
        }

        public void Push(T item) => _list.Add(item);

        public T Pop()
        {
            var item = _list[^1]; // _list.Length - 1
            _list.RemoveAt(_list.Length - 1);
            return item;
        }

        public T Peek() => _list[^1];
        public int Count => _list.Length;
        public void Dispose() => _list.Dispose();
    }

    public class MapGenerator : MonoBehaviour
    {
        public readonly Dictionary<int, (GameObject start, GameObject end)> StagePoints = new();
        
        private readonly Dictionary<string, Transform> _prefabParents = new();
        private readonly List<GameObject> _spawnedObjects = new();
        
        [Header("Maze Settings")]
        [SerializeField] private int width = 20;
        [SerializeField] private int height = 20;
        [SerializeField] private int randomSeed = 0;
        [SerializeField] private int totalStages = 3;
        [SerializeField] private float cellSize = 2f;

        [Header("Prefabs")]
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject winPrefab;
        [SerializeField] private GameObject linePrefab;
        [SerializeField] private GameObject brickPrefab;
        [SerializeField] private GameObject winPosPrefab;
        [SerializeField] private GameObject unbrick;

        [Header("Settings")]
        [SerializeField] private float winPrefabHeight;
        [SerializeField] private float runwayHeight;

        private NativeArray<int> _maze;
        
        public static int BrickNeedToPass { get; private set; }

        [ContextMenu("Generate All Stages")]
        public void GenerateFromInspector() => GenerateAllStages();

        [ContextMenu("Clear Map")]
        public void ClearFromInspector() => ClearMap();

        private void Start()
        {
            GenerateAllStages();
        }

        private void GenerateAllStages()
        {
            ClearMap();
            
            PoolManager.Instance.CreatePool(floorPrefab, 50, 500);
            PoolManager.Instance.CreatePool(wallPrefab, 50, 500);
            PoolManager.Instance.CreatePool(winPrefab, 10, 50);
            PoolManager.Instance.CreatePool(linePrefab, 20, 100);
            PoolManager.Instance.CreatePool(brickPrefab, 50, 500);
            PoolManager.Instance.CreatePool(winPosPrefab, 1, 5);
            PoolManager.Instance.CreatePool(unbrick, 20, 100);

            var offset = Vector3.zero;

            for (var stage = 1; stage <= totalStages; stage++)
            {
                GenerateStage(stage, offset, out int blocksInStage, out GameObject endObj, out GameObject startObj);
                
                if (startObj != null) startObj.tag = "StartPoint";
                if (endObj != null) endObj.tag = "EndPoint";
                
                StagePoints[stage] = (startObj, endObj);
                
                var fenceZ = offset.z + height * cellSize;
                
                GenerateFence(new Vector3(offset.x, offset.y, fenceZ));
                
                if (stage < totalStages)
                {
                    offset.z = fenceZ + cellSize;
                }
                else
                {
                    var runwayStartZ = fenceZ + cellSize;
                    GenerateRunwayAtPosition(runwayStartZ, endObj, 35);
                }
                
                BrickNeedToPass = (int)Math.Round(blocksInStage * 0.3);
            }
        }
        
        private void GenerateStage(int stageIndex, Vector3 offset, out int blockCount, out GameObject endObj, out GameObject startObj)
        {
            blockCount = 0;
            endObj = null;
            startObj = null;

            var seed = (randomSeed == 0) 
                ? (uint)UnityEngine.Random.Range(1, int.MaxValue) 
                : (uint)(randomSeed + stageIndex * 100);

            _maze = new NativeArray<int>(width * height, Allocator.TempJob);

            var midX = (width % 2 == 0) ? width / 2 - 1 : width / 2;
            var startY = (stageIndex == 1) ? 1 : 0;
            var startPos = new int2(midX, startY);
            var desiredEnd = new int2(midX, height - 1);

            var job = new DFSMazeJob
            {
                width = width,
                height = height,
                maze = _maze,
                startPos = startPos,
                endPos = new int2(midX, height - 1),
                seed = seed
            };

            var handle = job.Schedule();
            handle.Complete();
            
            var topIndex = (height - 1) * width + midX;
            
            if (topIndex >= 0 && topIndex < _maze.Length) _maze[topIndex] = 1;
            
            var startIndex = startY * width + midX;
            
            if (startIndex >= 0 && startIndex < _maze.Length) _maze[startIndex] = 1;
            
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pos = offset + new Vector3(x * cellSize, 0, y * cellSize);
                    GameObject obj;

                    if (_maze[y * width + x] == 1)
                    {
                        obj = SpawnFromPool(floorPrefab, pos, Quaternion.identity);
                        blockCount++;
                    }
                    else
                    {
                        obj = SpawnFromPool(wallPrefab, pos + Vector3.up * 0.1f, Quaternion.identity);
                    }

                    if (x == midX && y == height - 1 && obj != null) endObj = obj;
                    if (x == midX && y == startY && obj != null) startObj = obj;
                }
            }

            _maze.Dispose();
        }

        private void GenerateFence(Vector3 offset)
        {
            var fenceWidth = width + 2;

            for (var i = 0; i < fenceWidth; i++)
            {
                var pos = offset + new Vector3((i - 1) * cellSize, 0, 0);
                GameObject obj;

                if (i == 0 || i == fenceWidth - 1)
                {
                    var posHigh = pos + Vector3.up * winPrefabHeight;
                    obj = SpawnFromPool(winPrefab, posHigh, Quaternion.identity);
                }
                else
                {
                    obj = SpawnFromPool(unbrick, pos, Quaternion.Euler(-90, 0, 0));
                }
            }
        }
        
        private void GenerateRunwayAtPosition(float runwayStartZ, GameObject endObj, int length)
        {
            if (endObj == null) return;

            var centerX = endObj.transform.position.x;

            for (var i = 0; i < length; i++)
            {
                var pos = new Vector3(centerX, runwayHeight, runwayStartZ + i * cellSize);
                SpawnFromPool(linePrefab, pos, Quaternion.Euler(-90, 0, 0));
                SpawnFromPool(brickPrefab, pos + Vector3.up * 0.1f, Quaternion.Euler(-90, 0, 0));
            }

            var winPos = new Vector3(centerX, runwayHeight - 2.5f, runwayStartZ + length * cellSize);
            SpawnFromPool(winPosPrefab, winPos, Quaternion.identity);
        }

        private GameObject SpawnFromPool(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            var obj = PoolManager.Instance.GetObject(prefab, pos, rot);
            
            if (obj != null)
            {
                obj.transform.SetParent(GetParentForPrefab(prefab));
                _spawnedObjects.Add(obj);
            }
            return obj;
        }

        private Transform GetParentForPrefab(GameObject prefab)
        {
            var key = prefab.name;
            
            if (!_prefabParents.ContainsKey(key))
            {
                var parentObj = new GameObject(key + "_Container");
                _prefabParents[key] = parentObj.transform;
            }
            return _prefabParents[key];
        }

        private void ClearMap()
        {
            foreach (var obj in _spawnedObjects)
            {
                if (obj != null) PoolManager.Instance.ReturnObject(obj);
            }
            _spawnedObjects.Clear();
            
            foreach (var kvp in _prefabParents)
            {
                if (kvp.Value != null)
                {
                    for (var i = kvp.Value.childCount - 1; i >= 0; i--)
                    {
                        var child = kvp.Value.GetChild(i).gameObject;
                        PoolManager.Instance.ReturnObject(child);
                    }
                }
            }
            
            StagePoints.Clear();
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
                for (var i = 0; i < maze.Length; i++) maze[i] = 0;

                var rng = new Unity.Mathematics.Random(seed);
                var current = startPos;
                maze[current.y * width + current.x] = 1;

                var firstUp = new int2(current.x, current.y + 1);
                if (firstUp.y >= 2 && firstUp.y < height - 1)
                {
                    maze[firstUp.y * width + firstUp.x] = 1;
                    current = firstUp;
                }

                while (current.y < height - 2)
                {
                    var dirs = new int2[]
                    {
                        new int2(0, 1),
                        new int2(1, 0),
                        new int2(-1, 0),
                    };

                    for (var i = 0; i < dirs.Length; i++)
                    {
                        var swap = rng.NextInt(0, dirs.Length);
                        (dirs[i], dirs[swap]) = (dirs[swap], dirs[i]);
                    }

                    var moved = false;
                    
                    foreach (var dir in dirs)
                    {
                        var next = current + dir;
                        if (next.x >= 0 && next.x < width &&
                            next.y >= 2 && next.y < height - 1)
                        {
                            if (maze[next.y * width + next.x] == 0)
                            {
                                maze[next.y * width + next.x] = 1;
                                current = next;
                                moved = true;
                                break;
                            }
                        }
                    }

                    if (!moved)
                    {
                        int newY = current.y + 1;
                        if (newY >= height - 1) break;
                        current = new int2(current.x, newY);
                        maze[current.y * width + current.x] = 1;
                    }
                }

                var connectorY = height - 2;
                
                if (current.y < connectorY)
                {
                    for (var y = current.y + 1; y <= connectorY; y++)
                        maze[y * width + current.x] = 1;
                    current = new int2(current.x, connectorY);
                }

                var x0 = math.min(current.x, endPos.x);
                var x1 = math.max(current.x, endPos.x);
                
                for (var x = x0; x <= x1; x++)
                    maze[connectorY * width + x] = 1;

                for (var x = 0; x < width; x++)
                    maze[(height - 1) * width + x] = 0;

                maze[endPos.y * width + endPos.x] = 1;
            }
        }
    }
}
