using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;

public class ChunkData {

    public const byte CHUNK_SIZE = 16;
    public const short CHUNK_HEIGHT = 256;
    public const byte CHUNK_DIRECTIONS_COUNT = 4;


    public enum ChunkDirection : byte { North, East, South, West }


    private interface IChunkMeshDataShared {
        public int VertexIndex { get; set; }
        public List<Vector3> VerticesList { get; set; }
        public List<int> TrianglesList { get; set; }
        public List<Vector2> UVSList { get; set; }
        void ClearAllChunkMeshData(bool isMeshDataReady, bool isRendered);
        void DestroyAllChunkMeshData();
    }


    public struct ChunkMeshData : IChunkMeshDataShared {
        public bool isMeshDataReady;
        public bool isRendered;
        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Vector2> uvs;
        private int vertexIndex;
        public ChunkWaterMeshData waterMesh;

        public int VertexIndex { get { return vertexIndex; } set { vertexIndex = value; } }
        public List<Vector3> VerticesList { get { return vertices; } set { vertices = value; } }
        public List<int> TrianglesList { get { return triangles; } set { triangles = value; } }
        public List<Vector2> UVSList { get { return uvs; } set { uvs = value; } }

        public ChunkMeshData(bool isMeshDataReady, bool isRendered) {
            this.isMeshDataReady = isMeshDataReady;
            this.isRendered = isRendered;
            vertices = new List<Vector3>();
            triangles = new List<int>();
            uvs = new List<Vector2>();
            vertexIndex = 0;
            waterMesh = new ChunkWaterMeshData(new List<Vector3>(), new List<int>(), new List<Vector2>());
        }

        public void ClearAllChunkMeshData(bool isMeshDataReady, bool isRendered) {
            this.isMeshDataReady = isMeshDataReady;
            this.isRendered = isRendered;
            vertices.Clear();
            vertices.TrimExcess();
            triangles.Clear();
            triangles.TrimExcess();
            uvs.Clear();
            uvs.TrimExcess();
            vertexIndex = 0;
            waterMesh.ClearAllChunkMeshData(isMeshDataReady, isRendered);
        }

        public void DestroyAllChunkMeshData() {
            vertices = null;
            triangles = null;
            uvs = null;
            waterMesh.DestroyAllChunkMeshData();
        }
    }


    public struct ChunkWaterMeshData : IChunkMeshDataShared {
        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Vector2> uvs;
        private int vertexIndex;

        public int VertexIndex { get { return vertexIndex; } set { vertexIndex = value; } }
        public List<Vector3> VerticesList { get { return vertices; } set { vertices = value; } }
        public List<int> TrianglesList { get { return triangles; } set { triangles = value; } }
        public List<Vector2> UVSList { get { return uvs; } set { uvs = value; } }

        public ChunkWaterMeshData(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs) {
            this.vertices = vertices;
            this.triangles = triangles;
            this.uvs = uvs;
            vertexIndex = 0;
        }

        public void ClearAllChunkMeshData(bool isMeshDataReady, bool isRendered) {
            vertices.Clear();
            vertices.TrimExcess();
            triangles.Clear();
            triangles.TrimExcess();
            uvs.Clear();
            uvs.TrimExcess();
            vertexIndex = 0;
        }

        public void DestroyAllChunkMeshData() {
            vertices = null;
            triangles = null;
            uvs = null;
        }
    }


    public struct ChunkCoord : IEquatable<ChunkCoord> {
        public int coordX;
        public int coordZ;

        public ChunkCoord(int coordX, int coordZ) {
            // World space coordinates.
            this.coordX = coordX;
            this.coordZ = coordZ;
        }

        public HashSet<ChunkCoord> PopulateNeighborChunksCoords() {
            Vector3Int chunkCoordVector3Int = CoordToVector3Int();
            HashSet<ChunkCoord> neighborCoords = new HashSet<ChunkCoord>(CHUNK_DIRECTIONS_COUNT);
            for (int i = 0; i < CHUNK_DIRECTIONS_COUNT; i++) {
                Vector3Int neighborPosition = chunkCoordVector3Int + DirectionExtension.GetChunkDirectionVector((ChunkDirection) i);
                neighborCoords.Add(new ChunkCoord(neighborPosition.x, neighborPosition.z));
            }
            return neighborCoords;
        }

        public Vector3Int CoordToVector3Int() => new Vector3Int(coordX, 0, coordZ);

        public static ChunkCoord Vector3IntToCoord(Vector3Int position) => new ChunkCoord(position.x, position.z);

