using src.Simulation;

namespace src.Domain
{
    public sealed record SimConfig(
        int NumPhysicians, // кол-во врачей
        int NumTriageNurses, // Кол-во медсестер
        int NumTreatmentSpaces, // 
        int NumInpatientBeds, // Кол-во коек
        TimeSpan SimHorizon, // Продолжительность симуляции
        TimeSpan Warmup, // Время на разогрев симуляции
        double[] TriageProbabilities, // Вероятности появления пациента разной степени тяжести (5 шт.) 
        double LwbsPatienceMinutes, // Терпение пациента (в минутах)
        double LowAcuityProbability,
        double AdmissionProbability, // доля госпитализированных
        ArrivalModel ArrivalModel, // Насколько часто пациенты приходят днем/ночью.
        TriageDistributionModel TriageModel // распределения по TriageLevel
    );
}