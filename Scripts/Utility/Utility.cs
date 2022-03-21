using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utility {

    public static Vector3Int ConvertPositionToVector3Int(Vector3 position) => 
        new Vector3Int(Mathf.FloorToInt(position.x), 
        Mathf.FloorToInt(position.y), 
        Mathf.FloorToInt(position.z));

}
