using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GfcTimeStamp : IEquatable<GfcTimeStamp>, IComparable<GfcTimeStamp>, IFormattable
{
    private static int StampsInFrame = 1;
    private static int LastFrame = 0;

    public int Frame { get; private set; }
    public int Id { get; private set; }

    public GfcTimeStamp(byte aJunk)
    {
        Frame = Time.frameCount;
        if (LastFrame != Frame)
            StampsInFrame = 1;

        Id = StampsInFrame++;
    }

    public readonly bool Equals(GfcTimeStamp anOther) { return Frame == anOther.Frame && Id == anOther.Id; }

    public readonly int CompareTo(GfcTimeStamp aData)
    {
        int val = Frame - aData.Frame;

        if (val == 0)
            val = Id - aData.Id;

        return val.Sign();
    }

    public readonly string ToString(string format, IFormatProvider formatProvider) { return ToString(); }
    public new readonly string ToString() { return "TimeStamp(Frame: " + Frame + ", Id: " + Id + ")"; }
    public readonly bool Valid() { return Frame != 0 && Id != 0; }
}