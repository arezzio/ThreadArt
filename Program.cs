using System;
using System.Collections.Generic;
using System.Threading;

namespace ThreadArt;

class Program
{
    // ---------- Shared memory (the chosen thread-communication technique) ----------
    // Producer enqueues, consumers dequeue. Access is guarded by lockObj.
    // Monitor.Pulse / Monitor.Wait is how the producer "alerts" a consumer.
    private static readonly Queue<int> dataQueue = new();
    private static readonly object lockObj = new();
    private static volatile bool running = true;

    static void Main()
    {
        Console.CursorVisible = false;
        Console.Clear();
        Console.Title = "Thread Art - press any key to stop";

        // One producer
        Thread producer = new(ProducerThread) { Name = "Producer", IsBackground = true };

        // Multiple consumers (assignment says "consumer threads" - plural)
        const int consumerCount = 3;
        Thread[] consumers = new Thread[consumerCount];
        for (int i = 0; i < consumerCount; i++)
        {
            int id = i;
            consumers[i] = new Thread(() => ConsumerThread(id))
            {
                Name = $"Consumer-{id}",
                IsBackground = true
            };
        }

        producer.Start();
        foreach (var c in consumers) c.Start();

        // Run until any key is pressed
        Console.ReadKey(true);

        // Signal shutdown and wake every waiting consumer so they exit cleanly
        running = false;
        lock (lockObj) Monitor.PulseAll(lockObj);

        producer.Join();
        foreach (var c in consumers) c.Join();

        Console.ResetColor();
        Console.Clear();
        Console.CursorVisible = true;
        Console.WriteLine("Thread art finished.");
    }

    // ---------------- Producer ----------------
    static void ProducerThread()
    {
        while (running)
        {
            // 1. Generate a random number
            int number = Random.Shared.Next(20, 200);

            // 2. Push number in queue   +   4. Alert a consumer
            lock (lockObj)
            {
                dataQueue.Enqueue(number);
                Monitor.Pulse(lockObj); // wake exactly one waiting consumer
            }

            // 3. Sleep a random interval
            Thread.Sleep(Random.Shared.Next(150, 900));
        }
    }

    // ---------------- Consumer ----------------
    static void ConsumerThread(int id)
    {
        const string charPool =
            "!@#$%^&*()_+-=[]{}|;:,.<>?/~`ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        while (true)
        {
            int number;

            // ---- Wait for data ----
            lock (lockObj)
            {
                while (dataQueue.Count == 0 && running)
                    Monitor.Wait(lockObj);

                // Shutdown path: nothing left and we've been told to stop
                if (!running && dataQueue.Count == 0) return;

                // ---- Retrieve data ----
                number = dataQueue.Dequeue();
            }

            // ---- Set font color to that number (mod 16 to fit valid ConsoleColors) ----
            ConsoleColor color = (ConsoleColor)(number % 16);

            int width = Console.WindowWidth;
            int height = Console.WindowHeight;

            // ---- Loop 0..number, painting random chars at random X/Y positions ----
            for (int i = 0; i < number && running; i++)
            {
                int x = Random.Shared.Next(0, Math.Max(1, width));
                int y = Random.Shared.Next(0, Math.Max(1, height));
                char ch = charPool[Random.Shared.Next(charPool.Length)];

                // Console isn't thread-safe; serialize writes so color+position+char
                // for one paint don't get interleaved with another consumer's paint.
                lock (Console.Out)
                {
                    try
                    {
                        Console.ForegroundColor = color;
                        Console.SetCursorPosition(x, y);
                        Console.Write(ch);
                    }
                    catch
                    {
                        // Window resized mid-paint? Just skip this one.
                    }
                }

                Thread.Sleep(8); // small pause so the art "draws" rather than flashes
            }
        }
    }
}