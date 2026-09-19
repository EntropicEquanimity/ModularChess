using System;

namespace ModularChess.Core
{
    public static class ModeCatalog
    {
        public static readonly ModeDefinition FogOfWar = new ModeDefinition(
            ModeId.FogOfWar,
            "Fog of War",
            "You see Squares in Vision. Shadow marks a ray one Square beyond. Hidden occupancy is unknown.",
            allowsHotseat: false);

        public static readonly ModeDefinition PowerfulPieces = new ModeDefinition(
            ModeId.PowerfulPieces,
            "Powerful Pieces",
            "Each Side empowers N Pieces in Setup. Each Core PieceType has one power.");

        public static readonly ModeDefinition Martyr = new ModeDefinition(
            ModeId.Martyr,
            "Martyr",
            "Lost Material unlocks Drafts. Powers apply Status, summons, and extra Moves.");

        public static ModeDefinition Get(ModeId id)
        {
            switch (id)
            {
                case ModeId.FogOfWar:
                    return FogOfWar;
                case ModeId.PowerfulPieces:
                    return PowerfulPieces;
                case ModeId.Martyr:
                    return Martyr;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        public static ModeDefinition[] All { get; } = { FogOfWar, PowerfulPieces, Martyr };
    }
}
