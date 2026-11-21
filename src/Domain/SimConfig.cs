using System.Text.Json.Serialization;
using MedicalModel;
using src.Simulation;

namespace src.Domain
{
    public sealed record SimConfig(
        string Name,                    // Имя сценария
        int NumPhysicians,              // Кол-во врачей
        int NumTriageNurses,            // Кол-во медсестер триажа
        int NumTreatmentSpaces,         // Кол-во мест для лечения
        int NumInpatientBeds,           // Кол-во коек в стационаре
        [property: JsonConverter(typeof(NumberToTimeSpanConverter))]
        TimeSpan SimHorizon,            // Продолжительность симуляции
        [property: JsonConverter(typeof(NumberToTimeSpanConverter))]
        TimeSpan Warmup,                // Время на разогрев
        [property: JsonConverter(typeof(NumberToTimeSpanConverter))]
        TimeSpan BedsOccupiedForDays, // насколько долго пациент занимает койку в стационаре
        double[] TriageProbabilities,   // Вероятности уровней 1-5
        double LwbsPatienceMinutes,     // Терпение пациента (в минутах)
        double LowAcuityProbability,    // Вероятность, что пациент "легкий" и не займет койку
        double AdmissionProbability,    // Шанс, что будет госпитализирован
        ArrivalModelConfig ArrivalConfig, // Настройки потока пациентов
        double[] TriageTimesMeanMinutes,  // Среднее время триажа по уровням
        double[] TreatmentTimesMeanMinutes // Среднее время лечения по уровням
    );
}