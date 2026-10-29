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
        [Tooltip("Frame jalan hadap kanan dari rimbakecil-walk.png, urut 1..9. Pivot center bawaan file.")]
        public Sprite[] walkRight = new Sprite[9];
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
        [Tooltip("Rimba menerima karinding dari Abah. File: rimba_s3_receivekarinding.png")]
        public Sprite receiveKarinding;
        [Tooltip("Rimba memegang karinding (diam). File: rimba_s3_holdkarinding.png")]
        public Sprite holdKarinding;
        [Tooltip("Rimba memainkan karinding. File: rimba_s3_playkarinding.png")]
        public Sprite playKarinding;
        [Tooltip("Rimba sudah pegang karinding, masih duduk. File: s3_rimba_pegang_canggung.png (legacy fallback)")]
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

        [Header("Scene 9 - Flipbook bangun + stand back (rombak S9)")]
        [Tooltip("5 frame bangun urut: sleep, wake_up, sit_confused, check_waist, sit_realization. Kanvas 1800x1800 pivot center.")]
        public Sprite[] s9WakeSequence = new Sprite[0];
        [Tooltip("Rimba berdiri membelakangi kamera (layer ter-crop via Sprite Editor, overlay bg 1920x1080). File: 6rimba_s9_stand_back.png")]
        public Sprite s9StandBack;

        [Header("Scene 5 - Teras Siang (berdiri, rombak S5)")]
        [Tooltip("Rimba berbicara (berdiri). File: rimba_s5_stand_talk.png")]
        public Sprite standTalk;
        [Tooltip("Rimba diam mendengarkan (berdiri). File: rimba_s5_stand_listen.png")]
        public Sprite standListen;
        [Tooltip("Rimba sampai titik berhenti membawa bambu. File: rimba_s5_bringbamboo.png")]
        public Sprite bringBamboo;
    }
}
