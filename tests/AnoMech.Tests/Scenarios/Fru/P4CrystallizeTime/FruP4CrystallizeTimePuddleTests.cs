using System.Text.RegularExpressions;
using AnoMech.Scenarios.Fru.P4CrystallizeTime;

namespace AnoMech.Tests;

// A dragon puddle cleanses whoever steps in it first, so a bot crossing another player's puddle on
// its way spends it and leaves its owner uncleansed. Every step-in must be the one its stepper's
// strat sent it to.
public partial class FruP4CrystallizeTimePuddleTests
{
    private const int Seeds = 60;

    private static IEnumerable<TestCaseData> Strats() =>
        new FruP4CrystallizeTimeScenario().AiStrats.Select((ai, index) => new TestCaseData(index).SetArgDisplayNames(ai.Name));

    [TestCaseSource(nameof(Strats))]
    public void EveryoneStepsOnlyInThePuddleTheirStratSentThemTo(int strat)
    {
        var wrong = new List<string>();
        for (var seed = 0; seed < Seeds; seed++)
        {
            IReadOnlyList<string> lines = [];
            var run = ScenarioRun.Execute(typeof(FruP4CrystallizeTimeScenario), strat, seed,
                new ScenarioRunOptions { Probe = p => lines = p.LogLines, WriteArtifactsOnFailure = false });

            var heading = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                if (HeadsFor().Match(line) is { Success: true } heads)
                    heading[heads.Groups["role"].Value] = heads.Groups["catcher"].Value;
                else if (SteppedIn().Match(line) is { Success: true } step)
                {
                    var role = step.Groups["role"].Value;
                    var catcher = step.Groups["catcher"].Value;
                    if (!heading.TryGetValue(role, out var meant) || meant != catcher)
                        wrong.Add($"seed {seed}: {role} stepped in {catcher}'s puddle at {step.Groups["at"].Value}s, "
                                  + (meant is null ? "sent to none" : $"sent to {meant}'s") + $"{step.Groups["empty"].Value}"
                                  + (run.Passed ? "" : $" [run: {string.Join("; ", run.Deaths.Select(d => $"{d.Role} died to {d.Cause} at {d.Time:F1}s"))}]"));
                }
            }
        }
        foreach (var line in wrong.Take(30)) TestContext.Out.WriteLine(line);
        Assert.That(wrong, Is.Empty, $"{wrong.Count} puddle(s) taken by the wrong player over {Seeds} seeds");
    }

    [GeneratedRegex(@"\[CrystallizeTime\] (?<role>\w+) heads for (?<catcher>\w+)'s puddle\.")]
    private static partial Regex HeadsFor();

    [GeneratedRegex(@"t=\s*(?<at>[\d.]+) .*\[CrystallizeTime\] (?<role>\w+) stepped in (?<catcher>\w+)'s puddle(?<empty>[^.]*)\.")]
    private static partial Regex SteppedIn();
}
