using System;
using System.Text;

namespace ModularChess.Core
{
    public static class Fen
    {
        public const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static GameState Parse(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen))
            {
                throw new ArgumentException("FEN cannot be empty.", nameof(fen));
            }

            string[] parts = fen.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6)
            {
                throw new FormatException("FEN must contain 6 fields.");
            }

            Board board = ParsePlacement(parts[0]);
            Side sideToMove = ParseSide(parts[1]);
            CastlingRights castling = ParseCastling(parts[2]);
            Square? enPassant = ParseEnPassant(parts[3]);
            if (!int.TryParse(parts[4], out int halfmove) || halfmove < 0)
            {
                throw new FormatException("Invalid halfmove clock.");
            }

            if (!int.TryParse(parts[5], out int fullmove) || fullmove < 1)
            {
                throw new FormatException("Invalid fullmove number.");
            }

            ValidateKings(board);
            board = ApplyHasMovedFromCastling(board, castling);
            return GameState.FromPosition(board, sideToMove, enPassant, castling, halfmove, fullmove);
        }

        public static string Format(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return $"{FormatPlacement(state.Board)} {FormatSide(state.SideToMove)} {state.CastlingRights} {FormatEnPassant(state.EnPassantTarget)} {state.HalfmoveClock} {state.FullmoveNumber}";
        }

        internal static string PositionKey(Board board, Side side, CastlingRights castling, Square? enPassant)
        {
            return $"{FormatPlacement(board)} {FormatSide(side)} {castling} {FormatEnPassant(enPassant)}";
        }

        internal static char PieceTypeToFenChar(PieceType type, Side side)
        {
            char letter;
            switch (type)
            {
                case PieceType.Pawn:
                    letter = 'p';
                    break;
                case PieceType.Knight:
                    letter = 'n';
                    break;
                case PieceType.Bishop:
                    letter = 'b';
                    break;
                case PieceType.Rook:
                    letter = 'r';
                    break;
                case PieceType.Queen:
                    letter = 'q';
                    break;
                case PieceType.King:
                    letter = 'k';
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return side == Side.White ? char.ToUpperInvariant(letter) : letter;
        }

        private static Board ParsePlacement(string placement)
        {
            string[] ranks = placement.Split('/');
            if (ranks.Length != Square.BoardSize)
            {
                throw new FormatException("FEN placement must contain 8 ranks.");
            }

            Piece[] squares = new Piece[Square.BoardSize * Square.BoardSize];
            for (int fenRank = 0; fenRank < ranks.Length; fenRank++)
            {
                int rank = 7 - fenRank;
                int file = 0;
                string rankText = ranks[fenRank];
                for (int i = 0; i < rankText.Length; i++)
                {
                    char symbol = rankText[i];
                    if (char.IsDigit(symbol))
                    {
                        int empty = symbol - '0';
                        if (empty < 1 || empty > 8)
                        {
                            throw new FormatException("Invalid empty-square count in FEN.");
                        }

                        file += empty;
                        continue;
                    }

                    if (file >= Square.BoardSize)
                    {
                        throw new FormatException("FEN rank overflows the board.");
                    }

                    Piece piece = ParsePiece(symbol, file, rank);
                    squares[new Square(file, rank).ToIndex()] = piece;
                    file++;
                }

                if (file != Square.BoardSize)
                {
                    throw new FormatException("FEN rank does not cover 8 files.");
                }
            }

            return new Board(squares);
        }

        private static Piece ParsePiece(char symbol, int file, int rank)
        {
            Side side = char.IsUpper(symbol) ? Side.White : Side.Black;
            PieceType type;
            switch (char.ToLowerInvariant(symbol))
            {
                case 'p':
                    type = PieceType.Pawn;
                    break;
                case 'n':
                    type = PieceType.Knight;
                    break;
                case 'b':
                    type = PieceType.Bishop;
                    break;
                case 'r':
                    type = PieceType.Rook;
                    break;
                case 'q':
                    type = PieceType.Queen;
                    break;
                case 'k':
                    type = PieceType.King;
                    break;
                default:
                    throw new FormatException($"Unknown piece character '{symbol}'.");
            }

            bool hasMoved = HasMovedFromPlacement(type, side, file, rank);
            return new Piece(type, side, hasMoved);
        }

        private static Board ApplyHasMovedFromCastling(Board board, CastlingRights rights)
        {
            Board next = board;
            next = FlagUnmovedIfRightsLost(
                next,
                new Square(4, 0),
                Side.White,
                PieceType.King,
                rights.WhiteKingSide || rights.WhiteQueenSide);
            next = FlagUnmovedIfRightsLost(
                next,
                new Square(4, 7),
                Side.Black,
                PieceType.King,
                rights.BlackKingSide || rights.BlackQueenSide);
            next = FlagUnmovedIfRightsLost(next, new Square(0, 0), Side.White, PieceType.Rook, rights.WhiteQueenSide);
            next = FlagUnmovedIfRightsLost(next, new Square(7, 0), Side.White, PieceType.Rook, rights.WhiteKingSide);
            next = FlagUnmovedIfRightsLost(next, new Square(0, 7), Side.Black, PieceType.Rook, rights.BlackQueenSide);
            next = FlagUnmovedIfRightsLost(next, new Square(7, 7), Side.Black, PieceType.Rook, rights.BlackKingSide);
            return next;
        }

        private static Board FlagUnmovedIfRightsLost(
            Board board,
            Square square,
            Side side,
            PieceType type,
            bool stillHasRight)
        {
            Piece piece = board.GetPiece(square);
            if (piece == null || piece.Side != side || piece.Type != type || piece.HasMoved || stillHasRight)
            {
                return board;
            }

            return board.WithPiece(square, new Piece(type, side, true, piece.Id));
        }

        private static bool HasMovedFromPlacement(PieceType type, Side side, int file, int rank)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return side == Side.White ? rank != 1 : rank != 6;
                case PieceType.King:
                    return side == Side.White ? !(file == 4 && rank == 0) : !(file == 4 && rank == 7);
                case PieceType.Rook:
                    if (side == Side.White)
                    {
                        return !(rank == 0 && (file == 0 || file == 7));
                    }

                    return !(rank == 7 && (file == 0 || file == 7));
                default:
                    return false;
            }
        }

        private static Side ParseSide(string token)
        {
            if (token == "w")
            {
                return Side.White;
            }

            if (token == "b")
            {
                return Side.Black;
            }

            throw new FormatException("Active color must be 'w' or 'b'.");
        }

        private static CastlingRights ParseCastling(string token)
        {
            if (token == "-")
            {
                return CastlingRights.None;
            }

            bool whiteKing = false;
            bool whiteQueen = false;
            bool blackKing = false;
            bool blackQueen = false;
            for (int i = 0; i < token.Length; i++)
            {
                switch (token[i])
                {
                    case 'K':
                        whiteKing = true;
                        break;
                    case 'Q':
                        whiteQueen = true;
                        break;
                    case 'k':
                        blackKing = true;
                        break;
                    case 'q':
                        blackQueen = true;
                        break;
                    default:
                        throw new FormatException("Invalid castling rights.");
                }
            }

            return new CastlingRights(whiteKing, whiteQueen, blackKing, blackQueen);
        }

        private static Square? ParseEnPassant(string token)
        {
            if (token == "-")
            {
                return null;
            }

            if (!Square.TryParse(token, out Square square))
            {
                throw new FormatException("Invalid en passant target.");
            }

            return square;
        }

        private static void ValidateKings(Board board)
        {
            if (board.FindKing(Side.White) == null || board.FindKing(Side.Black) == null)
            {
                throw new FormatException("FEN must contain one king for each side.");
            }
        }

        private static string FormatPlacement(Board board)
        {
            StringBuilder builder = new StringBuilder();
            for (int fenRank = 0; fenRank < Square.BoardSize; fenRank++)
            {
                if (fenRank > 0)
                {
                    builder.Append('/');
                }

                int empty = 0;
                int rank = 7 - fenRank;
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    Piece piece = board.GetPiece(new Square(file, rank));
                    if (piece == null)
                    {
                        empty++;
                        continue;
                    }

                    if (empty > 0)
                    {
                        builder.Append(empty);
                        empty = 0;
                    }

                    builder.Append(PieceTypeToFenChar(piece.Type, piece.Side));
                }

                if (empty > 0)
                {
                    builder.Append(empty);
                }
            }

            return builder.ToString();
        }

        private static string FormatSide(Side side)
        {
            switch (side)
            {
                case Side.White:
                    return "w";
                case Side.Black:
                    return "b";
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        private static string FormatEnPassant(Square? square)
        {
            return square == null ? "-" : square.Value.ToString();
        }
    }
}
