using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System.Linq;

using URandom = UnityEngine.Random;

public class World : MonoBehaviour {
    public static World Instance { get; private set; }


    public static readonly Vector3Int WORLD_CENTER_POSITION = new Vector3Int(0, 0, 0);


    public static event EventHandler<WorldGeneratedArgs> OnWorldGenerated;


    #region EventArgs_Definitions
    public class WorldGeneratedArgs : EventArgs {
        public ChunkData.Chunk spawnChunk;
        public WorldGeneratedArgs(ChunkData.Chunk spawnChunk) {
            this.spawnChunk = spawnChunk;
        }
    }
    #endregion


    [Header("Chunks")]
    [SerializeField] [Range(1, 32)] private int chunksDrawRange;
    [SerializeField] [Range(1, 16)] private int dstThresholdFromCenterPointToGenerateNewChunks;

    [Header("Noise Map Settings")]
    public NoiseSettings noiseSettings;
    [Range(0.01f, 1f)] public float noiseScale;
    [Range(1, 5)] public int octaves;
    [Range(0f, 0.99f)] public float persistance;
    [Range(0.1f, 10f)] public float lacunarity;

    [Header("Seed")]
    [SerializeField] private bool useRandomSeed;
    [SerializeField] private int seed;
    public Vector2Int mapSeedOffset;

    [Header("Sea Water Level")]
    public int seaLayer;

    [Header("World Material")]
    public Material worldMaterial;
    public Material worldMaterialTransparent;

    [Header("Debug")]
    public bool isWorldChunkDataGenerating; // View debug only.
    public bool isGeneratingDataMainLoop; // View debug only.
    public bool isGeneratingWorldDataOnMainThread; // View debug only.
    public bool isMainThreadCreatingChunksInstances; // View debug only.
    public bool isOtherThreadBuildingChunkData; // View debug only.
    public bool isOtherThreadBuildingChunkMeshData; // View debug only.
    public bool isOtherThreadUpdatingChunkMeshData; // View debug only.
    [SerializeField] private int worldChunksDictionaryCount; // View debug only.
    [SerializeField] private int activeChunksDictionaryCount; // View debug only.
    [SerializeField] private int chunksToBuildInstanceRemaining; // View debug only.
    [SerializeField] private int chunksToBuildDataRemaining; // View debug only.
    [SerializeField] private int chunksToBuildMeshDataRemaining; // View debug only.
    [SerializeField] private int chunksToUpdateMeshDataRemaining; // View debug only.
    [SerializeField] private int chunksToSkipUpdatingRemaining; // View debug only.
    [SerializeField] private int chunksReadyToRenderLoadQueueRemaining; // View debug only.
    [SerializeField] private int chunksReadyToRenderUpdateQueueRemaining; // View debug only.
    [SerializeField] private int chunksToUnloadRemaining; // View debug only.
    [SerializeField] private int chunksAliveCount; // View debug only.

    private ConcurrentDictionary<ChunkData.ChunkCoord, ChunkData.Chunk> worldChunksDictionary;
    private ConcurrentDictionary<ChunkData.ChunkCoord, ChunkData.Chunk> activeChunksDictionary;

    private ConcurrentQueue<ChunkData.ChunkCoord> chunksToBuildInstanceQueue;
    private ConcurrentQueue<ChunkData.Chunk> chunksToBuildDataQueue;
    private ConcurrentQueue<ChunkData.Chunk> chunksToBuildMeshQueue;
    private ConcurrentQueue<ChunkData.Chunk> chunksToUpdateMeshQueue;
    private ConcurrentQueue<ChunkData.Chunk> chunksReadyToRender_LoadQueue;
    private ConcurrentQueue<ChunkData.Chunk> chunksReadyToRender_UpdateQueue;
    private ConcurrentQueue<ChunkData.Chunk> chunksToUnloadQueue;

    private List<ChunkData.Chunk> oldWorldChunkEdgesList;
    private List<ChunkData.Chunk> oldWorldChunksList;

    private HashSet<ChunkData.Chunk> chunksToSkipUpdatingHashset;

    private Thread DetectChunksAroundPlayersThread;
    private readonly object _detectChunksAroundPlayersLocker = new object();
    private readonly object _generateWorldChunkDataLocker = new object();

    private CancellationTokenSource cancelBuildChunkDataToken;
    private CancellationTokenSource cancelBuildMeshDataToken;
    private CancellationTokenSource cancelUpdateMeshDataToken;

    private ChunkData.Chunk chunkThatWillUpdateMeshDataNow;


    public ConcurrentDictionary<ChunkData.ChunkCoord, ChunkData.Chunk> GetWorldChunksDictionary { get => worldChunksDictionary; }
    public ConcurrentDictionary<ChunkData.ChunkCoord, ChunkData.Chunk> GetActiveChunksDictionary { get => activeChunksDictionary;  }
    public int DstThresholdFromCenterPointToGenerateNewChunks { 
        get => dstThresholdFromCenterPointToGenerateNewChunks * ChunkData.CHUNK_SIZE + (ChunkData.CHUNK_SIZE / 2); 
    }


