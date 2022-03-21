using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Block Data" , menuName = "Data/Block Data")]
public class BlockDataSO : ScriptableObject {

    [Header("Block Texture Size (row/rowsCells , cols/colsCells)")]
    public float textureSizeX, textureSizeY;

    [Header("Block Data")]
    public List<BlockDataDB> blockDataList;

}

[System.Serializable]
public class BlockDataDB {
    public BlockData.BlockTypes blockType;
    public Vector2Int upsideUV, downsideUV, sidesUV;
    public bool isSolid = true;
    public bool generatesCollider = true;
}
