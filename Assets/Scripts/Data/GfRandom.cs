using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfRandom
{
        //A list of randomly ordered numbers from 0 to 99
    //used for rng in order to make sure we have the same seeds and outcomes
    private static int currentRandomIndex = 0;
    private static int[] shuffledList = { 18, 83, 27, 78, 11, 70, 68, 19,
                                          33, 56, 82, 13, 53, 49, 40, 54,
                                          8, 9, 67, 2, 73, 91, 50, 24, 37,
                                          52, 99, 4, 92, 45, 47, 62, 93, 66,
                                          20, 76, 71, 7, 31, 51, 97, 10, 42,
                                          48, 0, 38, 5, 16, 94, 1, 25, 39, 58,
                                          36, 86, 6, 95, 15, 84, 22, 79, 21, 80,
                                          44, 88, 65, 69, 81, 74, 63, 3, 29, 57,
                                          77, 85, 26, 23, 64, 60, 46, 90, 28, 96,
                                          98, 32, 12, 59, 55, 87, 89, 43, 30, 72,
                                          14, 17, 61, 41, 75, 34, 35 };

     /**
     * Get random int from 0. to 0.99
     */
    public static float GetRandomNum()
    {
        int numToReturn = shuffledList[currentRandomIndex] + 1;

        currentRandomIndex++;
        currentRandomIndex %= shuffledList.Length;

        return (float)numToReturn / 100.0f;
    }

    public static float Range(float edge1, float edge2) {
        if(edge1 > edge2) {
            float aux = edge1;
            edge2 = edge1;
            edge1 = aux;
        }

        return GetRandomNum() * (edge2 - edge1) + edge1;
    }
}
