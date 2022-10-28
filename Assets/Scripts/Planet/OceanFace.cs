using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanFace
{
    private Vector3 up;
    private Vector3 axisA;
    private Vector3 axisB;

    private int resolution;
    private Mesh mesh;

    private float seaLevel;

    private OceanGenerator generator;

    private Vector3[] vertices;

    public OceanFace(OceanGenerator generator, int resolution, Mesh mesh, Vector3 up, float seaLevel)
    {
        this.up = up;
        this.resolution = resolution;
        this.mesh = mesh;

        this.generator = generator;

        this.seaLevel = seaLevel;

        axisA = new Vector3(up.y, up.z, up.x);
        axisB = Vector3.Cross(up, axisA);
    }

    public void constructMesh(float[] elevations)
    {
        vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 2 * 3];
        Vector2[] uv = new Vector2[resolution * resolution];

        int triIndex = 0;

        for (int j = 0; j < resolution; ++j)
        {
            for (int i = 0; i < resolution; ++i)
            {
                int index = i + j * resolution;

                Vector2 percent = new Vector2(i, j) / (resolution - 1);
                Vector3 point = up + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                Vector3 pointOnSphere = point.normalized;
                vertices[index] = generator.getPointOnPlanet(pointOnSphere);

                Vector3 longitudeHelper = new Vector3(pointOnSphere.x, 0, pointOnSphere.z);

                uv[index] = new Vector2(elevations[index], Vector3.Angle(longitudeHelper, Vector3.forward));

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
        mesh.RecalculateNormals();
    }
}
