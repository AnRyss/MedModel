using MedicalModel.Domain;
using SimSharp;

public sealed class PhysicianPool
{
    public PhysicianPool(Simulation simulation, int physiciansCount)
    {
        this._env =simulation;
        this._physicians = new();
        for (int i = 0; i < physiciansCount; i++)
        {
            _physicians.Add(new Physician());
        }
    }
    private readonly Simulation _env;
    private readonly List<Physician> _physicians;
    private readonly Queue<(Patient p, double attention, Event e)> _queue = new();

    public IEnumerable<Event> Acquire(Patient p, double attention)
    {
        var e = new SimSharp.Event(_env); 
        _queue.Enqueue((p, attention, e));
        TryDispatch();
        yield return e;           
    }

    public void Release(Physician ph, double attention)
    {
        ph.CurrentLoad -= attention;
        TryDispatch();
    }

    private void TryDispatch()
    {
        
        while (_queue.Count > 0)
        {
            var (p, attention, e) = _queue.Peek();
            var ph = _physicians.FirstOrDefault(x => x.CanAccept(attention));
            if (ph == null) return;
            _queue.Dequeue();
            ph.CurrentLoad += attention;
            e.Succeed(ph);        
        }
    }
   
}