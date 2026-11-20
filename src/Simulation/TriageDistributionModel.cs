using SimSharp;
using src.Domain;

namespace src.Simulation;

public class TriageDistributionModel

{
    private readonly IDistribution<TimeSpan>[] _triageTimes;
    private readonly IDistribution<TimeSpan>[] _treatmentTimes;
    private readonly SimConfig _config;
    private readonly SimSharp.Simulation _env;

    public TriageDistributionModel(SimSharp.Simulation simulation, SimConfig config)
    {
        this._env = simulation;
        this._config = config;
        _triageTimes = new IDistribution<TimeSpan>[]
        {
            new LogNormalTime(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(0.5)), // Уровень 1
            new LogNormalTime(TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(1)), // Уровень 2
            new LogNormalTime(TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(1)), // Уровень 3
            new LogNormalTime(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1.5)), // Уровень 4
            new LogNormalTime(TimeSpan.FromMinutes(6), TimeSpan.FromMinutes(2)) // Уровень 5
        };

        _treatmentTimes = new IDistribution<TimeSpan>[]
        {
            new LogNormalTime(TimeSpan.FromMinutes(120), TimeSpan.FromMinutes(30)), // Уровень 1
            new LogNormalTime(TimeSpan.FromMinutes(90), TimeSpan.FromMinutes(20)), // Уровень 2
            new LogNormalTime(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(15)), // Уровень 3
            new LogNormalTime(TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(10)), // Уровень 4
            new LogNormalTime(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(5)) // Уровень 5
        };
    }

    public TriageLevel SampleTriage() // Какой тяжести будет пациент
    {
        double u = _env.Rand(new Uniform(0.0, 1.0));


        var probs = _config.TriageProbabilities;


        double cumulative = 0;

        for (int i = 0; i < probs.Length; i++)
        {
            cumulative += probs[i];
            if (u < cumulative)
            {
                return (TriageLevel)(i + 1);
            }
        }


        return TriageLevel.NonUrgent;
    }

    public IDistribution<TimeSpan> Triage(TriageLevel level)
    {
        return _triageTimes[(int)level - 1];
    }

    public IDistribution<TimeSpan> Treatment(TriageLevel level)
    {
        return _treatmentTimes[(int)level - 1];
    }
}