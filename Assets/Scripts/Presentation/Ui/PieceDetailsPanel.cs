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
        TMP_Text _pieceName;
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
            if (_pieceName != null)
                _pieceName.text = piece.Type.ToString();

            int row = 0;
            bool empowered = state.Runtime.IsEmpowered(piece.Id)
                || Contains(pendingEmpowered, piece.Id);
            if (empowered)
            {
                BindRow(row++, EmpoweredPowers.EffectName, EmpoweredPowers.Describe(piece.Type));
            }

            if (state.Runtime.ExtraLifeAvailable(piece.Id) && piece.Type != PieceType.Knight)
            {
                BindRow(row++, "Extra Life", "The first Capture of this Piece is negated. Then Extra Life is gone.");
            }

            if (state.Runtime.IsSummoned(piece.Id))
            {
                BindRow(row++, "Summoned", "Created by Martyr. Counts 0 Lost Material if it leaves.");
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

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void EnsureName()
        {
            if (_pieceName != null)
                return;
            Transform nameTf = transform.Find("UnitName");
            if (nameTf != null)
                _pieceName = nameTf.GetComponent<TMP_Text>();
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
                    return "Invulnerable";
                case StatusKind.Stasis:
                    return "Stasis";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        static string StatusDescription(PieceStatus status)
        {
            switch (status.Kind)
            {
                case StatusKind.Invulnerable:
                    return $"Cannot be targeted. {status.RemainingTurns} Turns remaining.";
                case StatusKind.Stasis:
                    return $"Cannot Move, be targeted, or attack. {status.RemainingTurns} Turns remaining.";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(status.Kind), status.Kind, null);
            }
        }
    }
}
