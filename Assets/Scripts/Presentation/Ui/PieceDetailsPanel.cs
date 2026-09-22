using System;
using System.Collections.Generic;
using ModularChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class PieceDetailsPanel : MonoBehaviour
    {
        readonly List<EffectDescriptionView> _rows = new List<EffectDescriptionView>();
        [SerializeField] TMP_Text pieceName;
        GameObject _effectPrefab;

        public void Show(Piece piece, GameState state, IReadOnlyCollection<Guid> pendingEmpowered)
        {
            if (piece == null || state == null)
            {
                Hide();
                return;
            }

            EnsureName();
            gameObject.SetActive(true);
            if (pieceName != null)
                pieceName.text = Loc.PieceName(piece.Type);

            int row = 0;
            bool empowered = state.Runtime.IsEmpowered(piece.Id)
                || Contains(pendingEmpowered, piece.Id);
            if (empowered)
            {
                BindRow(row++, Loc.Get("piece.empowered"), Loc.EmpoweredDescription(piece.Type));
            }

            if (state.Runtime.ExtraLifeCount(piece.Id) > 0 && piece.Type != PieceType.Knight)
            {
                int lives = state.Runtime.ExtraLifeCount(piece.Id);
                string body = lives > 1
                    ? Loc.Format("piece.extraLife.body.stacks", lives)
                    : Loc.Get("piece.extraLife.body");
                BindRow(row++, Loc.Get("piece.extraLife"), body);
            }

            if (state.Runtime.IsSummoned(piece.Id))
            {
                BindRow(row++, Loc.Get("piece.summoned"), Loc.Get("piece.summoned.body"));
            }

            if (state.Runtime.TryGetStatus(piece.Id, out PieceStatus status))
            {
                BindRow(row++, StatusName(status.Kind), StatusDescription(status));
            }

            HideUnused(row);
            transform.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            for (int i = 0; i < row; i++)
                _rows[i].RefreshLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        public void ShowText(string title, string body)
        {
            EnsureName();
            gameObject.SetActive(true);
            if (pieceName != null)
                pieceName.text = title ?? string.Empty;
            int row = 0;
            if (!string.IsNullOrEmpty(body))
                BindRow(row++, string.Empty, body);
            HideUnused(row);
            transform.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            for (int i = 0; i < row; i++)
                _rows[i].RefreshLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void EnsureName()
        {
            if (pieceName != null)
                return;
            Transform nameTf = transform.Find("UnitName");
            if (nameTf == null)
                nameTf = transform.Find("Unit/UnitName");
            if (nameTf != null)
                pieceName = nameTf.GetComponent<TMP_Text>();
            if (pieceName == null)
                pieceName = GetComponentInChildren<TMP_Text>(true);
        }

        void BindRow(int index, string effectName, string description)
        {
            EffectDescriptionView row = RowAt(index);
            row.gameObject.SetActive(true);
            row.transform.SetAsLastSibling();
            row.Bind(effectName, description);
        }

        EffectDescriptionView RowAt(int index)
        {
            while (_rows.Count <= index)
            {
                GameObject prefab = EffectPrefab();
                EffectDescriptionView view;
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, transform);
                    instance.name = "EffectDescription";
                    view = instance.GetComponent<EffectDescriptionView>();
                    if (view == null)
                        view = instance.AddComponent<EffectDescriptionView>();
                }
                else
                {
                    var go = new GameObject("EffectDescription", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    view = go.AddComponent<EffectDescriptionView>();
                }

                _rows.Add(view);
            }

            return _rows[index];
        }

        void HideUnused(int used)
        {
            for (int i = used; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                    _rows[i].gameObject.SetActive(false);
            }
        }

        GameObject EffectPrefab()
        {
            if (_effectPrefab == null)
                _effectPrefab = RuntimePrefabs.EffectDescription;
            return _effectPrefab;
        }

        static bool Contains(IReadOnlyCollection<Guid> ids, Guid id)
        {
            if (ids == null)
                return false;
            foreach (Guid candidate in ids)
            {
                if (candidate == id)
                    return true;
            }

            return false;
        }

        static string StatusName(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Invulnerable:
                    return Loc.Get("status.invulnerable");
                case StatusKind.Stasis:
                    return Loc.Get("status.stasis");
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        static string StatusDescription(PieceStatus status)
        {
            switch (status.Kind)
            {
                case StatusKind.Invulnerable:
                    return Loc.Format("status.invulnerable.body", status.RemainingTurns);
                case StatusKind.Stasis:
                    return Loc.Format("status.stasis.body", status.RemainingTurns);
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(status.Kind), status.Kind, null);
            }
        }
    }
}
