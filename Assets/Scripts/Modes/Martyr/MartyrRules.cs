using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    internal static class MartyrRules
    {
        const int ReinforcementCount = 2;
        const int RearguardCount = 2;
        static readonly MartyrPower[] NotInUse =
        {
            MartyrPower.Bombard,
            MartyrPower.Phalanx
        };
        static readonly MartyrPower[] Pool =
        {
            MartyrPower.Reinforcements,
            MartyrPower.FleetPawns,
            MartyrPower.UntouchableKing,
            MartyrPower.StasisField,
            MartyrPower.KnightAscension,
            MartyrPower.BattlefieldPromotion,
            MartyrPower.Rally,
            MartyrPower.Revival,
            MartyrPower.Exile,
            MartyrPower.SecondFront,
            MartyrPower.IronCurtain,
            MartyrPower.Turncoat,
            MartyrPower.VanishingAct,
            MartyrPower.BloodDebt,
            MartyrPower.Rearguard,
            MartyrPower.Overload,
            MartyrPower.Landmine,
            MartyrPower.ReserveCall,
            MartyrPower.FogVision,
            MartyrPower.DustCloud
        };

        public static DraftOffer BuildOffer(GameState state, ModeRuntime runtime, Side side, Board board = null)
        {
            board = board ?? state.Board;
            var eligible = new List<MartyrPower>(Pool.Length);
            for (int i = 0; i < Pool.Length; i++)
            {
                MartyrPower power = Pool[i];
                if (!IsOffered(power) || !IsRelevant(board, state.Rules, runtime, side, power) || !CanObtain(runtime, side, power))
                {
                    continue;
                }
                eligible.Add(power);
            }
            if (eligible.Count == 0)
            {
                eligible.Add(MartyrPower.Rally);
            }
            int want = state.Rules.Settings.MartyrDraftOptions;
            if (want < 1) want = 3;
            if (want > 5) want = 5;
            if (want > eligible.Count) want = eligible.Count;
            var bag = new List<MartyrPower>(eligible);
            var picked = new List<MartyrPower>(want);
            for (int i = 0; i < want; i++)
            {
                picked.Add(TakePick(state, side, i, bag));
            }
            PieceType battlefield = StablePick(state, side, 99, 2) == 0 ? PieceType.Knight : PieceType.Bishop;
            return new DraftOffer(picked, battlefield);
        }

        public static ModeRuntime Apply(
            GameState state,
            MartyrPower power,
            Guid? targetId,
            Square[] reinforcements,
            out Board board)
        {
            board = state.Board;
            PieceType battlefield = state.Runtime.PendingBattlefieldType ?? PieceType.Knight;
            ModeRuntime runtime = state.Runtime.Unlock(state.SideToMove, power);
            switch (power)
            {
                case MartyrPower.Reinforcements:
                    board = PlaceReinforcements(state, runtime, reinforcements, out runtime);
                    break;
                case MartyrPower.FleetPawns:
                    break;
                case MartyrPower.Bombard:
                    break;
                case MartyrPower.UntouchableKing:
                    Square? king = state.Board.FindKing(state.SideToMove);
                    if (king != null)
                    {
                        Piece kingPiece = state.Board.GetPiece(king.Value);
                        runtime = runtime.WithStatus(
                            kingPiece.Id,
                            new PieceStatus(StatusKind.Invulnerable, state.SideToMove, 3));
                    }
                    break;
                case MartyrPower.StasisField:
                    runtime = ApplyStasis(state, runtime, targetId);
                    break;
                case MartyrPower.KnightAscension:
                    board = AscendKnights(state.Board, state.SideToMove);
                    break;
                case MartyrPower.BattlefieldPromotion:
                    board = PromotePawn(state, runtime, battlefield, targetId, out runtime);
                    break;
                case MartyrPower.Rally:
                    runtime = runtime.WithRally(true);
                    break;
                case MartyrPower.Revival:
                    board = ApplyRevival(state, runtime, out runtime);
                    break;
                case MartyrPower.Exile:
                    board = ApplyExile(state, runtime, targetId, out runtime);
                    break;
                case MartyrPower.SecondFront:
                    board = PlaceSecondFront(state, runtime, reinforcements, out runtime);
                    break;
                case MartyrPower.IronCurtain:
                    runtime = runtime.WithIronCurtain(state.SideToMove, 2);
                    break;
                case MartyrPower.Turncoat:
                    board = ApplyTurncoat(state, runtime, targetId, out runtime);
                    break;
                case MartyrPower.VanishingAct:
                    board = ApplyVanishingAct(state, targetId, reinforcements);
                    break;
                case MartyrPower.BloodDebt:
                    runtime = runtime.AddBloodDebt(state.SideToMove);
                    break;
                case MartyrPower.Rearguard:
                    runtime = ApplyRearguard(state, runtime, reinforcements);
                    break;
                case MartyrPower.Overload:
                    runtime = ApplyOverload(state, runtime, targetId);
                    break;
                case MartyrPower.Landmine:
                    runtime = PlaceLandmine(state, runtime, reinforcements);
                    break;
                case MartyrPower.ReserveCall:
                    runtime = runtime.WithReserveCall(state.SideToMove, true);
                    break;
                case MartyrPower.FogVision:
                    runtime = runtime.WithFogVision(state.SideToMove, 1);
                    break;
                case MartyrPower.DustCloud:
                    runtime = runtime.WithDustCloud(state.SideToMove, 2);
                    break;
                case MartyrPower.Phalanx:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(power), power, null);
            }
            return runtime.ConsumeDraftSlot(state.SideToMove);
        }
        public static Board ResolveExpiredExiles(Board board, ModeRuntime runtime, Side sideThatEnded, out ModeRuntime nextRuntime)
        {
            nextRuntime = runtime.TickExiles(sideThatEnded);
            Board nextBoard = board;
            for (int i = nextRuntime.Captures.Count - 1; i >= 0; i--)
            {
                CaptureRecord record = nextRuntime.Captures[i];
                if (!record.Exiled || record.RemainingTurns > 0) continue;
                Square? dest = ReturnSquare(nextBoard, record);
                if (dest != null)
                {
                    Piece piece = new Piece(record.Type, record.Side, record.HasMoved, record.Id);
                    nextBoard = nextBoard.WithPiece(dest.Value, piece);
                }
                nextRuntime = nextRuntime.RemoveCaptureAt(i);
            }
            return nextBoard;
        }
        public static int ObtainLimit(MartyrPower power)
        {
            switch (power)
            {
                case MartyrPower.FleetPawns:
                case MartyrPower.UntouchableKing:
                case MartyrPower.StasisField:
                case MartyrPower.Rally:
                case MartyrPower.IronCurtain:
                case MartyrPower.Turncoat:
                case MartyrPower.Overload:
                case MartyrPower.ReserveCall:
                case MartyrPower.FogVision:
                case MartyrPower.Bombard:
                case MartyrPower.Phalanx:
                    return 1;
                case MartyrPower.SecondFront:
                case MartyrPower.VanishingAct:
                case MartyrPower.BloodDebt:
                case MartyrPower.Rearguard:
                case MartyrPower.DustCloud:
                    return 2;
                case MartyrPower.Revival:
                case MartyrPower.Exile:
                case MartyrPower.Landmine:
                    return 3;
                case MartyrPower.Reinforcements:
                case MartyrPower.BattlefieldPromotion:
                case MartyrPower.KnightAscension:
                    return int.MaxValue;
                default:
                    return 0;
            }
        }
        public static bool IsOwnHalf(Square square, Side side)
        {
            if (!square.IsOnBoard) return false;
            return side == Side.White ? square.Rank <= 3 : square.Rank >= 4;
        }
        public static bool IsBackTwoRanks(Square square, Side side)
        {
            if (!square.IsOnBoard) return false;
            return side == Side.White ? square.Rank <= 1 : square.Rank >= 6;
        }
        public static bool IsAbsolutelyPinned(Board board, Square square, MatchRules rules, ModeRuntime runtime)
        {
            Piece piece = board.GetPiece(square);
            if (piece == null || piece.Type == PieceType.King) return false;
            if (AttackMap.IsInCheck(board, piece.Side, rules, runtime)) return false;
            Board without = board.WithPiece(square, null);
            return AttackMap.IsInCheck(without, piece.Side, rules, runtime);
        }
        public static bool CanExile(Board board, Square square, MatchRules rules, ModeRuntime runtime, Side draftingSide)
        {
            Piece piece = board.GetPiece(square);
            if (piece == null || piece.Side == draftingSide || piece.Type == PieceType.King) return false;
            return !IsAbsolutelyPinned(board, square, rules, runtime);
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
            int placed = 0;
            if (chosen != null)
            {
                for (int i = 0; i < chosen.Length && placed < ReinforcementCount; i++)
                {
                    if (TryPlacePawn(ref board, ref nextRuntime, state.SideToMove, chosen[i], back))
                    {
                        placed++;
                    }
                }
            }
            for (int file = 0; file < Square.BoardSize && placed < ReinforcementCount; file++)
            {
                if (TryPlacePawn(ref board, ref nextRuntime, state.SideToMove, new Square(file, back), back))
                {
                    placed++;
                }
            }
            return board;
        }
        static bool TryPlacePawn(ref Board board, ref ModeRuntime runtime, Side side, Square square, int back)
        {
            if (!square.IsOnBoard || square.Rank != back || !board.CanPlace(square)) return false;
            Piece pawn = new Piece(PieceType.Pawn, side);
            board = board.WithPiece(square, pawn);
            if (board.GetPiece(square) == null || board.GetPiece(square).Type != PieceType.Pawn) return false;
            runtime = runtime.AddSummoned(pawn.Id);
            return true;
        }
        static Board PlaceSecondFront(
            GameState state,
            ModeRuntime runtime,
            Square[] chosen,
            out ModeRuntime nextRuntime)
        {
            nextRuntime = runtime;
            Board board = state.Board;
            Side side = state.SideToMove;
            if (chosen != null)
            {
                for (int i = 0; i < chosen.Length; i++)
                {
                    if (TryPlaceKnight(ref board, ref nextRuntime, side, chosen[i]))
                    {
                        return board;
                    }
                }
            }
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                if (TryPlaceKnight(ref board, ref nextRuntime, side, square))
                {
                    return board;
                }
            }
            return board;
        }
        static bool TryPlaceKnight(ref Board board, ref ModeRuntime runtime, Side side, Square square)
        {
            if (!IsBackTwoRanks(square, side) || !board.CanPlace(square)) return false;
            Piece knight = new Piece(PieceType.Knight, side);
            board = board.WithPiece(square, knight);
            runtime = runtime.AddSummoned(knight.Id);
            return true;
        }
        static Board ApplyTurncoat(
            GameState state,
            ModeRuntime runtime,
            Guid? targetId,
            out ModeRuntime nextRuntime)
        {
            nextRuntime = runtime;
            Piece target = null;
            Square? targetSquare = null;
            if (targetId != null)
            {
                targetSquare = state.Board.FindSquare(targetId.Value);
                if (targetSquare != null)
                {
                    Piece piece = state.Board.GetPiece(targetSquare.Value);
                    if (piece != null && piece.Side != state.SideToMove && piece.Type == PieceType.Pawn
                        && IsOwnHalf(targetSquare.Value, state.SideToMove))
                    {
                        target = piece;
                    }
                }
            }
            if (target == null)
            {
                for (int i = 0; i < 64; i++)
                {
                    Square square = Square.FromIndex(i);
                    Piece piece = state.Board.GetPiece(square);
                    if (piece == null || piece.Side == state.SideToMove || piece.Type != PieceType.Pawn)
                        continue;
                    if (!IsOwnHalf(square, state.SideToMove)) continue;
                    if (target != null) return state.Board;
                    target = piece;
                    targetSquare = square;
                }
            }
            if (target == null || targetSquare == null) return state.Board;
            if (!IsOwnHalf(targetSquare.Value, state.SideToMove)) return state.Board;
            Piece converted = new Piece(PieceType.Pawn, state.SideToMove, true, target.Id);
            return state.Board.WithPiece(targetSquare.Value, converted);
        }
        static Board ApplyVanishingAct(GameState state, Guid? targetId, Square[] destinations)
        {
            Piece piece = null;
            Square? from = null;
            if (targetId != null)
            {
                from = state.Board.FindSquare(targetId.Value);
                if (from != null)
                {
                    Piece candidate = state.Board.GetPiece(from.Value);
                    if (candidate != null
                        && candidate.Side == state.SideToMove
                        && IsThreatenedHighValue(state.Board, state.Rules, state.Runtime, from.Value, candidate))
                    {
                        piece = candidate;
                    }
                }
            }
            if (piece == null || from == null) return state.Board;
            Square? dest = null;
            if (destinations != null)
            {
                for (int i = 0; i < destinations.Length; i++)
                {
                    if (IsOwnHalf(destinations[i], state.SideToMove) && state.Board.CanPlace(destinations[i]))
                    {
                        dest = destinations[i];
                        break;
                    }
                }
            }
            if (dest == null)
            {
                for (int i = 0; i < 64; i++)
                {
                    Square square = Square.FromIndex(i);
                    if (IsOwnHalf(square, state.SideToMove) && state.Board.CanPlace(square))
                    {
                        dest = square;
                        break;
                    }
                }
            }
            if (dest == null) return state.Board;
            return state.Board.WithPiece(from.Value, null).WithPiece(dest.Value, piece);
        }
        static ModeRuntime ApplyRearguard(GameState state, ModeRuntime runtime, Square[] chosen)
        {
            ModeRuntime next = runtime;
            int applied = 0;
            if (chosen != null)
            {
                for (int i = 0; i < chosen.Length && applied < RearguardCount; i++)
                {
                    if (TryApplyRearguard(state, ref next, chosen[i])) applied++;
                }
            }
            if (applied >= RearguardCount) return next;
            for (int i = 0; i < 64 && applied < RearguardCount; i++)
            {
                if (TryApplyRearguard(state, ref next, Square.FromIndex(i))) applied++;
            }
            return next;
        }
        static bool TryApplyRearguard(GameState state, ref ModeRuntime runtime, Square square)
        {
            Piece piece = state.Board.GetPiece(square);
            if (piece == null
                || piece.Side != state.SideToMove
                || piece.Type != PieceType.Pawn
                || !IsBackTwoRanks(square, state.SideToMove)
                || runtime.HasStatus(piece.Id, StatusKind.Rearguard))
            {
                return false;
            }
            runtime = runtime.WithStatus(piece.Id, new PieceStatus(StatusKind.Rearguard, state.SideToMove, 3));
            return true;
        }
        static ModeRuntime ApplyOverload(GameState state, ModeRuntime runtime, Guid? targetId)
        {
            Piece target = null;
            if (targetId != null)
            {
                Square? square = state.Board.FindSquare(targetId.Value);
                if (square != null)
                {
                    Piece piece = state.Board.GetPiece(square.Value);
                    if (piece != null && piece.Side == state.SideToMove && piece.Type != PieceType.King)
                    {
                        target = piece;
                    }
                }
            }
            if (target == null)
            {
                for (int i = 0; i < 64; i++)
                {
                    Piece piece = state.Board.GetPiece(Square.FromIndex(i));
                    if (piece == null || piece.Side != state.SideToMove || piece.Type == PieceType.King) continue;
                    if (target != null) return runtime;
                    target = piece;
                }
            }
            if (target == null) return runtime;
            return runtime.WithOverload(target.Id, 0);
        }
        static ModeRuntime PlaceLandmine(GameState state, ModeRuntime runtime, Square[] chosen)
        {
            Side side = state.SideToMove;
            if (chosen != null)
            {
                for (int i = 0; i < chosen.Length; i++)
                {
                    if (IsOwnHalf(chosen[i], side) && state.Board.CanPlace(chosen[i]) && !runtime.TryGetLandmine(chosen[i], out _))
                    {
                        return runtime.AddLandmine(side, chosen[i]);
                    }
                }
            }
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                if (IsOwnHalf(square, side) && state.Board.CanPlace(square) && !runtime.TryGetLandmine(square, out _))
                {
                    return runtime.AddLandmine(side, square);
                }
            }
            return runtime;
        }
        static bool CanObtain(ModeRuntime runtime, Side side, MartyrPower power)
        {
            return runtime.ObtainCount(side, power) < ObtainLimit(power);
        }
        static bool IsOffered(MartyrPower power)
        {
            for (int i = 0; i < NotInUse.Length; i++)
            {
                if (NotInUse[i] == power) return false;
            }
            return true;
        }
        static bool IsRelevant(Board board, MatchRules rules, ModeRuntime runtime, Side side, MartyrPower power)
        {
            switch (power)
            {
                case MartyrPower.Reinforcements:
                    return CountEmptyBackRank(board, side) > 0;
                case MartyrPower.StasisField:
                    return HasPiece(board, side.Opponent(), PieceType.Queen);
                case MartyrPower.KnightAscension:
                    return HasPiece(board, side, PieceType.Knight);
                case MartyrPower.BattlefieldPromotion:
                    return HasPiece(board, side, PieceType.Pawn);
                case MartyrPower.Revival:
                    return LastFriendlyCaptureIndex(runtime, side) >= 0;
                case MartyrPower.Exile:
                    return HasExilableEnemy(board, rules, runtime, side);
                case MartyrPower.IronCurtain:
                    return HasPieceOnBackTwo(board, side);
                case MartyrPower.VanishingAct:
                    return HasThreatenedHighValue(board, rules, runtime, side);
                case MartyrPower.Overload:
                    return !AttackMap.IsInCheck(board, side, rules, runtime);
                case MartyrPower.FogVision:
                    return rules != null && rules.Has(ModeId.FogOfWar);
                case MartyrPower.SecondFront:
                    return CountEmptyBackTwo(board, side) > 0;
                case MartyrPower.Turncoat:
                    return HasEnemyPawnOnOwnHalf(board, side);
                case MartyrPower.Rearguard:
                    return CountPawnsOnBackTwo(board, side) >= 1;
                case MartyrPower.Landmine:
                    return CountEmptyOwnHalf(board, side) > 0;
                default:
                    return true;
            }
        }
        static int CountEmptyBackRank(Board board, Side side)
        {
            int back = side == Side.White ? 0 : 7;
            int empty = 0;
            for (int file = 0; file < Square.BoardSize; file++)
            {
                if (board.CanPlace(new Square(file, back))) empty++;
            }
            return empty;
        }
        static int CountEmptyBackTwo(Board board, Side side)
        {
            int empty = 0;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                if (IsBackTwoRanks(square, side) && board.CanPlace(square)) empty++;
            }
            return empty;
        }
        static int CountEmptyOwnHalf(Board board, Side side)
        {
            int empty = 0;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                if (IsOwnHalf(square, side) && board.CanPlace(square)) empty++;
            }
            return empty;
        }
        static int CountPawnsOnBackTwo(Board board, Side side)
        {
            int count = 0;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = board.GetPiece(square);
                if (piece != null
                    && piece.Side == side
                    && piece.Type == PieceType.Pawn
                    && IsBackTwoRanks(square, side))
                {
                    count++;
                }
            }
            return count;
        }
        static bool HasPieceOnBackTwo(Board board, Side side)
        {
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = board.GetPiece(square);
                if (piece != null && piece.Side == side && IsBackTwoRanks(square, side)) return true;
            }
            return false;
        }
        static bool HasThreatenedHighValue(Board board, MatchRules rules, ModeRuntime runtime, Side side)
        {
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = board.GetPiece(square);
                if (piece != null && piece.Side == side && IsThreatenedHighValue(board, rules, runtime, square, piece))
                {
                    return true;
                }
            }
            return false;
        }
        static bool IsThreatenedHighValue(
            Board board,
            MatchRules rules,
            ModeRuntime runtime,
            Square square,
            Piece piece)
        {
            int? value = PieceValues.Get(piece.Type);
            if (value == null || value.Value <= 1) return false;
            return AttackMap.IsAttacked(board, square, piece.Side.Opponent(), rules, runtime);
        }
        static bool HasEnemyPawnOnOwnHalf(Board board, Side side)
        {
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = board.GetPiece(square);
                if (piece != null
                    && piece.Side != side
                    && piece.Type == PieceType.Pawn
                    && IsOwnHalf(square, side))
                {
                    return true;
                }
            }
            return false;
        }
        static bool HasPiece(Board board, Side side, PieceType type)
        {
            for (int i = 0; i < 64; i++)
            {
                Piece piece = board.GetPiece(Square.FromIndex(i));
                if (piece != null && piece.Side == side && piece.Type == type) return true;
            }
            return false;
        }
        static MartyrPower TakePick(GameState state, Side side, int salt, List<MartyrPower> bag)
        {
            int index = StablePick(state, side, salt, bag.Count);
            MartyrPower power = bag[index];
            bag.RemoveAt(index);
            return power;
        }
        static bool HasExilableEnemy(Board board, MatchRules rules, ModeRuntime runtime, Side side)
        {
            Side enemy = side.Opponent();
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = board.GetPiece(square);
                if (piece != null
                    && piece.Side == enemy
                    && piece.Type != PieceType.King
                    && !IsAbsolutelyPinned(board, square, rules, runtime))
                {
                    return true;
                }
            }
            return false;
        }
        static int LastFriendlyCaptureIndex(ModeRuntime runtime, Side side)
        {
            for (int i = runtime.Captures.Count - 1; i >= 0; i--)
            {
                CaptureRecord record = runtime.Captures[i];
                if (record.Side == side && !record.Exiled) return i;
            }
            return -1;
        }
        static Board ApplyRevival(GameState state, ModeRuntime runtime, out ModeRuntime nextRuntime)
        {
            nextRuntime = runtime;
            int index = LastFriendlyCaptureIndex(runtime, state.SideToMove);
            if (index < 0) return state.Board;
            CaptureRecord record = runtime.Captures[index];
            Square? dest = PickEmptyOnOpenRank(state, state.Board, state.SideToMove);
            if (dest == null) return state.Board;
            nextRuntime = runtime.RemoveCaptureAt(index);
            Piece revived = new Piece(record.Type, record.Side, true);
            Board board = state.Board.WithPiece(dest.Value, revived);
            nextRuntime = nextRuntime.AddSummoned(revived.Id);
            return board;
        }
        static Board ApplyExile(GameState state, ModeRuntime runtime, Guid? targetId, out ModeRuntime nextRuntime)
        {
            nextRuntime = runtime;
            Piece target = null;
            Square? targetSquare = null;
            if (targetId != null)
            {
                targetSquare = state.Board.FindSquare(targetId.Value);
                if (targetSquare != null
                    && CanExile(state.Board, targetSquare.Value, state.Rules, runtime, state.SideToMove))
                {
                    target = state.Board.GetPiece(targetSquare.Value);
                }
            }
            if (target == null)
            {
                for (int i = 0; i < 64; i++)
                {
                    Square square = Square.FromIndex(i);
                    if (!CanExile(state.Board, square, state.Rules, runtime, state.SideToMove)) continue;
                    Piece piece = state.Board.GetPiece(square);
                    if (target != null) return state.Board;
                    target = piece;
                    targetSquare = square;
                }
            }
            if (target == null || targetSquare == null) return state.Board;
            nextRuntime = runtime.AddCapture(target, true, targetSquare.Value, 2);
            return state.Board.WithPiece(targetSquare.Value, null);
        }
        static Square? ReturnSquare(Board board, CaptureRecord record)
        {
            if (record.Origin.IsOnBoard && board.CanPlace(record.Origin)) return record.Origin;
            return FirstEmpty(board, record.Side);
        }
        static Square? PickEmptyOnOpenRank(GameState state, Board board, Side side)
        {
            int start = side == Side.White ? 0 : 7;
            int dir = side == Side.White ? 1 : -1;
            var empties = new List<Square>(8);
            for (int step = 0; step < Square.BoardSize; step++)
            {
                int rank = start + dir * step;
                empties.Clear();
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    Square square = new Square(file, rank);
                    if (board.CanPlace(square)) empties.Add(square);
                }
                if (empties.Count > 0) return empties[UniformPick(empties.Count)];
            }
            return null;
        }
        static Square? FirstEmpty(Board board, Side side)
        {
            int start = side == Side.White ? 0 : 7;
            int dir = side == Side.White ? 1 : -1;
            for (int step = 0; step < Square.BoardSize; step++)
            {
                int rank = start + dir * step;
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    Square square = new Square(file, rank);
                    if (board.CanPlace(square)) return square;
                }
            }
            return null;
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
                        if (queen != null) return runtime;
                        queen = piece;
                    }
                }
            }
            if (queen == null) return runtime;
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
            if (pawn == null || pawnSquare == null) return state.Board;
            if (nextRuntime.IsEmpowered(pawn.Id) && pawn.Type == PieceType.Pawn)
            {
                nextRuntime = nextRuntime.WithoutEmpowered(pawn.Id);
            }
            return state.Board.WithPiece(pawnSquare.Value, pawn.WithType(type));
        }
        static int UniformPick(int count)
        {
            if (count <= 1) return 0;
            return (int)((uint)Guid.NewGuid().GetHashCode() % (uint)count);
        }
        static int StablePick(GameState state, Side side, int salt, int count)
        {
            if (count <= 1) return 0;
            int hash = state.FullmoveNumber * 397;
            hash = (hash * 31) + (int)side;
            hash = (hash * 31) + salt;
            hash = (hash * 31) + state.History.Count;
            if (hash < 0) hash = -hash;
            return hash % count;
        }
    }
}
