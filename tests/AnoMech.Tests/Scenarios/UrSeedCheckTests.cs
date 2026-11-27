using AnoMech.Scenarios.Fru.P3UltimateRelativity;

namespace AnoMech.Tests;

public class UrSeedCheckTests
{
    [TestCase(923725828)]
    [TestCase(1808252913)]
    [TestCase(1781444265)]
    public void Seed(int seed)
    {
        var run = ScenarioRun.Execute(typeof(FruP3UltimateRelativityScenario), 0, seed);
        Assert.That(run.Passed, run.ToString);
    }
}
