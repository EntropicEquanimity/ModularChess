using System;
using ModularChess.Core;

namespace ModularChess.Match
{
    public sealed class LocalLobby
    {
        public string Code { get; }
        public MatchRules Rules { get; }
        public MatchSettings Settings { get; }
        public bool FriendSeated { get; set; }

        public LocalLobby(string code, MatchRules rules, MatchSettings settings)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Settings = settings ?? MatchSettings.Default;
        }

        public static string CreateCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var chars = new char[6];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = alphabet[UnityEngine.Random.Range(0, alphabet.Length)];
            }

            return new string(chars);
        }
    }
}
