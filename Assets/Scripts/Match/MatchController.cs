using System;
using System.Collections.Generic;
using ModularChess.Core;
using ModularChess.Presentation;
using UnityEngine;

namespace ModularChess.Match
{
    [DefaultExecutionOrder(50)]
    public sealed class MatchController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private PromotionPicker promotionPicker;
        [SerializeField] private MatchHud hud;

        private GameState _state;
        private Square? _selected;
        private IReadOnlyList<Move> _movesFromSelection = Array.Empty<Move>();
        private List<Move> _pendingPromotions;
        private bool _subscribed;

        public GameState State => _state;

        public void Configure(BoardView view, PromotionPicker picker, MatchHud matchHud)
        {
            Unsubscribe();
            boardView = view;
            promotionPicker = picker;
            hud = matchHud;
            Subscribe();
        }

        private void Start()
        {
            if (boardView == null)
            {
                boardView = FindAnyObjectByType<BoardView>();
            }

            if (promotionPicker == null)
            {
                promotionPicker = FindAnyObjectByType<PromotionPicker>();
            }

            if (hud == null)
            {
                hud = FindAnyObjectByType<MatchHud>();
            }

            if (boardView == null || promotionPicker == null || hud == null)
            {
                Debug.LogError("MatchController needs BoardView, PromotionPicker, and MatchHud. Add MatchBootstrap to the scene.");
                enabled = false;
                return;
            }

            Subscribe();
            BeginMatch();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void BeginMatch()
        {
            _state = GameState.StartingPosition();
            _pendingPromotions = null;
            ClearSelection();
            if (promotionPicker != null)
            {
                promotionPicker.Hide();
            }

            RefreshPresentation();
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            if (boardView != null)
            {
                boardView.SquareClicked += OnSquareClicked;
            }

            if (promotionPicker != null)
            {
                promotionPicker.PromotionChosen += OnPromotionChosen;
            }

            _subscribed = boardView != null && promotionPicker != null;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (boardView != null)
            {
                boardView.SquareClicked -= OnSquareClicked;
            }

            if (promotionPicker != null)
            {
                promotionPicker.PromotionChosen -= OnPromotionChosen;
            }

            _subscribed = false;
        }

        private void OnSquareClicked(Square square)
        {
            if (!CanAcceptBoardInput())
            {
                return;
            }

            if (_selected.HasValue)
            {
                Square selected = _selected.Value;
                if (selected.Equals(square))
                {
                    ClearSelection();
                    RefreshPresentation();
                    return;
                }

                if (IsFriendly(square))
                {
                    Select(square);
                    RefreshPresentation();
                    return;
                }

                List<Move> destinations = MovesTo(square);
                if (destinations.Count == 0)
                {
                    ClearSelection();
                    RefreshPresentation();
                    return;
                }

                if (destinations.Count == 1)
                {
                    Commit(destinations[0]);
                    return;
                }

                BeginPromotion(destinations);
                return;
            }

            if (IsFriendly(square))
            {
                Select(square);
                RefreshPresentation();
            }
        }

        private void OnPromotionChosen(PieceType pieceType)
        {
            if (_pendingPromotions == null || _state == null)
            {
                return;
            }

            Move? chosen = null;
            for (int i = 0; i < _pendingPromotions.Count; i++)
            {
                Move candidate = _pendingPromotions[i];
                if (candidate.PromotionType == pieceType)
                {
                    chosen = candidate;
                    break;
                }
            }

            if (!chosen.HasValue)
            {
                return;
            }

            Commit(chosen.Value);
        }

        private bool CanAcceptBoardInput()
        {
            return enabled
                   && _state != null
                   && _state.Status == GameStatus.InProgress
                   && _pendingPromotions == null;
        }

        private bool IsFriendly(Square square)
        {
            Piece piece = _state.Board.GetPiece(square);
            return piece != null && piece.Side == _state.SideToMove;
        }

        private void Select(Square square)
        {
            _selected = square;
            _movesFromSelection = _state.LegalMovesFrom(square);
        }

        private void ClearSelection()
        {
            _selected = null;
            _movesFromSelection = Array.Empty<Move>();
        }

        private List<Move> MovesTo(Square destination)
        {
            List<Move> matches = new List<Move>();
            for (int i = 0; i < _movesFromSelection.Count; i++)
            {
                Move move = _movesFromSelection[i];
                if (move.To.Equals(destination))
                {
                    matches.Add(move);
                }
            }

            return matches;
        }

        private void BeginPromotion(List<Move> promotions)
        {
            _pendingPromotions = promotions;
            promotionPicker.Show(_state.SideToMove);
        }

        private void Commit(Move move)
        {
            _state = _state.Apply(move);
            _pendingPromotions = null;
            ClearSelection();
            promotionPicker.Hide();
            RefreshPresentation();
        }

        private void RefreshPresentation()
        {
            boardView.Bind(_state);
            if (_selected.HasValue)
            {
                boardView.SetSelection(_selected);
            }
            else
            {
                boardView.ClearSelection();
            }

            if (_state.History.Count > 0)
            {
                Move last = _state.History[_state.History.Count - 1];
                boardView.SetLastMove(last.From, last.To);
            }
            else
            {
                boardView.ClearLastMove();
            }

            hud.Bind(_state, _state.History);
        }
    }
}
