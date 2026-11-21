using System;
using SimSharp;
using src.Domain;

namespace src.Simulation
{
    public class TriageDistributionModel
    {
        private readonly IDistribution<TimeSpan>[] _triageTimes;
        private readonly IDistribution<TimeSpan>[] _treatmentTimes;
        private readonly double[] _triageProbabilities;

        public TriageDistributionModel(SimConfig config)
        {
            _triageProbabilities = config.TriageProbabilities;
            
           
            _triageTimes = new IDistribution<TimeSpan>[_triageProbabilities.Length];
            _treatmentTimes = new IDistribution<TimeSpan>[_triageProbabilities.Length];

            for (int i = 0; i < _triageProbabilities.Length; i++)
            {
                // Простое предположение: стандартное отклонение = 20%-40% от среднего
                double triageMean = config.TriageTimesMeanMinutes[i];
                double treatmentMean = config.TreatmentTimesMeanMinutes[i];

                _triageTimes[i] = new LogNormalTime(TimeSpan.FromMinutes(triageMean), TimeSpan.FromMinutes(triageMean * 0.2));
                _treatmentTimes[i] = new LogNormalTime(TimeSpan.FromMinutes(treatmentMean), TimeSpan.FromMinutes(treatmentMean * 0.4));
            }
        }

        public TriageLevel SampleTriage(SimSharp.Simulation sim)
        {
            double u = sim.Rand(new Uniform(0.0, 1.0));
            double cumulative = 0;

            for (int i = 0; i < _triageProbabilities.Length; i++)
            {
                cumulative += _triageProbabilities[i];
                if (u < cumulative) return (TriageLevel)(i + 1);
            }
            return TriageLevel.NonUrgent;
        }

        public IDistribution<TimeSpan> Triage(TriageLevel level) => _triageTimes[(int)level - 1];
        public IDistribution<TimeSpan> Treatment(TriageLevel level) => _treatmentTimes[(int)level - 1];
    }
}