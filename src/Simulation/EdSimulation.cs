using System;
using System.Collections.Generic;
using MathNet.Numerics.Distributions;
using MedicalModel.Domain;
using MedicalModel.Simulation;
using SimSharp;
using src.Domain;
using src.Simulation;

public sealed partial class EdSimulation
{
    private readonly SimConfig _config;
    private readonly Simulation _env;
    private readonly EdMetrics _metrics;
    private readonly int _seed;

    // Модели, созданные из данных конфига
    private readonly ArrivalModel _arrivalModel;
    private readonly TriageDistributionModel _triageModel;

    // Ресурсы
    private Resource _physicians;
    private Resource _triageNurses;
    private Resource _treatmentSpaces;
    private Resource _inpatientBeds;

    public EdSimulation(SimConfig config, int seed)
    {
        _seed = seed;
        _config = config;
        _env = new Simulation(seed);
        _metrics = new EdMetrics();

        // Создаем модели, передавая им конфиг
        _arrivalModel = new ArrivalModel(_config.ArrivalConfig);
        _triageModel = new TriageDistributionModel(_config);
    }

    public SimResult Run()
    {
        BuildResources();
        StartPatientGenerator();
        StartWarmupHandler();
        _env.Process(UpdateUtilization(_physicians, (TimeSeriesMonitor)_physicians.Utilization));
        _env.Process(UpdateUtilization(_treatmentSpaces, (TimeSeriesMonitor)_treatmentSpaces.Utilization));
        _env.Run(_config.SimHorizon);
        return _metrics.Build(_config, _seed, _physicians, _treatmentSpaces);
    }

    private void BuildResources()
    {
        _physicians = new Resource(_env, _config.NumPhysicians)
        {
            Utilization = new TimeSeriesMonitor(_env, "PhysUtil"),
            WaitingTime = new SampleMonitor("PhysWait", collect: true),
            QueueLength = new TimeSeriesMonitor(_env, "PhysQueue", collect: true),
        };

        _triageNurses = new Resource(_env, _config.NumTriageNurses)
        {
            Utilization = new TimeSeriesMonitor(_env, "TriageUtil"),
            WaitingTime = new SampleMonitor("TriageWait", collect: true),
            QueueLength = new TimeSeriesMonitor(_env, "TriageQueue", collect: true),
        };

        _treatmentSpaces = new Resource(_env, _config.NumTreatmentSpaces)
        {
            Utilization = new TimeSeriesMonitor(_env, "SpaceUtil"),
            WaitingTime = new SampleMonitor("SpaceWait", collect: true),
            QueueLength = new TimeSeriesMonitor(_env, "SpaceQueue", collect: true),
        };

        _inpatientBeds = new Resource(_env, _config.NumInpatientBeds)
        {
            Utilization = new TimeSeriesMonitor(_env, "BedUtil"),
            WaitingTime = new SampleMonitor("BedWait", collect: true),
            QueueLength = new TimeSeriesMonitor(_env, "BedQueue", collect: true),
        };
    }

    private void StartPatientGenerator()
    {
        _env.Process(PatientArrivalProcess());
    }

    private IEnumerable<Event> PatientArrivalProcess()
    {
        int id = 0;
        int patientsToday = 0;
        DateTime currentDay = _env.Now.Date;
        int? configDailyPatients = _config.ArrivalConfig.DailyPatients ??  1; 
        var maxDailyPatients = _env.Rand(new Poisson( //TODO: не на каждого пациента создавать новый лимит, перенести обновление на конец дня.
            (double)configDailyPatients));
        Console.WriteLine(maxDailyPatients);
        while (true)
        {
            if (configDailyPatients is not null && configDailyPatients > 1) // ограничиваем количество пациентов в день только, если в конфиге указано число
            {
                if (_env.Now.Date > currentDay)
                {
                    currentDay = _env.Now.Date;
                    patientsToday = 0;
                }


                if (patientsToday >= maxDailyPatients)
                {

                    var nextDay = currentDay.AddDays(1);
                    var timeToWait = nextDay - _env.Now;
                    yield return _env.Timeout(timeToWait);
                    continue;
                }
            }

            var interArrival = _arrivalModel.SampleNext(_env);
            yield return _env.Timeout(interArrival);

            var patient = new Patient(
                id++,
                _env.Now,
                _triageModel.SampleTriage(_env),
                IsLowAcuity: _env.Rand(new Uniform(0, 1)) < _config.LowAcuityProbability);

            _metrics.RecordArrival();
            _env.Process(PatientJourney(patient));
        }
    }

    private IEnumerable<Event> StartWarmupHandler()
    {
        var monitors = new IMonitor[]
        {
            _physicians.Utilization, _physicians.WaitingTime, _physicians.QueueLength,
            _triageNurses.Utilization, _triageNurses.WaitingTime, _triageNurses.QueueLength,
            _treatmentSpaces.Utilization, _treatmentSpaces.WaitingTime, _treatmentSpaces.QueueLength,
            _inpatientBeds.Utilization, _inpatientBeds.WaitingTime, _inpatientBeds.QueueLength
        };

        foreach (var m in monitors) m.Active = false;

        yield return _env.Timeout(_config.Warmup);

        foreach (var m in monitors) m.Active = true;
    }

    private IEnumerable<Event> UpdateUtilization(Resource resource, TimeSeriesMonitor monitor)
    {
        while (true)
        {
            double load = (double)resource.InUse / resource.Capacity;
            monitor.UpdateTo(load);
            yield return _env.Timeout(TimeSpan.FromMinutes(5));
        }
    }
}