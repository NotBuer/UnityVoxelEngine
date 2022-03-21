using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseMap {

    public static float RemapValue(float value, float initialMin, float initialMax, float outputMin, float outputMax) {
        return outputMin + (value - initialMin) * (outputMax - outputMin) / (initialMax - initialMin);
    }

    public static float RemapValue01(float value, float outputMin, float outputMax) {
        return outputMin + (value - 0) * (outputMax - outputMin) / (1 - 0);
    }

    public static int RemapValue01ToInt(float value, float outputMin, float outputMax) {
        return (int) RemapValue01(value, outputMin, outputMax);
    }

    public static float Redistribution(float noise, NoiseSettings noiseSettings) {
        return Mathf.Pow(noise * noiseSettings.redistributionModifier, noiseSettings.exponent);
    }

    public static float OctavePerlin(float x, float z, NoiseSettings noiseSettings) {
        x *= noiseSettings.noiseZoom;
        z *= noiseSettings.noiseZoom;
        x += noiseSettings.noiseZoom;
        z += noiseSettings.noiseZoom;

        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float amplitudeSum = 0;  // Used for normalizing result to 0.0 to 1.0 range
        for (int i = 0; i < noiseSettings.octaves; i++) {
            total += Mathf.PerlinNoise((noiseSettings.offset.x + noiseSettings.worldOffset.x + x) * frequency,
                                       (noiseSettings.offset.y + noiseSettings.worldOffset.y + z) * frequency) * amplitude;

            amplitudeSum += amplitude;

            amplitude *= noiseSettings.persistance;
            frequency *= 2;
        }

        return total / amplitudeSum;
    }



    /// scale : The scale of the "perlin noise" view
    /// heightMultiplier : The maximum height of the terrain
    /// octaves : Number of iterations (the more there is, the more detailed the terrain will be)
    /// persistance : The higher it is, the rougher the terrain will be (this value should be between 0 and 1 excluded)
    /// lacunarity : The higher it is, the more "feature" the terrain will have (should be strictly positive)
    public static int GetNoiseAt(int x, int z, float scale, float heightMultiplier, int octaves, float persistance, float lacunarity) {
        float perlinValue = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        // TODO: Adjust the noise map equation.

        for (int i = 0; i < octaves; i++) {
            // Get the perlin value at that octave and add it to the sum
            perlinValue += Mathf.PerlinNoise((x * frequency) * scale, (z * frequency) * scale) * amplitude;

            // Decrease the amplitude and the frequency
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        // Return the noise value
        //Debug.Log(perlinValue);
        return (int) (perlinValue * heightMultiplier);
    }

}
