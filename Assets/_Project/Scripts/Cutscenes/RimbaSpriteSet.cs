using UnityEngine;

namespace MainKarinding.Cutscenes
{
    [CreateAssetMenu(fileName = "RimbaSpriteSet", menuName = "Main Karinding/Cutscenes/Character Sprite Set/Rimba")]
    public class RimbaSpriteSet : ScriptableObject
    {
        [Header("Scene 1 - Teras Rumah")]
        public Sprite idlePintu;
        [Tooltip("Rimba baru bangun. File: rimba_idle_pintu1.png")]
        public Sprite idlePintuBangun;
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
        [Tooltip("Pose duduk Rimba di teras siang. File: s2_rimba_duduk.png (legacy fallback)")]
        public Sprite duduk;
        [Tooltip("Rimba ngomong. File: s2_rimba_sit_talk.png")]
        public Sprite sitTalk;
        [Tooltip("Rimba diam. File: s2_rimba_sit_idle.png")]
        public Sprite sitIdle;

        [Header("Scene 3 - Belajar Memainkan Karinding")]
        [Tooltip("Rimba sudah pegang karinding, masih duduk. File: s3_rimba_pegang_canggung.png")]
        public Sprite pegangCanggung;
        [Tooltip("Rimba bangga setelah berhasil. File: s3_rimba_bangga.png")]
        public Sprite bangga;

        [Header("Scene 9 - Hutan Nyata (terbangun)")]
        [Tooltip("Rimba bersandar pohon, sendirian. File: s9_rimba_sandar_pohon.png")]
        public Sprite sandarPohon;
        [Tooltip("Tangan meraba pinggang, kosong. File: s9_rimba_meraba_pinggang.png")]
        public Sprite merabaPinggang;
        [Tooltip("Rimba berdiri, End of Chapter 1. File: s9_rimba_berdiri.png")]
        public Sprite berdiri;
    }
}