    #region MONOBEHAVIOUR
    private void OnDrawGizmosSelected() {
        //if (worldChunksDictionary != null && Application.isPlaying) {

        //    //foreach(KeyValuePair<ChunkData.ChunkCoord, ChunkData.Chunk> entry in worldChunksDictionary) {
        //    //    if (entry.Value == null) { continue; }

        //    //    Gizmos.color = Color.blue;

        //    //    Vector3Int bottomLeft = new Vector3Int(entry.Key.coordX, 0, entry.Key.coordZ);
        //    //    Vector3Int bottomRight = new Vector3Int(entry.Key.coordX + ChunkData.CHUNK_SIZE, 0, entry.Key.coordZ);
        //    //    Vector3Int topLeft = new Vector3Int(entry.Key.coordX, 0, entry.Key.coordZ + ChunkData.CHUNK_SIZE);
        //    //    Vector3Int topRight = new Vector3Int(entry.Key.coordX + ChunkData.CHUNK_SIZE, 0, entry.Key.coordZ + ChunkData.CHUNK_SIZE);

        //    //    // Draw bottom left.
        //    //    Gizmos.DrawLine(bottomLeft, bottomLeft + (Vector3Int.up * ChunkData.CHUNK_HEIGHT));

        //    //    // Draw bottom right.
        //    //    Gizmos.DrawLine(bottomRight, bottomRight + (Vector3Int.up * ChunkData.CHUNK_HEIGHT));

        //    //    // Draw top left.
        //    //    Gizmos.DrawLine(topLeft, topLeft + (Vector3Int.up * ChunkData.CHUNK_HEIGHT));

        //    //    // Draw top right.
        //    //    Gizmos.DrawLine(topRight, topRight + (Vector3Int.up * ChunkData.CHUNK_HEIGHT));

        //    //}

        //}

        //if (worldChunksDictionary != null && Application.isPlaying) {
        //    ChunkData.ChunkCoord centerCoord = new ChunkData.ChunkCoord(0, 0);
        //    if (worldChunksDictionary.TryGetValue(centerCoord, out ChunkData.Chunk centerChunk)) {
        //        if (centerChunk != null) {

        //            if (!centerChunk.isChunkDataReady && !centerChunk.chunkMeshData.isRendered) { return; }

        //            foreach(KeyValuePair<BlockData.BlockCoord, BlockData.Block> blockEntry in centerChunk.chunkBlocksDictionary) {

        //                // Skip these block types.
        //                if (blockEntry.Value.blockType == BlockData.BlockTypes.Air ||
        //                    blockEntry.Value.blockType == BlockData.BlockTypes.Water) { continue; }

        //                if (blockEntry.Value.blockType == BlockData.BlockTypes.Grass) {
        //                    Gizmos.color = Color.green;
        //                    Vector3 center = new Vector3(blockEntry.Key.x, blockEntry.Key.y, blockEntry.Key.z);
        //                    Gizmos.DrawWireCube(center, Vector3.one);
        //                }

        //            }
        //        }
        //    }
        //}
    }

    private void Awake() {
        if (Instance == null) { Instance = this; } else { DestroyImmediate(gameObject); }

        int dictionaryInitialSize = (chunksDrawRange + chunksDrawRange + 1) * (chunksDrawRange + chunksDrawRange + 1);
        worldChunksDictionary = new ConcurrentDictionary<ChunkData.ChunkCoord, ChunkData.Chunk>(Environment.ProcessorCount, dictionaryInitialSize);
        activeChunksDictionary = new ConcurrentDictionary<ChunkData.ChunkCoord, ChunkData.Chunk>(Environment.ProcessorCount, dictionaryInitialSize);

        chunksToBuildInstanceQueue = new ConcurrentQueue<ChunkData.ChunkCoord>();
        chunksToBuildDataQueue = new ConcurrentQueue<ChunkData.Chunk>();
        chunksToBuildMeshQueue = new ConcurrentQueue<ChunkData.Chunk>();
        chunksToUpdateMeshQueue = new ConcurrentQueue<ChunkData.Chunk>();
        chunksReadyToRender_LoadQueue = new ConcurrentQueue<ChunkData.Chunk>();
        chunksReadyToRender_UpdateQueue = new ConcurrentQueue<ChunkData.Chunk>();
        chunksToUnloadQueue = new ConcurrentQueue<ChunkData.Chunk>();

        oldWorldChunkEdgesList = new List<ChunkData.Chunk>();
        oldWorldChunksList = new List<ChunkData.Chunk>();

        chunksToSkipUpdatingHashset = new HashSet<ChunkData.Chunk>();

        cancelBuildChunkDataToken = new CancellationTokenSource();
        cancelBuildMeshDataToken = new CancellationTokenSource();
        cancelUpdateMeshDataToken = new CancellationTokenSource();

        chunkThatWillUpdateMeshDataNow = null;

        if (dstThresholdFromCenterPointToGenerateNewChunks >= chunksDrawRange) {
            dstThresholdFromCenterPointToGenerateNewChunks /= 2;
        }
    }

    private void Start() {
        InitializeSeed();
        InitializeThreads();
        GenerateWorld();
    }

    private void Update() {
        worldChunksDictionaryCount = worldChunksDictionary.Keys.Count;
        activeChunksDictionaryCount = activeChunksDictionary.Keys.Count;
        chunksToBuildInstanceRemaining = chunksToBuildInstanceQueue.Count;
        chunksToBuildDataRemaining = chunksToBuildDataQueue.Count;
        chunksToBuildMeshDataRemaining = chunksToBuildMeshQueue.Count;
        chunksToUpdateMeshDataRemaining = chunksToUpdateMeshQueue.Count;
        chunksToSkipUpdatingRemaining = chunksToSkipUpdatingHashset.Count;
        chunksReadyToRenderLoadQueueRemaining = chunksReadyToRender_LoadQueue.Count;
        chunksReadyToRenderUpdateQueueRemaining = chunksReadyToRender_UpdateQueue.Count;
        chunksToUnloadRemaining = chunksToUnloadQueue.Count;

        DebuggingCanvas.Instance.UpdateWorldReferences(worldChunksDictionaryCount, activeChunksDictionaryCount,
            chunksToBuildDataRemaining, chunksToBuildInstanceRemaining, chunksToBuildMeshDataRemaining, 
            chunksToUpdateMeshDataRemaining, chunksToSkipUpdatingRemaining, chunksReadyToRenderLoadQueueRemaining, chunksReadyToRenderUpdateQueueRemaining, chunksToUnloadRemaining);
    }

