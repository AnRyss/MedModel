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

    public static void PrintResultsBatch(IEnumerable<SimResult> results, SimConfig config)
    {
        var list = results.ToList();
        if (list.Count == 0) return;


        var avgDtdtP50 = list.Average(r => r.DtdtP50);
        var avgDtdtP95 = list.Average(r => r.DtdtP95);
        var avgLosP50 = list.Average(r => r.LosP50);
        var avgLosP95 = list.Average(r => r.LosP95);
        var avgLwbsRate = list.Average(r => r.LwbsRate);
        var avgPhysUtil = list.Average(r => r.PhysicianUtilization);
        var avgSpaceUtil = list.Average(r => r.SpaceUtilization);
        var avgBoarding = list.Average(r => r.BoardingMeanMinutes);
        var avgTotalPatients = list.Average(r => r.TotalPatients);
        var avgLwbsCount = list.Average(r => r.LwbsCount);
        var avgPhysUtilByHour = Enumerable.Range(0, 24)
            .ToDictionary(
                hour => hour,
                hour => list
                    .SelectMany(r =>
                        r.PhysicianUtilizationByHours.TryGetValue(hour, out var vals) ? vals : new List<double>())
                    .DefaultIfEmpty(0.0)
                    .Average()
            );
        var scenarioName = list.First().ScenarioName;

        Console.WriteLine($"Сценарий: {scenarioName}");
        Console.WriteLine($"Количество прогонов: {list.Count}");
        Console.WriteLine($"Дней всего: {config.SimHorizon.TotalDays}");
        Console.WriteLine($"Пациентов всего (среднее): {avgTotalPatients:F0}");
        Console.WriteLine($"Ушли (LWBS): {avgLwbsCount:F0} ({avgLwbsRate:P1})");
        Console.WriteLine($"Время до врача (P50): {avgDtdtP50:F1} мин");
        Console.WriteLine($"Время до врача (P95): {avgDtdtP95:F1} мин");
        Console.WriteLine($"Время в отделении (P50): {avgLosP50:F1} мин");
        Console.WriteLine($"Время в отделении (P95): {avgLosP95:F1} мин");
        Console.WriteLine($"Загрузка мест: {avgSpaceUtil:P1}");
        Console.WriteLine($"Среднее ожидание койки: {avgBoarding:F1} мин");
        Console.WriteLine($"Загрузка врачей: {avgPhysUtil:P1}");
        avgPhysUtilByHour
            .ToList()
            .ForEach(kvp => Console.WriteLine($"Загрузка врачей в {kvp.Key} часов: {kvp.Value:P1}"));
    }
}