using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class PlayerInteraction : MonoBehaviour {

    private const byte RAY_MAX_LENGTH = 8;
    private const float RAY_STEP = 200;

    [SerializeField] private bool placeBlockRequested = false;
    [SerializeField] private bool removeBlockRequested = false;

    private Player thisPlayer;

    private Thread BlockInteractionThread;


    #region MONOBEHAVIOUR
    private void Start() {
        thisPlayer = GetComponent<Player>();

        ThreadStart handleBlockInteractionThreadStart = delegate { HandleBlockInteractionThread(); };
        BlockInteractionThread = new Thread(handleBlockInteractionThreadStart);
        BlockInteractionThread.Start();
        Debug.Log($"(HandleBlockInteractionThread) - Starting thread execution: Thread ID: '{BlockInteractionThread.ManagedThreadId}' - Thread state: '{BlockInteractionThread.ThreadState}'.");
    }

    private void Update() {
        HandleInteractions();
    }

    private void OnApplicationQuit() {
        if (BlockInteractionThread.IsAlive) {
            BlockInteractionThread.Abort();
            Debug.Log("(HandleBlockInteractionThread) - Aborting thread execution...");
        }
    }
    #endregion


    private void HandleInteractions() {
        if (Input.GetMouseButtonDown(0) && !placeBlockRequested) {
            placeBlockRequested = true;
        }
        else if (Input.GetMouseButtonDown(1) && !removeBlockRequested) {
            removeBlockRequested = true;
        }
    }

    private void HandleBlockInteractionThread() {
        while (true) {
            if (placeBlockRequested) {
                PlaceBlock();
                placeBlockRequested = false;
            }
            if (removeBlockRequested) {
                RemoveBlock();
                removeBlockRequested = false;
            }
        }
    }

    private void PlaceBlock() {
        ChunkData.Chunk targetChunk = null;
        float rayIncrementStep = RAY_MAX_LENGTH / RAY_STEP;
        float currentRayLength = 0f;
        Vector3 startPosition = thisPlayer.PlayerPosition;
        Vector3 forwardPosition = thisPlayer.PlayerPositionForward;

        do {

            // Update the current ray point.
            Vector3 currentRayPoint = startPosition + (forwardPosition * currentRayLength);

            // Try set the target chunk.
            targetChunk = ChunkDataUtils.GetChunkWhichContainsPosition(currentRayPoint);

            // Only executes when the target chunk were found.
            if (targetChunk != null) {

                // If the chunk is ready to be accessed.
                if (ChunkDataUtils.CanAccessChunkData(targetChunk)) {

                    // Transform the ray to local space inside the chunk.
                    currentRayPoint -= targetChunk.chunkCoord.CoordToVector3Int();

                    Debug.DrawRay(startPosition, forwardPosition * currentRayLength, Color.red, 25f);

                    // Get the block from the chunk in the current raycast point.
                    BlockData.Block raycastedBlock = ChunkDataUtils.GetBlockFromChunk(targetChunk, currentRayPoint, false);

                    // If the raycasted block is a solid block, then proceed.
                    if (ChunkDataUtils.IsBlockSolid(raycastedBlock)) {

                        Debug.Log($"(PlayerInteraction) - Interacting with block: {raycastedBlock.blockType}, " +
                            $"/ Chunk: {targetChunk.chunkCoord.CoordToVector3Int()}");

                        // Decrement the current ray lenght by ray step, to get the position to place the block.
                        currentRayLength -= rayIncrementStep * 3;
                        currentRayPoint = startPosition + (forwardPosition * currentRayLength);

                        Debug.DrawRay(startPosition, forwardPosition * currentRayLength, Color.blue, 25f);

                        // Update the current chunk again to ensure the new position is inside of it.
                        targetChunk = ChunkDataUtils.GetChunkWhichContainsPosition(currentRayPoint);

                        if (targetChunk == null) {
                            Debug.LogWarning($"(PlayerInteraction) - Position out target chunk, returning...");
                            return;
                        }

                        if (!ChunkDataUtils.CanAccessChunkData(targetChunk)) {
                            Debug.LogWarning("$(PlayerInteraction) - Target chunk not ready to access, returning...");
                            return;
                        }

                        // Transform the ray to local space inside the chunk.
                        currentRayPoint -= targetChunk.chunkCoord.CoordToVector3Int();

                        // Get the block from the position which want to place a new block.
                        BlockData.Block blockToPlace = ChunkDataUtils.GetBlockFromChunk(targetChunk, currentRayPoint, true);

                        // If is this block a non-solid block.
                        if (!ChunkDataUtils.IsBlockSolid(blockToPlace)) {

                            Debug.Log("(PlayerInteraction) - Placing block...");

                            // Update this block instance inside the chunk.
                            ChunkDataUtils.UpdateBlockInChunk(targetChunk, currentRayPoint);
                            World.Instance.UpdateModifiedChunkMesh(targetChunk);

                        }

                        return;
                    }

                }
            }

            // While no solid block, keep incrementing the ray lenght for the next iteration.
            currentRayLength += rayIncrementStep;


        } while (currentRayLength < RAY_MAX_LENGTH);

        if (targetChunk == null) { Debug.LogWarning("Impossible to find target chunk..."); }
    }

    private void RemoveBlock() {

    }

}
