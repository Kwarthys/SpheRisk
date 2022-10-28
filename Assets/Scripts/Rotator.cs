using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float rps = 1/7f;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0, -Time.deltaTime * rps * 360f, 0));
    }
}
