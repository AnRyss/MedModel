using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MedicalModel.Domain;
using src.Domain;
using src.Simulation;

namespace MedicalModel
{
    class Program
    {
        static void Main(string[] args)
        {
            var jsonString = File.ReadAllText("config.json");
            var jsonOpt = new JsonSerializerOptions();
            SimConfig? jsonConfig;
            jsonOpt.Converters.Add(new NumberToTimeSpanConverter());
            jsonOpt.Converters.Add(new JsonStringEnumConverter());

            jsonConfig = JsonSerializer.Deserialize<SimConfig>(jsonString, jsonOpt)
                         ?? throw new JsonException("Не получилось получить настройки из конфигурации");
            var expRunner = new ExperimentRunner();
            var SimResultsList = expRunner.RunScenario(jsonConfig, 100);
            ExperimentRunner.PrintResultsBatch(SimResultsList,jsonConfig);

            
            // Сценарий 2: Высокая нагрузка + больше врачей
            var baseHourlyRates = jsonConfig.ArrivalConfig.HourRates;
            var highLoadRates = new double[24];
            for (int i = 0; i < 24; i++) highLoadRates[i] = baseHourlyRates[i] * 1.5;

            var highLoadConfig = jsonConfig with
            {
                Name = "High Load",
                ArrivalConfig = new ArrivalModelConfig(ArrivalModelType.Thinning,jsonConfig.ArrivalConfig.DailyPatients, highLoadRates)
            };


            var simHighLoad = new EdSimulation(highLoadConfig, seed: 42);
            //  PrintResult(simHighLoad.Run(),jsonConfig);

            Console.ReadKey();
        }


        static void PrintResult(SimResult res, SimConfig config)
        {
            Console.WriteLine($"Сценарий: {res.ScenarioName}");
            Console.WriteLine($"Дней всего: {config.SimHorizon.TotalDays}");
            Console.WriteLine($"Пациентов всего: {res.TotalPatients}");
            Console.WriteLine($"Ушли (LWBS): {res.LwbsCount} ({res.LwbsRate:P1})");
            Console.WriteLine($"Время до врача (P50): {res.DtdtP50:F1} мин");
            Console.WriteLine($"Время до врача (P95): {res.DtdtP95:F1} мин");
            Console.WriteLine($"Время в отделении (P50): {res.LosP50:F1} мин");
            Console.WriteLine($"Время в отделении (P95): {res.LosP95:F1} мин");
            Console.WriteLine($"Загрузка врачей: {res.PhysicianUtilization:P1}");
            Console.WriteLine($"Загрузка мест: {res.SpaceUtilization:P1}");
            Console.WriteLine($"Среднее ожидание койки: {res.BoardingMeanMinutes:F1} мин");
        }
    }
}