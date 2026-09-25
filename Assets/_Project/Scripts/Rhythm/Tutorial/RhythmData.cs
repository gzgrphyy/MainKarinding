using System.Collections.Generic;
using UnityEngine;

public enum LaneSide
{
    LeftOuter,
    LeftInner,
    RightInner,
    RightOuter
}

public enum HitGrade
{
    Perfect,
    Good,
    Miss
}

public enum TapResult
{
    HitPerfect,
    HitGood,
    Miss, // auto miss when note expires
    EmptyNoNote, // no note within window at all
    WrongLane // note exists in other lane within window
}

[System.Serializable]
public class NoteData
{
    [Tooltip("Detik (relatif terhadap songTime) saat note HARUS di-hit di hit-zone.")]
    public float spawnTime;

    [Tooltip("Lane tempat note ini berada.")]
    public LaneSide lane;
}

public class RhythmMinigameResult
{
    public int totalScore;
    public int perfectCount;
    public int goodCount;
    public int missCount;
    public bool passed;
    public float accuracy;
    public string rank;

    public float GetAccuracy(int totalNotes)
    {
        if (totalNotes <= 0) return 0f;
        return (perfectCount + goodCount * 0.5f) / totalNotes * 100f;
    }

    public static string GetRank(float accuracy)
    {
        if (accuracy >= 95f) return "S";
        if (accuracy >= 80f) return "A";
        if (accuracy >= 60f) return "B";
        if (accuracy >= 40f) return "C";
        return "D";
    }
}
