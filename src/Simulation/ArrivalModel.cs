using System;
using SimSharp;
using src.Domain;

namespace src.Simulation
{
    public sealed class ArrivalModel
    {
        private readonly ArrivalModelConfig _config;


        public ArrivalModel(ArrivalModelConfig config)
        {
            _config = config;
        }

        public TimeSpan SampleNext(SimSharp.Simulation env)
        {
            var  arrivalModelType = _config.ArrivalModelType;
       
            switch (arrivalModelType)
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
            var hourlyRates = _config.HourRates;
            int currentHour = env.Now.Hour;
            double rate = hourlyRates[currentHour];

            // Защита от деления на ноль, если в каком-то часу вдруг 0 пациентов
            if (rate <= 0) return TimeSpan.FromHours(1);

            double meanMinutes = 60.0 / rate;
            var expDist = new ExponentialTime(TimeSpan.FromMinutes(meanMinutes));
            return env.Rand(expDist);
        }

        private TimeSpan SampleNextThinning(SimSharp.Simulation env)
        {  var hourlyRates = _config.HourRates;
            double maxRate = hourlyRates.Max(); 
            if (maxRate <= 0) return TimeSpan.FromDays(1);

            double totalTimeHours = 0;
            double virtualNow = env.Now.Hour + env.Now.Minute / 60.0;

            while (true)
            {
                double interArrivalHours = -Math.Log(env.Rand(new Uniform(0.0, 1.0))) / maxRate;
                totalTimeHours += interArrivalHours; 
                virtualNow += interArrivalHours;

                int hour = (int)(virtualNow % 24);
                double actualRate = hourlyRates[hour];

                if (env.Rand(new Uniform(0.0, 1.0)) <= actualRate / maxRate)
                {
                    return TimeSpan.FromHours(totalTimeHours); 
                }
            }
        }

       
    }

    public record ArrivalModelConfig(
        ArrivalModelType ArrivalModelType,
        int? DailyPatients, // Опцианально ограничить дневной потолок пациентов ( среднее Экспоненциального распределения)
        double[] HourRates);

    public enum ArrivalModelType
    {
        Empirical,
        Thinning
    }
}