        public Vector3 GetChunkMiddlePointVector3() {
            float xPos = CHUNK_SIZE / 2;
            float zPos = CHUNK_SIZE / 2;
            if (coordX > 0) { xPos = coordX / 2 + CHUNK_SIZE; }
            if (coordZ > 0) { zPos = coordZ / 2 + CHUNK_SIZE; }
            if (coordX < 0) { xPos = coordX / 2; }
            if (coordZ < 0) { zPos = coordZ / 2; }
            return new Vector3(xPos, 0, zPos);
        }

        public bool Equals(ChunkCoord other) => this == other;

        public override bool Equals(object other) {
            if (!(other is ChunkCoord)) return false;

            return Equals((ChunkCoord) other);
        }

        public override int GetHashCode() {
            int zHash = coordZ.GetHashCode();
            return coordX.GetHashCode() ^ (zHash >> 4) ^ (zHash << 28);
        }

        public static bool operator ==(ChunkCoord lhs, ChunkCoord rhs) =>
            lhs.coordX == rhs.coordX && lhs.coordZ == rhs.coordZ;

        public static bool operator !=(ChunkCoord lhs, ChunkCoord rhs) =>
            !(lhs == rhs);
    }


    public class Chunk {
        public GameObject chunkObject;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        public Dictionary<BlockData.BlockCoord, BlockData.Block> chunkBlocksDictionary; // Local space keys.
        public ChunkCoord chunkCoord;
        public ChunkMeshData chunkMeshData;
        private HashSet<ChunkCoord> neighborChunksCoords; // World space coords.
        public bool isChunkDataReady;

        public Chunk(ChunkCoord chunkCoord) {
            this.chunkCoord = chunkCoord;

            chunkObject = new GameObject($"Chunk: X: '{chunkCoord.coordX}', Z: '{chunkCoord.coordZ}'");
            chunkObject.transform.position = chunkCoord.CoordToVector3Int();
            SetMeshComponents();

            chunkBlocksDictionary = new Dictionary<BlockData.BlockCoord, BlockData.Block>(CHUNK_SIZE * CHUNK_HEIGHT * CHUNK_SIZE);
            chunkMeshData = new ChunkMeshData(false, false);
            neighborChunksCoords = chunkCoord.PopulateNeighborChunksCoords();
            isChunkDataReady = false;
        }

        public void DestroyChunk() {
            UnityEngine.Object.Destroy(meshFilter.sharedMesh);
            UnityEngine.Object.Destroy(meshFilter);
            UnityEngine.Object.Destroy(meshRenderer);
            UnityEngine.Object.Destroy(chunkObject);
            chunkBlocksDictionary = null;
            neighborChunksCoords = null;
            chunkMeshData.DestroyAllChunkMeshData();
        }

        private void SetMeshComponents() {
            meshFilter = chunkObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = null;

            meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            meshRenderer.materials = new Material[2] { World.Instance.worldMaterial, World.Instance.worldMaterialTransparent };
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.staticShadowCaster = true;
        }

        public async Task BuildChunkBlocksData(CancellationTokenSource tokenSource) {
            await Task.Run(() => {

                for (short z = 0; z < CHUNK_SIZE; z++) {
                    for (short y = 0; y < CHUNK_HEIGHT; y++) {
                        for (short x = 0; x < CHUNK_SIZE; x++) {

                            int worldX = x + chunkCoord.coordX;
                            int worldY = y;
                            int worldZ = z + chunkCoord.coordZ;

                            // ** Old perlin implementation **
                            //int terrainHeight = NoiseMap.GetNoiseAt (worldX,worldZ,world.noiseScale,CHUNK_HEIGHT,world.octaves,world.persistance,world.lacunarity);

                            // ** New perlin implementation **
                            World.Instance.noiseSettings.worldOffset = World.Instance.mapSeedOffset;
                            float height = NoiseMap.OctavePerlin(worldX, worldZ, World.Instance.noiseSettings);
                            height = NoiseMap.Redistribution(height, World.Instance.noiseSettings);
                            int surfaceHeight = NoiseMap.RemapValue01ToInt(height, 0, CHUNK_HEIGHT);

                            BlockData.BlockTypes blockType = BlockData.BlockTypes.Air;

                            if (worldY > surfaceHeight) {
                                if (worldY < World.Instance.seaLayer) {
                                    blockType = BlockData.BlockTypes.Water;
                                }
                                else {
                                    blockType = BlockData.BlockTypes.Air;
                                }
                            }
                            // Underwater Layer.
                            else if (worldY == surfaceHeight && worldY < World.Instance.seaLayer) {
                                blockType = BlockData.BlockTypes.Sand;
                            }
                            // Ground Layer.
                            else if (worldY == surfaceHeight) {
                                blockType = BlockData.BlockTypes.Grass;
                            }
                            // Dirt layer.
                            else if (worldY == surfaceHeight - 1) {
                                blockType = BlockData.BlockTypes.Dirt;
                            }
                            // Stone Layer.
                            else if (worldY < surfaceHeight - 1) {
                                blockType = BlockData.BlockTypes.Stone;
                            }

                            if (BlockDataManager.blockDataDictionary.TryGetValue(blockType, out BlockDataDB blockDb)) {
                                chunkBlocksDictionary.Add(new BlockData.BlockCoord(x, y, z), new BlockData.Block(blockType, blockDb.isSolid));
                            }
                            else {
                                Debug.LogError($"BuildChunkData() - Impossible to find BlockDataDB with the block type: '{blockType}', " +
                                    $"chunk data completely broken!");
                                chunkBlocksDictionary.Add(new BlockData.BlockCoord(x, y, z), new BlockData.Block(BlockData.BlockTypes.Air, false));
                            }

                        }
                    }
                }

                isChunkDataReady = true;

            }, tokenSource.Token);
            
        }

