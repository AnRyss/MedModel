using src.Simulation;

namespace src.Domain
{
    public sealed record SimConfig(
        int NumPhysicians,
        int NumTriageNurses,
        int NumTreatmentSpaces,
        int NumInpatientBeds,
        TimeSpan SimHorizon,
        TimeSpan Warmup,
        double LwbsPatienceMinutes, // Терпение пациента (в минутах)
        double AdmissionProbability, // доля госпитализированных
        ArrivalProfile Arrivals, // Насколько часто пациенты приходят днем/ночью.
        ServiceTimeProfile ServiceTimes // распределения по TriageLevel
    );
}