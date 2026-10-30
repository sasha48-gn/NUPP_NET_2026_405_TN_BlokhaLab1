public static class SyncDemo
{
    public static async Task RunAllAsync()
    {
        Console.WriteLine("=== Примітиви синхронізації ===");
        LockDemo();
        InterlockedDemo();
        await SemaphoreDemo();
        AutoResetEventDemo();
        ManualResetEventDemo();
        CountdownDemo();
        MutexDemo();
    }

    // lock: без нього лічильник втратить інкременти
    static void LockDemo()
    {
        int safe = 0, unsafeCnt = 0;
        var gate = new object();
        Parallel.For(0, 100_000, _ =>
        {
            lock (gate) safe++;
            unsafeCnt++;
        });
        Console.WriteLine($"[lock]        з lock: {safe}, без lock: {unsafeCnt}");
    }

    static void InterlockedDemo()
    {
        long c = 0;
        Parallel.For(0, 100_000, _ => Interlocked.Increment(ref c));
        Console.WriteLine($"[Interlocked] {c}");
    }

    // SemaphoreSlim: не більше 3 одночасних "завантажень"
    static async Task SemaphoreDemo()
    {
        var sem = new SemaphoreSlim(3);
        int current = 0, max = 0;
        await Task.WhenAll(Enumerable.Range(1, 10).Select(async _ =>
        {
            await sem.WaitAsync();
            try
            {
                int now = Interlocked.Increment(ref current);
                int snap;
                while (now > (snap = Volatile.Read(ref max)) &&
                       Interlocked.CompareExchange(ref max, now, snap) != snap) { }
                await Task.Delay(50);
                Interlocked.Decrement(ref current);
            }
            finally { sem.Release(); }
        }));
        Console.WriteLine($"[Semaphore]   макс. одночасно: {max} (ліміт 3)");
    }

    // AutoResetEvent: кожен Set() пропускає рівно один потік
    static void AutoResetEventDemo()
    {
        var are = new AutoResetEvent(false);
        int passed = 0;
        var threads = Enumerable.Range(0, 3).Select(_ => new Thread(() =>
        {
            are.WaitOne();
            Interlocked.Increment(ref passed);
        })).ToList();
        threads.ForEach(t => t.Start());

        for (int i = 1; i <= 3; i++)
        {
            are.Set();
            Thread.Sleep(50);
            Console.WriteLine($"[AutoReset]   після Set #{i} пройшло потоків: {passed}");
        }
        threads.ForEach(t => t.Join());
    }

    // ManualResetEventSlim: один Set() відпускає всіх
    static void ManualResetEventDemo()
    {
        var mre = new ManualResetEventSlim(false);
        int passed = 0;
        var tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(() =>
        {
            mre.Wait();
            Interlocked.Increment(ref passed);
        })).ToArray();
        Thread.Sleep(50);
        mre.Set();
        Task.WaitAll(tasks);
        Console.WriteLine($"[ManualReset] один Set() відпустив {passed} потоків");
    }

    // CountdownEvent: чекаємо завершення N воркерів
    static void CountdownDemo()
    {
        using var cd = new CountdownEvent(4);
        for (int i = 0; i < 4; i++)
            Task.Run(() => { Thread.Sleep(30); cd.Signal(); });
        cd.Wait();
        Console.WriteLine("[Countdown]   всі 4 воркери завершились");
    }

    // Mutex: іменований, працює і між процесами
    static void MutexDemo()
    {
        using var m = new Mutex(false, "Lab2_Bloha_Mutex");
        m.WaitOne();
        try { Console.WriteLine("[Mutex]       критична секція отримана"); }
        finally { m.ReleaseMutex(); }
    }
}