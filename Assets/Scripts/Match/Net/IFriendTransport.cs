namespace ModularChess.Match
{
    public enum FriendLobbyRole
    {
        Host,
        Guest
    }

    public enum FriendMessageKind : byte
    {
        Hello = 1,
        LobbySync = 2,
        Sit = 3,
        StartMatch = 4,
        Leave = 5,
        Move = 6,
        EndTurn = 7,
        Resign = 8,
        OfferDraw = 9,
        DrawReply = 10,
        DraftPick = 11,
        SetupConfirm = 12
    }

    public readonly struct FriendMessage
    {
        public FriendMessageKind Kind { get; }
        public string Payload { get; }

        public FriendMessage(FriendMessageKind kind, string payload)
        {
            Kind = kind;
            Payload = payload ?? string.Empty;
        }
    }

    public interface IFriendTransport
    {
        bool IsConnected { get; }
        FriendLobbyRole Role { get; }
        string JoinCode { get; }
        event System.Action<FriendMessage> Received;
        event System.Action Disconnected;
        void Host(string joinCode);
        void Join(string joinCode);
        void Send(FriendMessage message);
        void Close();
    }
}
