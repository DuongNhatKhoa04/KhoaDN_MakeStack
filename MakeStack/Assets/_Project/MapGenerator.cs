using MakeStack.Manager;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MakeStack.MapGenerator
{
    /// <summary>
    /// Custom NativeStack based on NativeList
    /// </summary>
    /// <typeparam name="T"> Native type </typeparam>
    public struct NativeStack<T> where T : unmanaged
    {
        public NativeStack(Allocator allocator)
        {
            _list = new NativeList<T>(allocator);
        }

        #region --- Methods ---

        /// <summary>
        /// Add new element to the last.
        /// </summary>
        /// <param name="item"> New element in Stack </param>
        public void Push(T item)
        {
            _list.Add(item);
        }

        /// <summary>
        /// Remove the last element.
        /// </summary>
        /// <returns> New Stack </returns>
        public T Pop()
        {
            var item = _list[_list.Length - 1];
            _list.RemoveAt(_list.Length - 1);
            return item;
        }

        /// <summary>
        /// Get the last element in Stack.
        /// </summary>
        /// <returns> The last element in Stack </returns>
        public T Peek() => _list[_list.Length - 1];

        /// <summary>
        /// Count element in Stack
        /// </summary>
        public int Count => _list.Length;

        /// <summary>
        /// Free memory
        /// </summary>
        public void Dispose() => _list.Dispose();

        #endregion

        #region --- Field ---

        private NativeList<T> _list;

        #endregion
    }

    public class MapGenerator : MonoBehaviour
    {
        [Header("Maze Settings")]
        public int width = 20;
        public int height = 20;
        public float cellSize = 2f;
        public int randomSeed = 0;

        [Header("Prefabs")]
        public GameObject floorPrefab;
        public GameObject wallPrefab;
        // public GameObject blockPrefab;

        private NativeArray<int> maze;
        private List<GameObject> spawnedObjects = new List<GameObject>();

        void Start()
        {
            uint seed = (randomSeed == 0) ? (uint)UnityEngine.Random.Range(1, int.MaxValue) : (uint)randomSeed;

            PoolManager.Instance.CreatePool(floorPrefab, 50, 500);
            PoolManager.Instance.CreatePool(wallPrefab, 50, 500);
            // PoolManager.Instance.CreatePool(blockPrefab, 50, 500);

            maze = new NativeArray<int>(width * height, Allocator.Persistent);


            int midX = (width % 2 == 0) ? width / 2 - 1 : width / 2;
            int2 startPos = new int2(midX, 1);
            int2 endPos = new int2(midX, height - 2);

            var job = new DFSMazeJob
            {
                width = width,
                height = height,
                maze = maze,
                startPos = startPos,
                endPos = endPos,
                seed = seed
            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
                    GameObject obj = null;

                    if (maze[y * width + x] == 1)
                    {
                        obj = PoolManager.Instance.GetObject(floorPrefab, pos, Quaternion.identity);
                    }
                    else
                    {
                        obj = PoolManager.Instance.GetObject(wallPrefab, new Vector3(x * cellSize, 0.1f, y * cellSize), Quaternion.identity);
                    }

                    if (obj != null)
                        spawnedObjects.Add(obj);
                }
            }

            maze.Dispose();
        }

        public void ClearMap()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                    PoolManager.Instance.ReturnObject(obj, obj);
            }
            spawnedObjects.Clear();
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
                for (int i = 0; i < maze.Length; i++)
                    maze[i] = 0;

                NativeStack<int2> stack = new NativeStack<int2>(Allocator.Temp);
                stack.Push(startPos);
                maze[startPos.y * width + startPos.x] = 1;

                Unity.Mathematics.Random rng = new Unity.Mathematics.Random(seed);

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

                    for (int i = 0; i < dirs.Length; i++)
                    {
                        int swap = rng.NextInt(0, dirs.Length);
                        (dirs[i], dirs[swap]) = (dirs[swap], dirs[i]);
                    }

                    foreach (var dir in dirs)
                    {
                        int2 next = current + dir;

                        if (next.x > 0 && next.y > 0 &&
                            next.x < width - 1 && next.y < height - 1)
                        {
                            if (maze[next.y * width + next.x] == 0)
                            {
                                int2 wall = current + dir / 2;
                                maze[wall.y * width + wall.x] = 1;
                                maze[next.y * width + next.x] = 1;

                                stack.Push(next);
                                carved = true;

                                if (next.Equals(endPos))
                                {
                                    stack.Pop();
                                    break;
                                }
                                break;
                            }
                        }
                    }

                    if (!carved)
                        stack.Pop();
                }

                stack.Dispose();
            }
        }
    }

}