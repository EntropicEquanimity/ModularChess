using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public sealed class ModeRuntime
    {
        #region Fields
        public static ModeRuntime Empty { get; } = new ModeRuntime();
        private readonly HashSet<Guid> _empowered;
        private readonly Dictionary<Guid, int> _extraLife;
        private readonly HashSet<Guid> _extraLifeSpent;
        private readonly HashSet<Guid> _summoned;
        private readonly Dictionary<Guid, PieceStatus> _statuses;
        private readonly HashSet<MartyrPower> _whiteUnlocks;
        private readonly HashSet<MartyrPower> _blackUnlocks;
        private readonly Dictionary<MartyrPower, int> _whiteObtains;
        private readonly Dictionary<MartyrPower, int> _blackObtains;
        private readonly List<CaptureRecord> _captures;
        public int WhiteLostMaterial { get; private set; }
        public int BlackLostMaterial { get; private set; }
        public int WhiteDraftsQueued { get; private set; }
        public int BlackDraftsQueued { get; private set; }
        public int WhiteDraftsResolved { get; private set; }
        public int BlackDraftsResolved { get; private set; }
        public bool WhiteFleetPawns { get; private set; }
        public bool BlackFleetPawns { get; private set; }
        public bool WhiteBombard { get; private set; }
        public bool BlackBombard { get; private set; }
        public Guid? ExtraMoveKingId { get; private set; }
        public int MovesThisTurn { get; private set; }
        public bool RallyArmed { get; private set; }
        public DraftOffer? PendingDraft { get; private set; }
        public PieceType? PendingBattlefieldType { get; private set; }
        public bool SetupComplete { get; private set; }
        #endregion

        #region Public Methods
        public bool IsEmpowered(Guid pieceId) => _empowered.Contains(pieceId);
        public bool ExtraLifeAvailable(Guid pieceId)
        {
            return ExtraLifeCount(pieceId) > 0;
        }
        public int ExtraLifeCount(Guid pieceId)
        {
            return _extraLife.TryGetValue(pieceId, out int count) ? count : 0;
        }
        public bool ExtraLifeSpent(Guid pieceId) => _extraLifeSpent.Contains(pieceId);
        public bool IsSummoned(Guid pieceId) => _summoned.Contains(pieceId);
        public bool TryGetStatus(Guid pieceId, out PieceStatus status)
        {
            return _statuses.TryGetValue(pieceId, out status);
        }
        public bool HasStatus(Guid pieceId, StatusKind kind)
        {
            return _statuses.TryGetValue(pieceId, out PieceStatus status) && status.Kind == kind;
        }
        public bool FleetPawns(Side side) => side == Side.White ? WhiteFleetPawns : BlackFleetPawns;
        public bool Bombard(Side side) => side == Side.White ? WhiteBombard : BlackBombard;
        public int LostMaterial(Side side) => side == Side.White ? WhiteLostMaterial : BlackLostMaterial;
        public bool Unlocked(Side side, MartyrPower power)
        {
            return side == Side.White
                ? _whiteUnlocks.Contains(power)
                : _blackUnlocks.Contains(power);
        }
        public int ObtainCount(Side side, MartyrPower power)
        {
            Dictionary<MartyrPower, int> obtains = side == Side.White ? _whiteObtains : _blackObtains;
            return obtains.TryGetValue(power, out int count) ? count : 0;
        }
        public IReadOnlyList<CaptureRecord> Captures => _captures;
        public IReadOnlyCollection<MartyrPower> Unlocks(Side side)
        {
            return side == Side.White ? _whiteUnlocks : _blackUnlocks;
        }
        public ModeRuntime Clone()
        {
            return new ModeRuntime(this);
        }
        public ModeRuntime WithEmpowered(IEnumerable<Guid> ids, IEnumerable<Guid> extraLifeIds)
        {
            ModeRuntime next = Clone();
            next._empowered.Clear();
            next._extraLife.Clear();
            if (ids != null)
            {
                foreach (Guid id in ids)
                {
                    next._empowered.Add(id);
                }
            }

            if (extraLifeIds != null)
            {
                foreach (Guid id in extraLifeIds)
                {
                    next._extraLife.TryGetValue(id, out int have);
                    next._extraLife[id] = have + 1;
                }
            }

            next.SetupComplete = true;
            return next;
        }
        public ModeRuntime WithMovesThisTurn(int count)
        {
            ModeRuntime next = Clone();
            next.MovesThisTurn = count;
            return next;
        }
        public ModeRuntime WithRally(bool armed)
        {
            ModeRuntime next = Clone();
            next.RallyArmed = armed;
            return next;
        }
        public ModeRuntime WithExtraKing(Guid? kingId)
        {
            ModeRuntime next = Clone();
            next.ExtraMoveKingId = kingId;
            return next;
        }
        public ModeRuntime WithoutEmpowered(Guid pieceId)
        {
            ModeRuntime next = Clone();
            next._empowered.Remove(pieceId);
            return next;
        }
        public ModeRuntime SpendExtraLife(Guid pieceId)
        {
            ModeRuntime next = Clone();
            next._extraLifeSpent.Add(pieceId);
            next._extraLife.TryGetValue(pieceId, out int have);
            if (have <= 1)
            {
                next._extraLife.Remove(pieceId);
                next._empowered.Remove(pieceId);
            }
            else
            {
                next._extraLife[pieceId] = have - 1;
            }
            return next;
        }
        public ModeRuntime GrantExtraLives(Guid pieceId, int count)
        {
            if (count <= 0)
                return this;
            ModeRuntime next = Clone();
            next._extraLife.TryGetValue(pieceId, out int have);
            next._extraLife[pieceId] = have + count;
            return next;
        }
        public ModeRuntime AddSummoned(Guid pieceId)
        {
            ModeRuntime next = Clone();
            next._summoned.Add(pieceId);
            return next;
        }
        public ModeRuntime AddLostMaterial(Side side, int amount, int threshold)
        {
            ModeRuntime next = Clone();
            if (side == Side.White)
            {
                int before = next.WhiteLostMaterial / threshold;
                next.WhiteLostMaterial += amount;
                int after = next.WhiteLostMaterial / threshold;
                next.WhiteDraftsQueued += Math.Max(0, after - before);
            }
            else
            {
                int before = next.BlackLostMaterial / threshold;
                next.BlackLostMaterial += amount;
                int after = next.BlackLostMaterial / threshold;
                next.BlackDraftsQueued += Math.Max(0, after - before);
            }

            return next;
        }
        public ModeRuntime WithStatus(Guid pieceId, PieceStatus status)
        {
            ModeRuntime next = Clone();
            next._statuses[pieceId] = status;
            return next;
        }
        public ModeRuntime Unlock(Side side, MartyrPower power)
        {
            ModeRuntime next = Clone();
            next.UnlocksMutable(side).Add(power);
            Dictionary<MartyrPower, int> obtains = side == Side.White ? next._whiteObtains : next._blackObtains;
            obtains.TryGetValue(power, out int count);
            obtains[power] = count + 1;
            if (power == MartyrPower.FleetPawns)
            {
                if (side == Side.White)
                {
                    next.WhiteFleetPawns = true;
                }
                else
                {
                    next.BlackFleetPawns = true;
                }
            }

            if (power == MartyrPower.Bombard)
            {
                if (side == Side.White)
                {
                    next.WhiteBombard = true;
                }
                else
                {
                    next.BlackBombard = true;
                }
            }

            return next;
        }
        public ModeRuntime ConsumeDraftSlot(Side side)
        {
            ModeRuntime next = Clone();
            if (side == Side.White)
            {
                if (next.WhiteDraftsQueued > 0)
                {
                    next.WhiteDraftsQueued--;
                }

                next.WhiteDraftsResolved++;
            }
            else
            {
                if (next.BlackDraftsQueued > 0)
                {
                    next.BlackDraftsQueued--;
                }

                next.BlackDraftsResolved++;
            }

            next.PendingDraft = null;
            next.PendingBattlefieldType = null;
            return next;
        }
        public ModeRuntime AddCapture(Piece piece, bool exiled = false, Square origin = default, int remainingTurns = 0)
        {
            if (piece == null) return this;
            ModeRuntime next = Clone();
            next._captures.Add(new CaptureRecord(
                piece.Id,
                piece.Side,
                piece.Type,
                exiled,
                origin,
                remainingTurns,
                piece.HasMoved));
            return next;
        }
        public ModeRuntime RemoveCaptureAt(int index)
        {
            ModeRuntime next = Clone();
            next._captures.RemoveAt(index);
            return next;
        }
        public ModeRuntime TickExiles(Side sideThatEndedTurn)
        {
            ModeRuntime next = Clone();
            for (int i = 0; i < next._captures.Count; i++)
            {
                CaptureRecord record = next._captures[i];
                if (!record.Exiled || record.Side != sideThatEndedTurn)
                {
                    continue;
                }
                next._captures[i] = record.WithRemaining(record.RemainingTurns - 1);
            }
            return next;
        }
        public ModeRuntime WithPendingDraft(DraftOffer? offer, PieceType? battlefieldType)
        {
            ModeRuntime next = Clone();
            next.PendingDraft = offer;
            next.PendingBattlefieldType = battlefieldType;
            return next;
        }
        public ModeRuntime TickStatuses(Side sideThatEndedTurn)
        {
            ModeRuntime next = Clone();
            if (next._statuses.Count == 0) return next;
            Guid[] keys = new Guid[next._statuses.Count];
            next._statuses.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                PieceStatus status = next._statuses[keys[i]];
                if (status.AffectedSide != sideThatEndedTurn) continue;
                PieceStatus ticked = status.Tick();
                if (ticked.RemainingTurns <= 0)
                {
                    next._statuses.Remove(keys[i]);
                }
                else
                {
                    next._statuses[keys[i]] = ticked;
                }
            }
            return next;
        }
        #endregion

        #region Private Methods
        private ModeRuntime()
        {
            _empowered = new HashSet<Guid>();
            _extraLife = new Dictionary<Guid, int>();
            _extraLifeSpent = new HashSet<Guid>();
            _summoned = new HashSet<Guid>();
            _statuses = new Dictionary<Guid, PieceStatus>();
            _whiteUnlocks = new HashSet<MartyrPower>();
            _blackUnlocks = new HashSet<MartyrPower>();
            _whiteObtains = new Dictionary<MartyrPower, int>();
            _blackObtains = new Dictionary<MartyrPower, int>();
            _captures = new List<CaptureRecord>();
        }
        private ModeRuntime(ModeRuntime source)
        {
            _empowered = new HashSet<Guid>(source._empowered);
            _extraLife = new Dictionary<Guid, int>(source._extraLife);
            _extraLifeSpent = new HashSet<Guid>(source._extraLifeSpent);
            _summoned = new HashSet<Guid>(source._summoned);
            _statuses = new Dictionary<Guid, PieceStatus>(source._statuses);
            _whiteUnlocks = new HashSet<MartyrPower>(source._whiteUnlocks);
            _blackUnlocks = new HashSet<MartyrPower>(source._blackUnlocks);
            _whiteObtains = new Dictionary<MartyrPower, int>(source._whiteObtains);
            _blackObtains = new Dictionary<MartyrPower, int>(source._blackObtains);
            _captures = new List<CaptureRecord>(source._captures);
            WhiteLostMaterial = source.WhiteLostMaterial;
            BlackLostMaterial = source.BlackLostMaterial;
            WhiteDraftsQueued = source.WhiteDraftsQueued;
            BlackDraftsQueued = source.BlackDraftsQueued;
            WhiteDraftsResolved = source.WhiteDraftsResolved;
            BlackDraftsResolved = source.BlackDraftsResolved;
            WhiteFleetPawns = source.WhiteFleetPawns;
            BlackFleetPawns = source.BlackFleetPawns;
            WhiteBombard = source.WhiteBombard;
            BlackBombard = source.BlackBombard;
            ExtraMoveKingId = source.ExtraMoveKingId;
            MovesThisTurn = source.MovesThisTurn;
            RallyArmed = source.RallyArmed;
            PendingDraft = source.PendingDraft;
            PendingBattlefieldType = source.PendingBattlefieldType;
            SetupComplete = source.SetupComplete;
        }
        private HashSet<MartyrPower> UnlocksMutable(Side side)
        {
            return side == Side.White ? _whiteUnlocks : _blackUnlocks;
        }
        #endregion
    }
}
