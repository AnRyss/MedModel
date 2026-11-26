using System;
using MedicalModel.Domain;
using SimSharp;
using src.Domain;

namespace src.Simulation
{
    public class TriageDistributionModel
    {private readonly double[][] _departmentProbsByLevel; // [уровень][отделение]
        private readonly IDistribution<TimeSpan>[] _triageTimes;
        private readonly IDistribution<TimeSpan>[] _treatmentTimes;
        private readonly double[] _triageProbabilities;
        private  readonly SimConfig _config;
        public TriageDistributionModel(SimConfig config)
        {
            _triageProbabilities = NormalizeProbs(config.TriageProbabilities);
            _config = config;
           
            _triageTimes = new IDistribution<TimeSpan>[_triageProbabilities.Length];
            _treatmentTimes = new IDistribution<TimeSpan>[_triageProbabilities.Length];

            for (int i = 0; i < _triageProbabilities.Length; i++)
            {
                // Простое предположение: стандартное отклонение = 20%-70% от среднего
                double triageMean = config.TriageTimesMeanMinutes[i];
                double treatmentMean = config.TreatmentTimesMeanMinutes[i];
                if (i == 0)
                {
                    _triageTimes[i] = new LogNormalTime(TimeSpan.FromMinutes(triageMean),
                        TimeSpan.FromMinutes(triageMean * 0.2));
                    _treatmentTimes[i] = new LogNormalTime(TimeSpan.FromMinutes(treatmentMean),
                        TimeSpan.FromMinutes(treatmentMean * 0.7));
                    continue;
                }

                _triageTimes[i] = new LogNormalTime(TimeSpan.FromMinutes(triageMean),
                    TimeSpan.FromMinutes(triageMean * 0.2));
                _treatmentTimes[i] = new LogNormalTime(TimeSpan.FromMinutes(treatmentMean),
                    TimeSpan.FromMinutes(treatmentMean * 0.3));
            }
            _departmentProbsByLevel = new double[5][]; // 5 уровней триажа
            for (int lvl = 0; lvl < 5; lvl++)
            {
                _departmentProbsByLevel[lvl] = new double[config.Departments.Count];
                for (int dep = 0; dep < config.Departments.Count; dep++)
                {
                    _departmentProbsByLevel[lvl][dep] = config.Departments[dep].AdmissionProbsByTriage[lvl];
                }
            }
        }

        public double TriageLevelToPhysAttention(SimSharp.Simulation simulation, TriageLevel level)
        {
            double baseAttention = level switch
            {
                TriageLevel.Resuscitation => 1.0,
                TriageLevel.Emergent => 0.8,
                TriageLevel.Urgent => 0.6,
                TriageLevel.LessUrgent => 0.4,
                TriageLevel.NonUrgent => 0.2,
                _ => 0.2
            };
            double deviation = 0.1;
            var dist = new Normal(baseAttention, deviation);
            double sample = simulation.Rand(dist);
            return Math.Clamp(sample, 0.05, 1.0);
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
        public DepartmentConfig SampleDepartment(SimSharp.Simulation sim, TriageLevel level)
        {
       
            int levelIndex = (int)level - 1;
    
      
            var probs = _departmentProbsByLevel[levelIndex]; 
    
        
            double sum = probs.Sum();
            if (sum <= 0)
                throw new InvalidDataException("Настройки вероятностей для департамента невалидны");

       
            double u = sim.Rand(new Uniform(0.0, 1.0));
            double cumulative = 0;

      
            for (int i = 0; i < probs.Length; i++)
            {
                cumulative += probs[i] / sum; 
                if (u < cumulative)
                {
                    return _config.Departments[i]; 
                }
            }

        
            return _config.Departments.Last();
        }
        public bool AdmissionSampleFromTriage(SimSharp.Simulation sim, TriageLevel level){
            double baseProb = _config.AdmissionProbabilitiesByTriage[(int)level - 1];
            double noise = sim.Rand(new Uniform(-0.05, 0.05));
            double finalProb = Math.Clamp(baseProb + noise, 0.0, 1.0);
            double u = sim.Rand(new Uniform(0.0, 1.0));
            return u < finalProb;
        }
        private double[] NormalizeProbs(double[] probs)
        {
            var normilizedProbs = new double[probs.Length];
            var summ = probs.Sum();
            for (var i = 0; i < probs.Length; i++)
            {
                normilizedProbs[i] = probs[i] / summ;
            }

            return normilizedProbs;
        }

        public IDistribution<TimeSpan> Triage(TriageLevel level) => _triageTimes[(int)level - 1];
        public IDistribution<TimeSpan> Treatment(TriageLevel level) => _treatmentTimes[(int)level - 1];
    }
}