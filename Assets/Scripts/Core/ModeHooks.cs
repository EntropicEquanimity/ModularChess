using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ModularChess.Core.Tests")]

namespace ModularChess.Core
{
    internal enum CaptureResolutionKind
    {
        Continue,
        Remove,
        Negate
    }

    internal readonly struct CaptureResolution
    {
        #region Fields
        public CaptureResolutionKind Kind { get; }
        public ModeRuntime Runtime { get; }
        #endregion

        #region Public Methods
        public static CaptureResolution Continue(ModeRuntime runtime)
        {
            return new CaptureResolution(CaptureResolutionKind.Continue, runtime);
        }
        public static CaptureResolution Remove(ModeRuntime runtime)
        {
            return new CaptureResolution(CaptureResolutionKind.Remove, runtime);
        }
        public static CaptureResolution Negate(ModeRuntime runtime)
        {
            return new CaptureResolution(CaptureResolutionKind.Negate, runtime);
        }
        #endregion

        #region Private Methods
        CaptureResolution(CaptureResolutionKind kind, ModeRuntime runtime)
        {
            Kind = kind;
            Runtime = runtime ?? ModeRuntime.Empty;
        }
        #endregion
    }

    internal interface IMoveHook
    {
        void Append(Board board, Side side, ModeRuntime runtime, List<Move> moves);
    }

    internal interface ICaptureResolution
    {
        int Priority { get; }
        CaptureResolution Resolve(Board board, Move move, Piece captured, ModeRuntime runtime);
    }

    internal interface ITurnHook
    {
        bool PoolKeepsTurnOpen(ModeRuntime runtime, MatchSettings settings);
        bool KeepsTurnAfterMove(int paidAfter, bool extrasOpen, MatchSettings settings);
        bool BlocksRepeatPiece(ModeRuntime runtime, Guid pieceId);
    }

    internal sealed class ModeHooks
    {
        #region Fields
        public static ModeHooks None { get; } = new ModeHooks(
            Array.Empty<IMoveHook>(),
            Array.Empty<ICaptureResolution>(),
            Array.Empty<ITurnHook>());
        readonly IMoveHook[] _moves;
        readonly ICaptureResolution[] _captures;
        readonly ITurnHook[] _turns;
        public bool HasTurnEconomy => _turns.Length > 0;
        #endregion

        #region Public Methods
        public static ModeHooks For(IReadOnlyList<ModeId> modes)
        {
            if (modes == null || modes.Count == 0)
            {
                return None;
            }
            var moves = new List<IMoveHook>();
            var captures = new List<ICaptureResolution>();
            var turns = new List<ITurnHook>();
            for (int i = 0; i < modes.Count; i++)
            {
                switch (modes[i])
                {
                    case ModeId.PowerfulPieces:
                        moves.Add(new PowerfulPiecesMoves());
                        captures.Add(new ExtraLifeResolution());
                        break;
                    case ModeId.Martyr:
                        moves.Add(new MartyrMoves());
                        break;
                    case ModeId.ActionEconomy:
                        turns.Add(new ActionEconomyTurn());
                        break;
                }
            }
            SortByPriority(captures);
            return new ModeHooks(moves.ToArray(), captures.ToArray(), turns.ToArray());
        }
        public bool TurnStaysOpen(ModeRuntime runtime, MatchSettings settings)
        {
            for (int i = 0; i < _turns.Length; i++)
            {
                if (_turns[i].PoolKeepsTurnOpen(runtime, settings))
                    return true;
            }
            return false;
        }
        public bool KeepsTurnAfterMove(int paidAfter, bool extrasOpen, MatchSettings settings)
        {
            for (int i = 0; i < _turns.Length; i++)
            {
                if (_turns[i].KeepsTurnAfterMove(paidAfter, extrasOpen, settings))
                    return true;
            }
            return false;
        }
        public bool BlocksRepeatPiece(ModeRuntime runtime, Guid pieceId)
        {
            for (int i = 0; i < _turns.Length; i++)
            {
                if (_turns[i].BlocksRepeatPiece(runtime, pieceId))
                    return true;
            }
            return false;
        }
        public void AppendMoves(Board board, Side side, ModeRuntime runtime, List<Move> moves)
        {
            for (int i = 0; i < _moves.Length; i++)
            {
                _moves[i].Append(board, side, runtime, moves);
            }
        }
        public CaptureResolution ResolveCapture(
            Board board,
            Move move,
            Piece captured,
            ModeRuntime runtime)
        {
            ModeRuntime current = runtime ?? ModeRuntime.Empty;
            if (captured == null)
            {
                return CaptureResolution.Remove(current);
            }
            for (int i = 0; i < _captures.Length; i++)
            {
                CaptureResolution step = _captures[i].Resolve(board, move, captured, current);
                current = step.Runtime;
                if (step.Kind != CaptureResolutionKind.Continue)
                {
                    return step;
                }
            }
            return CaptureResolution.Remove(current);
        }
        #endregion

        #region Private Methods
        ModeHooks(IMoveHook[] moves, ICaptureResolution[] captures, ITurnHook[] turns)
        {
            _moves = moves ?? Array.Empty<IMoveHook>();
            _captures = captures ?? Array.Empty<ICaptureResolution>();
            _turns = turns ?? Array.Empty<ITurnHook>();
        }
        static void SortByPriority(List<ICaptureResolution> captures)
        {
            for (int i = 1; i < captures.Count; i++)
            {
                ICaptureResolution item = captures[i];
                int j = i;
                while (j > 0 && captures[j - 1].Priority < item.Priority)
                {
                    captures[j] = captures[j - 1];
                    j--;
                }
                captures[j] = item;
            }
        }
        #endregion
    }
}