    private void OnApplicationQuit() {
        // Stop the thread which detects the chunks around the players if it still alive.
        if (DetectChunksAroundPlayersThread.IsAlive) {
            DetectChunksAroundPlayersThread.Abort();
            Debug.Log("(DetectChunksAroundPlayersThread) - Aborting thread execution...");
        }

        // Stop the async operation when stop executin the application.
        cancelBuildChunkDataToken.Cancel(false);
        Debug.Log("(HandleChunkDataTaskAsync) - Parallel chunk building data threadpool aborted...");

        // Stop the async operation when stop executing the application.
        cancelBuildMeshDataToken.Cancel(false);
        Debug.Log("(HandleBuildChunkMeshDataTaskAsync) - Parallel chunk mesh data load threadpool aborted...");

        // Stop the async operation when stop executing the application.
        cancelUpdateMeshDataToken.Cancel(false);
        Debug.Log("(HandleUpdateChunkMeshDataTaskAsync) - Parallel chunk mesh data updating threadpool aborted...");
    }
    #endregion


    #region WORLD_INITIALIZATION
    private void InitializeSeed() {
        if (useRandomSeed) {
            seed = URandom.Range(-16000, 16000);
            URandom.InitState(seed);
            mapSeedOffset = new Vector2Int(URandom.Range(-seed, seed), URandom.Range(-seed, seed));
        } 
        else {
            if (mapSeedOffset.x == 0 && mapSeedOffset.y == 0) {
                mapSeedOffset = new Vector2Int(seed, seed);
            }
        }
    }

    private void InitializeThreads() {
        // Start the thread to handle the chunk detection around the players.
        ThreadStart startDetectingChunksAroundPlayers = delegate { DetectChunksToGenerateAroundPlayersThread(); };
        DetectChunksAroundPlayersThread = new Thread(startDetectingChunksAroundPlayers);
        DetectChunksAroundPlayersThread.Name = nameof(DetectChunksAroundPlayersThread);
        DetectChunksAroundPlayersThread.Start();
        Debug.Log($"(DetectChunksAroundPlayersThread) - Starting thread execution: Thread ID: '{DetectChunksAroundPlayersThread.ManagedThreadId}' - Thread state: '{DetectChunksAroundPlayersThread.ThreadState}'.");

        // Set the main thread name for debugging porpuses.
        if (Thread.CurrentThread.Name == null) {
            Thread.CurrentThread.Name = "MainThread";
        }
    }

    private void GenerateWorld() {
        // Start the main world data generation.
        GenerateWorldData(WORLD_CENTER_POSITION, true);

        // Start the coroutine to handle the chunk instances creation.
        StartCoroutine(CreateChunksInstancesCoroutine());

        // Start asynchronous building chunk data in a threadpool.
        HandleChunkDataTaskAsync();

        // Start asynchronous building chunks mesh data in a threadpool.
        HandleBuildChunkMeshDataTaskAsync();
        
        // Start asynchronous updating chunk mesh data in a threadpool.
        HandleUpdateChunkMeshDataTaskAsync();

        // Start the coroutines to handle chunk state actions(render mesh /update mesh /unload mesh).
        StartCoroutine(ChunkLoadCoroutine());
        StartCoroutine(ChunkUpdateCoroutine());
        StartCoroutine(ChunkUnloadCoroutine());

        // Start the coroutine which will wait until the first world generation completed,
        // before setting the world spawn chunk. (Non-serialized yet...)
        StartCoroutine(WaitGenerationCompletedToSetSpawnChunk());
    }
    #endregion


    #region CHUNKS_LOAD/UPDATING/UNLOAD
    private IEnumerator ChunkLoadCoroutine() {
        while (true) {

            // Handle rendering the chunks that are ready to be rendered.
            if (chunksReadyToRender_LoadQueue.Count > 0) {
                while (chunksReadyToRender_LoadQueue.Count > 0) {

                    chunksReadyToRender_LoadQueue.TryDequeue(out ChunkData.Chunk chunkToRender);

                    // Is this chunk to render still existing inside the current active chunks?
                    if (activeChunksDictionary.TryGetValue(chunkToRender.chunkCoord, out ChunkData.Chunk activeChunk)) {
                        if (activeChunk != null) {

                            chunkToRender.BuildChunkMesh();
                            yield return new WaitUntil(() => chunkToRender.chunkMeshData.isRendered);
                        }
                    }

                    yield return null;
                }
            }

            yield return null;
        }
    }

    private IEnumerator ChunkUpdateCoroutine() {
        while (true) {

            //Handle updating the chunks that are ready to be updated.
            if (chunksReadyToRender_UpdateQueue.Count > 0) {
                while (chunksReadyToRender_UpdateQueue.Count > 0) {

                    chunksReadyToRender_UpdateQueue.TryDequeue(out ChunkData.Chunk chunkToRender);

                    // Is this chunk to render still existing inside the current active chunks?
                    if (activeChunksDictionary.TryGetValue(chunkToRender.chunkCoord, out ChunkData.Chunk activeChunk)) {
                        if (activeChunk != null) {

                            chunkToRender.BuildChunkMesh();
                            activeChunksDictionary[chunkToRender.chunkCoord] = chunkToRender;
                            worldChunksDictionary[chunkToRender.chunkCoord] = chunkToRender;
                            yield return new WaitUntil(() => chunkToRender.chunkMeshData.isRendered);
                        }
                    }

                    yield return null;
                }
            }

            yield return null;
        }
    }

