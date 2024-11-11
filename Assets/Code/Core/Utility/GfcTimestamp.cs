using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GfcTimestamp : IEquatable<GfcTimestamp>
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

    public readonly bool Equals(GfcTimestamp anOther)
    {
        return this.Frame == anOther.Frame && this.Id == anOther.Id;
    }
}