        public async Task BuildChunkMeshDataAsync(CancellationTokenSource tokenSource) {
            await Task.Run(() => {

                ChunkMeshData newChunkMeshData = chunkMeshData;
                newChunkMeshData.ClearAllChunkMeshData(false, false);

                // Cache the chunk coord as vector3int to use many times.
                Vector3Int chunkCoordVector3Int = chunkCoord.CoordToVector3Int();

                // Loop through each block in this chunk and use their data to set the chunk mesh later on.
                foreach (KeyValuePair<BlockData.BlockCoord, BlockData.Block> chunkBlock in chunkBlocksDictionary) {

                    BlockData.BlockCoord blockCoord = chunkBlock.Key;
                    BlockData.Block thisBlock = chunkBlock.Value;
                    Vector3Int blockCoordVector3Int = blockCoord.CoordToVector3Int();

                    if (thisBlock.blockType == BlockData.BlockTypes.Air) { continue; }

                    HashSet<BlockData.BlockFaceDirection> facesDirectionsToBuild = new HashSet<BlockData.BlockFaceDirection>(BlockData.BLOCK_FACES);
                    int neighborsCount = 0;

                    for (int faceIndex = 0; faceIndex < BlockData.BLOCK_FACES; faceIndex++) {
                        // If no neighbor block adjascent face to this block position faces.
                        if (!FindNeighborBlock((BlockData.BlockFaceDirection) faceIndex, blockCoord, thisBlock)) {
                            facesDirectionsToBuild.Add((BlockData.BlockFaceDirection) faceIndex);
                        }
                        else { neighborsCount++; }
                    }

                    // Means is that block surrounded by neighbor blocks and can be culled.
                    if (neighborsCount == BlockData.BLOCK_FACES) { continue; }

                    // Otherwise read the facesDirectionToDraw list to create faces where neighbors was not found.
                    else {

                        List<Vector3> blockVertices;

                        //If is a chunk edge block, go check and remove the faces if needs to.
                        if (ChunkEdgeBlockClearFaces(blockCoord, ref facesDirectionsToBuild, chunkCoordVector3Int)) {
                            blockVertices = thisBlock.GetBlockVerticesPerFace(facesDirectionsToBuild.ToList());
                        }
                        // Otherwise will skip the face removing process and just gonna find the vertices.
                        else {
                            blockVertices = thisBlock.GetBlockVerticesPerFace(facesDirectionsToBuild.ToList());
                        }

                        // If no vertices in this block, skip to the next iteration.
                        if (blockVertices.Count == 0) { continue; }

                        // Grab the water chunk mesh data apart.
                        if (thisBlock.blockType == BlockData.BlockTypes.Water) {

                            // Get the vertex position in world space coordinates.
                            for (int vertex = 0; vertex < blockVertices.Count; vertex++) {
                                Vector3 worldSpacePosition = chunkCoordVector3Int + blockCoordVector3Int;
                                newChunkMeshData.waterMesh.VerticesList.Add(blockVertices[vertex] + worldSpacePosition);
                            }

                            // Do it many times as visible faces amount that block have.
                            for (int f = 0; f < facesDirectionsToBuild.Count; f++) {
                                // Build the triangles on a clockwise direction order (0,1,2 - 0,2,3).
                                newChunkMeshData.waterMesh.TrianglesList.Add(newChunkMeshData.waterMesh.VertexIndex);     // 0
                                newChunkMeshData.waterMesh.TrianglesList.Add(newChunkMeshData.waterMesh.VertexIndex + 1); // 1
                                newChunkMeshData.waterMesh.TrianglesList.Add(newChunkMeshData.waterMesh.VertexIndex + 2); // 2
                                newChunkMeshData.waterMesh.TrianglesList.Add(newChunkMeshData.waterMesh.VertexIndex);     // 0
                                newChunkMeshData.waterMesh.TrianglesList.Add(newChunkMeshData.waterMesh.VertexIndex + 2); // 2
                                newChunkMeshData.waterMesh.TrianglesList.Add(newChunkMeshData.waterMesh.VertexIndex + 3); // 3 

                                // Increment vertex index by the amount of vertices a face have.
                                newChunkMeshData.waterMesh.VertexIndex += BlockData.BLOCK_VERTICES_PER_FACES;
                            }

                            // Get the block face UVs coordinates for each face.
                            newChunkMeshData.waterMesh.UVSList.AddRange(GetChunkUVCoordinates(facesDirectionsToBuild, thisBlock.blockType));

                            continue;
                        }

                        // Get the vertex position in world space coordinates.
                        for (int vertex = 0; vertex < blockVertices.Count; vertex++) {
                            Vector3 worldSpacePosition = chunkCoordVector3Int + blockCoordVector3Int;
                            newChunkMeshData.VerticesList.Add(blockVertices[vertex] + worldSpacePosition);
                        }

                        // Do it many times as visible faces amount that block have.
                        for (int f = 0; f < facesDirectionsToBuild.Count; f++) {
                            // Build the triangles on a clockwise direction order (0,1,2 - 0,2,3).
                            newChunkMeshData.TrianglesList.Add(newChunkMeshData.VertexIndex);     // 0
                            newChunkMeshData.TrianglesList.Add(newChunkMeshData.VertexIndex + 1); // 1
                            newChunkMeshData.TrianglesList.Add(newChunkMeshData.VertexIndex + 2); // 2
                            newChunkMeshData.TrianglesList.Add(newChunkMeshData.VertexIndex);     // 0
                            newChunkMeshData.TrianglesList.Add(newChunkMeshData.VertexIndex + 2); // 2
                            newChunkMeshData.TrianglesList.Add(newChunkMeshData.VertexIndex + 3); // 3

                            // Increment vertex index by the amount of vertices a face have.
                            newChunkMeshData.VertexIndex += BlockData.BLOCK_VERTICES_PER_FACES;
                        }

                        // Get the block face UVs coordinates for each face.
                        newChunkMeshData.UVSList.AddRange(GetChunkUVCoordinates(facesDirectionsToBuild, thisBlock.blockType));
                    }

                }

                // Get the chunk mesh vertex positions back in local space coordinates.
                for (int v = 0; v < newChunkMeshData.VerticesList.Count; v++) {
                    Vector3 localSpacePosition = newChunkMeshData.VerticesList[v] - chunkCoordVector3Int;
                    newChunkMeshData.VerticesList[v] = localSpacePosition;
                }

                // Get5 the chunk mesh water mesh vertex position back in local space coordinates.
                for (int wv = 0; wv < newChunkMeshData.waterMesh.VerticesList.Count; wv++) {
                    Vector3 localSpacePosition = newChunkMeshData.waterMesh.VerticesList[wv] - chunkCoordVector3Int;
                    newChunkMeshData.waterMesh.VerticesList[wv] = localSpacePosition;
                }

                // Set the new chunk mesh data.
                newChunkMeshData.isMeshDataReady = true;
                chunkMeshData = newChunkMeshData;

            }, tokenSource.Token);
        }