    private IEnumerator ChunkUnloadCoroutine() {
        bool canCleanUnusedMemory = false;
        while (true) {

            // Handle the old chunk unload queue.
            if (chunksToUnloadQueue.Count > 0) {
                while(chunksToUnloadQueue.Count > 0) {

                    if (chunksToUnloadQueue.TryDequeue(out ChunkData.Chunk chunk)) {
                        if (worldChunksDictionary.TryGetValue(chunk.chunkCoord, out ChunkData.Chunk worldChunk)) {

                            // If the chunk instance are loaded into the world.
                            if (worldChunk != null) {

                                // And the chunk gameobject holder is also loaded, destroy it.
                                if (worldChunk.chunkObject != null) {

                                    // Destroy 
                                    worldChunk.DestroyChunk();

                                    // And clean it from the main world chunk dictionary.
                                    worldChunksDictionary[worldChunk.chunkCoord] = null;
                                }
                            }

                            // Null the instances
                            chunk = null;
                            worldChunk = null;
                        }
                    }

                    canCleanUnusedMemory = true;
                    yield return new WaitForEndOfFrame();
                }
            }

            // After all chunks unloaded, then clean the unused assets from memory and lock it.
            if (canCleanUnusedMemory) {
                AsyncOperation asyncUnloading = Resources.UnloadUnusedAssets();
                while (!asyncUnloading.isDone) {
                    Debug.Log("(ChunkUnloadCoroutine) - Cleaning memory...");
                    yield return null;
                }
                canCleanUnusedMemory = false;
            }

            yield return null;
        }
    }

    private async void HandleChunkDataTaskAsync() {
        while (true) {
            await BuildChunksDataTaskAsync(cancelBuildChunkDataToken);
        }
    }

    private async Task BuildChunksDataTaskAsync(CancellationTokenSource tokenSource) {
        await Task.Run(async () => {

            // Handle the new chunks to build data queue.
            if (chunksToBuildDataQueue.Count > 0) {
                while (chunksToBuildDataQueue.Count > 0) {
                    isOtherThreadBuildingChunkData = true;

                    // Get the chunk to build the data.
                    chunksToBuildDataQueue.TryDequeue(out ChunkData.Chunk chunk);

                    // If no one other queue already have chunk with same coord.
                    if (!chunksToBuildMeshQueue.Any(t => t.chunkCoord.Equals(chunk)) && !chunksReadyToRender_LoadQueue.Any(t => t.chunkCoord.Equals(chunk))) {

                        // And await to chunk data be built before continue to the next one.
                        await chunk.BuildChunkBlocksData(tokenSource);

                        chunksToBuildMeshQueue.Enqueue(chunk);
                    }
                    else {
                        Debug.LogWarning("Chunk is already in the provided queues, skipping this instance...");
                    }
                }
            }

            isOtherThreadBuildingChunkData = false;
        });
    }

    private async void HandleBuildChunkMeshDataTaskAsync() {
        while (true) {

            // Build all pending chunks mesh data.
            await BuildChunksMeshDataTaskAsync(cancelBuildMeshDataToken);
        }
    }

    private async Task BuildChunksMeshDataTaskAsync(CancellationTokenSource tokenSource) {
        await Task.Run(async () => {

            // Handle the new chunks to load queue.
            if (chunksToBuildMeshQueue.Count > 0) {
                while (chunksToBuildMeshQueue.Count > 0) {
                    isOtherThreadBuildingChunkMeshData = true;

                    if (chunksToBuildMeshQueue.TryDequeue(out ChunkData.Chunk chunk)) {

                        // Wait while the world chunk data is generating to access the active chunks dictionary after completes.
                        while (isWorldChunkDataGenerating) { }

                        // Is this chunk to build mesh data still existing inside the current active chunks?
                        if (activeChunksDictionary.ContainsKey(chunk.chunkCoord)) {

                            // Skip if this chunks is the same chunk that will update mesh which was dequeued from the queue which it were in.
                            if (chunkThatWillUpdateMeshDataNow == chunk) { 
                                Debug.Log("(BuildChunksMeshDataTaskAsync) - Chunk which would build the mesh data is the same that will update the mesh data, skipping it... Chunk Coord: " + chunk.chunkCoord.CoordToVector3Int()); 
                                continue; 
                            }

                            // If no other queue has chunk with the same coords.
                            if (!chunksReadyToRender_LoadQueue.Any(t => t.chunkCoord.Equals(chunk.chunkCoord))
                            && !chunksToUpdateMeshQueue.Any(t => t.chunkCoord.Equals(chunk.chunkCoord))
                            && !chunksReadyToRender_UpdateQueue.Any(t => t.chunkCoord.Equals(chunk.chunkCoord)))  {

                            // Await to chunk mesh data be built asynchronously.
                            await chunk.BuildChunkMeshDataAsync(tokenSource);

                            // Then add it to the queue to render the ready chunks.
                            chunksReadyToRender_LoadQueue.Enqueue(chunk);

                            } 
                            //else {
                            //    Debug.LogWarning("(BuildChunksMeshDataTaskAsync) - Chunk is already in the provided queues, skipping this instance... Chunk Coord: " + chunk.chunkCoord.CoordToVector3Int());
                            //}

                        }
                        //else {
                        //    Debug.LogWarning("(BuildChunksMeshDataTaskAsync) - Chunk coord: " + chunk.chunkCoord.CoordToVector3Int() + " was not found inside the active chunks dictionary...");
                        //}

                    }

                }
            }

            isOtherThreadBuildingChunkMeshData = false;

        }, tokenSource.Token);
    }

