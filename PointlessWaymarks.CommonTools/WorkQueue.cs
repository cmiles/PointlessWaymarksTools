using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;

namespace PointlessWaymarks.CommonTools;

public class WorkQueue<T>
{
    //This is basically the BlockingCollection version from https://michaelscodingspot.com/c-job-queues/
    private readonly BlockingCollection<(DateTime created, T job)> _jobs = new();

    private readonly List<(DateTime created, T job)> _pausedQueue = [];

    public WorkQueue(bool suspended = false)
    {
        Suspended = suspended;
        var thread = new Thread(OnStart) { IsBackground = true };
        thread.Start();
    }

    public Func<T, Task>? Processor { get; set; }

    public bool Suspended { get; private set; }

    public void Enqueue(T job)
    {
        if (Suspended) _pausedQueue.Add((DateTime.Now, job));
        else _jobs.Add((DateTime.Now, job));
    }

    private void OnStart()
    {
        foreach (var job in _jobs.GetConsumingEnumerable(CancellationToken.None))
            try
            {
                if (Suspended)
                    _pausedQueue.Add(job);
                else
                    Processor?.Invoke(job.job).Wait();
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                Log.Error(e, "WorkQueue Error");
            }
    }

    public void Suspend(bool suspend)
    {
        Suspended = suspend;

        Debug.WriteLine("WorkQueue Suspend To: " + suspend);

        if (!Suspended && _pausedQueue.Count != 0)
        {
            _pausedQueue.OrderBy(x => x.created).ToList().ForEach(x => _jobs.Add(x));
            _pausedQueue.Clear();
        }
    }
}