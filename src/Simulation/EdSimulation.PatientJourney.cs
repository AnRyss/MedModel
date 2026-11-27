using MedicalModel.Domain;
using SimSharp;


public sealed partial class EdSimulation
{
    private IEnumerable<Event> PatientJourney(Patient patient, double attentionToPatient)
    {

        var arrivedAt = _env.Now;
        bool treated = false;

        // 1. ТРИАЖ
        using (var triage = _triageNurses.Request())
        {
            yield return triage;
            yield return _env.Timeout(_triageModel.Triage(patient.Triage));
        }

        // 2. ОЖИДАНИЕ ВРАЧА И МЕСТА
        var physicianRequest = _physicianPool.Acquire(attentionToPatient, patient.Triage);
       
            var patience = _env.Timeout(TimeSpan.FromMinutes(_config.LwbsPatienceMinutes));
            yield return physicianRequest | patience;

            if (!physicianRequest.IsProcessed)
            {
                _metrics.RecordLwbs(patient, _env.Now - arrivedAt);
                physicianRequest.Dispose();
                yield break;
            }

            using (var space = _treatmentSpaces.Request())
            {
                yield return space;
                _metrics.RecordDtdt(patient, _env.Now - arrivedAt);
                treated = true;

                yield return _env.Timeout(_triageModel.Treatment(patient.Triage));
                physicianRequest.Dispose(); // doctor release


                bool admit = _triageModel.AdmissionSampleFromTriage(_env, patient.Triage);
             
                // 

                if (admit)
                {
                    var depConfig = _triageModel.SampleDepartment(_env,patient.Triage);
                    _departmentPools.TryGetValue(depConfig.Name, out var depPool);
                    if (depPool is null)
                        throw new KeyNotFoundException($" Ошибка при получении отделения{depConfig.Name}");
                    // Ждем койку (Boarding),  уже не занимая место в приемке
                    var bedReq = depPool.InpatientBeds.Request();
                    var boardingStart = _env.Now;
                    var maxBoardingTime = _env.Timeout(TimeSpan.FromHours(24));

                    yield return bedReq | maxBoardingTime;

                    if (!maxBoardingTime.IsTriggered)
                    {
                        _metrics.RecordBoarding(_env.Now - boardingStart);
                        _env.Process(HoldBed(bedReq, depPool.Config.AvgOccupationDays)); 
                    }
                    else
                    {
                        _metrics.RecordBoarding(TimeSpan.FromHours(24));
                    }
                }
            }

            if (treated)
                _metrics.RecordLos(patient, _env.Now - arrivedAt);
        
    }

   
    private IEnumerable<Event> HoldBed(Request bedReq, double days)
    {
        using (bedReq)
        {
          
            yield return _env.Timeout(TimeSpan.FromDays(days)); 
        }
    }
}