using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BridgeBuilder : MonoBehaviour
{
    public GameObject smallBridgeGate;
    public GameObject bigBridgeGate;

    public Transform bridgesHolder;

    public GameObject emptyBridgePrefab;

    public float angleStep = 0.3f;
    public float uvTileWidthPerStep = 3;
    public float width = 0.3f;
    public float largeBridgeThreshold = 1;

    [Range(2,8)]
    public int widthResolution = 4;

    public void buildAndSpawnBridge(Bridge b)
    {
        GameObject bridge = Instantiate(emptyBridgePrefab, bridgesHolder);

        Mesh mesh = new Mesh();

        bridge.GetComponent<MeshFilter>().mesh = mesh;

        Vector3 start = b.normalStart;
        Vector3 end = b.normalEnd;

        float totalAngleLength = Vector3.Angle(start, end);
        bool isLarge = totalAngleLength > largeBridgeThreshold;

        Vector3 side = Vector3.Cross(b.normalStart, b.normalEnd).normalized;
        if(isLarge)
        {
            Vector3 gate1Pos = Vector3.Lerp(start, end, 0.3f).normalized;
            Vector3 gate2Pos = Vector3.Lerp(start, end, 0.7f).normalized;

            Instantiate(bigBridgeGate, gate1Pos * 10, Quaternion.LookRotation(Vector3.Cross(side, start), start), bridge.transform);
            Instantiate(bigBridgeGate, gate2Pos * 10, Quaternion.LookRotation(-Vector3.Cross(side, end), end), bridge.transform);
        }
        else
        {
            Vector3 gatePos = Vector3.Lerp(start, end, 0.5f).normalized;
            Instantiate(smallBridgeGate, gatePos * 10, Quaternion.LookRotation(Vector3.Cross(side, start), start), bridge.transform);
        }

        float bridgeWidth = isLarge ? width * 1.5f : width;

        int numberOfSteps = (int)(totalAngleLength / angleStep);
        float step = totalAngleLength / numberOfSteps * 1.0f / totalAngleLength;

        Vector3[] vertices = new Vector3[(numberOfSteps + 1) * widthResolution];
        Vector2[] uvs = new Vector2[(numberOfSteps + 1) * widthResolution];
        int[] triangles = new int[(widthResolution-1) * (numberOfSteps) * 2 * 3];

        //vertices[0] = start * 10 + side * bridgeWidth;
        //vertices[1] = start * 10 - side * bridgeWidth;

        float t = 0;
        int vindex = 0;
        int triIndex = 0;

        for(int i = 0; i < numberOfSteps+1; ++i)
        {
            Vector3 current = Vector3.Lerp(start, end, t).normalized;
            float altModifier = Mathf.Sin(t * Mathf.PI) * 0.04f * (isLarge ? 1.5f : 1);

            float xTilePercent = (i * 1.0f) % (uvTileWidthPerStep * 2 + 1) / (uvTileWidthPerStep * 2);
            if(xTilePercent > 0.5f)
            {
                xTilePercent = 1 - xTilePercent;
            }
            xTilePercent *= 2;

            if(xTilePercent > 1 || xTilePercent < 0)
            {
                Debug.Log("xTile : " + xTilePercent + " : " + i + "/" + uvTileWidthPerStep + " | " + numberOfSteps);
            }

            for(int j = 0; j < widthResolution; ++j)
            {
                float yTilePercent = j * 1.0f / (widthResolution - 1);
                yTilePercent = (yTilePercent - 0.5f) * 2;

                if (yTilePercent > 1 || yTilePercent < -1)
                {
                    Debug.LogError("yTile : " + yTilePercent + " - " + j + "/" + (widthResolution-1));
                }

                vertices[vindex + j] = current * (10 + altModifier) + side * bridgeWidth * yTilePercent;

                uvs[vindex + j] = new Vector2(xTilePercent, j * 1.0f / (widthResolution - 1));

                if(i!=0 && j != widthResolution-1)
                {
                    triangles[triIndex++] = vindex + j;
                    triangles[triIndex++] = vindex + j - widthResolution + 1;
                    triangles[triIndex++] = vindex + j - widthResolution;

                    triangles[triIndex++] = vindex + j;
                    triangles[triIndex++] = vindex + j + 1;
                    triangles[triIndex++] = vindex + j - widthResolution + 1;
                }
            }

            vindex += widthResolution;
            t += step;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
    }
}

public struct Bridge
{
    public Vector3 normalStart;
    public Vector3 normalEnd;
}
