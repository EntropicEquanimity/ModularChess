using System;
using ModularChess.Core;
using ModularChess.Core.Tests;
using ModularChess.Match;

internal static class Program
{
    static int _fails;

    static int Main()
    {
        ClockNoneStoresZeroElapsed();
        ClockIncrementDoesNotAddElapsed();
        ClockStopDoesNotAdvanceElapsed();
        FogHidesEnemyHomeWhileInProgress();
        FogLiftsOnCheckmate();
        EmpoweredKingExtraMoveKeepsTurn();
        MartyrCaptureQueuesDraft();
        Console.WriteLine(_fails == 0 ? "ALL PASS" : $"FAILS {_fails}");
        return _fails == 0 ? 0 : 1;
    }

    static void ClockNoneStoresZeroElapsed()
    {
        var clock = new MatchClock(TimeControl.None);
        clock.Start();
        clock.Tick(12f, Side.White);
        Expect(clock.IsNone, "none clock is none");
        Expect(clock.ElapsedSeconds == 0f, "none elapsed stays 0");
        Expect(!clock.Running, "none never runs");
    }

    static void ClockIncrementDoesNotAddElapsed()
    {
        var clock = new MatchClock(new TimeControl(1, 5));
        clock.Start();
        clock.Tick(1f, Side.White);
        Expect(Near(clock.WhiteSeconds, 59f), "tick consumes main time");
        Expect(Near(clock.ElapsedSeconds, 1f), "elapsed is ticked seconds");
        clock.AddIncrement(Side.White);
        Expect(Near(clock.WhiteSeconds, 64f), "increment adds remaining time");
        Expect(Near(clock.ElapsedSeconds, 1f), "increment does not add elapsed");
    }

    static void ClockStopDoesNotAdvanceElapsed()
    {
        var clock = new MatchClock(new TimeControl(1, 0));
        clock.Start();
        clock.Tick(2f, Side.White);
        clock.Stop();
        clock.Tick(8f, Side.White);
        Expect(Near(clock.ElapsedSeconds, 2f), "pause/setup/draft stop does not tick elapsed");
        Expect(Near(clock.WhiteSeconds, 58f), "stopped clock does not drain");
    }

    static void FogHidesEnemyHomeWhileInProgress()
    {
        MatchRules rules = new MatchRules(new[] { ModeId.FogOfWar }, MatchSettings.Default);
        GameState state = GameState.StartingPosition(rules);
        VisionMap vision = VisionMap.Compute(state, Side.White);
        Expect(state.Status == GameStatus.InProgress, "starting fog match in progress");
        Expect(vision.IsIdentified(new Square(4, 1)), "white home pawn rank identified");
        Expect(!vision.IsIdentified(new Square(4, 6)), "enemy pawn rank hidden in play");
    }

    static void FogLiftsOnCheckmate()
    {
        MatchRules rules = new MatchRules(new[] { ModeId.FogOfWar }, MatchSettings.Default);
        GameState state = MoveTestHelper.Play(
            GameState.StartingPosition(rules),
            "f2f3",
            "e7e5",
            "g2g4",
            "d8h4");
        Expect(state.Status == GameStatus.Checkmate, "fools mate is checkmate");
        VisionMap vision = VisionMap.Compute(state, Side.White);
        Expect(vision.IsIdentified(new Square(4, 6)), "fog lifts: true board including e7");
        Expect(vision.IsIdentified(new Square(0, 7)), "fog lifts: true board including a8");
    }

    static void EmpoweredKingExtraMoveKeepsTurn()
    {
        MatchRules rules = new MatchRules(new[] { ModeId.PowerfulPieces }, MatchSettings.Default);
        GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/4K2R w - - 0 1", rules);
        Piece king = state.Board.GetPiece(new Square(4, 0));
        state = state.ConfirmEmpowered(new[] { king.Id });
        state = MoveTestHelper.Play(state, "e1e2");
        Expect(state.SideToMove == Side.White, "extra king move keeps side");
        Expect(state.TurnOpen, "turn stays open");
        state = state.EndTurn();
        Expect(state.SideToMove == Side.Black, "end turn passes the side");
    }

    static void MartyrCaptureQueuesDraft()
    {
        MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
        GameState state = GameState.FromFen("4k3/8/8/8/8/8/3p4/3QK3 w - - 0 1", rules);
        state = MoveTestHelper.Play(state, "d1d2");
        Expect(state.Runtime.LostMaterial(Side.Black) == 1, "lost material counts capture");
        Expect(state.DraftPending, "draft queues at start of that side turn");
        Expect(state.SideToMove == Side.Black, "opponent finished before draft");
    }

    static bool Near(float value, float expected) => Math.Abs(value - expected) < 0.001f;

    static void Expect(bool condition, string name)
    {
        if (condition)
        {
            Console.WriteLine("PASS " + name);
            return;
        }

        _fails++;
        Console.WriteLine("FAIL " + name);
    }
}
