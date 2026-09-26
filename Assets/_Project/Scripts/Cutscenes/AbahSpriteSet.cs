using UnityEngine;

namespace MainKarinding.Cutscenes
{
    [CreateAssetMenu(fileName = "AbahSpriteSet", menuName = "Main Karinding/Cutscenes/Character Sprite Set/Abah")]
    public class AbahSpriteSet : ScriptableObject
    {
        [Header("Scene 1 - Teras Rumah")]
        public Sprite idleTerasTalking;
        public Sprite lookoutFacingMountain;

        [Header("Scene 1 - Walk Off (ending jalan ke kebun)")]
        [Tooltip("Frame jalan hadap kanan, urut 1..N. Pivot center bawaan file.")]
        public Sprite[] walkRight = new Sprite[4];
        [Tooltip("Idle hadap kanan. Kosong = pakai walkRight[0].")]
        public Sprite idleRight;

        [Header("Scene 2 - Teras Siang (duduk)")]
        [Tooltip("Pose duduk Abah di teras siang. File: s2_abah_duduk.png")]
        public Sprite duduk;

        [Header("Scene 3 - Belajar Memainkan Karinding")]
        [Tooltip("Abah tersenyum tipis di closing Scene 3. File: s3_abah_senyum_tipis.png")]
        public Sprite senyumTipis;
    }
}
