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
        [Tooltip("Pose duduk Abah di teras siang. File: s2_abah_duduk.png (legacy fallback)")]
        public Sprite duduk;
        [Tooltip("Abah ngomong / diam. File: s2_abah_sit_talk.png")]
        public Sprite sitTalk;
        [Tooltip("Abah main karinding saat backsound bunyi. File: s2_abah_sit_play_karinding.png")]
        public Sprite sitPlayKarinding;
        [Tooltip("Abah pegang karinding saat jelaskan Pancepengan/Panabeuh/Cecet Ucing. File: s2_abah_sit_hold_karinding.png")]
        public Sprite sitHoldKarinding;

        [Header("Scene 3 - Belajar Memainkan Karinding")]
        [Tooltip("Abah menyerahkan karinding. File: abah_s3_givekarinding.png")]
        public Sprite giveKarinding;
        [Tooltip("Abah idle Scene 3. File: abah_s3_idle.png")]
        public Sprite idleS3;
        [Tooltip("Abah tersenyum Scene 3. File: abah_s3_smile.png")]
        public Sprite smileS3;
        [Tooltip("Abah berbicara Scene 3. File: abah_s3_talking.png")]
        public Sprite talkingS3;
        [Tooltip("Abah tersenyum tipis di closing Scene 3. File: s3_abah_senyum_tipis.png (legacy fallback)")]
        public Sprite senyumTipis;

        [Header("Scene 5 - Ajak ke Hutan + Reaksi Bambu")]
        [Tooltip("Abah mengajak ke hutan di teras. File: s5_abah_ajak_teras.png (legacy, diganti standTalk/standIdle)")]
        public Sprite ajakTeras;
        [Tooltip("Reaksi bambu tua/kolot. File: abah_s5_reaksi_tua.png")]
        public Sprite reaksiTua;
        [Tooltip("Reaksi bambu tipis/kopong. File: s5_abah_reaksi_tipis.png (legacy)")]
        public Sprite reaksiTipis;
        [Tooltip("Reaksi bambu pas. File: abah_s5_reaksi_pas.png")]
        public Sprite reaksiPas;
        [Tooltip("Reaksi bambu muda/ijo. File: abah_s5_reaksi_muda.png (dipakai untuk tag Tipis)")]
        public Sprite reaksiMuda;

        [Header("Scene 5 - Teras Siang (berdiri, rombak S5)")]
        [Tooltip("Abah berbicara (berdiri). File: abah_s5_stand_talk.png")]
        public Sprite standTalk;
        [Tooltip("Abah diam (berdiri). File: abah_s5_stand_idle.png")]
        public Sprite standIdle;
        [Tooltip("Abah menerima bambu saat Rimba sampai titik berhenti. File: abah_s5_receivebamboo.png")]
        public Sprite receiveBamboo;

        [Header("Scene 6 - Assembly Bark (tanpa bubble)")]
        [Tooltip("Bark lancar saat crossing Cut->Tap. File: s6_abah_encourage.png")]
        public Sprite encourage;
        [Tooltip("Bark salah pertama. File: s6_abah_koreksi.png")]
        public Sprite koreksi;
    }
}
