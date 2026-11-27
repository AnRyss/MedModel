using System;

namespace MedicalModel.Domain;

public sealed record SimResult(
    string ScenarioName,
    int Seed,
    double DtdtP50,
    double DtdtP95,
    double LosP50,
    double LosP95,
    double LwbsRate,
    double PhysicianUtilization,
    Dictionary<int, List<double>> PhysicianUtilizationByHours,
    double SpaceUtilization,
    IReadOnlyDictionary<string, DepartmentUtilizationStats> DepartmentUtilization,
    double BoardingMeanMinutes,
    int TotalPatients,
    int LwbsCount);