        public void BuildChunkMesh() {
            // Set the new mesh data.
            Mesh chunkMesh = new Mesh();
            chunkMesh.subMeshCount = 2;
            chunkMesh.SetVertices(chunkMeshData.VerticesList.Concat(chunkMeshData.waterMesh.VerticesList).ToArray());
            chunkMesh.SetTriangles(chunkMeshData.TrianglesList.ToArray(), 0);
            chunkMesh.SetTriangles(chunkMeshData.waterMesh.TrianglesList.Select(value => value + chunkMeshData.VerticesList.Count).ToArray(), 1);
            chunkMesh.SetUVs(0, chunkMeshData.UVSList.Concat(chunkMeshData.waterMesh.UVSList).ToArray());
            chunkMesh.RecalculateNormals();
            chunkMesh.RecalculateBounds();
            chunkMesh.Optimize();
            meshFilter.sharedMesh = chunkMesh;

            // Update the chunk mesh data.
            ChunkMeshData updatedChunkMeshData = chunkMeshData;
            updatedChunkMeshData.isRendered = true;
            updatedChunkMeshData.ClearAllChunkMeshData(true, true);
            chunkMeshData = updatedChunkMeshData;
        }

        private bool FindNeighborBlock(BlockData.BlockFaceDirection faceDirection, BlockData.BlockCoord blockCoord, BlockData.Block mainBlock) {
            // Logic:
            // Will look for neighbor blocks towards the face direction from the provided position,
            // and will return false if no neighbor block found matching certain conditions,
            // otherwise will return true.

            /// <param name="faceDirection"> The face direction to check for neighbor blocks. </param>
            /// <param name="pos"> The current block position to check the neighbors from. </param>
            /// <param name="mainBlock"> The current main block which is used as the middle point, and used to search around it for neighbor blocks. </param>

            BlockData.BlockCoord blockDirectionCoord = blockCoord + DirectionExtension.GetBlockDirection(faceDirection);

            // If this returns false, that means this block face position is out of this chunk boundaries.
            if (!IsBlockInsideChunkBoundaries(blockDirectionCoord)) { return false; }

            chunkBlocksDictionary.TryGetValue(blockDirectionCoord, out BlockData.Block facingBlock);

            // Is the main block non-solid? (is a fluid).
            if (!ChunkDataUtils.IsBlockSolid(mainBlock)) {

                // Is the facing block facing a solid block (non-fluid).
                if (ChunkDataUtils.IsBlockSolid(facingBlock)) { return true; }

                // Is the facing block facing the non-solid AIR block?
                if (facingBlock.blockType == BlockData.BlockTypes.Air) { return false; }

                // Otherwise is the facing block a fluid block.
                // Dont mark as a neighbor in order to cull this face, because dont need to build faces in the same fluid blocks adjacent to each other.
                else { return true; }
            }

            // Otherwise the main block is solid (non-fluid).
            else {

                // Is the facing block not solid? (is a fluid).
                if (!ChunkDataUtils.IsBlockSolid(facingBlock)) { return false; }

                // Otherwise is the facing block a solid block.
                else { return true; }
            }

        }

