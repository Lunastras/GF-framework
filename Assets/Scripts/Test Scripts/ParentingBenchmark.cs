using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParentingBenchmark : MonoBehaviour
{
    [SerializeField]
    private Transform parentObject = null;

    [SerializeField]
    private bool usesParentFunction = false;

    [SerializeField]
    private bool usesTernary = false;

    [SerializeField]
    private bool usesWhile = false;

    [SerializeField]
    private int repeatsPerFrame = 10000;

    //private const int repeatsPerFrame = 10000;

    private bool parented = false;

    private void ThingToDo()
    {
        Transform parent;

        if (usesTernary)
            parent = parented ? null : parentObject;
        else
            if (parented)
            parent = null;
        else
            parent = parentObject;


        if (usesParentFunction)
            transform.SetParent(parent);
        else
            transform.parent = parent;

        parented = !parented;
    }

    // Update is called once per frame
    void Update()
    {
        if (usesWhile)
        {
            int count = repeatsPerFrame;
            while (--count >= 0)
                ThingToDo();
        }
        else
        {
            for (int i = 0; i < repeatsPerFrame; ++i)
                ThingToDo();
        }


    }
}
