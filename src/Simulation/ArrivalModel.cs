using System;
using SimSharp;
using src.Domain;

namespace src.Simulation
{
    public sealed class ArrivalModel
    {
        private readonly ArrivalModelType _arrivalModelType;
        private readonly double[] _hourlyRates;

        public ArrivalModel(ArrivalModelConfig config)
        {
            _arrivalModelType = config.ArrivalModelType;
            _hourlyRates = config.HourRates;
        }

        public TimeSpan SampleNext(SimSharp.Simulation env)
        {
            switch (_arrivalModelType)
            {
                case ArrivalModelType.Empirical:
                    return SampleNextEmpirical(env);

                case ArrivalModelType.Thinning:
                    return SampleNextThinning(env);
                default: return SampleNextThinning(env);
            }
        }

        private TimeSpan SampleNextEmpirical(SimSharp.Simulation env)
        {
            int currentHour = env.Now.Hour;
            double rate = _hourlyRates[currentHour];

            // Защита от деления на ноль, если в каком-то часу вдруг 0 пациентов
            if (rate <= 0) return TimeSpan.FromDays(1);

            double meanMinutes = 60.0 / rate;
            var expDist = new ExponentialTime(TimeSpan.FromMinutes(meanMinutes));
            return env.Rand(expDist);
        }

        private TimeSpan SampleNextThinning(SimSharp.Simulation env)
        {
            double maxRate = _hourlyRates.Max(); 
            if (maxRate <= 0) return TimeSpan.FromDays(1);

            double totalTimeHours = 0;
            double virtualNow = env.Now.Hour + env.Now.Minute / 60.0;

            while (true)
            {
                double interArrivalHours = -Math.Log(env.Rand(new Uniform(0.0, 1.0))) / maxRate;
                totalTimeHours += interArrivalHours; 
                virtualNow += interArrivalHours;

                int hour = (int)(virtualNow % 24);
                double actualRate = _hourlyRates[hour];

                if (env.Rand(new Uniform(0.0, 1.0)) <= actualRate / maxRate)
                {
                    return TimeSpan.FromHours(totalTimeHours); 
                }
            }
        }
    }

    public record ArrivalModelConfig(
        ArrivalModelType ArrivalModelType,
        double[] HourRates);

    public enum ArrivalModelType
    {
        Empirical,
        Thinning
    }
}