        private bool IsBlockInsideChunkBoundaries(BlockData.BlockCoord blockCoord) {
            // Return true if is the condition, otherwise false (meaning the position is beyond the chunk boundaries).
            return blockCoord.x >= 0 && blockCoord.x <= CHUNK_SIZE - 1 &&
                    blockCoord.y >= 0 && blockCoord.y <= CHUNK_HEIGHT - 1 &&
                    blockCoord.z >= 0 && blockCoord.z <= CHUNK_SIZE - 1;
        }

        private bool ChunkEdgeBlockClearFaces(BlockData.BlockCoord blockCoord, ref HashSet<BlockData.BlockFaceDirection> faces, Vector3Int chunkCoordVector3Int) {

            // If this block isn't in the chunk edge position, do nothing.
            if (!IsBlockInChunkEdge(blockCoord)) { return false; }

            // North Z+.
            if (blockCoord.z == CHUNK_SIZE - 1) {
                // If a solid block found inside the neighbour chunk edge at the position.
                if (faces.Contains(BlockData.BlockFaceDirection.North)) {
                    if (CanRemoveNeighborChunkBlockFace(blockCoord + DirectionExtension.GetBlockDirection(BlockData.BlockFaceDirection.North), blockCoord, chunkCoordVector3Int)) {
                        faces.Remove(BlockData.BlockFaceDirection.North);
                    }
                }
            }

            // South Z-.
            if (blockCoord.z == 0) {
                // If a solid block found inside the neighbour chunk edge at the position.
                if (faces.Contains(BlockData.BlockFaceDirection.South)) {
                    if (CanRemoveNeighborChunkBlockFace(blockCoord + DirectionExtension.GetBlockDirection(BlockData.BlockFaceDirection.South), blockCoord, chunkCoordVector3Int)) {
                        faces.Remove(BlockData.BlockFaceDirection.South);
                    }
                }
            }

            // East X+.
            if (blockCoord.x == CHUNK_SIZE - 1) {
                // If a solid block found inside the neighbour chunk edge at the position.
                if (faces.Contains(BlockData.BlockFaceDirection.East)) {
                    if (CanRemoveNeighborChunkBlockFace(blockCoord + DirectionExtension.GetBlockDirection(BlockData.BlockFaceDirection.East), blockCoord, chunkCoordVector3Int)) {
                        faces.Remove(BlockData.BlockFaceDirection.East);
                    }
                }
            }

            // West X-.
            if (blockCoord.x == 0) {
                // If a solid block found inside the neighbour chunk edge at the position.
                if (faces.Contains(BlockData.BlockFaceDirection.West)) {
                    if (CanRemoveNeighborChunkBlockFace(blockCoord + DirectionExtension.GetBlockDirection(BlockData.BlockFaceDirection.West), blockCoord, chunkCoordVector3Int)) {
                        faces.Remove(BlockData.BlockFaceDirection.West);
                    }
                }
            }

            // Bottom Y-.
            if (blockCoord.y == 0) {
                // If a solid block found inside the neighbour chunk edge at the position.
                if (faces.Contains(BlockData.BlockFaceDirection.Bottom)) {
                    if (CanRemoveNeighborChunkBlockFace(blockCoord + DirectionExtension.GetBlockDirection(BlockData.BlockFaceDirection.Bottom), blockCoord, chunkCoordVector3Int)) {
                        faces.Remove(BlockData.BlockFaceDirection.Bottom);
                    }
                }
            }

            return true;
        }

