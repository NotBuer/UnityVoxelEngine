using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockData {

    public const byte BLOCK_VERTICES_PER_FACES = 4;
    public const byte BLOCK_FACES = 6;
    public const byte BLOCK_SCALE = 1;
    public const float BLOCK_MAGNITUDE_1 = BLOCK_SCALE * 0.5f;


    public enum BlockTypes : byte {
        Air,
        Dirt,
        Grass,
        Stone,
        Sand,
        Water
    }


    // Basically the same direction order in the jagged arrays 'faceTriangles'.
    public enum BlockFaceDirection : byte { North, East, South, West, Top, Bottom }


    public struct BlockCoord : IEquatable<BlockCoord> {
        public short x;
        public short y;
        public short z;

        public BlockCoord(short x, short y, short z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Int CoordToVector3Int() => new Vector3Int(x, y, z);

        public static BlockCoord Vector3IntToCoord(Vector3Int vector) =>
            new BlockCoord((short) vector.x, (short) vector.y, (short) vector.z);

        public bool Equals(BlockCoord other) =>
            this == other;

        public override bool Equals(object other) {
            if (!(other is BlockCoord)) return false;

            return Equals((BlockCoord) other);
        }

        public override int GetHashCode() {
            int yHash = y.GetHashCode();
            int zHash = z.GetHashCode();
            return x.GetHashCode() ^ (yHash << 4) ^ (yHash >> 28) ^ (zHash >> 4) ^ (zHash << 28);
        }

        public static BlockCoord operator +(BlockCoord a, BlockCoord b) =>
            new BlockCoord(a.x += b.x, a.y += b.y, a.z += b.z);

        public static bool operator ==(BlockCoord lhs, BlockCoord rhs) =>
            lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;

        public static bool operator !=(BlockCoord lhs, BlockCoord rhs) =>
            !(lhs == rhs);

        public static BlockCoord Forward { get => forward; }
        public static BlockCoord Right { get => right; }
        public static BlockCoord Backward { get => backward; }
        public static BlockCoord Left { get => left; }
        public static BlockCoord Top { get => top; }
        public static BlockCoord Bottom { get => bottom; }

        private static readonly BlockCoord forward = new BlockCoord(0, 0, 1);
        private static readonly BlockCoord right = new BlockCoord(1, 0, 0);
        private static readonly BlockCoord backward = new BlockCoord(0, 0, -1);
        private static readonly BlockCoord left = new BlockCoord(-1, 0, 0);
        private static readonly BlockCoord top = new BlockCoord(0, 1, 0);
        private static readonly BlockCoord bottom = new BlockCoord(0, -1, 0);
    }


    public struct Block {

        public BlockTypes blockType;
        public bool isSolid;

        public Block(BlockTypes blockType, bool isSolid) {
            this.blockType = blockType;
            this.isSolid = isSolid;
        }

        public List<Vector3> GetBlockVerticesPerFace(List<BlockFaceDirection> facesDirections) {
            if (facesDirections != null) {
                List<Vector3> verticesList = new List<Vector3>();
                for (int i = 0; i < facesDirections.Count; i++) {
                    // Switch using the same Direction enum order.
                    switch (facesDirections[i]) {
                        case BlockFaceDirection.North: AddFaceVertices(BlockFaceDirection.North, ref verticesList); break;
                        case BlockFaceDirection.East: AddFaceVertices(BlockFaceDirection.East, ref verticesList); break;
                        case BlockFaceDirection.South: AddFaceVertices(BlockFaceDirection.South, ref verticesList); break;
                        case BlockFaceDirection.West: AddFaceVertices(BlockFaceDirection.West, ref verticesList); break;
                        case BlockFaceDirection.Top: AddFaceVertices(BlockFaceDirection.Top, ref verticesList); break;
                        case BlockFaceDirection.Bottom: AddFaceVertices(BlockFaceDirection.Bottom, ref verticesList); break;
                    }
                }
                return verticesList;
            }

            Debug.LogError("GetBlockVerticesPerFace() - Returning a null List<Vector3>...");
            return null;
        }

        private void AddFaceVertices(BlockFaceDirection dir, ref List<Vector3> verticesList) {
            Vector3[] verticesVectors = BlockDataUtils.GetFaceVertices(dir, BLOCK_MAGNITUDE_1);
            for (int i = 0; i < verticesVectors.Length; i++) {
                verticesList.Add(verticesVectors[i]);
            }
        }

        public static Block AirBlock { get => airBlock; }

        private static readonly Block airBlock = new Block(BlockTypes.Air, false);

    }

}

public static class BlockDataUtils {

    public static Vector3[] GetFaceVertices(BlockData.BlockFaceDirection dir, float scale) {
        Vector3[] faceVertices = new Vector3[BlockData.BLOCK_VERTICES_PER_FACES];
        for (int i = 0; i < faceVertices.Length; i++) {
            // The face triangle arrays are indices into the vertex arrays
            faceVertices[i] = vertices[faceTriangles[(int) dir][i]] * scale;
        }
        return faceVertices;
    }

    // Vertices array where the triangles will be indexes to indicate which vertices to use.
    private static readonly Vector3[] vertices = { 
            // Z+ , north face.
            new Vector3(1, 1, 1),
            new Vector3(-1, 1, 1),
            new Vector3(-1, -1, 1),
            new Vector3(1, -1, 1),

            // Z-, south face.
            new Vector3(-1, 1, -1),
            new Vector3(1, 1, -1),
            new Vector3(1, -1, -1),
            new Vector3(-1, -1, -1)
        };

    // Basically the same direction order in the enum 'BlockDirection'.
    // Jagged array to handle int[array][element]
    private static readonly int[][] faceTriangles = {
            new int[]{ 0, 1, 2, 3 }, // North Face.
            new int[]{ 5, 0, 3, 6 }, // East Face.
            new int[]{ 4, 5, 6, 7 }, // South Face.
            new int[]{ 1, 4, 7, 2 }, // West Face.
            new int[]{ 5, 4, 1, 0 }, // Top Face.
            new int[]{ 3, 2, 7, 6 }, // Bottom Face.
        };

    public static Vector2Int TexturePosition(BlockData.BlockFaceDirection direction, BlockData.BlockTypes blockType) {
        return direction switch {
            BlockData.BlockFaceDirection.Top => BlockDataManager.blockDataDictionary[blockType].upsideUV,
            BlockData.BlockFaceDirection.Bottom => BlockDataManager.blockDataDictionary[blockType].downsideUV,
            _ => BlockDataManager.blockDataDictionary[blockType].sidesUV
        };
    }

    public static Vector2[] FaceUVs(BlockData.BlockFaceDirection direction, BlockData.BlockTypes blockType) {
        Vector2[] UVs = new Vector2[4];
        Vector2Int tilePos = TexturePosition(direction, blockType);

        // Top Right.
        UVs[3] = new Vector2(BlockDataManager.tileSizeX * tilePos.x + BlockDataManager.tileSizeX - BlockDataManager.textureOffset,
            BlockDataManager.tileSizeY * tilePos.y + BlockDataManager.textureOffset);

        // Top Left.
        UVs[0] = new Vector2(BlockDataManager.tileSizeX * tilePos.x + BlockDataManager.tileSizeX - BlockDataManager.textureOffset,
            BlockDataManager.tileSizeY * tilePos.y + BlockDataManager.tileSizeY - BlockDataManager.textureOffset);

        // Bottom Left.
        UVs[1] = new Vector2(BlockDataManager.tileSizeX * tilePos.x + BlockDataManager.textureOffset,
            BlockDataManager.tileSizeY * tilePos.y + BlockDataManager.tileSizeY - BlockDataManager.textureOffset);

        // Bottom Right.
        UVs[2] = new Vector2(BlockDataManager.tileSizeX * tilePos.x + BlockDataManager.textureOffset,
            BlockDataManager.tileSizeY * tilePos.y + BlockDataManager.textureOffset);

        return UVs;
    }

    public static bool IsBlockInThisPosition(BlockData.BlockCoord blockCoord, Vector3 position) {
        // Offset the start by -0.5f.
        float blockStartX = blockCoord.x - 0.5f;
        float blockStartY = blockCoord.y - 0.5f;
        float blockStartZ = blockCoord.z - 0.5f;

        // Offset the end by +1 from the start.
        float blockEndX = blockStartX + 1;
        float blockEndY = blockStartY + 1;
        float blockEndZ = blockStartZ + 1;

        //if (position.x >= blockStartX && position.x <= blockEndX &&
        //    position.y >= blockStartY && position.y <= blockEndY &&
        //    position.z >= blockStartZ && position.z <= blockEndZ) {
        //    Debug.Log($"Position: {position} is inside block boundaries - startX: {blockStartX}, endX: {blockEndX} / " +
        //        $"startY: {blockStartY}, endY: {blockEndY} / " +
        //        $"startZ: {blockStartZ}, endZ: {blockEndZ}");
        //}

        return position.x >= blockStartX && position.x <= blockEndX &&
            position.y >= blockStartY && position.y <= blockEndY &&
            position.z >= blockStartZ && position.z <= blockEndZ;
    }

}
