using System;
using System.Collections.Generic;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Match
{
    public sealed class BoonOfferView : MonoBehaviour
    {
        #region Fields
        [SerializeField] Transform cardRoot;
        [SerializeField] BoonCardView cardPrefab;
        [SerializeField] GameObject root;
        Action<BoonDefinition> _onPick;
        readonly List<BoonCardView> _spawned = new List<BoonCardView>();
        #endregion

        #region Public Methods
        public void Bind(Action<BoonDefinition> onPick)
        {
            Resolve();
            _onPick = onPick;
        }
        public void Present(IReadOnlyList<BoonDefinition> offer)
        {
            Resolve();
            ClearCards();
            if (root != null)
                root.SetActive(true);
            gameObject.SetActive(true);
            if (offer == null || cardRoot == null)
                return;
            for (int i = 0; i < offer.Count; i++)
            {
                BoonCardView card = SpawnCard();
                if (card == null)
                    continue;
                BoonDefinition def = offer[i];
                card.Present(def, () => _onPick?.Invoke(def));
                _spawned.Add(card);
            }
        }
        public void Hide()
        {
            ClearCards();
            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods
        BoonCardView SpawnCard()
        {
            if (cardPrefab == null)
                return null;
            return Instantiate(cardPrefab, cardRoot);
        }
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
            if (root == null)
                root = gameObject;
            if (cardRoot == null)
            {
                Transform t = transform.Find("CardRoot");
                cardRoot = t != null ? t : transform;
            }
            if (cardPrefab == null)
                cardPrefab = GetComponentInChildren<BoonCardView>(true);
        }
        #endregion
    }
}
