using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ModularChess.Core.Tests")]
[assembly: InternalsVisibleTo("ModularChess.Roguelike")]

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

    internal interface IVisionHook
    {
        VisionMap Compute(GameState state, Side viewer);
    }

    internal interface ITurnHook
    {
        ModeRuntime AfterCaptureRemoved(Piece captured, Rules rules, ModeRuntime runtime);
        Board OnTurnEnd(Board board, ModeRuntime runtime, Side endingSide, out ModeRuntime nextRuntime);
        ModeRuntime MaybeOpenDraft(GameState state, ModeRuntime runtime, Side sideToMove, Board board);
    }

    internal interface IDraftHook
    {
        void CollectTargets(GameState state, MartyrPower power, List<Square> into);
    }

    internal sealed class ModeHooks
    {
        #region Fields
        public static ModeHooks None { get; } = new ModeHooks(
            Array.Empty<IMoveHook>(),
            Array.Empty<ICaptureResolution>(),
            Array.Empty<IVisionHook>(),
            Array.Empty<ITurnHook>(),
            Array.Empty<IDraftHook>());
        public static ModeHooks ExtraLife { get; } = new ModeHooks(
            Array.Empty<IMoveHook>(),
            new ICaptureResolution[] { new ExtraLifeResolution() },
            Array.Empty<IVisionHook>(),
            Array.Empty<ITurnHook>(),
            Array.Empty<IDraftHook>());
        readonly IMoveHook[] _moves;
        readonly ICaptureResolution[] _captures;
        readonly IVisionHook[] _visions;
        readonly ITurnHook[] _turns;
        readonly IDraftHook[] _drafts;
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
            var visions = new List<IVisionHook>();
            var turns = new List<ITurnHook>();
            var drafts = new List<IDraftHook>();
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
                        turns.Add(new MartyrTurnHook());
                        drafts.Add(new MartyrDraftHook());
                        break;
                    case ModeId.FogOfWar:
                        visions.Add(new FogVisionHook());
                        break;
                }
            }
            SortByPriority(captures);
            return new ModeHooks(
                moves.ToArray(),
                captures.ToArray(),
                visions.ToArray(),
                turns.ToArray(),
                drafts.ToArray());
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
        public VisionMap ComputeVision(GameState state, Side viewer)
        {
            if (_visions.Length == 0)
            {
                return VisionMap.AllIdentified;
            }
            return _visions[0].Compute(state, viewer);
        }
        public ModeRuntime AfterCaptureRemoved(Piece captured, Rules rules, ModeRuntime runtime)
        {
            ModeRuntime current = runtime ?? ModeRuntime.Empty;
            for (int i = 0; i < _turns.Length; i++)
            {
                current = _turns[i].AfterCaptureRemoved(captured, rules, current);
            }
            return current;
        }
        public Board OnTurnEnd(Board board, ModeRuntime runtime, Side endingSide, out ModeRuntime nextRuntime)
        {
            nextRuntime = runtime ?? ModeRuntime.Empty;
            Board current = board;
            for (int i = 0; i < _turns.Length; i++)
            {
                current = _turns[i].OnTurnEnd(current, nextRuntime, endingSide, out nextRuntime);
            }
            return current;
        }
        public ModeRuntime MaybeOpenDraft(GameState state, ModeRuntime runtime, Side sideToMove, Board board)
        {
            ModeRuntime current = runtime ?? ModeRuntime.Empty;
            for (int i = 0; i < _turns.Length; i++)
            {
                current = _turns[i].MaybeOpenDraft(state, current, sideToMove, board);
            }
            return current;
        }
        public IReadOnlyList<Square> CollectDraftTargets(GameState state, MartyrPower power)
        {
            var into = new List<Square>();
            for (int i = 0; i < _drafts.Length; i++)
            {
                _drafts[i].CollectTargets(state, power, into);
            }
            return into;
        }
        #endregion

        #region Private Methods
        ModeHooks(
            IMoveHook[] moves,
            ICaptureResolution[] captures,
            IVisionHook[] visions,
            ITurnHook[] turns,
            IDraftHook[] drafts)
        {
            _moves = moves ?? Array.Empty<IMoveHook>();
            _captures = captures ?? Array.Empty<ICaptureResolution>();
            _visions = visions ?? Array.Empty<IVisionHook>();
            _turns = turns ?? Array.Empty<ITurnHook>();
            _drafts = drafts ?? Array.Empty<IDraftHook>();
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
