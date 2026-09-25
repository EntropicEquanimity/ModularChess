using TMPro;
using UnityEngine;

namespace ModularChess.Presentation
{
    public sealed class MeritIndicator : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_Text amount;
        #endregion

        #region Unity
        void OnEnable()
        {
            MeritWallet.Changed += Refresh;
            Refresh();
        }
        void OnDisable()
        {
            MeritWallet.Changed -= Refresh;
        }
        #endregion

        #region Public Methods
        public void Refresh()
        {
            if (amount != null)
                amount.text = MeritWallet.Balance.ToString();
        }
        #endregion
    }
}
