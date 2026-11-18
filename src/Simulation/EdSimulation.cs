using System;
using MedicalModel.Domain;
using SimSharp;
using src.Domain;
using src.Simulation;



public sealed partial class EdSimulation
{
    private readonly SimConfig _config;
    private readonly Simulation _env;
    private readonly EdMetrics _metrics;
    private readonly int _seed;

    // Ресурсы
    private Resource _physicians;
    private Resource _triageNurses;
    private Resource _treatmentSpaces;
    private Resource _inpatientBeds;
    private PriorityQueue _doctorQueue; // приоритетная очередь к врачу

    public EdSimulation(SimConfig config, int seed)
    {
        _seed = seed;
        _config = config;
        _env = new Simulation(seed);
        _metrics = new EdMetrics();
    }

    public SimResult Run()
    {
        BuildResources();
        StartPatientGenerator();
        StartWarmupHandler();
        _env.Run(_config.SimHorizon);
        return _metrics.Build(_config, _seed);
    }

    private void BuildResources()
    {
        _physicians = new Resource(_env, _config.NumPhysicians)
        {
            Utilization = new TimeSeriesMonitor(_env, "PhysUtil"),
            WaitingTime = new SampleMonitor("PhysWait", collect: true),
            QueueLength = new TimeSeriesMonitor(_env, "PhysQueue", collect: true),
        };
        // ... аналогично для остальных
    }

    private void StartPatientGenerator()
    {
        _env.Process(PatientArrivalProcess());
    }

    private IEnumerable<Event> PatientArrivalProcess()
    {
        int id = 0;
        while (true)
        {
            var interArrival = _config.Arrivals.SampleNext(_env); // NHPP
            yield return _env.Timeout(interArrival);

            var patient = new Patient(
                id++,
                _env.Now, // или DateTime, если используешь календарь
                SampleTriage(_env.Rand()),
                IsLowAcuity: _env.Random.NextDouble() < 0.3);

            _env.Process(PatientJourney(patient));
        }
    }

    private TriageLevel SampleTriage()
    {
        double u = _env.Rand(new Uniform(0.0, 1.0));
    }

}