    private async void HandleUpdateChunkMeshDataTaskAsync() {
        while (true) {

            // Update all pending chunks mesh data.
            await UpdateChunksMeshDataTaskAsync(cancelUpdateMeshDataToken);
        }
    }

    private async Task UpdateChunksMeshDataTaskAsync(CancellationTokenSource tokenSource) {
        await Task.Run(async () => {

            if (chunksToUpdateMeshQueue.Count > 0) {
                while (chunksToUpdateMeshQueue.Count > 0) {
                    isOtherThreadUpdatingChunkMeshData = true;

                    if (chunksToUpdateMeshQueue.TryDequeue(out ChunkData.Chunk chunk)) {

                        // Skip the chunks that are selected to skip from updating.
                        if (chunksToSkipUpdatingHashset.Remove(chunk)) {
                            Debug.Log("(UpdateChunksMeshDataTaskAsync) - Skipping chunk from update queue, because it is selected to be unloaded! Chunk: " + chunk.chunkCoord.CoordToVector3Int());
                            continue;
                        }

                        // Set the current chunk as the global access chunk that will update the mesh data now.
                        chunkThatWillUpdateMeshDataNow = chunk;

                        // Wait while the world chunk data is generating to access the active chunks dictionary after completes.
                        while (isWorldChunkDataGenerating) { }

                        //Is this chunk to update mesh data still existing inside the current active chunks?
                        if (activeChunksDictionary.ContainsKey(chunk.chunkCoord)) {

                            // If no other queue has the chunk with the same coords.
                            if (!chunksReadyToRender_UpdateQueue.Any(t => t.chunkCoord.Equals(chunk.chunkCoord) && 
                                !chunksReadyToRender_LoadQueue.Any(t => t.chunkCoord.Equals(chunk.chunkCoord)))) {

                                // Then await it to build all the new chunk mesh data again.
                                await chunk.BuildChunkMeshDataAsync(tokenSource);

                                // Then add it to the queue to render the ready chunks.
                                chunksReadyToRender_UpdateQueue.Enqueue(chunk);

                            } 
                            //else {
                            //    Debug.LogWarning("(UpdateChunksMeshDataTaskAsync) - Chunk is already in the provided queue, skipping this instance...");
                            //}

                        }
                        //else {
                        //    Debug.LogWarning("(UpdateChunksMeshDataTaskAsync) - Chunk coord: " + chunk.chunkCoord.CoordToVector3Int() + " was not found inside the active chunks dictionary...");
                        //}

                    }

                }
            }

            isOtherThreadUpdatingChunkMeshData = false;

        }, tokenSource.Token);
    }

    public void UpdateModifiedChunkMesh(ChunkData.Chunk chunk) {
        // If this chunk is not already inside the chunks to update mesh queue.
        if (!chunksToUpdateMeshQueue.Contains(chunk)) {

            // If no other queue has the chunk with the same coords.
            if (!chunksReadyToRender_UpdateQueue.Any(t => t.chunkCoord.Equals(chunk.chunkCoord) &&
                !chunksReadyToRender_LoadQueue.Any(t => t.chunkCoord.Equals(chunk.chunkCoord)))) {
                chunksToUpdateMeshQueue.Enqueue(chunk);
                Debug.Log("Adding chunk mesh to update because it got modified...");
            }
        }
    }
    #endregion


    #region DYNAMIC_CHUNKS_GENERATION
    private void GenerateWorldData(Vector3Int position, bool useMainThread) {
        lock (_generateWorldChunkDataLocker) {
            Debug.Log("**** World Data Generation Started... ****");

            // Keep the bool flag as true to avoid any race conflicts.
            isWorldChunkDataGenerating = true;

            int chunkDrawSize = chunksDrawRange * ChunkData.CHUNK_SIZE;
            int startX = position.x - chunkDrawSize;
            int startZ = position.z - chunkDrawSize;
            int endX = position.x + chunkDrawSize;
            int endZ = position.z + chunkDrawSize;

            activeChunksDictionary.Clear();
            HashSet<ChunkData.ChunkCoord> chunksCoordsRange = new HashSet<ChunkData.ChunkCoord>();
            isGeneratingDataMainLoop = false;
            int chunksToLoadThisGen = 0;

            // Core world data generation loop.
            for (int x = startX; x <= endX; x += ChunkData.CHUNK_SIZE) {
                for (int z = startZ; z <= endZ; z += ChunkData.CHUNK_SIZE) {

                    // Set the bool flag as true to synchronize with the main thread and make it wait while is generating the data.
                    isGeneratingDataMainLoop = true;

                    Vector3Int chunkPosition = new Vector3Int(x, 0, z);
                    ChunkData.ChunkCoord chunkCoord = new ChunkData.ChunkCoord(chunkPosition.x, chunkPosition.z);
                    chunksCoordsRange.Add(chunkCoord);

                    // The world already contains a chunk in this coords?
                    if (worldChunksDictionary.TryGetValue(chunkCoord, out ChunkData.Chunk chunk)) {

                        // If this chunk data in that coords is already loaded.
                        if (chunk != null) {
                            // Try add this new chunk to the active chunks dictionary, if it fails will catch it automatically.
                            TryAddToActiveChunksDictionary(chunkCoord, chunk);
                            continue;
                        }

                        // If no chunk data loaded, override with a new chunk instance in that coords.
                        if (!chunksToBuildInstanceQueue.Any(t => t.Equals(chunkCoord))) { chunksToBuildInstanceQueue.Enqueue(chunkCoord); }
                        chunksToLoadThisGen++;
                    }
                    // Otherwise the world not contains a key in that coords.
                    else {

                        // Then create a brand new chunk in that coords.
                        if (!chunksToBuildInstanceQueue.Any(t => t.Equals(chunkCoord))) { chunksToBuildInstanceQueue.Enqueue(chunkCoord); }
                        chunksToLoadThisGen++;
                    }

                }
            }

            Debug.Log("Chunks to load this gen: " + chunksToLoadThisGen);

            // If is running this on the main thread, do it synchronized.
            if (useMainThread) { CreateChunkInstancesSynchronized(); }

            // Set the bool flag as false to synchronize with the main thread and make it start creating the chunks instances.
            // And also to the main thread lock this execution while it is creating the instances, before proceeding to unloading and updating chunks.
            isGeneratingDataMainLoop = false;

            // If is not running this inside the main thread, then synchronize with it to avoid any possible race condition or desynchronization with the main thread..
            if (!useMainThread) {
                //Make the thread which is executing it sleep for few milliseconds.
                Thread.Sleep(100);

                // Wait while the main thread still creating the chunks instances before unload or update them.
                while (isMainThreadCreatingChunksInstances) { }
            }

            // Get the chunks to unload from the previous generation.
            GetChunksToUnload(chunksCoordsRange);

            // Get the chunks to update from the previous generation.
            GetChunksToUpdate();

            // Set the bool flag as false after all the world data generated.
            isWorldChunkDataGenerating = false;

            Debug.Log("**** World Data Generation Completed! ****");
        }
    }

