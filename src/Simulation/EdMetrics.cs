using MathNet.Numerics.Statistics;
using MedicalModel.Domain;
using SimSharp;
using src.Domain;
using src.Simulation;

namespace MedicalModel.Simulation
{
    public sealed class EdMetrics
    {
        private readonly List<double> _doorToDoorTime = new();
        private readonly List<double> _lengthOfStay = new();
        private readonly List<double> _departmentsUtilizations = new();
        private readonly List<double> _boardingTime = new();
        private readonly List<double> _physicianLoads = new();
        private readonly Dictionary<int, List<double>> _physicianLoadsByHoursMap = new();
        private int _lwbsCount;
        private int _totalPatients;

        public EdMetrics()
        {
            for (int i = 0; i <= 23; i++)
            {
                _physicianLoadsByHoursMap.Add(i, new List<double>());
            }

        }
        public void RecordDtdt(Patient p, TimeSpan dt) => _doorToDoorTime.Add(dt.TotalMinutes);
        public void RecordLos(Patient p, TimeSpan los) => _lengthOfStay.Add(los.TotalMinutes);
        public void RecordBoarding(TimeSpan b) => _boardingTime.Add(b.TotalMinutes);

        public void RecordArrival() => _totalPatients++;
        public void RecordInpatientBedsUtilization(double utilization)
                   => _departmentsUtilizations.Add(utilization);

        public void RecordLwbs(Patient p, TimeSpan waited) => _lwbsCount++;
        public void RecordPhysicianLoad(double load) => _physicianLoads.Add(load);
        public void RecordPhysicianLoadByHour(int hour, double load) => _physicianLoadsByHoursMap.GetValueOrDefault(hour)!.Add(load);
        public SimResult Build(SimConfig config, int seed, Resource spaces,
        IReadOnlyDictionary<string, DepartmentPool> departmentPools) => new(
            ScenarioName: config.Name,
            Seed: seed,
            DtdtP50: _doorToDoorTime.Percentile(50),
            DtdtP95: _doorToDoorTime.Percentile(95),
            LosP50: _lengthOfStay.Percentile(50),
            LosP95: _lengthOfStay.Percentile(95),
            LwbsRate: _totalPatients > 0 ? (double)_lwbsCount / _totalPatients : 0,
            PhysicianUtilization: _physicianLoads.Count > 0 ? _physicianLoads.Average() : 0,
            PhysicianUtilizationByHours: _physicianLoadsByHoursMap,
            SpaceUtilization: spaces.Utilization.Mean,
            BoardingMeanMinutes: _boardingTime.Count > 0 ? _boardingTime.Average() : 0,
            DepartmentUtilization: departmentPools.ToDictionary(
            kvp => kvp.Key,
            kvp => new DepartmentUtilizationStats(
                DepartmentName: kvp.Key,
                Mean: kvp.Value.Utilization.Mean,
                P50:  kvp.Value.Utilization.GetPercentile(0.50),
                P95:  kvp.Value.Utilization.GetPercentile(0.95),
                Max:  kvp.Value.Utilization.Max)),
            TotalPatients: _totalPatients,
            LwbsCount: _lwbsCount
        );
    }
}