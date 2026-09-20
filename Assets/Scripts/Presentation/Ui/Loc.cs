using System;
using System.Collections.Generic;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public static class Loc
    {
        #region Fields
        public const string PrefsKey = "Language";
        public const string English = "en";
        public static readonly string[] Codes = { "en", "es", "tl", "zh-Hans", "zh-Hant" };
        public static event Action Changed;
        static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>();
        static string _language = English;
        static bool _loaded;
        public static string Language
        {
            get
            {
                EnsureLoaded();
                return _language;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>Looks up a key in the active language, then English.</summary>
        public static string Get(string key)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (TryGet(_language, key, out string value))
            {
                return value;
            }

            if (_language != English && TryGet(English, key, out value))
            {
                return value;
            }

            return key;
        }

        /// <summary>Formats a localized pattern with the given arguments.</summary>
        public static string Format(string key, params object[] args)
        {
            string pattern = Get(key);
            if (args == null || args.Length == 0)
            {
                return pattern;
            }

            try
            {
                return string.Format(pattern, args);
            }
            catch (FormatException)
            {
                return pattern;
            }
        }

        public static void SetLanguage(string code)
        {
            string next = Normalize(code);
            EnsureLoaded();
            if (next == _language && _loaded)
            {
                return;
            }

            _language = next;
            LoadTable(_language);
            PlayerPrefs.SetString(PrefsKey, _language);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static int LanguageIndex()
        {
            EnsureLoaded();
            for (int i = 0; i < Codes.Length; i++)
            {
                if (Codes[i] == _language)
                {
                    return i;
                }
            }

            return 0;
        }

        public static string[] LanguageLabels()
        {
            EnsureLoaded();
            var labels = new string[Codes.Length];
            for (int i = 0; i < Codes.Length; i++)
                labels[i] = Get("lang." + Codes[i]);
            return labels;
        }

        public static string ModeName(ModeId id)
        {
            switch (id)
            {
                case ModeId.FogOfWar:
                    return Get("mode.fog.name");
                case ModeId.PowerfulPieces:
                    return Get("mode.powerful.name");
                case ModeId.Martyr:
                    return Get("mode.martyr.name");
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        public static string ModeSummary(ModeId id)
        {
            switch (id)
            {
                case ModeId.FogOfWar:
                    return Get("mode.fog.summary");
                case ModeId.PowerfulPieces:
                    return Get("mode.powerful.summary");
                case ModeId.Martyr:
                    return Get("mode.martyr.summary");
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        public static string SideName(Side side)
        {
            switch (side)
            {
                case Side.White:
                    return Get("side.white");
                case Side.Black:
                    return Get("side.black");
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        public static string PieceName(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return Get("piece.pawn");
                case PieceType.Knight:
                    return Get("piece.knight");
                case PieceType.Bishop:
                    return Get("piece.bishop");
                case PieceType.Rook:
                    return Get("piece.rook");
                case PieceType.Queen:
                    return Get("piece.queen");
                case PieceType.King:
                    return Get("piece.king");
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string EmpoweredDescription(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return Get("piece.empowered.pawn");
                case PieceType.Knight:
                    return Get("piece.empowered.knight");
                case PieceType.Bishop:
                    return Get("piece.empowered.bishop");
                case PieceType.Rook:
                    return Get("piece.empowered.rook");
                case PieceType.Queen:
                    return Get("piece.empowered.queen");
                case PieceType.King:
                    return Get("piece.empowered.king");
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        #endregion

        #region Private Methods
        static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            _language = Normalize(PlayerPrefs.GetString(PrefsKey, English));
            LoadTable(English);
            if (_language != English)
            {
                LoadTable(_language);
            }
        }

        static string Normalize(string code)
        {
            if (string.IsNullOrEmpty(code))
                return English;
            for (int i = 0; i < Codes.Length; i++)
            {
                if (Codes[i] == code)
                    return code;
            }
            return English;
        }

        static bool TryGet(string language, string key, out string value)
        {
            if (Tables.TryGetValue(language, out Dictionary<string, string> table)
                && table.TryGetValue(key, out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        static void LoadTable(string language)
        {
            if (Tables.ContainsKey(language))
            {
                return;
            }

            string text = ReadFile(language);
            Tables[language] = Parse(text);
        }

        static string ReadFile(string language)
        {
            TextAsset resource = Resources.Load<TextAsset>($"Localization/{language}");
            return resource != null ? resource.text : string.Empty;
        }

        static Dictionary<string, string> Parse(string text)
        {
            var table = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(text))
            {
                return table;
            }

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                int split = line.IndexOf('=');
                if (split <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, split).Trim();
                string value = line.Substring(split + 1).Replace("\\n", "\n");
                table[key] = value;
            }

            return table;
        }
        #endregion
    }
}
