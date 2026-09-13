using System;
using System.Collections.Generic;
using System.IO;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Match
{
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
        public MatchHistoryAction[] moves;
        public int clockSeconds;
        public int[] modes;
        public int empoweredCount;
        public int martyrThreshold;
        public int martyrDraftOptions;
        public int mainMinutes;
        public int incrementSeconds;
        public int hostSide;
        public int aiStrength;
        public int result;
    }

    [Serializable]
    sealed class MatchHistoryFile
    {
        public MatchHistoryRecord[] records = new MatchHistoryRecord[0];
    }

    public static class MatchHistoryStore
    {
        const string FileName = "match-history.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Record(MatchSession session, GameState state, int clockSeconds)
        {
            if (session == null || state == null || state.Status == GameStatus.InProgress || state.Status == GameStatus.Aborted)
                return;

            var record = new MatchHistoryRecord
            {
                moves = BuildActions(state.History),
                clockSeconds = Math.Max(0, clockSeconds),
                modes = ModeIds(session.Rules),
                empoweredCount = session.Rules.Settings.EmpoweredCount,
                martyrThreshold = session.Rules.Settings.MartyrThreshold,
                martyrDraftOptions = session.Rules.Settings.MartyrDraftOptions,
                mainMinutes = session.Rules.Settings.Time.BaseMinutes,
                incrementSeconds = session.Rules.Settings.Time.IncrementSeconds,
                hostSide = (int)session.PlayerSide,
                aiStrength = (int)session.Rules.Settings.AiStrength,
                result = ResultCode(state)
            };

            MatchHistoryFile file = Load();
            var next = new MatchHistoryRecord[file.records.Length + 1];
            for (int i = 0; i < file.records.Length; i++)
                next[i] = file.records[i];
            next[file.records.Length] = record;
            file.records = next;
            Save(file);
        }

        public static void Delete()
        {
            string path = FilePath;
            if (File.Exists(path))
                File.Delete(path);
        }

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

        static MatchHistoryAction[] BuildActions(IReadOnlyList<Move> moves)
        {
            var list = new List<MatchHistoryAction>();
            if (moves == null)
                return list.ToArray();

            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];
                list.Add(ToAction(move.From, move.To, move.Kind, move.PromotionType));
                if (move.Kind == MoveKind.Bombard)
                    list.Add(ToAction(move.To, move.From, MoveKind.Quiet, null));
            }

            return list.ToArray();
        }

        static MatchHistoryAction ToAction(Square from, Square to, MoveKind kind, PieceType? promotion)
        {
            return new MatchHistoryAction
            {
                from = from.ToString(),
                to = to.ToString(),
                kind = (int)kind,
                promotion = promotion.HasValue ? (int)promotion.Value : -1
            };
        }

        static MatchHistoryFile Load()
        {
            string path = FilePath;
            if (!File.Exists(path))
                return new MatchHistoryFile { records = new MatchHistoryRecord[0] };
            try
            {
                string json = File.ReadAllText(path);
                var file = JsonUtility.FromJson<MatchHistoryFile>(json);
                if (file == null || file.records == null)
                    return new MatchHistoryFile { records = new MatchHistoryRecord[0] };
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
    }
}
