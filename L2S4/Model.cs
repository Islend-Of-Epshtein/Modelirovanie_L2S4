using System;
using System.Collections.Generic;
using System.Linq;

namespace WholesaleStoreSimulation
{
    // Делегат для записи логов
    public delegate void LogEventHandler(string time, string eventType, string description, string extra);

    public class Customer
    {
        public int Id { get; set; }
        public double ArrivalTime { get; set; }
        public int ProductsCount { get; set; }
        public double ServiceStartTime { get; set; }
        public double ServiceEndTime { get; set; }
        public double WaitTime => ServiceStartTime - ArrivalTime;
        public double TotalTime => ServiceEndTime - ArrivalTime;

        public Customer(int id, double arrivalTime)
        {
            Id = id;
            ArrivalTime = arrivalTime;
            ProductsCount = GenerateProductsCount();
        }

        private int GenerateProductsCount()
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            return rnd.Next(3, 8);
        }
    }

    public class Clerk
    {
        public int Id { get; set; }
        public bool IsWorking { get; set; }     // не в перерыве
        public bool IsBusy { get; set; }        // обслуживает клиентов
        public double TotalWorkTime { get; set; } // накопленное рабочее время (без перерывов)
        public int CustomersServed { get; set; }
        public List<Customer> CurrentBatch { get; set; }
        public double CurrentServiceTime { get; set; }

        // Для перерывов
        public bool HasTakenFirstBreak { get; set; }
        public bool HasTakenSecondBreak { get; set; }
        public double BreakStartTime { get; set; }
        public double BreakEndTime { get; set; }

        public Clerk(int id)
        {
            Id = id;
            IsWorking = true;
            IsBusy = false;
            CurrentBatch = new List<Customer>();
            TotalWorkTime = 0;
            CustomersServed = 0;
            HasTakenFirstBreak = false;
            HasTakenSecondBreak = false;
        }

        public void StartBreak(double currentTime)
        {
            IsWorking = false;
            BreakStartTime = currentTime;
            BreakEndTime = currentTime + 40;
        }

        public void EndBreak(double currentTime)
        {
            IsWorking = true;
            IsBusy = false;
        }

        public double GetLoadFactor(double simulationTime)
        {
            if (simulationTime <= 0) return 0;
            return TotalWorkTime / simulationTime;
        }
    }

    public class WholesaleStoreSimulation
    {
        private double _simulationTime;
        private double _lambda = 0.5;      // 1 клиент в 2 минуты
        private int _clerksCount = 5;
        private int _maxBatchSize = 3;
        private int _maxQueueLength = 500;

        private int _totalCustomers = 0;
        private int _servedCustomers = 0;
        private int _rejectedCustomers = 0;
        private double _totalWaitTime = 0;
        private double _totalServiceTime = 0;

        private Queue<Customer> _queue;
        private List<Clerk> _clerks;
        private List<Customer> _servedCustomersList;
        private Random _random;
        private PriorityQueue<Event> _eventQueue;

        public SimulationResults Results { get; private set; }

        // Событие для логов
        public event LogEventHandler OnLog;

        public WholesaleStoreSimulation(double simulationTimeMinutes)
        {
            _simulationTime = simulationTimeMinutes;
            _random = new Random();
            _queue = new Queue<Customer>();
            _clerks = new List<Clerk>();
            _servedCustomersList = new List<Customer>();
            _eventQueue = new PriorityQueue<Event>();

            for (int i = 1; i <= _clerksCount; i++)
                _clerks.Add(new Clerk(i));
        }

        private void Log(string time, string eventType, string description, string extra = "")
        {
            OnLog?.Invoke(time, eventType, description, extra);
        }

        private string FormatTime(double minutes)
        {
            DateTime baseTime = new DateTime(1, 1, 1, 10, 0, 0);
            DateTime time = baseTime.AddMinutes(minutes);
            return time.ToString("HH:mm");
        }

        public void Run()
        {
            Initialize();

            while (_eventQueue.Count > 0)
            {
                var currentEvent = _eventQueue.Dequeue();
                if (currentEvent.Time > _simulationTime)
                {
                    foreach (var clerk in _clerks)
                    {
                        if (clerk.IsBusy && clerk.CurrentBatch.Count > 0)
                        {
                            double worked = _simulationTime - clerk.CurrentBatch[0].ServiceStartTime;
                            if (worked > 0) clerk.TotalWorkTime += worked;
                        }
                    }
                    break;
                }

                switch (currentEvent.Type)
                {
                    case EventType.CustomerArrival:
                        ProcessArrival(currentEvent);
                        break;
                    case EventType.ServiceComplete:
                        ProcessServiceComplete(currentEvent);
                        break;
                }
            }
            CalculateResults();
        }

        private void Initialize()
        {
            double firstArrival = GenerateExponential(_lambda);
            _eventQueue.Enqueue(new Event(EventType.CustomerArrival, firstArrival, null));
        }

        private void ProcessArrival(Event ev)
        {
            _totalCustomers++;
            var customer = new Customer(_totalCustomers, ev.Time);
            Log(FormatTime(ev.Time), "ПРИХОД", $"Клиент {customer.Id}, товаров: {customer.ProductsCount}", "");

            // Проверка возможности перерыва у свободных клерков
            foreach (var clerk in _clerks)
            {
                TryTakeBreak(clerk, ev.Time);
            }

            var availableClerk = GetAvailableClerk(ev.Time);
            if (availableClerk != null && !availableClerk.IsBusy)
            {
                StartService(availableClerk, customer, ev.Time);
            }
            else
            {
                if (_queue.Count < _maxQueueLength)
                {
                    _queue.Enqueue(customer);
                    Log(FormatTime(ev.Time), "ОЧЕРЕДЬ", $"Клиент {customer.Id} встал в очередь (длина {_queue.Count})", "");
                }
                else
                {
                    _rejectedCustomers++;
                    Log(FormatTime(ev.Time), "ОТКАЗ", $"Клиент {customer.Id} получил отказ (очередь полна)", "");
                }
            }

            double nextArrival = ev.Time + GenerateExponential(_lambda);
            if (nextArrival <= _simulationTime)
                _eventQueue.Enqueue(new Event(EventType.CustomerArrival, nextArrival, null));
        }

        private Clerk GetAvailableClerk(double currentTime)
        {
            // Ищем работающего и не занятого
            var available = _clerks.FirstOrDefault(c => c.IsWorking && !c.IsBusy);
            if (available != null) return available;

            // Проверяем, не закончился ли перерыв у кого-то
            var finishedBreak = _clerks.FirstOrDefault(c => !c.IsWorking && currentTime >= c.BreakEndTime);
            if (finishedBreak != null)
            {
                finishedBreak.EndBreak(currentTime);
                Log(FormatTime(currentTime), "ПЕРЕРЫВ_КОНЕЦ", $"Клерк {finishedBreak.Id} вернулся с перерыва", "");
                return finishedBreak;
            }
            return null;
        }

        private void StartService(Clerk clerk, Customer customer, double currentTime)
        {
            var batch = new List<Customer> { customer };
            while (batch.Count < _maxBatchSize && _queue.Count > 0)
            {
                batch.Add(_queue.Dequeue());
            }

            clerk.CurrentBatch = batch;
            clerk.IsBusy = true;

            foreach (var c in batch)
            {
                c.ServiceStartTime = currentTime;
                Log(FormatTime(currentTime), "НАЧАЛО_ОБСЛУЖИВАНИЯ", $"Клерк {clerk.Id} начал обслуживание клиента {c.Id} (партия из {batch.Count} клиентов)", "");
            }

            double serviceTime = CalculateServiceTime(batch);
            clerk.CurrentServiceTime = serviceTime;
            _eventQueue.Enqueue(new Event(EventType.ServiceComplete, currentTime + serviceTime, clerk));

            Log(FormatTime(currentTime), "СКЛАД", $"Клерк {clerk.Id} ушёл на склад (вернётся через {serviceTime:F1} мин)", "");
        }

        private double CalculateServiceTime(List<Customer> batch)
        {
            double travelTime = GenerateUniform(2, 5);
            int totalProducts = batch.Sum(c => c.ProductsCount);
            double searchTime = GenerateNormal(totalProducts, 2); // как в условии (утроенное не требуется)
            searchTime = Math.Max(1, Math.Min(searchTime, 30));
            double checkoutTime = GenerateUniform(2, 4);
            return travelTime + searchTime + checkoutTime;
        }

        private void ProcessServiceComplete(Event ev)
        {
            var clerk = ev.Clerk;
            clerk.TotalWorkTime += clerk.CurrentServiceTime;
            clerk.CustomersServed += clerk.CurrentBatch.Count;

            foreach (var customer in clerk.CurrentBatch)
            {
                customer.ServiceEndTime = ev.Time;
                _servedCustomers++;
                _servedCustomersList.Add(customer);
                _totalWaitTime += customer.WaitTime;
                _totalServiceTime += customer.TotalTime;
                Log(FormatTime(ev.Time), "ЗАВЕРШЕНИЕ", $"Клерк {clerk.Id} завершил обслуживание клиента {customer.Id} (время в магазине: {customer.TotalTime:F1} мин)", "");
            }

            Log(FormatTime(ev.Time), "СКЛАД", $"Клерк {clerk.Id} вернулся со склада", "");

            clerk.CurrentBatch.Clear();
            clerk.IsBusy = false;
            clerk.CurrentServiceTime = 0;

            // После освобождения пробуем взять перерыв
            TryTakeBreak(clerk, ev.Time);

            // Если есть очередь, берём следующего клиента
            if (_queue.Count > 0 && clerk.IsWorking && !clerk.IsBusy)
            {
                var nextCustomer = _queue.Dequeue();
                StartService(clerk, nextCustomer, ev.Time);
            }
        }

        private void TryTakeBreak(Clerk clerk, double currentTime)
        {
            // Условия: не в перерыве, не занят, отработал достаточно, ещё не брал соответствующий перерыв
            if (!clerk.IsWorking) return;
            if (clerk.IsBusy) return;

            // Первый перерыв (после 2.5 часов = 150 мин)
            if (!clerk.HasTakenFirstBreak && clerk.TotalWorkTime >= 150)
            {
                // Вероятность 90%
                if (_random.NextDouble() < 0.9)
                {
                    // Проверяем, сколько клерков уже отдыхает (не более 2)
                    int onBreakCount = _clerks.Count(c => !c.IsWorking);
                    if (onBreakCount < 2)
                    {
                        clerk.StartBreak(currentTime);
                        clerk.HasTakenFirstBreak = true;
                        Log(FormatTime(currentTime), "ПЕРЕРЫВ_НАЧАЛО", $"Клерк {clerk.Id} ушёл на первый перерыв (отдых 40 мин)", "");
                        // Перепланируем окончание перерыва (можно просто запланировать событие, но проще – GetAvailableClerk проверит окончание)
                        // Для корректности запланируем событие BreakEnd
                        _eventQueue.Enqueue(new Event(EventType.BreakEnd, clerk.BreakEndTime, clerk));
                    }
                }
                return;
            }

            // Второй перерыв (после 7 часов = 420 мин)
            if (!clerk.HasTakenSecondBreak && clerk.HasTakenFirstBreak && clerk.TotalWorkTime >= 420)
            {
                if (_random.NextDouble() < 0.9)
                {
                    int onBreakCount = _clerks.Count(c => !c.IsWorking);
                    if (onBreakCount < 2)
                    {
                        clerk.StartBreak(currentTime);
                        clerk.HasTakenSecondBreak = true;
                        Log(FormatTime(currentTime), "ПЕРЕРЫВ_НАЧАЛО", $"Клерк {clerk.Id} ушёл на второй перерыв (отдых 40 мин)", "");
                        _eventQueue.Enqueue(new Event(EventType.BreakEnd, clerk.BreakEndTime, clerk));
                    }
                }
            }
        }

        private void ProcessBreakEnd(Event ev)
        {
            var clerk = ev.Clerk;
            clerk.EndBreak(ev.Time);
            Log(FormatTime(ev.Time), "ПЕРЕРЫВ_КОНЕЦ", $"Клерк {clerk.Id} вернулся с перерыва", "");
            // После возвращения, если есть очередь, начать обслуживание
            if (_queue.Count > 0 && !clerk.IsBusy)
            {
                var nextCustomer = _queue.Dequeue();
                StartService(clerk, nextCustomer, ev.Time);
            }
        }

        private double GenerateExponential(double lambda) => -Math.Log(1.0 - _random.NextDouble()) / lambda;
        private double GenerateUniform(double min, double max) => min + _random.NextDouble() * (max - min);
        private double GenerateNormal(double mean, double stdDev)
        {
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        private void CalculateResults()
        {
            Results = new SimulationResults
            {
                TotalCustomers = _totalCustomers,
                ServedCustomers = _servedCustomers,
                RejectedCustomers = _rejectedCustomers,
                RejectionProbability = _totalCustomers > 0 ? (double)_rejectedCustomers / _totalCustomers : 0,
                AverageWaitTime = _servedCustomers > 0 ? _totalWaitTime / _servedCustomers : 0,
                AverageTotalTime = _servedCustomers > 0 ? _totalServiceTime / _servedCustomers : 0,
                ClerkLoadFactors = _clerks.Select(c => c.GetLoadFactor(_simulationTime)).ToList(),
                CustomersPerClerk = _clerks.Select(c => c.CustomersServed).ToList(),
                QueueLength = _queue.Count
            };
        }
    }

    public enum EventType { CustomerArrival, ServiceComplete, BreakEnd }

    public class Event : IComparable<Event>
    {
        public EventType Type { get; set; }
        public double Time { get; set; }
        public Clerk Clerk { get; set; }
        public Event(EventType type, double time, Clerk clerk) { Type = type; Time = time; Clerk = clerk; }
        public int CompareTo(Event other) => Time.CompareTo(other.Time);
    }

    public class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> _data = new List<T>();
        public int Count => _data.Count;
        public void Enqueue(T item) { /* реализация кучи */ _data.Add(item); int ci = _data.Count - 1; while (ci > 0) { int pi = (ci - 1) / 2; if (_data[ci].CompareTo(_data[pi]) >= 0) break; T tmp = _data[ci]; _data[ci] = _data[pi]; _data[pi] = tmp; ci = pi; } }
        public T Dequeue() { int li = _data.Count - 1; T frontItem = _data[0]; _data[0] = _data[li]; _data.RemoveAt(li); li--; int pi = 0; while (true) { int ci = pi * 2 + 1; if (ci > li) break; int rc = ci + 1; if (rc <= li && _data[rc].CompareTo(_data[ci]) < 0) ci = rc; if (_data[pi].CompareTo(_data[ci]) <= 0) break; T tmp = _data[pi]; _data[pi] = _data[ci]; _data[ci] = tmp; pi = ci; } return frontItem; }
    }

    public class SimulationResults
    {
        public int TotalCustomers { get; set; }
        public int ServedCustomers { get; set; }
        public int RejectedCustomers { get; set; }
        public double RejectionProbability { get; set; }
        public double AverageWaitTime { get; set; }
        public double AverageTotalTime { get; set; }
        public List<double> ClerkLoadFactors { get; set; }
        public List<int> CustomersPerClerk { get; set; }
        public int QueueLength { get; set; }
        public double AverageLoadFactor => ClerkLoadFactors?.Average() ?? 0;
    }
}