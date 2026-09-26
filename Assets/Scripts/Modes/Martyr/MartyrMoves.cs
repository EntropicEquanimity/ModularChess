using System.Collections.Generic;

namespace ModularChess.Core
{
    internal sealed class MartyrMoves : IMoveHook
    {
        #region Public Methods
        public void Append(Board board, Side side, ModeRuntime runtime, List<Move> moves)
        {
            if (!runtime.Bombard(side))
            {
                return;
            }
            for (int i = 0; i < 64; i++)
            {
                Square from = Square.FromIndex(i);
                Piece piece = board.GetPiece(from);
                if (piece == null || piece.Side != side || piece.Type != PieceType.Rook)
                {
                    continue;
                }
                if (runtime.HasStatus(piece.Id, StatusKind.Stasis))
                {
                    continue;
                }
                for (int d = 0; d < Directions.RookFiles.Length; d++)
                {
                    int distance = 0;
                    Square cursor = from.Offset(Directions.RookFiles[d], Directions.RookRanks[d]);
                    while (cursor.IsOnBoard)
                    {
                        distance++;
                        Piece occupant = board.GetPiece(cursor);
                        if (occupant == null)
                        {
                            if (TerrainRules.BlocksMoveThrough(board, cursor))
                            {
                                break;
                            }
                            cursor = cursor.Offset(Directions.RookFiles[d], Directions.RookRanks[d]);
                            continue;
                        }
                        if (occupant.Side != side && occupant.Type != PieceType.King && distance >= 5)
                        {
                            moves.Add(new Move(from, cursor, MoveKind.Bombard, capturedType: occupant.Type));
                        }
                        break;
                    }
                }
            }
        }
        #endregion
    }
}
