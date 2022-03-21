using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebuggingCanvas : MonoBehaviour {

    public static DebuggingCanvas Instance { get; private set; }


    [Header("World References")]
    [SerializeField] private TMP_Text worldChunksDictionaryTXT;
    [SerializeField] private TMP_Text activeChunksDictionaryTXT;
    [SerializeField] private TMP_Text chunksToBuildDataTXT;
    [SerializeField] private TMP_Text chunksToBuildInstanceTXT;
    [SerializeField] private TMP_Text chunksToBuildMeshDataTXT;
    [SerializeField] private TMP_Text chunksToUpdateMeshDataTXT;
    [SerializeField] private TMP_Text chunksToSkipUpdatingTXT;
    [SerializeField] private TMP_Text chunksReadyToRenderLoadQueueTXT;
    [SerializeField] private TMP_Text chunksReadyToRenderUpdateQueueTXT;
    [SerializeField] private TMP_Text chunksToUnloadTXT;

    [Header("Player References")]
    [SerializeField] private TMP_Text currentPositionTXT;
    [SerializeField] private TMP_Text currentChunkCoordTXT;


    private void Awake() {
        if (Instance == null) { Instance = this; } else { DestroyImmediate(gameObject); }
    }

    public void UpdateWorldReferences(int worldChunks, int activeChunks, int chunksToBuildData,int chunksToBuildInstance, int chunksToBuildMeshData, int chunksToUpdateMeshData, int chunksToSkipUpdating, int chunksReadyToRenderLoadQueue, int chunksReadyToRenderUpdateQueue, int chunksToUnload) {

        worldChunksDictionaryTXT.SetText("" + worldChunks);
        activeChunksDictionaryTXT.SetText("" + activeChunks);
        chunksToBuildDataTXT.SetText("" + chunksToBuildData);
        chunksToBuildInstanceTXT.SetText("" + chunksToBuildInstance);
        chunksToBuildMeshDataTXT.SetText("" + chunksToBuildMeshData);
        chunksToUpdateMeshDataTXT.SetText("" + chunksToUpdateMeshData);
        chunksToSkipUpdatingTXT.SetText("" + chunksToSkipUpdating);
        chunksReadyToRenderLoadQueueTXT.SetText("" + chunksReadyToRenderLoadQueue);
        chunksReadyToRenderUpdateQueueTXT.SetText("" + chunksReadyToRenderUpdateQueue);
        chunksToUnloadTXT.SetText("" + chunksToUnload);
    }

    public void UpdatePlayerReferences(Vector3 position, ChunkData.Chunk currentChunk) {
        currentPositionTXT.SetText($"X: {position.x:F2} / Y: {position.y:F2} / Z: {position.z:F2}");
        if (currentChunk != null) currentChunkCoordTXT.SetText($"ChunkX: {currentChunk.chunkCoord.coordX} / ChunkZ {currentChunk.chunkCoord.coordZ}");
    }

}
