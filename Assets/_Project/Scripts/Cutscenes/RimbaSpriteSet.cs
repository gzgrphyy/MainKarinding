using UnityEngine;

namespace MainKarinding.Cutscenes
{
    [CreateAssetMenu(fileName = "RimbaSpriteSet", menuName = "Main Karinding/Cutscenes/Character Sprite Set/Rimba")]
    public class RimbaSpriteSet : ScriptableObject
    {
        [Header("Scene 1 - Teras Rumah")]
        public Sprite idlePintu;
        public Sprite lookoutFacingAbah;
        public Sprite lookoutNoleh1;
        public Sprite lookoutNoleh2;
        public Sprite lookoutFacingMountain;

        [Header("Scene 1 - Walk Off (ending jalan ke kebun)")]
        [Tooltip("Frame jalan hadap kanan, urut 1..N. Pivot center bawaan file.")]
        public Sprite[] walkRight = new Sprite[4];
        [Tooltip("Idle hadap kanan. Kosong = pakai walkRight[0].")]
        public Sprite idleRight;

        [Header("Scene 2 - Teras Siang (duduk)")]
        [Tooltip("Pose duduk Rimba di teras siang. File: s2_rimba_duduk.png")]
        public Sprite duduk;
    }
}
