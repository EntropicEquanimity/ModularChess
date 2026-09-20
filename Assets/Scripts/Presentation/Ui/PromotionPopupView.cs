using System;
using ModularChess.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class PromotionPopupView : MonoBehaviour
    {
        #region Fields
        static readonly PieceType[] Options =
        {
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight
        };
        [SerializeField] Button[] buttons;
        [SerializeField] Image[] icons;
        Action<PieceType> _onChosen;
        #endregion

        #region Public Methods
        public void Bind(Action<PieceType> onChosen)
        {
            Resolve();
            _onChosen = onChosen;
            for (int i = 0; i < Options.Length; i++)
            {
                if (buttons == null || i >= buttons.Length || buttons[i] == null) { continue; }
                PieceType type = Options[i];
                GameAudio.Bind(buttons[i], () => _onChosen?.Invoke(type));
            }
        }
        public void SetSide(Side side)
        {
            Resolve();
            if (icons == null) { return; }  
            for (int i = 0; i < Options.Length && i < icons.Length; i++)
            {
                if (icons[i] != null) { icons[i].sprite = ChessGlyphs.GetSprite(Options[i], side); }
            }
        }
        public Button FirstButton
        {
            get
            {
                Resolve();
                return buttons != null && buttons.Length > 0 ? buttons[0] : null;
            }
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (buttons == null || buttons.Length == 0) { buttons = GetComponentsInChildren<Button>(true); }
            if (icons == null || icons.Length == 0)
            {
                if (buttons == null) { return; }
                icons = new Image[buttons.Length];
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == null) { continue; }
                    icons[i] = buttons[i].GetComponentInChildren<Image>(true);
                }
            }
        }
        #endregion
    }
}
