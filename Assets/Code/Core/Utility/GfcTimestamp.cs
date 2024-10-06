using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GfcTimestamp
{
    private static int StampsInFrame = 0;
    private static int LastFrame = 0;

    public GfcTimestamp(byte aJunk = 0)
    {
        Frame = Time.frameCount;
        if (LastFrame != Frame)
            StampsInFrame = 0;

        Id = StampsInFrame++;
    }

    public int Frame { get; private set; }
    public int Id { get; private set; }

    public static bool operator ==(GfcTimestamp obj1, GfcTimestamp obj2)
    {
        return obj1.Frame == obj2.Frame && obj1.Id == obj2.Id;
    }

    public static bool operator !=(GfcTimestamp obj1, GfcTimestamp obj2)
    {
        return !(obj1 == obj2);
    }
}
