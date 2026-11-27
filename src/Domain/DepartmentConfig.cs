namespace MedicalModel.Domain;

public class DepartmentConfig
{
    public string Name { get; set; }
    public int NumBeds { get; set; }
    public double AvgOccupationDays { get; set; }
    public double[] AdmissionProbsByTriage { get; set; }
}