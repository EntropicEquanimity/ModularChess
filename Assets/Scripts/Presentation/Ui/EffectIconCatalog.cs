using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    [CreateAssetMenu(menuName = "ModularChess/Effect Icon Catalog", fileName = "EffectIconCatalog")]
    public sealed class EffectIconCatalog : ScriptableObject
    {
        #region Fields
        const string AssetPath = "Assets/Data/EffectIconCatalog.asset";
        static EffectIconCatalog _cached;
        [SerializeField] GameObject effectIconPrefab;
        [SerializeField] Sprite ironCurtain;
        [SerializeField] Sprite bloodDebt;
        [SerializeField] Sprite fogVision;
        [SerializeField] Sprite dustCloud;
        [SerializeField] Sprite reserveCall;
        [SerializeField] Sprite fleetPawns;
        [SerializeField] Sprite landmine;
        [SerializeField] Sprite overload;
        [SerializeField] Sprite reinforcements;
        [SerializeField] Sprite untouchableKing;
        [SerializeField] Sprite stasisField;
        [SerializeField] Sprite knightAscension;
        [SerializeField] Sprite battlefieldPromotion;
        [SerializeField] Sprite rally;
        [SerializeField] Sprite revival;
        [SerializeField] Sprite exile;
        [SerializeField] Sprite secondFront;
        [SerializeField] Sprite turncoat;
        [SerializeField] Sprite vanishingAct;
        [SerializeField] Sprite rearguard;
        #endregion

        #region Public Methods
        public GameObject EffectIconPrefab => effectIconPrefab;
        public static EffectIconCatalog Load()
        {
            if (_cached != null)
                return _cached;
#if UNITY_EDITOR
            _cached = UnityEditor.AssetDatabase.LoadAssetAtPath<EffectIconCatalog>(AssetPath);
#endif
            return _cached;
        }
        public Sprite SpriteFor(MartyrPower power)
        {
            switch (power)
            {
                case MartyrPower.Reinforcements: return reinforcements;
                case MartyrPower.FleetPawns: return fleetPawns;
                case MartyrPower.UntouchableKing: return untouchableKing;
                case MartyrPower.StasisField: return stasisField;
                case MartyrPower.KnightAscension: return knightAscension;
                case MartyrPower.BattlefieldPromotion: return battlefieldPromotion;
                case MartyrPower.Rally: return rally;
                case MartyrPower.Revival: return revival;
                case MartyrPower.Exile: return exile;
                case MartyrPower.SecondFront: return secondFront;
                case MartyrPower.IronCurtain: return ironCurtain;
                case MartyrPower.Turncoat: return turncoat;
                case MartyrPower.VanishingAct: return vanishingAct;
                case MartyrPower.BloodDebt: return bloodDebt;
                case MartyrPower.Rearguard: return rearguard;
                case MartyrPower.Overload: return overload;
                case MartyrPower.Landmine: return landmine;
                case MartyrPower.ReserveCall: return reserveCall;
                case MartyrPower.FogVision: return fogVision;
                case MartyrPower.DustCloud: return dustCloud;
                default: return null;
            }
        }
        #endregion
    }
}
