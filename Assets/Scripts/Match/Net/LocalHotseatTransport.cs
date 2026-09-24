using System;

namespace ModularChess.Match
{
    public sealed class LocalHotseatTransport : IFriendTransport
    {
        #region Fields
        public bool IsConnected { get; private set; }
        public FriendLobbyRole Role { get; private set; }
        public string JoinCode { get; private set; }
        public event Action<FriendMessage> Received;
        public event Action Disconnected;
        static LocalHotseatTransport _openHost;
        #endregion

        #region Public Methods
        public void Host(string joinCode)
        {
            JoinCode = joinCode ?? string.Empty;
            Role = FriendLobbyRole.Host;
            IsConnected = true;
            _openHost = this;
        }
        public void Join(string joinCode)
        {
            if (_openHost == null || !string.Equals(_openHost.JoinCode, joinCode, StringComparison.Ordinal))
                throw new InvalidOperationException("No open Lobby for that Join Code.");
            JoinCode = joinCode;
            Role = FriendLobbyRole.Guest;
            IsConnected = true;
            _openHost.Received?.Invoke(new FriendMessage(FriendMessageKind.Hello, "guest"));
            Received?.Invoke(new FriendMessage(FriendMessageKind.LobbySync, "host"));
        }
        public void Send(FriendMessage message)
        {
            if (!IsConnected) return;
            if (Role == FriendLobbyRole.Host)
                return;
            _openHost?.Received?.Invoke(message);
        }
        public void Close()
        {
            if (!IsConnected) return;
            IsConnected = false;
            if (Role == FriendLobbyRole.Host && _openHost == this)
                _openHost = null;
            Disconnected?.Invoke();
        }
        public static LocalHotseatTransport OpenHost => _openHost;
        #endregion
    }
}
