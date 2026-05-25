# ThreadArt

An Operating Systems assignment demonstrating multithreading and inter-thread
communication in C# (.NET).

A single **producer** thread generates random numbers and places them on a
shared queue, then signals a waiting **consumer**. Three consumer threads wait
for data, dequeue a number, and use it to paint random colored characters at
random positions in the console — producing animated "thread art."

## Concepts demonstrated

- Shared memory communication via a `Queue<int>` guarded by a lock
- `Monitor.Wait` / `Monitor.Pulse` / `Monitor.PulseAll` for thread signaling
- Multiple consumers competing for work from one producer
- Clean, coordinated shutdown of all threads

## Run

```bash
dotnet run
```

Press any key to stop.
