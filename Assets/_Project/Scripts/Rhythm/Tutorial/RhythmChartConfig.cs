using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RhythmChartConfig", menuName = "Karinding/RhythmChartConfig")]
public class RhythmChartConfig : ScriptableObject
{
    [Tooltip("ID unik untuk chart ini.")]
    public string chartId;

    [Tooltip("Clip BGM yang di-play saat StartMinigame. Boleh null (diem).")]
    public AudioClip backgroundMusic;

    [Tooltip("Durasi (detik) yang dibutuhkan note untuk pindah dari spawn point ke hit-zone.")]
    public float noteTravelDuration = 1.2f;

    [Tooltip("Daftar note yang di-spawn selama minigame, diurutkan berdasarkan spawnTime.")]
    public List<NoteData> notes = new List<NoteData>();

    [Header("Judgement Windows")]
    [Tooltip("Ambang delta (detik) untuk nilai Perfect.")]
    public float perfectWindow = 0.08f;

    [Tooltip("Ambang delta (detik) untuk nilai Good. Harus >= perfectWindow.")]
    public float goodWindow = 0.18f;

    [Header("Scoring")]
    public int scorePerPerfect = 100;
    public int scorePerGood = 50;
    [Tooltip("Tiap berapa combo naik multiplier (0 = tanpa multiplier).")]
    public int comboStep = 10;
    [Tooltip("Penambahan multiplier tiap step combo.")]
    public float comboMultiplier = 0.5f;

    [Header("Fail Condition")]
    [Tooltip("Maks jumlah Miss sebelum result.passed = false.")]
    public int maxMisses = 5;
}
