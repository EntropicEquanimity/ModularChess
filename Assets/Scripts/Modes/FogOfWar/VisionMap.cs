using System;

namespace ModularChess.Core
{
    public sealed class VisionMap
    {
        readonly SquareSight[] _cells;

        VisionMap(SquareSight[] cells)
        {
            _cells = cells;
        }

        public SquareSight this[Square square]
        {
            get
            {
                if (!square.IsOnBoard)
                {
                    return SquareSight.Hidden;
                }

                return _cells[square.ToIndex()];
            }
        }

        public bool IsIdentified(Square square) => this[square] == SquareSight.Identified;

        public bool IsShadow(Square square) => this[square] == SquareSight.Shadow;

        public static VisionMap AllIdentified { get; } = Fill(SquareSight.Identified);

        public static VisionMap Compute(GameState state, Side viewer)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Status != GameStatus.InProgress)
            {
                return AllIdentified;
            }
            Rules rules = state.Rules ?? VersusRules.CoreOnly;
            return rules.Hooks.ComputeVision(state, viewer);
        }

        public static VisionMap Fill(SquareSight sight)
        {
            var cells = new SquareSight[Square.BoardSize * Square.BoardSize];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = sight;
            }

            return new VisionMap(cells);
        }

        internal static VisionMap FromCells(SquareSight[] cells)
        {
            return new VisionMap(cells);
        }
    }
}
