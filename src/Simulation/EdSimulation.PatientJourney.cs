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
                yield return _env.Timeout(_config.ServiceTimes.Triage(patient.Triage));
            }

            // 2. ОЖИДАНИЕ ВРАЧА + LWBS
            using (var physician = _physicians.Request())
            using (var space = _treatmentSpaces.Request())
            {
                // Приоритетная очередь: тяжёлые вперёд
                var patience = _env.Timeout(TimeSpan.FromMinutes(_config.LwbsPatienceMinutes));
                yield return physician | patience;   // ждём либо врача, либо ухода

                if (!physician.IsProcessed)
                {
                    // LWBS — ушёл, не дождавшись
                    _metrics.RecordLwbs(patient, _env.Now - arrivedAt);
                    yield break;
                }

                // Дождался врача — фиксируем DTDT
                _metrics.RecordDtdt(patient, _env.Now - arrivedAt);
                treated = true;

                yield return space;   // ждём место (может быть быстрее врача)
                yield return _env.Timeout(_config.ServiceTimes.Treatment(patient.Triage));

                // 3. РЕШЕНИЕ
                bool admit = _env.Random.NextDouble() < _config.AdmissionProbability;

                if (admit)
                {
                    // 4. BOARDING — держим место, ждём койку
                    using (var bed = _inpatientBeds.Request())
                    {
                        var boardingStart = _env.Now;
                        yield return bed;
                        _metrics.RecordBoarding(_env.Now - boardingStart);
                        // Выходим из ED только когда получили койку
                        yield return _env.Timeout(TimeSpan.FromMinutes(15)); // передача
                    }
                }
            }

            // 5. ВЫХОД
            if (treated)
                _metrics.RecordLos(patient, _env.Now - arrivedAt);
        }
    }