    private void CreateChunkInstancesSynchronized() {
        isGeneratingWorldDataOnMainThread = true;
        isMainThreadCreatingChunksInstances = true;

        if (chunksToBuildInstanceQueue.Count > 0) {
            while (chunksToBuildInstanceQueue.Count > 0) {

                CreateInstances();

            }
        }

        isMainThreadCreatingChunksInstances = false;
        isGeneratingWorldDataOnMainThread = false;
    }

    private IEnumerator CreateChunksInstancesCoroutine() {
        while (true) {

            if (chunksToBuildInstanceQueue.Count > 0) {

                // Set the bool flag as true to synchronize with the other threads (by making them wait until this queue is cleared).
                isMainThreadCreatingChunksInstances = true;

                // Wait while the data main generation loop is generating the data.
                yield return new WaitWhile(() => isGeneratingDataMainLoop);

                // If it is already generating the world data on the main thread, no need to to it in here concurrently.
                yield return new WaitWhile(() => isGeneratingWorldDataOnMainThread);

                while (chunksToBuildInstanceQueue.Count > 0) {

                    CreateInstances();

                    yield return null;
                }
            }

            isMainThreadCreatingChunksInstances = false;

            yield return null;
        }
    }

    private void CreateInstances() {
        chunksToBuildInstanceQueue.TryDequeue(out ChunkData.ChunkCoord chunkCoord);

        // If this chunk instance is not already being processed by any other queue.
        if (!chunksToBuildDataQueue.Any(t => t.chunkCoord.Equals(chunkCoord)) &&
            !chunksToBuildMeshQueue.Any(t => t.chunkCoord.Equals(chunkCoord)) &&
            !chunksReadyToRender_LoadQueue.Any(t => t.chunkCoord.Equals(chunkCoord)) &&
            !chunksToUpdateMeshQueue.Any(t => t.chunkCoord.Equals(chunkCoord)) &&
            !chunksReadyToRender_UpdateQueue.Any(t => t.chunkCoord.Equals(chunkCoord)) &&
            !chunksToUnloadQueue.Any(t => t.chunkCoord.Equals(chunkCoord))) {

            ChunkData.Chunk newChunk = new ChunkData.Chunk(chunkCoord);

            // Try add new chunk to the world chunks dictionary, if it fails will catch it automatically.
            TryAddToWorldChunksDictionary(chunkCoord, newChunk);

            // Try add this new chunk to the active chunks dictionary, if it fails will catch it automatically.
            TryAddToActiveChunksDictionary(chunkCoord, newChunk);

            // Enqueue it to build the data.
            chunksToBuildDataQueue.Enqueue(newChunk);
        }
        else {
            Debug.LogWarning("Chunk is already in the provided queues, skipping this instance...");
        }
    }

    private void GetChunksToUnload(HashSet<ChunkData.ChunkCoord> chunksCoordsRange) {
        int chunksToUnloadThisGen = 0;
        // Look for chunks that can be unloaded because are far away from the requested position.
        foreach (KeyValuePair<ChunkData.ChunkCoord, ChunkData.Chunk> chunkToUnload in worldChunksDictionary) {
            // If this new range not contains that entry key, that means this entry chunk is out of the new range, and can be unloaded.
            if (!chunksCoordsRange.Contains(chunkToUnload.Key) && chunkToUnload.Value != null) {

                // If is this value not already inside the old chunks to unload queue, add to it.
                if (!chunksToUnloadQueue.Contains(chunkToUnload.Value)) {

                    // If this chunk to unload is inside the update mesh queue, then mark it to be skipped.
                    if (chunksToUpdateMeshQueue.Contains(chunkToUnload.Value)) {
                        chunksToSkipUpdatingHashset.Add(chunkToUnload.Value);
                        //Debug.Log("(GetChunksToUnload) - Chunk that will be unloaded is marked to be updated! Chunk : " + chunkToUnload.Value.chunkCoord.CoordToVector3Int());
                    }

                    chunksToUnloadQueue.Enqueue(chunkToUnload.Value);
                    chunksToUnloadThisGen++;
                }
            }
        }
        Debug.Log("Chunks to unload this gen: " + chunksToUnloadThisGen);
    }

