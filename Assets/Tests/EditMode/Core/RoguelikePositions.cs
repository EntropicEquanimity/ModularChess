using System;
using ModularChess.Core;

namespace ModularChess.Core.Tests
{
    internal static class RoguelikePositions
    {
        public static GameState KingsOnly(MatchRules rules)
        {
            Board board = Board.Empty()
                .WithPiece(Sq("e1"), new Piece(PieceType.King, Side.White))
                .WithPiece(Sq("e8"), new Piece(PieceType.King, Side.Black));
            return GameState.FromPosition(board, Side.White, null, CastlingRights.None, 0, 1, rules: rules);
        }
        public static GameState WhiteRookOnA1(MatchRules rules)
        {
            Board board = Board.Empty()
                .WithPiece(Sq("e1"), new Piece(PieceType.King, Side.White))
                .WithPiece(Sq("a1"), new Piece(PieceType.Rook, Side.White))
                .WithPiece(Sq("e8"), new Piece(PieceType.King, Side.Black));
            return GameState.FromPosition(board, Side.White, null, CastlingRights.None, 0, 1, rules: rules);
        }
        public static GameState WhiteInCheckWithKnightEscape(MatchRules rules)
        {
            Board board = Board.Empty()
                .WithPiece(Sq("e1"), new Piece(PieceType.King, Side.White))
                .WithPiece(Sq("b1"), new Piece(PieceType.Knight, Side.White))
                .WithPiece(Sq("e8"), new Piece(PieceType.King, Side.Black))
                .WithPiece(Sq("e4"), new Piece(PieceType.Rook, Side.Black));
            return GameState.FromPosition(board, Side.White, null, CastlingRights.None, 0, 1, rules: rules);
        }
        public static GameState WhiteQueenAttacksEnemyKing(MatchRules rules)
        {
            Board board = Board.Empty()
                .WithPiece(Sq("e1"), new Piece(PieceType.King, Side.White))
                .WithPiece(Sq("e5"), new Piece(PieceType.Queen, Side.White))
                .WithPiece(Sq("e8"), new Piece(PieceType.King, Side.Black));
            return GameState.FromPosition(board, Side.White, null, CastlingRights.None, 0, 1, rules: rules);
        }
        public static GameState BlackQueenAttacksPlayerKing(MatchRules rules)
        {
            Board board = Board.Empty()
                .WithPiece(Sq("e1"), new Piece(PieceType.King, Side.White))
                .WithPiece(Sq("e8"), new Piece(PieceType.King, Side.Black))
                .WithPiece(Sq("e4"), new Piece(PieceType.Queen, Side.Black));
            return GameState.FromPosition(board, Side.Black, null, CastlingRights.None, 0, 1, rules: rules);
        }
        public static GameState WhitePawnOnSeventh(MatchRules rules)
        {
            Board board = Board.Empty()
                .WithPiece(Sq("e1"), new Piece(PieceType.King, Side.White))
                .WithPiece(Sq("a7"), new Piece(PieceType.Pawn, Side.White, true))
                .WithPiece(Sq("e8"), new Piece(PieceType.King, Side.Black));
            return GameState.FromPosition(board, Side.White, null, CastlingRights.None, 0, 1, rules: rules);
        }
        public static GameState WhiteKingAloneNoMoves(MatchRules rules)
        {
            Board board = Board.Empty()
                .WithPiece(Sq("a1"), new Piece(PieceType.King, Side.White))
                .WithPiece(Sq("b3"), new Piece(PieceType.Queen, Side.Black))
                .WithPiece(Sq("c2"), new Piece(PieceType.Queen, Side.Black))
                .WithPiece(Sq("e8"), new Piece(PieceType.King, Side.Black));
            return GameState.FromPosition(board, Side.White, null, CastlingRights.None, 0, 1, rules: rules);
        }
        static Square Sq(string algebraic)
        {
            if (!Square.TryParse(algebraic, out Square square))
                throw new ArgumentException(algebraic);
            return square;
        }
    }
}
