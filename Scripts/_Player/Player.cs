using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    [Header("Player References")]
    [SerializeField] private PlayerInteraction playerInteraction;

    private bool isChunksRequestPending;
    private ChunkData.Chunk sourceChunk;
    private ChunkData.Chunk currentChunk;
    private bool isFarEnoughFromSourceChunk;
    private Vector3 playerPosition;
    private Vector3 playerPositionForward;

    public Vector3 PlayerPosition { get => playerPosition; }
    public Vector3 PlayerPositionForward { get => playerPositionForward; }
    public ChunkData.Chunk SourceChunk { get => sourceChunk; set => sourceChunk = value; }
    public bool IsPlayerChunkRequestPending { get => isChunksRequestPending; set => isChunksRequestPending = value; }
    public bool IsPlayerFarEnoughFromSourceChunk { get => isFarEnoughFromSourceChunk; set => isFarEnoughFromSourceChunk = value; }


    #region MONOBEHAVIOUR
    private void Awake() {
        sourceChunk = null;
        isChunksRequestPending = false;
    }

    private void Start() {
        playerInteraction = GetComponent<PlayerInteraction>();
        World.OnWorldGenerated += World_OnWorldGenerated;
    }

    private void Update() {
        playerPosition = transform.position;
        playerPositionForward = transform.forward;
        DebuggingCanvas.Instance.UpdatePlayerReferences(playerPosition, currentChunk);
    }

    private void OnDestroy() {
        World.OnWorldGenerated -= World_OnWorldGenerated;
    }
    #endregion


    #region PLAYER_POSITION_DETECTION
    private IEnumerator CheckPlayerPositionFromSourceChunk() {
        while (true) {

            // Wait if is the request pending.
            yield return new WaitWhile(() => isChunksRequestPending);

            int generationThreshold = World.Instance.DstThresholdFromCenterPointToGenerateNewChunks;
            Vector3 playerPosNoY = new Vector3(playerPosition.x, 0, playerPosition.z);
            Vector3 chunkCoord = sourceChunk.chunkCoord.CoordToVector3Int();
            Vector3 chunkCoordNoY = new Vector3(chunkCoord.x, 0, chunkCoord.z);
            Vector3 distance = playerPosNoY - chunkCoordNoY;
            float distanceLenght = distance.sqrMagnitude;

            // Is this offset magnitude greater than (threshold + chunksize) magnitude? 
            if (distanceLenght >= (generationThreshold * generationThreshold)) {
                isFarEnoughFromSourceChunk = true;
            }
            else { 
                isFarEnoughFromSourceChunk = false;
                //    Debug.Log("Inside chunk generation threshold: " + distanceLenght + " - Limit Size: " + ((generationThreshold * generationThreshold)/* + (ChunkData.CHUNK_SIZE * ChunkData.CHUNK_SIZE)*/));
            }

            yield return new WaitForEndOfFrame();

        }
    }

    private IEnumerator CheckInWhichChunkThePlayerAre() {
        while (true) {

            // If the current chunk does not contains the position the player are in anymore, find the one which contains.

            if (!ChunkDataUtils.IsChunkInThisPosition(playerPosition, currentChunk)) {
                currentChunk = ChunkDataUtils.GetChunkWhichContainsPosition(playerPosition);
            }

            yield return new WaitForEndOfFrame();
            
        }
    }
    #endregion

    
    #region EVENTS
    private void World_OnWorldGenerated(object sender, World.WorldGeneratedArgs args) {
        Vector3 chunkMiddle = args.spawnChunk.chunkCoord.GetChunkMiddlePointVector3();
        Vector3Int desiredXZ = new Vector3Int(Mathf.RoundToInt(chunkMiddle.x), 0, Mathf.RoundToInt(chunkMiddle.z));
        Vector3 position = new Vector3(desiredXZ.x,
            ChunkDataUtils.GetChunkSurfaceBlockYPosition(args.spawnChunk, desiredXZ), 
            desiredXZ.z);
        transform.position = position;
        sourceChunk = args.spawnChunk;
        currentChunk = args.spawnChunk;
        StartCoroutine(CheckPlayerPositionFromSourceChunk());
        StartCoroutine(CheckInWhichChunkThePlayerAre());
    }
    #endregion

}
