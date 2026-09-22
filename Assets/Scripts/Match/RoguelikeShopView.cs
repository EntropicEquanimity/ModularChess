using System;
using System.Collections.Generic;
using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class RoguelikeShopView : MonoBehaviour
    {
        #region Fields
        [SerializeField] Transform itemRoot;
        [SerializeField] ShopCardView cardPrefab;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text goldLabel;
        [SerializeField] Button rerollButton;
        [SerializeField] Button skipButton;
        readonly List<ShopCardView> _spawned = new List<ShopCardView>();
        #endregion

        #region Public Methods
        public void Present(
            IReadOnlyList<ShopItem> items,
            RoguelikeRunState run,
            GameState state,
            Action<int> onBuy,
            UnityAction onReroll,
            UnityAction onSkip)
        {
            Resolve();
            OverlayMotion.Ensure(gameObject)?.PlayEnter();
            if (titleLabel != null)
                titleLabel.text = Loc.Get("roguelike.shop.title");
            if (goldLabel != null && run != null)
                goldLabel.text = Loc.Format("roguelike.gold", run.Gold);
            LocalizedText.Bind(rerollButton, "roguelike.shop.reroll");
            LocalizedText.Bind(skipButton, "roguelike.shop.skip");
            if (rerollButton != null)
            {
                rerollButton.onClick.RemoveAllListeners();
                bool canReroll = run != null && run.Gold >= RoguelikeBalance.ShopRerollCost;
                rerollButton.interactable = canReroll;
                if (canReroll)
                    GameAudio.Bind(rerollButton, onReroll);
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                GameAudio.Bind(skipButton, onSkip);
            }
            ClearCards();
            if (items == null || itemRoot == null || cardPrefab == null)
                return;
            int army = CountArmy(state, run != null ? run.PlayerSide : Side.White);
            int cap = run != null ? run.ArmySizeCap : 0;
            int gold = run != null ? run.Gold : 0;
            bool armyFull = army >= cap;
            for (int i = 0; i < items.Count; i++)
            {
                ShopCardView card = Instantiate(cardPrefab, itemRoot);
                int index = i;
                card.Present(items[i], gold, armyFull, () => onBuy?.Invoke(index));
                _spawned.Add(card);
            }
        }
        public void Dismiss()
        {
            ClearCards();
            OverlayMotion.Ensure(gameObject)?.PlayExit();
        }
        #endregion

        #region Private Methods
        void ClearCards()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Destroy(_spawned[i].gameObject);
            }
            _spawned.Clear();
        }
        void Resolve()
        {
            if (itemRoot == null)
            {
                Transform t = transform.Find("ItemRoot");
                if (t == null)
                    t = FindChild(transform, "ItemRoot");
                itemRoot = t != null ? t : transform;
            }
            if (cardPrefab == null)
                cardPrefab = GetComponentInChildren<ShopCardView>(true);
            if (titleLabel == null)
                titleLabel = FindTmp("Title");
            if (goldLabel == null)
                goldLabel = FindTmp("GoldLabel");
            if (rerollButton == null)
                rerollButton = FindButton("RerollButton");
            if (skipButton == null)
                skipButton = FindButton("SkipButton");
        }
        static int CountArmy(GameState state, Side player)
        {
            if (state == null)
                return 0;
            int count = 0;
            foreach (Piece piece in state.Board.OccupiedPieces)
            {
                if (piece.Side == player && piece.Type != PieceType.King && !state.Runtime.IsSummoned(piece.Id))
                    count++;
            }
            return count;
        }
        TMP_Text FindTmp(string name)
        {
            Transform t = FindChild(transform, name);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }
        Button FindButton(string name)
        {
            Transform t = FindChild(transform, name);
            return t != null ? t.GetComponent<Button>() : null;
        }
        static Transform FindChild(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }
        #endregion
    }
}
