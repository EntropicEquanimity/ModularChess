using System;
using ModularChess.Core;

namespace ModularChess.Presentation
{
    public interface IBoardView
    {
        event Action<Square> SquareClicked;

        void Bind(GameState state);
        void SetSelection(Square? square);
        void ClearSelection();
        void SetLastMove(Square from, Square to);
        void ClearLastMove();
    }
}
