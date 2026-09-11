using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    internal static class MartyrRules
    {
        static readonly MartyrPower[] Pool =
        {
            MartyrPower.Reinforcements,
            MartyrPower.FleetPawns,
            MartyrPower.Bombard,
            MartyrPower.UntouchableKing,
            MartyrPower.StasisField,
            MartyrPower.KnightAscension,
            MartyrPower.BattlefieldPromotion
        };

        public static DraftOffer BuildOffer(GameState state, ModeRuntime runtime, Side side)
        {
            var fresh = new List<MartyrPower>(Pool.Length);
            var unlocked = new List<MartyrPower>();
            for (int i = 0; i < Pool.Length; i++)
            {
                MartyrPower power = Pool[i];
                if (runtime.Unlocked(side, power))
                {
                    unlocked.Add(power);
                }
                else
                {
                    fresh.Add(power);
                }
            }

            var picked = new MartyrPower[3];
            int n = 0;
            while (n < 3 && fresh.Count > 0)
            {
                int index = StablePick(state, side, n, fresh.Count);
                picked[n] = fresh[index];
                fresh.RemoveAt(index);
                n++;
            }

            while (n < 3)
            {
                if (unlocked.Count == 0)
                {
                    picked[n] = Pool[n % Pool.Length];
                }
                else
                {
                    picked[n] = unlocked[StablePick(state, side, n + 10, unlocked.Count)];
                }

                n++;
            }

            PieceType battlefield = StablePick(state, side, 99, 2) == 0 ? PieceType.Knight : PieceType.Bishop;
            return new DraftOffer(picked[0], picked[1], picked[2], battlefield);
        }

        public static ModeRuntime Apply(
            GameState state,
            MartyrPower power,
            Guid? targetId,
            Square[] reinforcements,
            out Board board)
        {
            board = state.Board;
            ModeRuntime runtime = state.Runtime.Unlock(state.SideToMove, power).ConsumeDraftSlot(state.SideToMove);

            switch (power)
            {
                case MartyrPower.Reinforcements:
                    board = PlaceReinforcements(state, runtime, reinforcements, out runtime);
                    break;
                case MartyrPower.FleetPawns:
                case MartyrPower.Bombard:
                    break;
                case MartyrPower.UntouchableKing:
                    Square? king = state.Board.FindKing(state.SideToMove);
                    if (king != null)
                    {
                        Piece kingPiece = state.Board.GetPiece(king.Value);
                        runtime = runtime.WithStatus(
                            kingPiece.Id,
                            new PieceStatus(StatusKind.Invulnerable, state.SideToMove, 5));
                    }

                    break;
                case MartyrPower.StasisField:
                    runtime = ApplyStasis(state, runtime, targetId);
                    break;
                case MartyrPower.KnightAscension:
                    board = AscendKnights(state.Board, state.SideToMove);
                    break;
                case MartyrPower.BattlefieldPromotion:
                    board = PromotePawn(state, runtime, runtime.PendingBattlefieldType ?? PieceType.Knight, targetId, out runtime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(power), power, null);
            }

            return runtime;
        }

        static Board PlaceReinforcements(
            GameState state,
            ModeRuntime runtime,
            Square[] chosen,
            out ModeRuntime nextRuntime)
        {
            nextRuntime = runtime;
            Board board = state.Board;
            int back = state.SideToMove == Side.White ? 0 : 7;
            var empties = new List<Square>();
            for (int file = 0; file < Square.BoardSize; file++)
            {
                Square square = new Square(file, back);
                if (board.GetPiece(square) == null)
                {
                    empties.Add(square);
                }
            }

            var used = new HashSet<int>();
            int placed = 0;
            if (chosen != null)
            {
                for (int i = 0; i < chosen.Length && placed < 3; i++)
                {
                    Square square = chosen[i];
                    if (!square.IsOnBoard || square.Rank != back || board.GetPiece(square) != null)
                    {
                        continue;
                    }

                    Piece pawn = new Piece(PieceType.Pawn, state.SideToMove);
                    board = board.WithPiece(square, pawn);
                    nextRuntime = nextRuntime.AddSummoned(pawn.Id);
                    used.Add(square.File);
                    placed++;
                }
            }

            for (int i = 0; i < empties.Count && placed < 3; i++)
            {
                if (used.Contains(empties[i].File))
                {
                    continue;
                }

                Piece pawn = new Piece(PieceType.Pawn, state.SideToMove);
                board = board.WithPiece(empties[i], pawn);
                nextRuntime = nextRuntime.AddSummoned(pawn.Id);
                placed++;
            }

            return board;
        }

        static ModeRuntime ApplyStasis(GameState state, ModeRuntime runtime, Guid? targetId)
        {
            Piece queen = null;
            if (targetId != null)
            {
                Square? square = state.Board.FindSquare(targetId.Value);
                if (square != null)
                {
                    Piece piece = state.Board.GetPiece(square.Value);
                    if (piece != null && piece.Side != state.SideToMove && piece.Type == PieceType.Queen)
                    {
                        queen = piece;
                    }
                }
            }

            if (queen == null)
            {
                for (int i = 0; i < 64; i++)
                {
                    Piece piece = state.Board.GetPiece(Square.FromIndex(i));
                    if (piece != null && piece.Side != state.SideToMove && piece.Type == PieceType.Queen)
                    {
                        if (queen != null)
                        {
                            return runtime;
                        }

                        queen = piece;
                    }
                }
            }

            if (queen == null)
            {
                return runtime;
            }

            return runtime.WithStatus(queen.Id, new PieceStatus(StatusKind.Stasis, queen.Side, 3));
        }

        static Board AscendKnights(Board board, Side side)
        {
            Board next = board;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = board.GetPiece(square);
                if (piece != null && piece.Side == side && piece.Type == PieceType.Knight)
                {
                    next = next.WithPiece(square, piece.WithType(PieceType.Rook));
                }
            }

            return next;
        }

        static Board PromotePawn(
            GameState state,
            ModeRuntime runtime,
            PieceType type,
            Guid? targetId,
            out ModeRuntime nextRuntime)
        {
            nextRuntime = runtime;
            Piece pawn = null;
            Square? pawnSquare = null;
            if (targetId != null)
            {
                pawnSquare = state.Board.FindSquare(targetId.Value);
                if (pawnSquare != null)
                {
                    Piece piece = state.Board.GetPiece(pawnSquare.Value);
                    if (piece != null && piece.Side == state.SideToMove && piece.Type == PieceType.Pawn)
                    {
                        pawn = piece;
                    }
                }
            }

            if (pawn == null)
            {
                for (int i = 0; i < 64; i++)
                {
                    Square square = Square.FromIndex(i);
                    Piece piece = state.Board.GetPiece(square);
                    if (piece != null && piece.Side == state.SideToMove && piece.Type == PieceType.Pawn)
                    {
                        pawn = piece;
                        pawnSquare = square;
                        break;
                    }
                }
            }

            if (pawn == null || pawnSquare == null)
            {
                return state.Board;
            }

            if (nextRuntime.IsEmpowered(pawn.Id) && pawn.Type == PieceType.Pawn)
            {
                nextRuntime = nextRuntime.WithoutEmpowered(pawn.Id);
            }

            return state.Board.WithPiece(pawnSquare.Value, pawn.WithType(type));
        }

        static int StablePick(GameState state, Side side, int salt, int count)
        {
            if (count <= 1)
            {
                return 0;
            }

            int hash = state.FullmoveNumber * 397;
            hash = (hash * 31) + (int)side;
            hash = (hash * 31) + salt;
            hash = (hash * 31) + state.History.Count;
            if (hash < 0)
            {
                hash = -hash;
            }

            return hash % count;
        }
    }
}
