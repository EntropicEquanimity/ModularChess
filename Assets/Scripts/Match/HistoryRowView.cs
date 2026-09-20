using System;
using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class HistoryRowView : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_Text label;
        [SerializeField] Image background;
        MatchHistoryRecord _record;
        int _index;
        bool _selected;
        static readonly Color Selected = new Color(0.85f, 0.9f, 1f, 1f);
        static readonly Color Idle = new Color(1f, 1f, 1f, 0.4f);
        #endregion

        #region Public Methods
        public MatchHistoryRecord Record => _record;
        public int Index => _index;
        public void Bind(MatchHistoryRecord record, int index, bool selected, UnityAction<int> onSelect)
        {
            _record = record;
            _index = index;
            EnsureRefs();
            if (label != null) { label.text = FormatRow(record); }
            SetSelected(selected);
            Button button = GetComponent<Button>();
            if (button == null) { button = gameObject.AddComponent<Button>(); }
            if (background == null) { background = GetComponent<Image>(); }
            if (background == null) { background = gameObject.AddComponent<Image>(); }
            background.color = Idle;
            background.raycastTarget = true;
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            int captured = index; GameAudio.Bind(button, () => onSelect?.Invoke(captured)); }
        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (background != null) { background.color = selected ? Selected : Idle; }
        }
        #endregion

        #region Private Methods
        void EnsureRefs()
        {
            if (label == null) { label = GetComponentInChildren<TMP_Text>(true); }
            if (background == null) { background = GetComponent<Image>(); }
        }
        static string FormatRow(MatchHistoryRecord record)
        {
            if (record == null) return string.Empty;
            string date = FormatDate(record.endedAtUnix);
            string activity = record.activity == (int)Activity.VersusFriend
                ? Loc.Get("play.versusFriend")
                : Loc.Get("play.versusAi");
            string modes = FormatModes(record.modes);
            string result = FormatResult(record);
            if (string.IsNullOrEmpty(modes)) return $"{date} {activity}: {result}";
            return $"{date} {activity}: {result}\n{modes}";
        }
        static string FormatDate(long unix)
        {
            if (unix <= 0) return "—";
            try { return DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().ToString("g"); }
            catch (ArgumentOutOfRangeException) { return "—"; }
        }
        static string FormatModes(int[] modes)
        {
            if (modes == null || modes.Length == 0) return string.Empty;
            var parts = new string[modes.Length];
            for (int i = 0; i < modes.Length; i++) { parts[i] = Loc.ModeName((ModeId)modes[i]); }
            return string.Join(", ", parts);
        }
        static string FormatResult(MatchHistoryRecord record)
        {
            if (record.result == 2) return Loc.Get("history.draw");
            Side winner = record.result == 0 ? Side.White : Side.Black;
            Side player = (Side)record.playerSide;
            if (winner == player) return Loc.Get("history.win");
            return Loc.Get("history.loss");
        }
        #endregion
    }
}
