using System;
using UnityEngine;

namespace ModularChess.Presentation
{
    [Serializable]
    public struct BoardTheme
    {
        public Color LightSquare;
        public Color DarkSquare;
        public Color Selected;
        public Color LegalMove;
        public Color LastMove;
        public Color WhitePieceFill;
        public Color WhitePieceGlyph;
        public Color WhitePieceOutline;
        public Color BlackPieceFill;
        public Color BlackPieceGlyph;
        public Color BlackPieceOutline;

        public bool IsConfigured => LightSquare.a > 0f && DarkSquare.a > 0f;

        public static BoardTheme Default => new BoardTheme
        {
            LightSquare = new Color32(240, 217, 181, 255),
            DarkSquare = new Color32(181, 136, 99, 255),
            Selected = new Color32(246, 246, 105, 180),
            LegalMove = new Color32(36, 130, 78, 210),
            LastMove = new Color32(205, 210, 106, 165),
            WhitePieceFill = new Color32(248, 247, 240, 255),
            WhitePieceGlyph = new Color32(28, 28, 28, 255),
            WhitePieceOutline = new Color32(36, 32, 34, 255),
            BlackPieceFill = new Color32(38, 34, 32, 255),
            BlackPieceGlyph = new Color32(245, 240, 228, 255),
            BlackPieceOutline = new Color32(236, 232, 220, 255)
        };
    }
}
