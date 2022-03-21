using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DirectionExtension {

    public static Vector3Int GetBlockDirectionVector(BlockData.BlockFaceDirection faceDirection) {
        return faceDirection switch {
            BlockData.BlockFaceDirection.North => Vector3Int.forward,
            BlockData.BlockFaceDirection.East => Vector3Int.right,
            BlockData.BlockFaceDirection.South => Vector3Int.back,
            BlockData.BlockFaceDirection.West => Vector3Int.left,
            BlockData.BlockFaceDirection.Top => Vector3Int.up,
            BlockData.BlockFaceDirection.Bottom => Vector3Int.down,
            _ => throw new System.Exception("Invalid block direction vector!")
        };
    }

    public static BlockData.BlockCoord GetBlockDirection(BlockData.BlockFaceDirection faceDirection) {
        return faceDirection switch {
            BlockData.BlockFaceDirection.North => BlockData.BlockCoord.Forward,
            BlockData.BlockFaceDirection.East => BlockData.BlockCoord.Right,
            BlockData.BlockFaceDirection.South => BlockData.BlockCoord.Backward,
            BlockData.BlockFaceDirection.West => BlockData.BlockCoord.Left,
            BlockData.BlockFaceDirection.Top => BlockData.BlockCoord.Top,
            BlockData.BlockFaceDirection.Bottom => BlockData.BlockCoord.Bottom,
            _ => throw new System.Exception("Invalid block direction!")
        };
    }

    public static Vector3Int GetChunkDirectionVector(ChunkData.ChunkDirection chunkDirection) {
        return chunkDirection switch {
            ChunkData.ChunkDirection.North => new Vector3Int(0, 0, ChunkData.CHUNK_SIZE),
            ChunkData.ChunkDirection.East => new Vector3Int(ChunkData.CHUNK_SIZE, 0, 0),
            ChunkData.ChunkDirection.South => new Vector3Int(0, 0, -ChunkData.CHUNK_SIZE),
            ChunkData.ChunkDirection.West => new Vector3Int(-ChunkData.CHUNK_SIZE, 0, 0),
            _ => throw new System.Exception("Invalid chunk direction vector!")
        };
    }

}