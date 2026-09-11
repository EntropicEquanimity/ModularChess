using System;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public static class ChessArt
    {
        public static Sprite Get(PieceType type, Side side)
        {
            string path = PathFor(type, side);
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(path))
            {
                Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }
#endif
            return null;
        }

        static string PathFor(PieceType type, Side side)
        {
            bool white = side == Side.White;
            string folder = white ? "White Pieces" : "Black Pieces";
            string name;
            switch (type)
            {
                case PieceType.Pawn:
                    name = white ? "spr_pawn_white.png" : "spr_pawn_black.png";
                    break;
                case PieceType.Knight:
                    name = white ? "spr_knight_white.png" : "spr_knight_black.png";
                    break;
                case PieceType.Bishop:
                    name = white ? "spr_bishop_white.png" : "spr_bishop_black.png";
                    break;
                case PieceType.Rook:
                    name = white ? "spr_tower_white.png" : "spr_tower_black.png";
                    break;
                case PieceType.Queen:
                    name = white ? "spr_queen_white.png" : "spr_queen_black.png";
                    break;
                case PieceType.King:
                    name = white ? "spr_king_white.png" : "spr_king_black.png";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return $"Assets/Sprites/Simple_Chess_by_skyel/{folder}/{name}";
        }
    }
}
