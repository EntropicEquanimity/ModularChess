using System.Collections.Generic;

namespace ModularChess.Core
{
    internal sealed class PowerfulPiecesMoves : IMoveHook
    {
        #region Public Methods
        public void Append(Board board, Side side, ModeRuntime runtime, List<Move> moves)
        {
            for (int i = 0; i < 64; i++)
            {
                Square from = Square.FromIndex(i);
                Piece piece = board.GetPiece(from);
                if (piece == null || piece.Side != side || piece.Type != PieceType.Bishop)
                {
                    continue;
                }
                if (!runtime.IsEmpowered(piece.Id) || runtime.HasStatus(piece.Id, StatusKind.Stasis))
                {
                    continue;
                }
                for (int d = 0; d < Directions.KingFiles.Length; d++)
                {
                    Square to = from.Offset(Directions.KingFiles[d], Directions.KingRanks[d]);
                    if (!to.IsOnBoard || !TerrainRules.CanLand(board, to))
                    {
                        continue;
                    }
                    Piece occupant = board.GetPiece(to);
                    if (occupant != null && occupant.Side == side && occupant.Type == PieceType.Pawn)
                    {
                        moves.Add(new Move(from, to, MoveKind.Swap));
                    }
                }
            }
        }
        #endregion
    }
}
