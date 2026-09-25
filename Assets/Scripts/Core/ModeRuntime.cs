using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public sealed class ModeRuntime
    {
        #region Fields
        public static ModeRuntime Empty { get; } = new ModeRuntime();
        private readonly HashSet<Guid> _empowered;
        private readonly HashSet<Guid> _extraLife;
        private readonly HashSet<Guid> _extraLifeSpent;
        private readonly HashSet<Guid> _summoned;
        private readonly Dictionary<Guid, PieceStatus> _statuses;
        private readonly HashSet<MartyrPower> _whiteUnlocks;
        private readonly HashSet<MartyrPower> _blackUnlocks;
        private readonly Dictionary<MartyrPower, int> _whiteObtains;
        private readonly Dictionary<MartyrPower, int> _blackObtains;
        private readonly List<CaptureRecord> _captures;
        private readonly List<LandmineMarker> _landmines;
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
        public int WhiteIronCurtainTurns { get; private set; }
        public int BlackIronCurtainTurns { get; private set; }
        public int WhiteBloodDebtCharges { get; private set; }
        public int BlackBloodDebtCharges { get; private set; }
        public bool WhiteReserveCallArmed { get; private set; }
        public bool BlackReserveCallArmed { get; private set; }
        public int WhiteFogVisionTurns { get; private set; }
        public int BlackFogVisionTurns { get; private set; }
        public int WhiteDustCloudTurns { get; private set; }
        public int BlackDustCloudTurns { get; private set; }
        public Guid? ExtraMoveKingId { get; private set; }
        public Guid? OverloadPieceId { get; private set; }
        public int OverloadMovesMade { get; private set; }
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
            return _extraLife.Contains(pieceId) && !_extraLifeSpent.Contains(pieceId);
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
        public int IronCurtainTurns(Side side) => side == Side.White ? WhiteIronCurtainTurns : BlackIronCurtainTurns;
        public int BloodDebtCharges(Side side) => side == Side.White ? WhiteBloodDebtCharges : BlackBloodDebtCharges;
        public bool ReserveCallArmed(Side side) => side == Side.White ? WhiteReserveCallArmed : BlackReserveCallArmed;
        public int FogVisionTurns(Side side) => side == Side.White ? WhiteFogVisionTurns : BlackFogVisionTurns;
        public int DustCloudTurns(Side side) => side == Side.White ? WhiteDustCloudTurns : BlackDustCloudTurns;
        public IReadOnlyList<LandmineMarker> Landmines => _landmines;
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
                    next._extraLife.Add(id);
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
        public ModeRuntime WithOverload(Guid? pieceId, int movesMade)
        {
            ModeRuntime next = Clone();
            next.OverloadPieceId = pieceId;
            next.OverloadMovesMade = movesMade;
            return next;
        }
        public ModeRuntime ClearOverload()
        {
            return WithOverload(null, 0);
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
            next._extraLife.Remove(pieceId);
            next._empowered.Remove(pieceId);
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
        public ModeRuntime WithIronCurtain(Side side, int turns)
        {
            ModeRuntime next = Clone();
            if (side == Side.White) next.WhiteIronCurtainTurns = turns;
            else next.BlackIronCurtainTurns = turns;
            return next;
        }
        public ModeRuntime AddBloodDebt(Side side)
        {
            ModeRuntime next = Clone();
            if (side == Side.White) next.WhiteBloodDebtCharges++;
            else next.BlackBloodDebtCharges++;
            return next;
        }
        public ModeRuntime SpendBloodDebt(Side side)
        {
            ModeRuntime next = Clone();
            if (side == Side.White)
            {
                if (next.WhiteBloodDebtCharges > 0) next.WhiteBloodDebtCharges--;
            }
            else
            {
                if (next.BlackBloodDebtCharges > 0) next.BlackBloodDebtCharges--;
            }
            return next;
        }
        public ModeRuntime WithReserveCall(Side side, bool armed)
        {
            ModeRuntime next = Clone();
            if (side == Side.White) next.WhiteReserveCallArmed = armed;
            else next.BlackReserveCallArmed = armed;
            return next;
        }
        public ModeRuntime WithFogVision(Side side, int turns)
        {
            ModeRuntime next = Clone();
            if (side == Side.White) next.WhiteFogVisionTurns = turns;
            else next.BlackFogVisionTurns = turns;
            return next;
        }
        public ModeRuntime WithDustCloud(Side side, int turns)
        {
            ModeRuntime next = Clone();
            if (side == Side.White) next.WhiteDustCloudTurns = turns;
            else next.BlackDustCloudTurns = turns;
            return next;
        }
        public ModeRuntime AddLandmine(Side owner, Square square)
        {
            ModeRuntime next = Clone();
            next._landmines.Add(new LandmineMarker(owner, square));
            return next;
        }
        public ModeRuntime RemoveLandmineAt(Square square)
        {
            ModeRuntime next = Clone();
            for (int i = next._landmines.Count - 1; i >= 0; i--)
            {
                if (next._landmines[i].Square.Equals(square))
                {
                    next._landmines.RemoveAt(i);
                }
            }
            return next;
        }
        public bool TryGetLandmine(Square square, out LandmineMarker marker)
        {
            for (int i = 0; i < _landmines.Count; i++)
            {
                if (_landmines[i].Square.Equals(square))
                {
                    marker = _landmines[i];
                    return true;
                }
            }
            marker = default;
            return false;
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
                if (side == Side.White) next.WhiteFleetPawns = true;
                else next.BlackFleetPawns = true;
            }
            if (power == MartyrPower.Bombard)
            {
                if (side == Side.White) next.WhiteBombard = true;
                else next.BlackBombard = true;
            }
            return next;
        }
        public ModeRuntime ConsumeDraftSlot(Side side)
        {
            ModeRuntime next = Clone();
            if (side == Side.White)
            {
                if (next.WhiteDraftsQueued > 0) next.WhiteDraftsQueued--;
                next.WhiteDraftsResolved++;
            }
            else
            {
                if (next.BlackDraftsQueued > 0) next.BlackDraftsQueued--;
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
                if (!record.Exiled || record.Side != sideThatEndedTurn) continue;
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
                if (ticked.RemainingTurns <= 0) next._statuses.Remove(keys[i]);
                else next._statuses[keys[i]] = ticked;
            }
            return next;
        }
        public ModeRuntime TickSideEffects(Side sideThatEndedTurn)
        {
            ModeRuntime next = Clone();
            if (sideThatEndedTurn == Side.White)
            {
                if (next.WhiteIronCurtainTurns > 0) next.WhiteIronCurtainTurns--;
                if (next.WhiteFogVisionTurns > 0) next.WhiteFogVisionTurns--;
                if (next.BlackDustCloudTurns > 0) next.BlackDustCloudTurns--;
            }
            else
            {
                if (next.BlackIronCurtainTurns > 0) next.BlackIronCurtainTurns--;
                if (next.BlackFogVisionTurns > 0) next.BlackFogVisionTurns--;
                if (next.WhiteDustCloudTurns > 0) next.WhiteDustCloudTurns--;
            }
            return next;
        }
        #endregion

        #region Private Methods
        private ModeRuntime()
        {
            _empowered = new HashSet<Guid>();
            _extraLife = new HashSet<Guid>();
            _extraLifeSpent = new HashSet<Guid>();
            _summoned = new HashSet<Guid>();
            _statuses = new Dictionary<Guid, PieceStatus>();
            _whiteUnlocks = new HashSet<MartyrPower>();
            _blackUnlocks = new HashSet<MartyrPower>();
            _whiteObtains = new Dictionary<MartyrPower, int>();
            _blackObtains = new Dictionary<MartyrPower, int>();
            _captures = new List<CaptureRecord>();
            _landmines = new List<LandmineMarker>();
        }
        private ModeRuntime(ModeRuntime source)
        {
            _empowered = new HashSet<Guid>(source._empowered);
            _extraLife = new HashSet<Guid>(source._extraLife);
            _extraLifeSpent = new HashSet<Guid>(source._extraLifeSpent);
            _summoned = new HashSet<Guid>(source._summoned);
            _statuses = new Dictionary<Guid, PieceStatus>(source._statuses);
            _whiteUnlocks = new HashSet<MartyrPower>(source._whiteUnlocks);
            _blackUnlocks = new HashSet<MartyrPower>(source._blackUnlocks);
            _whiteObtains = new Dictionary<MartyrPower, int>(source._whiteObtains);
            _blackObtains = new Dictionary<MartyrPower, int>(source._blackObtains);
            _captures = new List<CaptureRecord>(source._captures);
            _landmines = new List<LandmineMarker>(source._landmines);
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
            WhiteIronCurtainTurns = source.WhiteIronCurtainTurns;
            BlackIronCurtainTurns = source.BlackIronCurtainTurns;
            WhiteBloodDebtCharges = source.WhiteBloodDebtCharges;
            BlackBloodDebtCharges = source.BlackBloodDebtCharges;
            WhiteReserveCallArmed = source.WhiteReserveCallArmed;
            BlackReserveCallArmed = source.BlackReserveCallArmed;
            WhiteFogVisionTurns = source.WhiteFogVisionTurns;
            BlackFogVisionTurns = source.BlackFogVisionTurns;
            WhiteDustCloudTurns = source.WhiteDustCloudTurns;
            BlackDustCloudTurns = source.BlackDustCloudTurns;
            ExtraMoveKingId = source.ExtraMoveKingId;
            OverloadPieceId = source.OverloadPieceId;
            OverloadMovesMade = source.OverloadMovesMade;
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
