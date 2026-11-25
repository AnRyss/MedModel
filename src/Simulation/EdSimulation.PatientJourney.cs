using SimSharp;


public sealed partial class EdSimulation
{
    private IEnumerable<Event> PatientJourney(Patient patient)
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
        using (var physician = _physicianPool.Acquire())
        using (var space = _treatmentSpaces.Request())
        {
            var patience = _env.Timeout(TimeSpan.FromMinutes(_config.LwbsPatienceMinutes));
            yield return physician | patience;

            if (!physician.IsProcessed)
            {
                _metrics.RecordLwbs(patient, _env.Now - arrivedAt);
                yield break;
            }

            yield return space;
            _metrics.RecordDtdt(patient, _env.Now - arrivedAt);
            treated = true;

            yield return _env.Timeout(_triageModel.Treatment(patient.Triage));

            bool admit = _env.Rand(new Uniform(0, 1)) < _config.AdmissionProbability;

            if (admit)
            {
                // Ждем койку (Boarding), все еще занимая место в ED
                var bedReq = _inpatientBeds.Request();
                var boardingStart = _env.Now;
                yield return bedReq;
                _metrics.RecordBoarding(_env.Now - boardingStart);

                // Запускаем ОТДЕЛЬНЫЙ процесс, который будет держать койку 10 дней.
                // Место в ED освободится, как только мы выйдем из блока using (space).
                _env.Process(HoldBed(bedReq));
            }
        } // Врач и место в ED освободились

        if (treated)
            _metrics.RecordLos(patient, _env.Now - arrivedAt);
    }

    private IEnumerable<Event> HoldBed(Request bedReq)
    {
        using (bedReq)
        {
            
            yield return _env.Timeout(_config.BedsOccupiedForDays);
        }
    }
}