    private void GetChunksToUpdate() {
        List<ChunkData.Chunk> newWorldChunkEdgesList = new List<ChunkData.Chunk>();
        List<ChunkData.Chunk> newWorldChunksList = new List<ChunkData.Chunk>();
        List<ChunkData.Chunk> chunksToUpdateList = new List<ChunkData.Chunk>();

        // Loop to find the new world edges chunks of this current generation.
        foreach (KeyValuePair<ChunkData.ChunkCoord, ChunkData.Chunk> activeChunkEntry in activeChunksDictionary) {

            // If is this chunk is an old world chunk edge, skip iteration.
            if (oldWorldChunkEdgesList.Contains(activeChunkEntry.Value)) { continue; }

            // Ensure that chunk is in the world edge, then add it to the list.
            if (IsChunkWorldEdge(activeChunkEntry.Key)) {

                // If the previous non-edge old world chunk is now an world edge chunk, then skip that iteration,
                // to avoid adding it as a new world chunk edge.
                if (oldWorldChunksList.Contains(activeChunkEntry.Value)) {
                    continue;
                }

                // Add this chunk as a new world edge.
                newWorldChunkEdgesList.Add(activeChunkEntry.Value);
            }
            // Otherwise is a non-edge chunk.
            else {

                // If this chunk is not an old world chunk.
                if (!oldWorldChunksList.Contains(activeChunkEntry.Value)) {

                    // Add this chunk as a non-edge new world chunk.
                    newWorldChunksList.Add(activeChunkEntry.Value);
                }
            }

        }

        bool skipToFirst = false;
        // Loop through all the old world chunk edges.
        for (int i = 0; i < oldWorldChunkEdgesList.Count; i++) {

            // Then loop through all the new world chunk edges.
            for (int j = 0; j < newWorldChunkEdgesList.Count; j++) {

                if (skipToFirst) { skipToFirst = false; break; }

                // If this old chunk edge is neighbor of any of these new world chunk edges.
                if (IsChunkNeighbor(oldWorldChunkEdgesList[i], newWorldChunkEdgesList[j])) {

                    // Then add it to the update queue, and break.
                    chunksToUpdateList.Add(oldWorldChunkEdgesList[i]);
                    break;
                }
                // Otherwise check against the new chunks that are not edges, but now also exists (dead zone occurring when creating more than 1 chunk row/col in one direction).
                else {

                    // Loop through the non-edge new world chunks list.
                    for (int k = 0; k < newWorldChunksList.Count; k++) {

                        // If is this old chunk edge is neighbor of any of these non-edge new world chunks.
                        if (IsChunkNeighbor(oldWorldChunkEdgesList[i], newWorldChunksList[k])) {

                            // Then add it to the update queue, and break.
                            chunksToUpdateList.Add(oldWorldChunkEdgesList[i]);
                            skipToFirst = true;
                            break;
                        }
                    }
                }
            }
        }

        // Clear the old world chunk edges list to repopulate it again.
        oldWorldChunkEdgesList.Clear();
        oldWorldChunksList.Clear();

        // Loop through all the current active chunks.
        foreach (KeyValuePair<ChunkData.ChunkCoord, ChunkData.Chunk> activeChunkEntry in activeChunksDictionary) {
            // Is this chunk world edge?
            if (IsChunkWorldEdge(activeChunkEntry.Key)) {

                // Then add it in the old list, to be used in the next generation.
                oldWorldChunkEdgesList.Add(activeChunkEntry.Value);
            }
            else {

                // Then add it non-edge chunk to the non-edge old world chunks.
                oldWorldChunksList.Add(activeChunkEntry.Value);
            }
        }

        //Deal with the case where the selected chunk to be updated are selected to be unloaded.
        //List<ChunkData.Chunk> chunksToRemoveFromUpdating = new List<ChunkData.Chunk>(chunksToUpdateList);
        //foreach (ChunkData.Chunk chunk in chunksToRemoveFromUpdating) {
        //    if (chunksToUnloadQueue.Contains(chunk)) {
        //        chunksToUpdateList.Remove(chunk);
        //        Debug.Log("(GetChunksToUpdate) - Removing chunk to update: " + chunk.chunkCoord.CoordToVector3Int() + " because are selected to be unloaded");
        //    }
        //}

        int chunksToUpdateThisGen = 0;
        // Then set the 'chunk to update list', to the 'chunk to updated queue'.
        for (int i = 0; i < chunksToUpdateList.Count; i++) {

            // Skip updating chunks selected to be unloaded.
            if (chunksToUnloadQueue.Contains(chunksToUpdateList[i])) {
                Debug.Log("Skipping chunk that would be updated because it is in unload queue...");
                continue; }

            // Skip updating the same chunk twice or more.
            if (chunksToUpdateMeshQueue.Contains(chunksToUpdateList[i])) {
                Debug.Log("Skipping chunk that would be updated because it is already in chunks to update...");
                continue; }

            // Skip updating if this chunks is already in the chunks to render queue.
            if (chunksReadyToRender_UpdateQueue.Contains(chunksToUpdateList[i])) {
                Debug.Log("Skipping chunk that would be updated because it is already in the chunks ready to render update queue...");
                continue; }

            chunksToUpdateMeshQueue.Enqueue(chunksToUpdateList[i]);
            chunksToUpdateThisGen++;
        }

        Debug.Log("Chunks to update this gen: " + chunksToUpdateThisGen);
    }   

