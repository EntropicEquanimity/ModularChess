using System;
using System.Collections.Generic;
using ModularChess.Core;

namespace ModularChess.Match
{
    public static class MatchHistoryPlayback
    {
        #region Public Methods
        public static GameState StartingState(MatchHistoryRecord record)
        {
            return GameState.StartingPosition(MatchHistoryStore.RulesFrom(record));
        }
        public static bool TryApply(ref GameState state, MatchHistoryEvent e)
        {
            if (state == null || e == null) return false;
            switch ((MatchHistoryEventKind)e.kind)
            {
                case MatchHistoryEventKind.Move:
                    return ApplyMove(ref state, e);
                case MatchHistoryEventKind.Empowered:
                    return ApplyEmpowered(ref state, e);
                case MatchHistoryEventKind.Draft:
                    return ApplyDraft(ref state, e);
                default:
                    return false;
            }
        }
        public static GameState StateAt(MatchHistoryRecord record, int eventCount)
        {
            GameState state = StartingState(record);
            if (record?.events == null || eventCount <= 0) return state;
            int n = Math.Min(eventCount, record.events.Length);
            for (int i = 0; i < n; i++)
            {
                MatchHistoryEvent e = record.events[i];
                if (!TryApply(ref state, e))
                    break;
            }
            return state;
        }
        public static bool EndsTurn(GameState before, GameState after)
        {
            if (before == null || after == null) return false;
            return before.SideToMove != after.SideToMove;
        }
        #endregion

        #region Private Methods
        static bool ApplyMove(ref GameState state, MatchHistoryEvent e)
        {
            if (!Square.TryParse(e.from, out Square from) || !Square.TryParse(e.to, out Square to)) return false;
            PieceType? promotion = e.promotion >= 0 ? (PieceType)e.promotion : (PieceType?)null;
            var kind = (MoveKind)e.moveKind;
            IReadOnlyList<Move> legal = state.LegalMovesFrom(from);
            Move? match = null;
            for (int i = 0; i < legal.Count; i++)
            {
                Move move = legal[i];
                if (!move.To.Equals(to) || move.Kind != kind) continue;
                if (promotion.HasValue && move.PromotionType != promotion.Value) continue;
                match = move;
                break;
            }
            if (!match.HasValue)
            {
                for (int i = 0; i < legal.Count; i++)
                {
                    Move move = legal[i];
                    if (!move.To.Equals(to)) continue;
                    if (promotion.HasValue && move.PromotionType != promotion.Value) continue;
                    match = move;
                    break;
                }
            }
            if (!match.HasValue) return false;
            state = state.Apply(match.Value);
            return true;
        }
        static bool ApplyEmpowered(ref GameState state, MatchHistoryEvent e)
        {
            if (e.squares == null || e.squares.Length == 0)
            {
                state = state.ConfirmEmpowered(Array.Empty<Guid>());
                return true;
            }
            var ids = new List<Guid>(e.squares.Length);
            for (int i = 0; i < e.squares.Length; i++)
            {
                if (!Square.TryParse(e.squares[i], out Square square)) continue;
                Piece piece = state.Board.GetPiece(square);
                if (piece != null)
                    ids.Add(piece.Id);
            }
            state = state.ConfirmEmpowered(ids);
            return true;
        }
        static bool ApplyDraft(ref GameState state, MatchHistoryEvent e)
        {
            var power = (MartyrPower)e.power;
            Guid? targetId = null;
            if (!string.IsNullOrEmpty(e.target) && Square.TryParse(e.target, out Square target))
            {
                Piece piece = state.Board.GetPiece(target);
                if (piece != null)
                    targetId = piece.Id;
            }
            Square[] reinforcements = null;
            if (e.reinforcements != null && e.reinforcements.Length > 0)
            {
                var list = new List<Square>(e.reinforcements.Length);
                for (int i = 0; i < e.reinforcements.Length; i++)
                {
                    if (Square.TryParse(e.reinforcements[i], out Square square))
                        list.Add(square);
                }
                reinforcements = list.ToArray();
            }
            state = state.ApplyDraft(power, targetId, reinforcements);
            return true;
        }
        #endregion
    }
}