        private bool CanRemoveNeighborChunkBlockFace(BlockData.BlockCoord neighborBlockCoord, BlockData.BlockCoord sourceBlockCoord, Vector3Int chunkCoordVector3Int) {
            // Logic:
            // Only apply the face remotion logic if this neighbor coord contains that neighbor block position (look for it in world space coordinates).
            // If no neighbor chunk around the current chunk (E.g: map borders), remove the faces which are facing towards that direction which has nothing beyond.
            // If the chunk block is facing a solid neighbor chunk block, remove the face.
            // If the chunk block is facing a non-solid neighbor chunk block, keep the face.
            // If the chunk block is facing a non-solid neighbor chunk block, check against the block in the source chunk and if it is solid keep the face, if is not remove the faces.

            foreach (ChunkCoord neighborCoord in neighborChunksCoords) {

                // Try get the chunk in this neighbor chunk coord (No need to implement a else condition to catch this).
                if (World.Instance.GetWorldChunksDictionary.TryGetValue(neighborCoord, out Chunk neighborChunk)) {

                    // Skip if the neighbor chunk does not exists.
                    if (neighborChunk == null) { continue; }

                    Vector3Int neighborBlockPosCacheVector3Int = neighborBlockCoord.CoordToVector3Int();

                    // Transform the neighbor block position in world space position.
                    neighborBlockPosCacheVector3Int += chunkCoordVector3Int;

                    // Then look if this neighbor block in world position belongs to this neighbor chunk coord range.
                    if (NeighborChunkContainsBlock(neighborBlockPosCacheVector3Int, neighborCoord)) {

                        // Transform back the neighbor block position in local space position.
                        neighborBlockPosCacheVector3Int -= chunkCoordVector3Int;

                        // Convert the block position to match the wanted position inside the neighbor chunk.
                        // (Remember: all chunk blocks positions were set in local position (E.g X: 0-15, Y: 0-255, Z: 0-15).
                        ConvertPositionAxisToNeighborChunkPosition(ref neighborBlockPosCacheVector3Int);

                        // Wait while is neighbor chunk data is not ready access.
                        while (!neighborChunk.isChunkDataReady) { }

                        BlockData.Block desiredBlock = neighborChunk.chunkBlocksDictionary[BlockData.BlockCoord.Vector3IntToCoord(neighborBlockPosCacheVector3Int)];
                        //BlockData.Block desiredBlock = neighborChunk.chunkBlocksDictionary[neighborBlockPosCacheVector3Int];

                        // Is this neighbor chunk block a fluid block? Then process this condition as an exception.
                        if (desiredBlock.blockType == BlockData.BlockTypes.Water) {

                            BlockData.Block sourceBlock = chunkBlocksDictionary[sourceBlockCoord];
                            //BlockData.Block sourceBlock = chunkBlocksDictionary[sourceBlockCoord.CoordToVector3Int()];

                            // If this source block in the source chunk is solid blocks? Then keep the faces.
                            if (ChunkDataUtils.IsBlockSolid(sourceBlock)) { return false; }

                            // Remove the faces by default if the condition above was not valid and the desired block is fluid block.
                            return true;
                        }

                        // Otherwise return the neighbor chunk block state (if is solid or not).
                        else {
                            return ChunkDataUtils.IsBlockSolid(desiredBlock);
                        }

                    }

                }

            }

            // Remove the faces if all neighbor chunks keys was checked, but no one value of them already exists
            // (E.g: Map boundaries where no chunks around loaded yet).
            return true;
        }

