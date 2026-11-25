using MedicalModel.Domain;
using SimSharp;

public sealed class PhysicianPool
{
    public PhysicianPool(Simulation simulation, int physiciansCount)
    {
        _env = simulation;
        Physicians = new List<Physician>();
        for (int i = 0; i < physiciansCount; i++)
        {
            Physicians.Add(new Physician { Id = i + 1, CurrentLoad = 0 });
        }
    }

    private readonly Simulation _env;
    public readonly List<Physician> Physicians;
    private LinkedList<(double attention, PhysicianRequest request)> _queue = new();
   

    public PhysicianRequest Acquire(double attention,TriageLevel triage)
    {
        var req = new PhysicianRequest(this, attention, _env,triage);
        _queue.AddLast((attention, req));
        TryDispatch();
        return req;
    }

    internal void Release(Physician ph, double attention)
    {
        ph.CurrentLoad -= attention;
        if (ph.CurrentLoad < 0) ph.CurrentLoad = 0;


        TryDispatch();
    }

    internal void RemoveFromQueue(PhysicianRequest req)
    {
        var node = _queue.First;
        while (node != null)
        {
            if (node.Value.request == req)
            {
                _queue.Remove(node);
                break;
            }

            node = node.Next;
        }
    }

    internal void TryDispatch()
    {
        if (_queue.Count == 0) return;

        foreach (var ph in Physicians)
        {
            if (_queue.Count == 0) break;

            // Ищем в очереди самого тяжелого пациента, которого может взять этот врач
            LinkedListNode<(double attention, PhysicianRequest request)>? bestNode = null;
            int highestPriority = int.MaxValue; // Меньше = тяжелее (уровень 1 самый тяжелый)

            var node = _queue.First;
            while (node != null)
            {
                var (attention, req) = node.Value;


                if (ph.CanAccept(attention))
                {
                    int priority = (int)req.TriageLevel;
                    if (priority < highestPriority)
                    {
                        highestPriority = priority;
                        bestNode = node;
                    }
                }

                node = node.Next;
            }


            if (bestNode != null)
            {
                var (attention, req) = bestNode.Value;
                _queue.Remove(bestNode);
                ph.CurrentLoad += attention;
                req.Succeed(ph);
                
            }
        }
    }
}

public sealed class PhysicianRequest : Event, IDisposable
{
    private readonly PhysicianPool _pool;
    private readonly double _attention;
    public TriageLevel TriageLevel { get; }
    public Physician? Physician { get; private set; }

    public PhysicianRequest(PhysicianPool pool, double attention, Simulation env, TriageLevel triageLevel) : base(env)
    { 
        this.TriageLevel = triageLevel;
        _pool = pool;
        _attention = attention;
        AddCallback(evt =>
        {
            if (evt.IsOk)
            {
                Physician = (Physician)evt.Value;
            }
        });
    }

    public void Dispose()
    {
        if (Physician != null)
        {
            _pool.Release(Physician, _attention);
            Physician = null;
        }
        else
        {
            _pool.RemoveFromQueue(this);
        }
    }
}