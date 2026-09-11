using System;
using System.Collections.Generic;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public static class ChessGlyphs
    {
        const int Width = 5;
        const int Height = 7;
        const int Scale = 6;
        const int Padding = 4;

        static readonly Dictionary<PieceType, Sprite> Sprites = new Dictionary<PieceType, Sprite>();

        public static string GetLetter(PieceType type)
        {
            return type switch
            {
                PieceType.Pawn => "P",
                PieceType.Knight => "N",
                PieceType.Bishop => "B",
                PieceType.Rook => "R",
                PieceType.Queen => "Q",
                PieceType.King => "K",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static string GetUnicode(PieceType type, Side side)
        {
            return (type, side) switch
            {
                (PieceType.King, Side.White) => "\u2654",
                (PieceType.Queen, Side.White) => "\u2655",
                (PieceType.Rook, Side.White) => "\u2656",
                (PieceType.Bishop, Side.White) => "\u2657",
                (PieceType.Knight, Side.White) => "\u2658",
                (PieceType.Pawn, Side.White) => "\u2659",
                (PieceType.King, Side.Black) => "\u265A",
                (PieceType.Queen, Side.Black) => "\u265B",
                (PieceType.Rook, Side.Black) => "\u265C",
                (PieceType.Bishop, Side.Black) => "\u265D",
                (PieceType.Knight, Side.Black) => "\u265E",
                (PieceType.Pawn, Side.Black) => "\u265F",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static Sprite GetSprite(PieceType type, Side side)
        {
            Sprite art = ChessArt.Get(type, side);
            if (art != null)
            {
                return art;
            }

            return GetSprite(type);
        }

        public static Sprite GetSprite(PieceType type)
        {
            if (Sprites.TryGetValue(type, out Sprite sprite) && sprite != null)
                return sprite;

            sprite = Rasterize(GetPattern(type));
            Sprites[type] = sprite;
            return sprite;
        }

        static string GetPattern(PieceType type)
        {
            return type switch
            {
                PieceType.Pawn =>
                    "01110" +
                    "10001" +
                    "10001" +
                    "11110" +
                    "10000" +
                    "10000" +
                    "10000",
                PieceType.Knight =>
                    "10001" +
                    "11001" +
                    "10101" +
                    "10011" +
                    "10001" +
                    "10001" +
                    "10001",
                PieceType.Bishop =>
                    "11110" +
                    "10001" +
                    "10000" +
                    "11110" +
                    "10001" +
                    "10001" +
                    "11110",
                PieceType.Rook =>
                    "11110" +
                    "10001" +
                    "10001" +
                    "11110" +
                    "10100" +
                    "10010" +
                    "10001",
                PieceType.Queen =>
                    "01110" +
                    "10001" +
                    "10001" +
                    "10001" +
                    "10001" +
                    "10010" +
                    "01101",
                PieceType.King =>
                    "10001" +
                    "10001" +
                    "01010" +
                    "00100" +
                    "01010" +
                    "10001" +
                    "10001",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        static Sprite Rasterize(string pattern)
        {
            int texW = Width * Scale + Padding * 2;
            int texH = Height * Scale + Padding * 2;
            var texture = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[texW * texH];
            for (int gy = 0; gy < Height; gy++)
            {
                for (int gx = 0; gx < Width; gx++)
                {
                    if (pattern[gy * Width + gx] != '1')
                        continue;

                    int destX0 = Padding + gx * Scale;
                    int destY0 = Padding + (Height - 1 - gy) * Scale;
                    for (int sy = 0; sy < Scale; sy++)
                    {
                        for (int sx = 0; sx < Scale; sx++)
                            pixels[(destY0 + sy) * texW + destX0 + sx] = Color.white;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            float pixelsPerUnit = Mathf.Max(texW, texH);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texW, texH),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