        private bool NeighborChunkContainsBlock(Vector3Int neighborBlockPosition, ChunkCoord neighborCoord) {
            // Decrement CHUNK_SIZE by 1, because it is enumerated from 0 - 15, resulting in 16 numbers.
            int chunkSizeX = CHUNK_SIZE - 1;
            int chunkSizeZ = CHUNK_SIZE - 1;
            int chunkStartX = neighborCoord.coordX;
            int chunkEndX = neighborCoord.coordX + chunkSizeX;
            int chunkStartZ = neighborCoord.coordZ;
            int chunkEndZ = neighborCoord.coordZ + chunkSizeZ;
            if (neighborBlockPosition.x > chunkEndX || neighborBlockPosition.x < chunkStartX ||
                neighborBlockPosition.z > chunkEndZ || neighborBlockPosition.z < chunkStartZ) { return false; }
            else { return true; }
        }

        private void ConvertPositionAxisToNeighborChunkPosition(ref Vector3Int neighborBlockPosition) {
            // Convert the block position to match the wanted position inside the neighbor chunk.
            // (Remember: all chunk blocks positions were set in local position (E.g X: 0-15, Y: 0-255, Z: 0-15).
            if (neighborBlockPosition.z == CHUNK_SIZE) {
                neighborBlockPosition = new Vector3Int(neighborBlockPosition.x, neighborBlockPosition.y, 0);
            }
            else if (neighborBlockPosition.z == -1) {
                neighborBlockPosition = new Vector3Int(neighborBlockPosition.x, neighborBlockPosition.y, CHUNK_SIZE - 1);
            }

            if (neighborBlockPosition.x == CHUNK_SIZE) {
                neighborBlockPosition = new Vector3Int(0, neighborBlockPosition.y, neighborBlockPosition.z);
            }
            else if (neighborBlockPosition.x == -1) {
                neighborBlockPosition = new Vector3Int(CHUNK_SIZE - 1, neighborBlockPosition.y, neighborBlockPosition.z);
            }

            // Deal with the special case where the vector becomes 0 and is searching the bottom position (0 - 1 = -1), where the block position get out the chunk height range.
            if (neighborBlockPosition.y == -1) {
                neighborBlockPosition = new Vector3Int(neighborBlockPosition.x, 0, neighborBlockPosition.z);
            }
        }

        private bool IsBlockInChunkEdge(BlockData.BlockCoord blockCoord) {
            return blockCoord.x >= 0 && blockCoord.x <= CHUNK_SIZE - 1 &&
                   blockCoord.z == 0 || blockCoord.z == CHUNK_SIZE - 1 ||
                   blockCoord.y == 0 || blockCoord.y == CHUNK_HEIGHT - 1
                   ||
                   blockCoord.x == 0 || blockCoord.x == CHUNK_SIZE - 1 &&
                   blockCoord.z >= 0 && blockCoord.z <= CHUNK_SIZE - 1 ||
                   blockCoord.y == 0 || blockCoord.y == CHUNK_HEIGHT - 1;
        }

        private List<Vector2> GetChunkUVCoordinates(HashSet<BlockData.BlockFaceDirection> faceDirections, BlockData.BlockTypes blockType) {
            List<Vector2> uvs = new List<Vector2>();
            foreach (BlockData.BlockFaceDirection blockFace in faceDirections) {
                uvs.AddRange(BlockDataUtils.FaceUVs(blockFace, blockType));
            }
            return uvs;
        }
    }

}

public static class ChunkDataUtils {

    public static bool IsBlockSolid(BlockData.Block blockInstance) => blockInstance.isSolid;

    public static bool CanAccessChunkData(ChunkData.Chunk chunk) => chunk.isChunkDataReady && chunk.chunkMeshData.isRendered;

