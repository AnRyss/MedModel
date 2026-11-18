using MedicalModel.Domain;
using src.Domain;

namespace src.Simulation
{
    // EdSim.Simulation/EdMetrics.cs
    public sealed class EdMetrics
    {
        private readonly List<double> _dtdt = new();
        private readonly List<double> _los = new();
        private readonly List<double> _boarding = new();
        private int _lwbsCount;
        private int _totalPatients;

        public void RecordDtdt(Patient p, TimeSpan dt) => _dtdt.Add(dt.TotalMinutes);
        public void RecordLos(Patient p, TimeSpan los) => _los.Add(los.TotalMinutes);
        public void RecordBoarding(TimeSpan b) => _boarding.Add(b.TotalMinutes);
        public void RecordLwbs(Patient p, TimeSpan waited) { _lwbsCount++; _totalPatients++; }

        public SimResult Build(SimConfig config, int seed) => new(
            ScenarioName: "",   
            Seed: seed,
            DtdtP50: Percentile(_dtdt, 50),
            DtdtP95: Percentile(_dtdt, 95),
            LosP50: Percentile(_los, 50),
            LosP95: Percentile(_los, 95),
            PhysicianUtilization: 5,
            SpaceUtilization: 5,
            BoardingMeanMinutes: 5,
            TotalPatients: 5,
            LwbsRate: _totalPatients > 0 ? (double)_lwbsCount / _totalPatients : 0,
            // ... остальное
            );
    }
}