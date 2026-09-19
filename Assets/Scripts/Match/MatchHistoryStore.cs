using System;
using System.Collections.Generic;
using System.IO;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Match
{
    public enum MatchHistoryEventKind
    {
        Move = 0,
        Empowered = 1,
        Draft = 2
    }

    [Serializable]
    public sealed class MatchHistoryEvent
    {
        public int kind;
        public string from;
        public string to;
        public int moveKind;
        public int promotion = -1;
        public string[] squares;
        public int power;
        public string target;
        public string[] reinforcements;
    }

    [Serializable]
    public sealed class MatchHistoryAction
    {
        public string from;
        public string to;
        public int kind;
        public int promotion;
    }

    [Serializable]
    public sealed class MatchHistoryRecord
    {
        public MatchHistoryEvent[] events;
        public MatchHistoryAction[] moves;
        public int clockSeconds;
        public int[] modes;
        public int empoweredCount;
        public int martyrThreshold;
        public int martyrDraftOptions;
        public int mainMinutes;
        public int incrementSeconds;
        public int hostSide;
        public int playerSide;
        public int aiStrength;
        public int result;
        public int activity;
        public long endedAtUnix;
        public bool Replayable => events != null;
    }

    [Serializable]
    sealed class MatchHistoryFile
    {
        public MatchHistoryRecord[] records = new MatchHistoryRecord[0];
    }

    public static class MatchHistoryStore
    {
        #region Fields
        const string FileName = "match-history.json";
        #endregion

        #region Public Methods
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        public static void Record(
            MatchSession session,
            GameState state,
            int clockSeconds,
            IReadOnlyList<MatchHistoryEvent> events)
        {
            if (session == null || state == null || state.Status == GameStatus.InProgress || state.Status == GameStatus.Aborted)
                return;
            var record = new MatchHistoryRecord
            {
                events = events != null ? ToArray(events) : new MatchHistoryEvent[0],
                moves = BuildLegacyMoves(events),
                clockSeconds = Math.Max(0, clockSeconds),
                modes = ModeIds(session.Rules),
                empoweredCount = session.Rules.Settings.EmpoweredCount,
                martyrThreshold = session.Rules.Settings.MartyrThreshold,
                martyrDraftOptions = session.Rules.Settings.MartyrDraftOptions,
                mainMinutes = session.Rules.Settings.Time.BaseMinutes,
                incrementSeconds = session.Rules.Settings.Time.IncrementSeconds,
                hostSide = (int)session.PlayerSide,
                playerSide = (int)session.PlayerSide,
                aiStrength = (int)session.Rules.Settings.AiStrength,
                result = ResultCode(state),
                activity = (int)session.Activity,
                endedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            MatchHistoryFile file = Load();
            var next = new MatchHistoryRecord[file.records.Length + 1];
            for (int i = 0; i < file.records.Length; i++)
                next[i] = file.records[i];
            next[file.records.Length] = record;
            file.records = next;
            file.records = Trim(file.records, HistoryPrefs.Cap);
            Save(file);
        }
        public static MatchHistoryRecord[] ListNewestFirst()
        {
            MatchHistoryFile file = Load();
            if (file.records == null || file.records.Length == 0) return new MatchHistoryRecord[0];
            var list = new MatchHistoryRecord[file.records.Length];
            for (int i = 0; i < file.records.Length; i++)
                list[i] = file.records[file.records.Length - 1 - i];
            return list;
        }
        public static MatchHistoryRecord Latest()
        {
            MatchHistoryFile file = Load();
            if (file.records == null || file.records.Length == 0) return null;
            return file.records[file.records.Length - 1];
        }
        public static void TrimToCap(int cap)
        {
            MatchHistoryFile file = Load();
            file.records = Trim(file.records, HistoryPrefs.Snap(cap));
            Save(file);
        }
        public static void Delete()
        {
            string path = FilePath;
            if (File.Exists(path))
                File.Delete(path);
        }
        public static MatchHistoryEvent MoveEvent(Square from, Square to, MoveKind kind, PieceType? promotion)
        {
            return new MatchHistoryEvent
            {
                kind = (int)MatchHistoryEventKind.Move,
                from = from.ToString(),
                to = to.ToString(),
                moveKind = (int)kind,
                promotion = promotion.HasValue ? (int)promotion.Value : -1
            };
        }
        public static MatchHistoryEvent EmpoweredEvent(IReadOnlyList<Square> squares)
        {
            var list = new string[squares != null ? squares.Count : 0];
            for (int i = 0; i < list.Length; i++)
                list[i] = squares[i].ToString();
            return new MatchHistoryEvent
            {
                kind = (int)MatchHistoryEventKind.Empowered,
                squares = list
            };
        }
        public static MatchHistoryEvent DraftEvent(MartyrPower power, Square? target, Square[] reinforcements)
        {
            string[] slots = null;
            if (reinforcements != null && reinforcements.Length > 0)
            {
                slots = new string[reinforcements.Length];
                for (int i = 0; i < reinforcements.Length; i++)
                    slots[i] = reinforcements[i].ToString();
            }
            return new MatchHistoryEvent
            {
                kind = (int)MatchHistoryEventKind.Draft,
                power = (int)power,
                target = target.HasValue ? target.Value.ToString() : string.Empty,
                reinforcements = slots
            };
        }
        public static MatchRules RulesFrom(MatchHistoryRecord record)
        {
            if (record == null) return new MatchRules(Array.Empty<ModeId>(), MatchSettings.Default);
            var modes = new List<ModeId>();
            if (record.modes != null)
            {
                for (int i = 0; i < record.modes.Length; i++)
                    modes.Add((ModeId)record.modes[i]);
            }
            var time = new TimeControl(Math.Max(0, record.mainMinutes), Math.Max(0, record.incrementSeconds));
            var settings = new MatchSettings(
                time,
                HostColor.White,
                (AiStrength)record.aiStrength,
                false,
                Math.Max(0, record.empoweredCount),
                Math.Max(0, record.martyrThreshold),
                Math.Max(0, record.martyrDraftOptions));
            return new MatchRules(modes, settings);
        }
        public static MatchSession SessionFrom(MatchHistoryRecord record)
        {
            MatchRules rules = RulesFrom(record);
            Side side = record != null ? (Side)record.playerSide : Side.White;
            return new MatchSession
            {
                Activity = record != null ? (Activity)record.activity : Activity.VersusAi,
                Rules = rules,
                PlayerSide = side,
                Hotseat = false
            };
        }
        #endregion

        #region Private Methods
        static int ResultCode(GameState state)
        {
            switch (state.Status)
            {
                case GameStatus.Stalemate:
                case GameStatus.Draw:
                    return 2;
                case GameStatus.Checkmate:
                case GameStatus.Timeout:
                case GameStatus.Resign:
                    return state.SideToMove == Side.White ? 1 : 0;
                case GameStatus.InProgress:
                case GameStatus.Aborted:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state.Status, null);
            }
        }
        static int[] ModeIds(MatchRules rules)
        {
            var ids = new int[rules.Modes.Count];
            for (int i = 0; i < rules.Modes.Count; i++)
                ids[i] = (int)rules.Modes[i];
            return ids;
        }
        static MatchHistoryEvent[] ToArray(IReadOnlyList<MatchHistoryEvent> events)
        {
            var list = new MatchHistoryEvent[events.Count];
            for (int i = 0; i < events.Count; i++)
                list[i] = events[i];
            return list;
        }
        static MatchHistoryAction[] BuildLegacyMoves(IReadOnlyList<MatchHistoryEvent> events)
        {
            var list = new List<MatchHistoryAction>();
            if (events == null) return list.ToArray();
            for (int i = 0; i < events.Count; i++)
            {
                MatchHistoryEvent e = events[i];
                if (e == null || e.kind != (int)MatchHistoryEventKind.Move) continue;
                list.Add(new MatchHistoryAction
                {
                    from = e.from,
                    to = e.to,
                    kind = e.moveKind,
                    promotion = e.promotion
                });
            }
            return list.ToArray();
        }
        static MatchHistoryRecord[] Trim(MatchHistoryRecord[] records, int cap)
        {
            if (records == null) return new MatchHistoryRecord[0];
            int keep = Math.Max(HistoryPrefs.Min, Math.Min(HistoryPrefs.Max, cap));
            if (records.Length <= keep) return records;
            var trimmed = new MatchHistoryRecord[keep];
            int start = records.Length - keep;
            for (int i = 0; i < keep; i++)
                trimmed[i] = records[start + i];
            return trimmed;
        }
        static MatchHistoryFile Load()
        {
            string path = FilePath;
            if (!File.Exists(path)) return new MatchHistoryFile { records = new MatchHistoryRecord[0] };
            try
            {
                string json = File.ReadAllText(path);
                var file = JsonUtility.FromJson<MatchHistoryFile>(json);
                if (file == null || file.records == null) return new MatchHistoryFile { records = new MatchHistoryRecord[0] };
                return file;
            }
            catch (Exception)
            {
                return new MatchHistoryFile { records = new MatchHistoryRecord[0] };
            }
        }
        static void Save(MatchHistoryFile file)
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(file));
        }
        #endregion
    }
}
