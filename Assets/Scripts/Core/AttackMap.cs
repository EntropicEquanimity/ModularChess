using System;

namespace ModularChess.Core
{
    internal static class AttackMap
    {
        #region Public Methods
        public static bool IsInCheck(
            Board board,
            Side side,
            MatchRules rules = null,
            ModeRuntime runtime = null)
        {
            Square? king = board.FindKing(side);
            if (king == null)
            {
                return false;
            }

            Piece kingPiece = board.GetPiece(king.Value);
            if (kingPiece != null && runtime != null && runtime.HasStatus(kingPiece.Id, StatusKind.Invulnerable))
            {
                return false;
            }

            return IsAttacked(board, king.Value, side.Opponent(), rules, runtime);
        }
        public static bool IsAttacked(
            Board board,
            Square square,
            Side bySide,
            MatchRules rules = null,
            ModeRuntime runtime = null)
        {
            if (!square.IsOnBoard)
            {
                return false;
            }

            Piece target = board.GetPiece(square);
            if (target != null && runtime != null && !CanTarget(runtime, target))
            {
                return false;
            }

            return IsAttackedByPawn(board, square, bySide, runtime)
                || IsAttackedByKnight(board, square, bySide, runtime)
                || IsAttackedByKing(board, square, bySide, runtime)
                || IsAttackedBySlider(
                    board,
                    square,
                    bySide,
                    Directions.BishopFiles,
                    Directions.BishopRanks,
                    PieceType.Bishop,
                    runtime)
                || IsAttackedBySlider(
                    board,
                    square,
                    bySide,
                    Directions.RookFiles,
                    Directions.RookRanks,
                    PieceType.Rook,
                    runtime);
        }
        public static bool CanTarget(ModeRuntime runtime, Piece target)
        {
            if (runtime == null || target == null)
            {
                return true;
            }

            if (runtime.HasStatus(target.Id, StatusKind.Invulnerable))
            {
                return false;
            }

            if (runtime.HasStatus(target.Id, StatusKind.Stasis))
            {
                return false;
            }

            return true;
        }
        #endregion

        #region Private Methods
        private static bool Attacks(ModeRuntime runtime, Piece piece)
        {
            if (piece == null)
            {
                return false;
            }

            if (runtime != null && runtime.HasStatus(piece.Id, StatusKind.Stasis))
            {
                return false;
            }

            return true;
        }
        private static bool IsEmpowered(ModeRuntime runtime, Piece piece)
        {
            return runtime != null && piece != null && runtime.IsEmpowered(piece.Id);
        }
        private static bool IsAttackedByPawn(Board board, Square target, Side bySide, ModeRuntime runtime)
        {
            int rankDelta = bySide == Side.White ? -1 : 1;
            return HasAttacker(board, target.Offset(-1, rankDelta), bySide, PieceType.Pawn, runtime)
                || HasAttacker(board, target.Offset(1, rankDelta), bySide, PieceType.Pawn, runtime);
        }
        private static bool IsAttackedByKnight(Board board, Square target, Side bySide, ModeRuntime runtime)
        {
            for (int i = 0; i < Directions.KnightFiles.Length; i++)
            {
                Square from = target.Offset(Directions.KnightFiles[i], Directions.KnightRanks[i]);
                if (!from.IsOnBoard)
                {
                    continue;
                }

                Piece piece = board.GetPiece(from);
                if (piece == null || piece.Side != bySide || !Attacks(runtime, piece))
                {
                    continue;
                }

                if (piece.Type == PieceType.Knight)
                {
                    return true;
                }

                if (piece.Type == PieceType.Queen && IsEmpowered(runtime, piece))
                {
                    return true;
                }
            }

            return false;
        }
        private static bool IsAttackedByKing(Board board, Square target, Side bySide, ModeRuntime runtime)
        {
            for (int i = 0; i < Directions.KingFiles.Length; i++)
            {
                Square from = target.Offset(Directions.KingFiles[i], Directions.KingRanks[i]);
                if (HasAttacker(board, from, bySide, PieceType.King, runtime))
                {
                    return true;
                }
            }

            return false;
        }
        private static bool IsAttackedBySlider(
            Board board,
            Square target,
            Side bySide,
            int[] fileDeltas,
            int[] rankDeltas,
            PieceType slider,
            ModeRuntime runtime)
        {
            for (int i = 0; i < fileDeltas.Length; i++)
            {
                bool passedAlly = false;
                Square cursor = target.Offset(fileDeltas[i], rankDeltas[i]);
                while (cursor.IsOnBoard)
                {
                    Piece piece = board.GetPiece(cursor);
                    if (piece == null)
                    {
                        cursor = cursor.Offset(fileDeltas[i], rankDeltas[i]);
                        continue;
                    }

                    if (piece.Side != bySide)
                    {
                        break;
                    }

                    bool isSlider = piece.Type == slider || piece.Type == PieceType.Queen;
                    if (isSlider && Attacks(runtime, piece))
                    {
                        bool rookPass = slider == PieceType.Rook
                            && piece.Type == PieceType.Rook
                            && IsEmpowered(runtime, piece);
                        if (!passedAlly || rookPass)
                        {
                            return true;
                        }

                        break;
                    }

                    passedAlly = true;
                    if (slider == PieceType.Rook && piece.Side == bySide)
                    {
                        cursor = cursor.Offset(fileDeltas[i], rankDeltas[i]);
                        continue;
                    }

                    break;
                }
            }

            return false;
        }
        private static bool HasAttacker(
            Board board,
            Square square,
            Side side,
            PieceType type,
            ModeRuntime runtime)
        {
            if (!square.IsOnBoard)
            {
                return false;
            }

            Piece piece = board.GetPiece(square);
            return piece != null
                && piece.Side == side
                && piece.Type == type
                && Attacks(runtime, piece);
        }
        #endregion
    }
}
