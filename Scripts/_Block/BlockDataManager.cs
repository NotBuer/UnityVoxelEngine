using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockDataManager : MonoBehaviour {

    public static float textureOffset = 0.001f;
    public static float tileSizeX, tileSizeY;
    public static Dictionary<BlockData.BlockTypes, BlockDataDB> blockDataDictionary;
    public BlockDataSO blocksDataSO;

    private void Awake() {
        // First initialize the dictionary.
        blockDataDictionary = new Dictionary<BlockData.BlockTypes, BlockDataDB>();

        // Loop through and check if the dictionary doesnot already have the keys, if not add the key and value.
        foreach(BlockDataDB blockData in blocksDataSO.blockDataList) {
            if (!blockDataDictionary.ContainsKey(blockData.blockType)) {
                blockDataDictionary.Add(blockData.blockType, blockData);
            }
        }

        //foreach (BlockDataDB blockData in blockDataDictionary.Values) {
        //    Debug.Log("***** Block Data ******");
        //    Debug.Log("Block Type: " + blockData.blockType);
        //    Debug.Log("Block Solid: " + blockData.isSolid);
        //    Debug.Log("Up Vector: " + blockData.upsideUV);
        //    Debug.Log("Down Vector: " + blockData.downsideUV);
        //    Debug.Log("Side Vector: " + blockData.sidesUV);
        //}

        // Set the tile X and Y sizes
        tileSizeX = blocksDataSO.textureSizeX;
        tileSizeY = blocksDataSO.textureSizeY;
    }

}
