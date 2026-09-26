using System;

namespace ModularChess.Core
{
    public static class ModeCatalog
    {
        public static readonly ModeDefinition FogOfWar = new ModeDefinition(
            ModeId.FogOfWar,
            "Fog of War",
            "You see Squares in Vision. Hidden occupancy is unknown.",
            allowsHotseat: false);

        public static readonly ModeDefinition PowerfulPieces = new ModeDefinition(
            ModeId.PowerfulPieces,
            "Powerful Pieces",
            "Each Side spends an Empower budget in Setup. Each Core PieceType has one power and one cost.");

        public static readonly ModeDefinition Martyr = new ModeDefinition(
            ModeId.Martyr,
            "Martyr",
            "Lost Material unlocks Drafts. Powers apply Status, summons, and extra Moves.");

        public static readonly ModeDefinition ActionEconomy = new ModeDefinition(
            ModeId.ActionEconomy,
            "Action Economy",
            "Each Turn that Side receives a pool of action points. A normal Move spends 1.");

        public static readonly ModeDefinition ComplexTerrain = new ModeDefinition(
            ModeId.ComplexTerrain,
            "Complex Terrain",
            "The Board carries Swamp, Forest, and Mountain.");

        public static readonly ModeDefinition Randomizer = new ModeDefinition(
            ModeId.Randomizer,
            "Randomizer",
            "Shuffle start, random colors, and random placement.");

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
                case ModeId.ActionEconomy:
                    return ActionEconomy;
                case ModeId.ComplexTerrain:
                    return ComplexTerrain;
                case ModeId.Randomizer:
                    return Randomizer;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        public static ModeDefinition[] All { get; } =
        {
            FogOfWar, PowerfulPieces, Martyr, ActionEconomy, ComplexTerrain, Randomizer
        };
    }
}
