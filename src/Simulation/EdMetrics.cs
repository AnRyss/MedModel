using MathNet.Numerics.Statistics;
using MedicalModel.Domain;
using SimSharp;
using src.Domain;

namespace MedicalModel.Simulation
{
    public sealed class EdMetrics
    {
        private readonly List<double> _doorToDoorTime = new();
        private readonly List<double> _lengthOfStay = new();
        private readonly List<double> _boardingTime = new();
        private int _lwbsCount;
        private int _totalPatients;

        public void RecordDtdt(Patient p, TimeSpan dt) => _doorToDoorTime.Add(dt.TotalMinutes);
        public void RecordLos(Patient p, TimeSpan los) => _lengthOfStay.Add(los.TotalMinutes);
        public void RecordBoarding(TimeSpan b) => _boardingTime.Add(b.TotalMinutes);
        
        public void RecordArrival() => _totalPatients++; 
        
        public void RecordLwbs(Patient p, TimeSpan waited) => _lwbsCount++; 

        public SimResult Build(SimConfig config, int seed, Resource physicians, Resource spaces) => new(
            ScenarioName: config.Name,
            Seed: seed,
            DtdtP50: _doorToDoorTime.Percentile(50),
            DtdtP95: _doorToDoorTime.Percentile(95),
            LosP50: _lengthOfStay.Percentile(50),
            LosP95: _lengthOfStay.Percentile(95),
            LwbsRate: _totalPatients > 0 ? (double)_lwbsCount / _totalPatients : 0,
            PhysicianUtilization: physicians.Utilization.Mean,
            SpaceUtilization: spaces.Utilization.Mean,
            BoardingMeanMinutes: _boardingTime.Count > 0 ? _boardingTime.Average() : 0,
            TotalPatients: _totalPatients,
            LwbsCount: _lwbsCount
        );
    }
}