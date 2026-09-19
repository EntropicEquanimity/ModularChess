using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    [CreateAssetMenu(menuName = "ModularChess/Chess Art Set", fileName = "ChessArt")]
    public sealed class ChessArtSet : ScriptableObject
    {
        #region Fields
        [SerializeField] Sprite whitePawn;
        [SerializeField] Sprite whiteKnight;
        [SerializeField] Sprite whiteBishop;
        [SerializeField] Sprite whiteRook;
        [SerializeField] Sprite whiteQueen;
        [SerializeField] Sprite whiteKing;
        [SerializeField] Sprite blackPawn;
        [SerializeField] Sprite blackKnight;
        [SerializeField] Sprite blackBishop;
        [SerializeField] Sprite blackRook;
        [SerializeField] Sprite blackQueen;
        [SerializeField] Sprite blackKing;
        #endregion

        #region Public Methods
        public Sprite Get(PieceType type, Side side)
        {
            bool white = side == Side.White;
            switch (type)
            {
                case PieceType.Pawn:
                    return white ? whitePawn : blackPawn;
                case PieceType.Knight:
                    return white ? whiteKnight : blackKnight;
                case PieceType.Bishop:
                    return white ? whiteBishop : blackBishop;
                case PieceType.Rook:
                    return white ? whiteRook : blackRook;
                case PieceType.Queen:
                    return white ? whiteQueen : blackQueen;
                case PieceType.King:
                    return white ? whiteKing : blackKing;
                default:
                    return null;
            }
        }
        #endregion
    }
}
