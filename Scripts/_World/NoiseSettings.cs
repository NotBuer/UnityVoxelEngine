using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Noise Settings", menuName = "Data/NoiseSettings")]
public class NoiseSettings : ScriptableObject {

    public float noiseZoom;
    public int octaves;
    public float persistance;
    public Vector2Int worldOffset;
    public Vector2Int offset;
    public float redistributionModifier;
    public float exponent;

}
