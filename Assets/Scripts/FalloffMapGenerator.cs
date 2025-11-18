using System;
using UnityEngine;

public class FalloffMapGenerator : MonoBehaviour
{

    //Falloff map 2d array
    public static float[,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];

        //loop through map pixels
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                //Gets middle
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                //Pythagorian theorem
                float r = Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(y, 2));

                //Gets value
                float value = Mathf.Max(Mathf.Abs(r * 2), Mathf.Abs(r * 2));
                map[i, j] = value;
            }
        }

        return map;
    }
}