// EdSim.Experiments/ExperimentRunner.cs
using MedicalModel.Domain;
using MedicalModel.Simulation;
using src.Domain;

public sealed class ExperimentRunner
{
    public IReadOnlyList<SimResult> RunScenario(SimConfig config, int nRuns)
    {
        var results = new List<SimResult>(nRuns);
        for (int seed = 1; seed <= nRuns; seed++)
        {
            var sim = new EdSimulation(config, seed);
            results.Add(sim.Run() with { ScenarioName = config.Name });
        }
        return results;
    }

    public IReadOnlyList<SimResult> RunSweep(IEnumerable<SimConfig> scenarios, int nRuns)
    {
        // параллельно, если нужно
        return scenarios
            .AsParallel()
            .SelectMany(cfg => RunScenario(cfg, nRuns))
            .ToList();
    }
}