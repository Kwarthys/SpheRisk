using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanGenerator
{
    public NoiseFilter noise;

    private NoiseSettings settings;

    public OceanGenerator(NoiseSettings settings, int seed)
    {
        noise = new NoiseFilter(seed);
        this.settings = settings;
    }

    public Vector3 getPointOnPlanet(Vector3 pointOnSphere)
    {
        float h = 0;

        for (int i = 0; i < settings.layers; ++i)
        {
            h += noise.evaluate(pointOnSphere * settings.speed * Mathf.Pow(settings.speedIncrease, i) + settings.offset * Time.realtimeSinceStartup * settings.timeFactor) * settings.strength * Mathf.Pow(settings.amplitudePersistence, i);
        }

        h = settings.radius * (h + 1);

        return pointOnSphere * h;
    }
}
