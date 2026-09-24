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
    }
}
