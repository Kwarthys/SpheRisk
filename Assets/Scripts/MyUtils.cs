using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MyUtils
{
    public static float[] getPartialArray(int start, int size, float[] array)
    {
        int realSize = Mathf.Min(size, array.Length - start);

        float[] outArray = new float[realSize];
        for (int i = 0; i < realSize; ++i)
        {
            outArray[i] = array[i + start];
        }

        return outArray;
    }

    public static int[] getPartialArray(int start, int size, int[] array)
    {
        int realSize = Mathf.Min(size, array.Length - start);

        int[] outArray = new int[realSize];
        for (int i = 0; i < realSize; ++i)
        {
            outArray[i] = array[i + start];
        }

        return outArray;
    }

    public static Vector2Int[] getPartialArray(int start, int size, Vector2Int[] array)
    {
        int realSize = Mathf.Min(size, array.Length - start);

        Vector2Int[] outArray = new Vector2Int[realSize];
        for (int i = 0; i < realSize; ++i)
        {
            outArray[i] = array[i + start];
        }

        return outArray;
    }

    public static Vector3[] getPartialArray(int start, int size, Vector3[] array)
    {
        int realSize = Mathf.Min(size, array.Length - start);

        Vector3[] outArray = new Vector3[realSize];
        for (int i = 0; i < realSize; ++i)
        {
            outArray[i] = array[i+start];
        }

        return outArray;
    }
}
