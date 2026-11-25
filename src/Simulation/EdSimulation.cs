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
private int counter = 0;
    // Ресурсы
    private PhysicianPool _physicianPool;
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
        _env.Process(MonitorPhysicianLoad()); 
        _env.Run(_config.SimHorizon);
            //Console.WriteLine(counter);
        return _metrics.Build(_config, _seed, _treatmentSpaces);
    }

    private void BuildResources()
    {
        _physicianPool = new PhysicianPool(_env,_config.NumPhysicians);
        

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

    int? configDailyPatients = _config.ArrivalConfig.DailyPatients;
    
    int maxDailyPatients = configDailyPatients.HasValue
        ? _env.Rand(new Poisson(configDailyPatients.Value))
        : int.MaxValue;
   
    while (true)
    {
        
        if (_env.Now.Date > currentDay)
        {
            currentDay = _env.Now.Date;
            patientsToday = 0;
            if (configDailyPatients.HasValue)
                maxDailyPatients = _env.Rand(new Poisson(configDailyPatients.Value));
        }

       
        if (maxDailyPatients > 1
            && patientsToday >= maxDailyPatients)
        {
            var nextDay = currentDay.AddDays(1);
            yield return _env.Timeout(nextDay - _env.Now);
            continue;
        }

        var interArrival = _arrivalModel.SampleNext(_env);
        yield return _env.Timeout(interArrival);

        var patient = new Patient(
            id++,
            _env.Now,
            _triageModel.SampleTriage(_env)
           );

        _metrics.RecordArrival();
        patientsToday++;
        var attentionToPatient = _triageModel.TriageLevelToPhysAttention(_env,patient.Triage);
        _env.Process(PatientJourney(patient, attentionToPatient));
        counter++;
    }
}

    private IEnumerable<Event> StartWarmupHandler()
    {
        var monitors = new IMonitor[]
        {
            
            _triageNurses.Utilization, _triageNurses.WaitingTime, _triageNurses.QueueLength,
            _treatmentSpaces.Utilization, _treatmentSpaces.WaitingTime, _treatmentSpaces.QueueLength,
            _inpatientBeds.Utilization, _inpatientBeds.WaitingTime, _inpatientBeds.QueueLength
        };

        foreach (var m in monitors) m.Active = false;

        yield return _env.Timeout(_config.Warmup);

        foreach (var m in monitors) m.Active = true;
    }
    private IEnumerable<Event> MonitorPhysicianLoad()
    {
        yield return _env.Timeout(_config.Warmup);
        
        while (true)
        {
           
            double totalLoad = _physicianPool.Physicians.Sum(p => p.CurrentLoad);
            double averageLoad = totalLoad / _physicianPool.Physicians.Count;
            _metrics.RecordPhysicianLoad(averageLoad);
            var hourNow= _env.Now.Hour;
            _metrics.RecordPhysicianLoadByHour(hourNow,averageLoad);
            yield return _env.Timeout(TimeSpan.FromMinutes(5));
        }
    }
   
    
}