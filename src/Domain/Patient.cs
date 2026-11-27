// EdSim.Domain/Patient.cs
public enum TriageLevel { Resuscitation = 1, Emergent = 2, Urgent = 3, LessUrgent = 4, NonUrgent = 5 }

public sealed record Patient(
    int Id,
    DateTime ArrivalTime,
    TriageLevel Triage);
        


