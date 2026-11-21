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
        TimeSpan SimHorizon,            // Продолжительность симуляции в днях
        [property: JsonConverter(typeof(NumberToTimeSpanConverter))]
        TimeSpan Warmup,                // Время на разогрев в днях
        [property: JsonConverter(typeof(NumberToTimeSpanConverter))]
        TimeSpan BedsOccupiedForDays, // насколько долго пациент занимает койку в стационаре
        double[] TriageProbabilities,   // Вероятности уровней тяжести 1-5
        double LwbsPatienceMinutes,     // Терпение пациента до ухода (в минутах)
        double LowAcuityProbability,    // TODO: что-то с ней сделать, пока ничего не делает
        double AdmissionProbability,    // Шанс, что будет госпитализирован
        ArrivalModelConfig ArrivalConfig, // Настройки потока пациентов
        double[] TriageTimesMeanMinutes,  // Среднее время триажа по уровням
        double[] TreatmentTimesMeanMinutes // Среднее время лечения по уровням
    );
}