    private bool IsChunkWorldEdge(ChunkData.ChunkCoord chunkCoord) {
        // Loop through each adjacent direction of this chunk.
        for (int dirIndex = 0; dirIndex < ChunkData.CHUNK_DIRECTIONS_COUNT; dirIndex++) {

            Vector3Int neighborPosition = chunkCoord.CoordToVector3Int() + DirectionExtension.GetChunkDirectionVector((ChunkData.ChunkDirection) dirIndex);
            ChunkData.ChunkCoord neighborCoord = ChunkData.ChunkCoord.Vector3IntToCoord(neighborPosition);

            // If the active chunks dicitionary does not contains that coord key, that means this is an world edge chunk,
            // As all the world edge chunks will have at least 1 empty neighbor position, or 2 when they are the world corners.
            if (!activeChunksDictionary.ContainsKey(neighborCoord)) {
                return true;
            }
        }
        return false;
    }

    private bool IsChunkNeighbor(ChunkData.Chunk origin, ChunkData.Chunk neighbor) {
        Vector3Int originVector3Int = origin.chunkCoord.CoordToVector3Int();
        Vector3Int neighborVector3Int = neighbor.chunkCoord.CoordToVector3Int();
        for (int dirIndex = 0; dirIndex < ChunkData.CHUNK_DIRECTIONS_COUNT; dirIndex++) {
            Vector3Int originPosition = originVector3Int + DirectionExtension.GetChunkDirectionVector((ChunkData.ChunkDirection) dirIndex);
            if (originPosition == neighborVector3Int) { return true; }
        }
        return false;
    }

    private void TryAddToWorldChunksDictionary(ChunkData.ChunkCoord chunkCoord, ChunkData.Chunk chunk) {
        if (worldChunksDictionary.TryAdd(chunkCoord, chunk)) { }
        else { worldChunksDictionary[chunkCoord] = chunk; }
    }

    private void TryAddToActiveChunksDictionary(ChunkData.ChunkCoord chunkCoord, ChunkData.Chunk chunk) {
        if (activeChunksDictionary.TryAdd(chunkCoord, chunk)) { }
        else { activeChunksDictionary[chunkCoord] = chunk; }
    }

    private void DetectChunksToGenerateAroundPlayersThread() {
        lock (_detectChunksAroundPlayersLocker) {
            while (true) {

                WaitExecution();

                for (int i = 0; i < GameManager.Instance.GetPlayers.Count; i++) {

                    Player currentPlayer = GameManager.Instance.GetPlayers[i];

                    // If no player request pending.
                    if (!currentPlayer.IsPlayerChunkRequestPending) {

                        if (currentPlayer.IsPlayerFarEnoughFromSourceChunk) {

                            WaitExecution();

                            // Lock the pending requests (flag).
                            currentPlayer.IsPlayerChunkRequestPending = true;

                            ChunkData.Chunk chunkRequestedByThePlayer = null;

                            // Try get the chunk requested by the player, if it was not possible to get the chunk, then try to find it until find.
                            while(chunkRequestedByThePlayer == null) {
                                chunkRequestedByThePlayer = 
                                    ChunkDataUtils.GetChunkWhichContainsPosition(currentPlayer.PlayerPosition);
                            }

                            // Update the chunk the player are in, as the source generation point.
                            currentPlayer.SourceChunk = chunkRequestedByThePlayer;

                            Debug.Log("(DetectChunksToGenerateAroundPlayersThread) - New chunks requested around position: " + currentPlayer.SourceChunk.chunkCoord.CoordToVector3Int());

                            // Reset to false to avoid doing it many times sequentially.
                            currentPlayer.IsPlayerFarEnoughFromSourceChunk = false;

                            // Then generate the world around the chunk coord the player are currently in.
                            GenerateWorldData(currentPlayer.SourceChunk.chunkCoord.CoordToVector3Int(), false);

                            WaitExecution();

                            // Unlock  the pending requests (flag).
                            currentPlayer.IsPlayerChunkRequestPending = false;

                            Debug.Log("(DetectChunksToGenerateAroundPlayersThread) - Request completed!");
                        }

                    }
                }

            }
        }
    }

    private void WaitExecution() {
        // Wait while is this thread generating the world chunk data.
        while (isWorldChunkDataGenerating) { }

        // Wait while is the main thread generating the chunks instances.
        while (isMainThreadCreatingChunksInstances) { }

        // Wait while is other thread building chunk data.
        while (isOtherThreadBuildingChunkData) { }
    }
    #endregion


    #region EVENTS
    private IEnumerator WaitGenerationCompletedToSetSpawnChunk() {
        // Wait if is other world thread building chunk data.
        yield return new WaitWhile(() => isWorldChunkDataGenerating);

        // Wait if is the main thread creating the chunks instances.
        yield return new WaitWhile(() => isMainThreadCreatingChunksInstances);

        // Wait if is other thread building the chunk data.
        yield return new WaitWhile(() => isOtherThreadBuildingChunkData);

        // Wait if is other thread building the chunk mesh data.
        yield return new WaitWhile(() => isOtherThreadBuildingChunkMeshData);

        // TODO: Change the world center position to the last chunk the player were in. (After serialization implemented...)
        OnWorldGenerated?.Invoke(this, new WorldGeneratedArgs(
            ChunkDataUtils.GetChunkWhichContainsPosition(WORLD_CENTER_POSITION)));
    }
    #endregion

}
