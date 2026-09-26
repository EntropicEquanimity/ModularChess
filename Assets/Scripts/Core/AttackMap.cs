using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    internal static class AttackMap
    {
        #region Fields
        static readonly List<PatternStep> RayBuffer = new List<PatternStep>(8);
        #endregion

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

            return IsAttackedByPawn(board, square, bySide, runtime, target)
                || IsAttackedByKnight(board, square, bySide, runtime, target)
                || IsAttackedByKing(board, square, bySide, runtime, target)
                || IsAttackedBySlider(
                    board,
                    square,
                    bySide,
                    Directions.BishopFiles,
                    Directions.BishopRanks,
                    PieceType.Bishop,
                    runtime,
                    target)
                || IsAttackedBySlider(
                    board,
                    square,
                    bySide,
                    Directions.RookFiles,
                    Directions.RookRanks,
                    PieceType.Rook,
                    runtime,
                    target);
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
            if (runtime.HasStatus(target.Id, StatusKind.Rearguard))
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
        private static bool CountsAsAttack(
            ModeRuntime runtime,
            Piece target,
            Square targetSquare,
            Square from)
        {
            if (target == null || target.Type != PieceType.Pawn || !IsEmpowered(runtime, target))
                return true;
            return Pattern.SuperPawnAllowsCapture(targetSquare, target.Side, from);
        }
        private static bool IsAttackedByPawn(
            Board board,
            Square target,
            Side bySide,
            ModeRuntime runtime,
            Piece occupant)
        {
            int rankDelta = bySide == Side.White ? -1 : 1;
            Square left = target.Offset(-1, rankDelta);
            Square right = target.Offset(1, rankDelta);
            return (HasPawnAttacker(board, left, bySide, runtime)
                    && CountsAsAttack(runtime, occupant, target, left))
                || (HasPawnAttacker(board, right, bySide, runtime)
                    && CountsAsAttack(runtime, occupant, target, right));
        }
        private static bool HasPawnAttacker(Board board, Square square, Side side, ModeRuntime runtime)
        {
            if (!HasAttacker(board, square, side, PieceType.Pawn, runtime))
            {
                return false;
            }
            return !IsEmpowered(runtime, board.GetPiece(square));
        }
        private static bool IsAttackedByKnight(
            Board board,
            Square target,
            Side bySide,
            ModeRuntime runtime,
            Piece occupant)
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
                    if (CountsAsAttack(runtime, occupant, target, from))
                        return true;
                    continue;
                }

                if (piece.Type == PieceType.Queen && IsEmpowered(runtime, piece))
                {
                    if (CountsAsAttack(runtime, occupant, target, from))
                        return true;
                }
            }

            return false;
        }
        private static bool IsAttackedByKing(
            Board board,
            Square target,
            Side bySide,
            ModeRuntime runtime,
            Piece occupant)
        {
            for (int i = 0; i < Directions.KingFiles.Length; i++)
            {
                Square from = target.Offset(Directions.KingFiles[i], Directions.KingRanks[i]);
                if (HasAttacker(board, from, bySide, PieceType.King, runtime)
                    && CountsAsAttack(runtime, occupant, target, from))
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
            ModeRuntime runtime,
            Piece occupant)
        {
            for (int i = 0; i < fileDeltas.Length; i++)
            {
                bool passedAlly = false;
                Pattern.Ray(board, target, fileDeltas[i], rankDeltas[i], RayBuffer);
                for (int s = 0; s < RayBuffer.Count; s++)
                {
                    PatternStep step = RayBuffer[s];
                    Piece piece = step.Occupant;
                    if (piece == null)
                    {
                        if (TerrainRules.BlocksMoveThrough(board, step.Square))
                        {
                            break;
                        }
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
                            if (CountsAsAttack(runtime, occupant, target, step.Square))
                                return true;
                            break;
                        }

                        break;
                    }

                    passedAlly = true;
                    if (slider == PieceType.Rook && piece.Side == bySide)
                    {
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
