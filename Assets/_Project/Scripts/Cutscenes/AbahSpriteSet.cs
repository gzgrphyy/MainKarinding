using UnityEngine;

namespace MainKarinding.Cutscenes
{
    [CreateAssetMenu(fileName = "AbahSpriteSet", menuName = "Main Karinding/Cutscenes/Character Sprite Set/Abah")]
    public class AbahSpriteSet : ScriptableObject
    {
        [Header("Scene 1 - Teras Rumah")]
        public Sprite idleTerasTalking;
        public Sprite lookoutFacingMountain;
    }
}
