using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public List<MeshRenderer> meshRenderers;

    public void setColor(Color color)
    {
        foreach (MeshRenderer r in meshRenderers)
        {
            r.materials[0].color = color;
        }
    }

    public void setColor(int playerIndex)
    {
        Color color = PlayerNetworkManager.colors[playerIndex];
        foreach (MeshRenderer r in meshRenderers)
        {
            r.materials[0].color = color;
        }
    }
}
