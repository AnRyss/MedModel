using SimSharp;
using MedicalModel.Domain;

namespace src.Simulation;

public class DepartmentPool
{
    public DepartmentConfig Config { get; }
    public Resource InpatientBeds { get; }

    public DepartmentPool(SimSharp.Simulation simulation, DepartmentConfig config)
    {
        Config = config;
        InpatientBeds = new Resource(simulation, config.NumBeds);
    }
}