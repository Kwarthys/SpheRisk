using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetFace
{
    private Vector3 up;
    private Vector3 axisA;
    private Vector3 axisB;

    private int resolution;
    private Mesh mesh;

    private TerrainGenerator generator;

    private float seaLevel;

    public PlanetFace(TerrainGenerator generator, int resolution, Mesh mesh, Vector3 up, float seaLevel)
    {
        this.up = up;
        this.resolution = resolution;
        this.mesh = mesh;

        this.seaLevel = seaLevel;

        this.generator = generator;

        axisA = new Vector3(up.y, up.z, up.x);
        axisB = Vector3.Cross(up, axisA);
    }

    public Vector3 getRandomOnLandPoint()
    {
        Vector3 point = Vector3.zero;
        float elevation = -1;
        int maxTries = 1000;
        Vector3[] v = mesh.vertices;
        
        while(elevation < (seaLevel + generator.settings.radius)*1.1f && maxTries > 0)
        {
            maxTries--;

            int x = (int)((Random.value * 0.8f + 0.1f) * resolution);
            int y = (int)((Random.value * 0.8f + 0.1f) * resolution);

            point = v[x + y*resolution];
            elevation = point.magnitude;
        }

        if(maxTries==0)
        {
            return Vector3.zero;
        }

        return point;
    }

    public Mesh constructMeshFromArray(float[] altitudes, int startingIndex)
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 2 * 3];
        Vector2[] uv = new Vector2[resolution * resolution];
        Vector2[] uv3 = new Vector2[resolution * resolution];

        int triIndex = 0;

        float texCenterCoef = (resolution - 1) / (resolution * 1.0f);

        for (int j = 0; j < resolution; ++j)
        {
            for (int i = 0; i < resolution; ++i)
            {
                int index = i + j * resolution;

                Vector2 percent = new Vector2(i, j) / (resolution - 1);
                Vector3 point = up + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                Vector3 pointOnSphere = point.normalized;
                vertices[index] = pointOnSphere * 10;//hacky planet radius

                float unscaledElevation = altitudes[index + startingIndex];
                //Debug.Log((index + startingIndex) + " - " + unscaledElevation);
                generator.elevationMinMax.register(unscaledElevation);

                //Vector3 normalizedTextureCenter = up + (percent.x - 0.5f) * 2 * axisA * texCenterCoef + (percent.y - 0.5f) * 2 * axisB * texCenterCoef;
                //normalizedTextureCenter.Normalize();

                uv[index] = percent;

                //Vector3 longitudeHelper = new Vector3(pointOnSphere.x, 0, pointOnSphere.z);
                uv3[index] = new Vector2(unscaledElevation, 0);//Vector3.Angle(longitudeHelper, Vector3.forward));

                if (j != resolution - 1 && i != resolution - 1)
                {
                    triangles[triIndex++] = index;
                    triangles[triIndex++] = index + resolution + 1;
                    triangles[triIndex++] = index + resolution;

                    triangles[triIndex++] = index;
                    triangles[triIndex++] = index + 1;
                    triangles[triIndex++] = index + resolution + 1;
                }
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.uv3 = uv3;
        mesh.RecalculateNormals();

        return mesh;
    }

    public float[] constructMesh(MinMax minMaxSteepness, GridMapManager mapManager, int mapIndex)
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 2 * 3];
        Vector2[] uv = new Vector2[resolution * resolution];
        Vector2[] uv3 = new Vector2[resolution * resolution];

        float[] elevations = new float[resolution * resolution];

        int triIndex = 0;

        float texCenterCoef =  (resolution - 1) / (resolution * 1.0f);

        for (int j = 0; j < resolution; ++j)
        {
            for (int i = 0; i < resolution; ++i)
            {
                int index = i + j * resolution;

                Vector2 percent = new Vector2(i, j) / (resolution - 1);
                Vector3 point = up + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                Vector3 pointOnSphere = point.normalized;
                vertices[index] = pointOnSphere * 10;//hacky planet radius

                generator.getPointOnPlanet(pointOnSphere, out float unscaledElevation);

                elevations[index] = unscaledElevation;

                Vector3 normalizedTextureCenter = up + (percent.x - 0.5f) * 2 * axisA * texCenterCoef + (percent.y - 0.5f) * 2 * axisB * texCenterCoef;
                normalizedTextureCenter.Normalize();
                mapManager.fillNode(mapIndex, i, j, normalizedTextureCenter, unscaledElevation <= seaLevel);

                uv[index] = percent;

                //Vector3 longitudeHelper = new Vector3(pointOnSphere.x, 0, pointOnSphere.z);
                uv3[index] = new Vector2(unscaledElevation, 0);//Vector3.Angle(longitudeHelper, Vector3.forward));

                if (j!=resolution-1 && i!=resolution-1)
                {
                    triangles[triIndex++] = index;
                    triangles[triIndex++] = index+resolution+1;
                    triangles[triIndex++] = index+resolution;

                    triangles[triIndex++] = index;
                    triangles[triIndex++] = index+1;
                    triangles[triIndex++] = index+resolution+1;
                }

            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.uv3 = uv3;
        mesh.RecalculateNormals();

        mapManager.registerMesh(mesh, mapIndex);

        /*
        Vector3[] newNormals = mesh.normals;
        for (int j = 0; j < resolution; ++j)
        {
            for (int i = 0; i < resolution; ++i)
            {
                int index = i + j * resolution;

                float steepness = 1 - Vector3.Dot(Vector3.Normalize(vertices[index]), Vector3.Normalize(newNormals[index]));
                minMaxSteepness.register(steepness);
            }
        }
        */
        return elevations;
    }

}
