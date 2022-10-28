using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphereBehaviour : MonoBehaviour
{
    private MeshFilter filter;

    [Range(2, 256)]
    public int resolution = 256;

    public int size = 10;

    public Material atmMaterial;

    public Transform camPos;

    public void setColorAndRandomize(Color c)
    {
        atmMaterial.SetColor("_AtmosphereColor", c);
        //atmMaterial.SetFloat("_AtmFallOffStop", Mathf.Lerp(0.7f, 0.8f, Mathf.Pow(Random.value,4)));        //biaised in favor of lower values
        //atmMaterial.SetFloat("_AtmGlobalAlpha", Mathf.Lerp(0.3f, 1, 1 - Mathf.Pow(Random.value, 4)));   //biaised in favor of higher values
    }

    void Start()
    {
        GameObject meshObject = new GameObject("atmMesh");
        meshObject.transform.parent = transform;

        meshObject.AddComponent<MeshRenderer>().sharedMaterial = atmMaterial;
        filter = meshObject.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        filter.sharedMesh = mesh;

        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 2 * 3];
        Vector2[] uv = new Vector2[resolution * resolution];

        int triIndex = 0;

        for (int j = 0; j < resolution; ++j)
        {
            for(int i = 0; i < resolution; ++i)
            {
                int index = i + j * resolution;
                Vector2 percent = new Vector2(i, j) / (resolution - 1);
                Vector3 pos = new Vector3((percent.x - 0.5f) * 2 * size, (percent.y - 0.5f) * 2 * size, 0);

                uv[index] = new Vector2(Mathf.Abs((percent.x - 0.5f)*2), Mathf.Abs((percent.y - 0.5f) * 2));

                vertices[index] = pos;

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

        meshObject.transform.rotation = Quaternion.LookRotation(camPos.position - transform.position);
        meshObject.transform.Translate(new Vector3(0, 0, -1.5f));
    }
}