    public static int GetChunkSurfaceBlockYPosition(ChunkData.Chunk chunk, Vector3Int desiredPositionXZ) {
        for (int i = 0; i < ChunkData.CHUNK_HEIGHT; i++) {
            Vector3Int position = new Vector3Int(desiredPositionXZ.x, i, desiredPositionXZ.z);
            if (chunk.chunkBlocksDictionary.TryGetValue(BlockData.BlockCoord.Vector3IntToCoord(desiredPositionXZ), out BlockData.Block desiredBlockXZ)) {

                int posY = position.y + 1;
                if (chunk.chunkBlocksDictionary.TryGetValue(BlockData.BlockCoord.Vector3IntToCoord(new Vector3Int(position.x, posY, position.z)), out BlockData.Block desiredBlockXYZ)) {
                    if (desiredBlockXYZ.blockType == BlockData.BlockTypes.Water) { continue; }

                    if (!IsBlockSolid(desiredBlockXYZ)) { return posY; }
                }
            }
        }

        Debug.LogWarning($"GetChunkSurfaceBlockYPosition() - " +
            $"Impossible to find chunk surface block in chunk with coords: '{chunk.chunkCoord.CoordToVector3Int()}', " +
            $"in the desired position XZ: '{desiredPositionXZ.x}' Z: '{desiredPositionXZ.z}'");
        return ChunkData.CHUNK_HEIGHT;
    }

    public static ChunkData.Chunk GetChunkWhichContainsPosition(Vector3 position) {
        // Look up for this position inside the active chunks.
        foreach (KeyValuePair<ChunkData.ChunkCoord, ChunkData.Chunk> activeEntry in World.Instance.GetActiveChunksDictionary) {
            if (activeEntry.Value == null) { continue; }

            if (IsChunkInThisPosition(position, activeEntry.Value)) {
                return activeEntry.Value;
            }
        }
        return null;
    }

    public static bool IsChunkInThisPosition(Vector3 position, ChunkData.Chunk chunkToCheck) {
        if (chunkToCheck == null) { return false; }

        // Decrement CHUNK_SIZE by 1, because it is enumerated from 0 - 15, resulting in 16 numbers.
        float chunkStartX = chunkToCheck.chunkCoord.coordX;
        float chunkEndX = chunkStartX + (ChunkData.CHUNK_SIZE - 1);
        float chunkStartZ = chunkToCheck.chunkCoord.coordZ;
        float chunkEndZ = chunkStartZ + (ChunkData.CHUNK_SIZE - 1);

        //if (!(position.x >= chunkStartX && position.x <= chunkEndX &&
        //    position.z >= chunkStartZ && position.z <= chunkEndZ)) {
        //    Debug.Log($"Position: {position} is out of chunk boundaries - startX: {chunkStartX}, endX: {chunkEndX} / " +
        //        $"startZ: {chunkStartZ}, endZ: {chunkEndZ}");
        //}

        return position.x >= chunkStartX && position.x <= chunkEndX &&
            position.z >= chunkStartZ && position.z <= chunkEndZ;
    }

    public static BlockData.Block GetBlockFromChunk(ChunkData.Chunk chunk, Vector3 blockPosition, bool checkNonSolidBlocks) {
        foreach(KeyValuePair<BlockData.BlockCoord, BlockData.Block> blockEntry in chunk.chunkBlocksDictionary) {

            // Skip checking non-solid blocks.
            if (!checkNonSolidBlocks) {
                if (blockEntry.Value.blockType == BlockData.BlockTypes.Air ||
                    blockEntry.Value.blockType == BlockData.BlockTypes.Water) { continue; }
            }

            // Check If this block position is inside this blockCoord.
            if (BlockDataUtils.IsBlockInThisPosition(blockEntry.Key, blockPosition)) { return blockEntry.Value; }

        }
        return BlockData.Block.AirBlock;
    }

    public static void UpdateBlockInChunk(ChunkData.Chunk chunk, Vector3 blockPosition) {
        foreach (KeyValuePair<BlockData.BlockCoord, BlockData.Block> blockEntry in chunk.chunkBlocksDictionary) {

            // Skip if it is a solid block.
            if (blockEntry.Value.blockType != BlockData.BlockTypes.Air) { continue; }

            // Check if this block position is inside this blockcoord.
            if (BlockDataUtils.IsBlockInThisPosition(blockEntry.Key, blockPosition)) {

                // Update the block in this position.
                BlockData.Block newBlock = new BlockData.Block() {
                    blockType = BlockData.BlockTypes.Sand,
                    isSolid = true
                };
                chunk.chunkBlocksDictionary[blockEntry.Key] = newBlock;
                Debug.Log("Block inside chunk updated successfully!");
                break;
            }

        }
    }

}