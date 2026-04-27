using System;
using System.Collections.Generic;
using System.Linq;

namespace WholesaleStoreSimulation
{
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

        private static Random _rnd = new Random();
        private static bool _fixedMode = false;

        public static void SetFixedMode(bool fixedMode)
        {
            _fixedMode = fixedMode;
        }

        public Customer(int id, double arrivalTime)
        {
            Id = id;
            ArrivalTime = arrivalTime;
            if (_fixedMode)
            {
                ProductsCount = 5;
            }
            else
            {
                ProductsCount = _rnd.Next(3, 8);
            }
        }
    }

    public class Clerk
    {
        public int Id { get; set; }
        public bool IsWorking { get; set; }
        public bool IsBusy { get; set; }
        public double TotalWorkTime { get; set; }
        public int CustomersServed { get; set; }
        public List<Customer> CurrentBatch { get; set; }
        public double CurrentServiceTime { get; set; }

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
        private double _lambda = 0.5;
        private int _clerksCount = 5;
        private int _maxBatchSize = 3;
        private int _maxQueueLength = 50;

        private int _totalCustomers = 0;
        private int _servedCustomers = 0;
        private int _rejectedCustomers = 0;
        private double _totalWaitTime = 0;
        private double _totalServiceTime = 0;
        private double _lastArrivalTime = 0;

        public static bool FixedMode = false;
        private static int _fixedIndex = 0;

        private Queue<Customer> _queue;
        private List<Clerk> _clerks;
        private Random _random;
        private PriorityQueue<Event> _eventQueue;

        public SimulationResults Results { get; private set; }
        public event LogEventHandler OnLog;

        public int ClerksCount => _clerksCount;

        public WholesaleStoreSimulation(double simulationTimeMinutes)
        {
            _simulationTime = simulationTimeMinutes;
            _random = new Random();
            _queue = new Queue<Customer>();
            _clerks = new List<Clerk>();
            _eventQueue = new PriorityQueue<Event>();

            Customer.SetFixedMode(FixedMode);

            for (int i = 1; i <= _clerksCount; i++)
                _clerks.Add(new Clerk(i));
        }

        private void Log(string time, string eventType, string description, string extra = "")
        {
            OnLog?.Invoke(time, eventType, description, extra);
        }

        public static void ResetDeterministicIndex()
        {
            _fixedIndex = 0;
            Customer.SetFixedMode(FixedMode);
        }

        private string FormatTime(double minutes)
        {
            DateTime baseTime = new DateTime(1, 1, 1, 10, 0, 0);
            return baseTime.AddMinutes(minutes).ToString("HH:mm");
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
                    case EventType.BreakEnd:
                        ProcessBreakEnd(currentEvent);
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

            Log(FormatTime(ev.Time), "ПРИХОД",
                $"Клиент {customer.Id} (товаров: {customer.ProductsCount})",
                $"Интервал с предыдущим: {(_totalCustomers > 1 ? (ev.Time - _lastArrivalTime).ToString("F1") : "первый клиент")} мин");

            foreach (var clerk in _clerks.Where(c => !c.IsBusy && c.IsWorking))
                TryTakeBreak(clerk, ev.Time);

            var availableClerks = _clerks.Where(c => c.IsWorking && !c.IsBusy).ToList();
            Clerk selectedClerk = null;

            if (availableClerks.Any())
            {
                int index = _random.Next(availableClerks.Count);
                selectedClerk = availableClerks[index];

                Log(FormatTime(ev.Time), "ВЫБОР_КЛЕРКА",
                    $"Выбран клерк {selectedClerk.Id}",
                    $"Свободных клерков: {availableClerks.Count} (№{string.Join(",", availableClerks.Select(c => c.Id))}), выбран случайный");

                StartService(selectedClerk, customer, ev.Time);
            }
            else
            {
                var finishedBreak = _clerks.FirstOrDefault(c => !c.IsWorking && ev.Time >= c.BreakEndTime);
                if (finishedBreak != null)
                {
                    finishedBreak.EndBreak(ev.Time);
                    Log(FormatTime(ev.Time), "ПЕРЕРЫВ_КОНЕЦ",
                        $"Клерк {finishedBreak.Id} вернулся с перерыва",
                        $"Перерыв длился 40 мин, клерк готов к работе");
                    StartService(finishedBreak, customer, ev.Time);
                }
                else if (_queue.Count < _maxQueueLength)
                {
                    _queue.Enqueue(customer);
                    Log(FormatTime(ev.Time), "ОЧЕРЕДЬ",
                        $"Клиент {customer.Id} встал в очередь",
                        $"Все клерки заняты ({_clerks.Count(c => c.IsBusy)} работают, {_clerks.Count(c => !c.IsWorking)} на перерыве). Длина очереди: {_queue.Count}");
                }
                else
                {
                    _rejectedCustomers++;
                    Log(FormatTime(ev.Time), "ОТКАЗ",
                        $"Клиент {customer.Id} получил отказ",
                        $"Очередь переполнена (макс. {_maxQueueLength}), все {_clerks.Count} клерков заняты");
                }
            }

            double nextArrival = ev.Time + GenerateExponential(_lambda);
            _lastArrivalTime = ev.Time;

            if (nextArrival <= _simulationTime)
                _eventQueue.Enqueue(new Event(EventType.CustomerArrival, nextArrival, null));
        }

        private void StartService(Clerk clerk, Customer customer, double currentTime)
        {
            var batch = new List<Customer> { customer };
            int takenFromQueue = 0;

            while (batch.Count < _maxBatchSize && _queue.Count > 0)
            {
                batch.Add(_queue.Dequeue());
                takenFromQueue++;
            }

            clerk.CurrentBatch = batch;
            clerk.IsBusy = true;

            double travelTime = GenerateUniform(2, 5);
            int totalProducts = batch.Sum(c => c.ProductsCount);
            double searchTime = GenerateNormal(totalProducts, 2);
            searchTime = Math.Max(1, Math.Min(searchTime, 30));
            double checkoutTime = GenerateUniform(2, 4);
            double serviceTime = travelTime + searchTime + checkoutTime;
            clerk.CurrentServiceTime = serviceTime;

            string batchInfo = $"Партия из {batch.Count} клиентов (№{string.Join(",", batch.Select(c => c.Id))})";
            string formulaInfo = $"Расчёт: путь={travelTime:F1} + поиск={searchTime:F1} + расчёт={checkoutTime:F1} = {serviceTime:F1} мин";

            if (takenFromQueue > 0)
                formulaInfo += $", взято из очереди: {takenFromQueue} клиентов";

            Log(FormatTime(currentTime), "НАЧАЛО_ОБСЛУЖИВАНИЯ",
                $"Клерк {clerk.Id} начал обслуживание: {batchInfo}",
                formulaInfo);

            foreach (var c in batch)
            {
                c.ServiceStartTime = currentTime;
            }

            _eventQueue.Enqueue(new Event(EventType.ServiceComplete, currentTime + serviceTime, clerk));

            Log(FormatTime(currentTime), "СКЛАД",
                $"Клерк {clerk.Id} ушёл на склад",
                $"Вернётся через {serviceTime:F1} мин (в {FormatTime(currentTime + serviceTime)})");
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
                _totalWaitTime += customer.WaitTime;
                _totalServiceTime += customer.TotalTime;

                Log(FormatTime(ev.Time), "ЗАВЕРШЕНИЕ",
                    $"Клерк {clerk.Id} завершил обслуживание клиента {customer.Id}",
                    $"Время ожидания: {customer.WaitTime:F1} мин, общее время в магазине: {customer.TotalTime:F1} мин");
            }

            Log(FormatTime(ev.Time), "СКЛАД",
                $"Клерк {clerk.Id} вернулся со склада",
                $"Обслужено {clerk.CurrentBatch.Count} клиентов за {clerk.CurrentServiceTime:F1} мин");

            clerk.CurrentBatch.Clear();
            clerk.IsBusy = false;
            clerk.CurrentServiceTime = 0;

            TryTakeBreak(clerk, ev.Time);

            if (_queue.Count > 0 && clerk.IsWorking)
            {
                var nextCustomer = _queue.Dequeue();
                Log(FormatTime(ev.Time), "ВЗЯТ_ИЗ_ОЧЕРЕДИ",
                    $"Клерк {clerk.Id} взял следующего клиента",
                    $"В очереди осталось {_queue.Count} клиентов");
                StartService(clerk, nextCustomer, ev.Time);
            }
        }

        private void TryTakeBreak(Clerk clerk, double currentTime)
        {
            if (!clerk.IsWorking || clerk.IsBusy) return;

            if (!clerk.HasTakenFirstBreak && clerk.TotalWorkTime >= 150)
            {
                if (_random.NextDouble() < 0.9)
                {
                    int onBreakCount = _clerks.Count(c => !c.IsWorking);
                    if (onBreakCount < 2)
                    {
                        clerk.StartBreak(currentTime);
                        clerk.HasTakenFirstBreak = true;
                        Log(FormatTime(currentTime), "ПЕРЕРЫВ_НАЧАЛО",
                            $"Клерк {clerk.Id} ушёл на первый перерыв",
                            $"Накоплено работы: {clerk.TotalWorkTime:F1} мин (>=150), вероятность 90% выпала, отдыхающих: {onBreakCount} (<2)");
                        _eventQueue.Enqueue(new Event(EventType.BreakEnd, clerk.BreakEndTime, clerk));
                        return;
                    }
                    else
                    {
                        Log(FormatTime(currentTime), "ПЕРЕРЫВ_ОТКЛОНЁН",
                            $"Клерк {clerk.Id} не может уйти на перерыв",
                            $"Слишком много отдыхающих: {onBreakCount} (макс. 2)");
                    }
                }
                else
                {
                    Log(FormatTime(currentTime), "ПЕРЕРЫВ_ОТКЛОНЁН",
                        $"Клерк {clerk.Id} не ушёл на перерыв",
                        $"Вероятность 90% не выпала (выпало {_random.NextDouble():F2})");
                }
                return;
            }

            if (!clerk.HasTakenSecondBreak && clerk.HasTakenFirstBreak && clerk.TotalWorkTime >= 420)
            {
                if (_random.NextDouble() < 0.9)
                {
                    int onBreakCount = _clerks.Count(c => !c.IsWorking);
                    if (onBreakCount < 2)
                    {
                        clerk.StartBreak(currentTime);
                        clerk.HasTakenSecondBreak = true;
                        Log(FormatTime(currentTime), "ПЕРЕРЫВ_НАЧАЛО",
                            $"Клерк {clerk.Id} ушёл на второй перерыв",
                            $"Накоплено работы: {clerk.TotalWorkTime:F1} мин (>=420), отдыхающих: {onBreakCount} (<2)");
                        _eventQueue.Enqueue(new Event(EventType.BreakEnd, clerk.BreakEndTime, clerk));
                    }
                    else
                    {
                        Log(FormatTime(currentTime), "ПЕРЕРЫВ_ОТКЛОНЁН",
                            $"Клерк {clerk.Id} не может уйти на второй перерыв",
                            $"Отдыхающих: {onBreakCount}");
                    }
                }
                else
                {
                    Log(FormatTime(currentTime), "ПЕРЕРЫВ_ОТКЛОНЁН",
                        $"Клерк {clerk.Id} не ушёл на второй перерыв",
                        $"Вероятность 90% не выпала");
                }
            }
        }

        private void ProcessBreakEnd(Event ev)
        {
            var clerk = ev.Clerk;
            clerk.EndBreak(ev.Time);
            Log(FormatTime(ev.Time), "ПЕРЕРЫВ_КОНЕЦ", $"Клерк {clerk.Id} вернулся с перерыва", "");
        }

        private double GenerateExponential(double lambda)
        {
            if (FixedMode)
            {
                double[] fixedValues = { 2.0, 2.0, 2.0, 2.0, 2.0 };
                double val = fixedValues[_fixedIndex % fixedValues.Length];
                _fixedIndex++;
                return val;
            }
            return -Math.Log(1.0 - _random.NextDouble()) / lambda;
        }

        private double GenerateUniform(double min, double max)
        {
            if (FixedMode)
            {
                return (min + max) / 2;
            }
            return min + _random.NextDouble() * (max - min);
        }

        private double GenerateNormal(double mean, double stdDev)
        {
            if (FixedMode)
            {
                return mean;
            }
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
                QueueLength = _queue.Count,
                ClerksCount = _clerksCount
            };
        }

        // ========== СТАТИЧЕСКИЙ МЕТОД ДЛЯ РУЧНОГО РАСЧЁТА ==========
        public static void RunManualCalculation(LogEventHandler logCallback, int customersCount)
        {
            ResetDeterministicIndex();

            logCallback?.Invoke("10:00", "РУЧНОЙ_РАСЧЁТ", $"Начало ручного расчёта (теоретического) для {customersCount} клиентов",
                "Интервал прихода = 2 мин, время поиска = число товаров, путь и расчёт = средние значения");

            double travelTime = 3.5;
            double checkoutTime = 3.0;
            int productsCount = 5;
            double searchTime = productsCount;
            double serviceTime = travelTime + searchTime + checkoutTime;

            // Для хранения информации о клиентах (время прихода, время ожидания, время обслуживания)
            var arrivals = new List<double>();
            var waitTimes = new List<double>();
            var serviceStartTimes = new List<double>();
            var serviceEndTimes = new List<double>();

            double currentTime = 0;
            double lastArrivalTime = 0;
            bool firstBatchProcessed = false;
            double firstBatchReturnTime = 0;
            int servedInFirstBatch = 1;
            int takenFromQueue = 0;

            for (int i = 0; i < customersCount; i++)
            {
                int customerId = i + 1;
                double arrivalTime = (i == 0) ? 2.0 : lastArrivalTime + 2.0;
                currentTime = arrivalTime;
                lastArrivalTime = arrivalTime;
                arrivals.Add(arrivalTime);

                string timeStr = new DateTime(1, 1, 1, 10, 0, 0).AddMinutes(currentTime).ToString("HH:mm");
                logCallback?.Invoke(timeStr, "ПРИХОД", $"Клиент {customerId} (товаров: {productsCount})",
                    $"Интервал с предыдущим: {(i == 0 ? "первый клиент" : "2.0 мин")}");

                if (i == 0)
                {
                    // Первый клиент обслуживается сразу
                    serviceStartTimes.Add(currentTime);
                    waitTimes.Add(0);

                    logCallback?.Invoke(timeStr, "НАЧАЛО_ОБСЛУЖИВАНИЯ", $"Клерк 1 начал обслуживание: партия из 1 клиента (№{customerId})",
                        $"Расчёт: путь={travelTime:F1} + поиск={searchTime:F1} + расчёт={checkoutTime:F1} = {serviceTime:F1} мин");

                    logCallback?.Invoke(timeStr, "СКЛАД", $"Клерк 1 ушёл на склад",
                        $"Вернётся через {serviceTime:F1} мин (в {new DateTime(1, 1, 1, 10, 0, 0).AddMinutes(currentTime + serviceTime):HH:mm})");

                    firstBatchReturnTime = currentTime + serviceTime;
                    serviceEndTimes.Add(firstBatchReturnTime);
                }
                else if (!firstBatchProcessed)
                {
                    // Клиенты, которые встают в очередь, пока первый клерк на складе
                    logCallback?.Invoke(timeStr, "ОЧЕРЕДЬ", $"Клиент {customerId} встал в очередь",
                        $"Клерк 1 занят (вернётся в {new DateTime(1, 1, 1, 10, 0, 0).AddMinutes(firstBatchReturnTime):HH:mm}), длина очереди: {i}");
                    takenFromQueue++;
                }
                else
                {
                    // Клиенты после возвращения клерка (должны быть обслужены во второй партии)
                    logCallback?.Invoke(timeStr, "ОЧЕРЕДЬ", $"Клиент {customerId} встал в очередь",
                        $"Клерк 1 занят (обслуживает партию из {takenFromQueue} клиентов), длина очереди: {serviceStartTimes.Count - 1}");
                }

                // Если это последний клиент в первой волне или достигли конца
                if (i == customersCount - 1)
                {
                    // Возвращение клерка после первого клиента
                    string returnTimeStr = new DateTime(1, 1, 1, 10, 0, 0).AddMinutes(firstBatchReturnTime).ToString("HH:mm");
                    logCallback?.Invoke(returnTimeStr, "СКЛАД", "Клерк 1 вернулся со склада",
                        $"Обслужено 1 клиент за {serviceTime:F1} мин");

                    firstBatchProcessed = true;

                    // Определяем, сколько клиентов в очереди (все, кроме первого)
                    int queueSize = customersCount - 1;
                    int batchSize = Math.Min(queueSize, 3);

                    if (batchSize > 0)
                    {
                        logCallback?.Invoke(returnTimeStr, "НАЧАЛО_ОБСЛУЖИВАНИЯ", $"Клерк 1 начал обслуживание: партия из {batchSize} клиентов (№2-{batchSize + 1})",
                            $"Расчёт: путь={travelTime:F1} + поиск={productsCount * batchSize:F1} + расчёт={checkoutTime:F1} = {travelTime + productsCount * batchSize + checkoutTime:F1} мин, взято из очереди: {batchSize} клиентов");

                        double secondServiceTime = travelTime + (productsCount * batchSize) + checkoutTime;

                        logCallback?.Invoke(returnTimeStr, "СКЛАД", "Клерк 1 ушёл на склад",
                            $"Вернётся через {secondServiceTime:F1} мин (в {new DateTime(1, 1, 1, 10, 0, 0).AddMinutes(firstBatchReturnTime + secondServiceTime):HH:mm})");

                        double secondReturnTime = firstBatchReturnTime + secondServiceTime;

                        // Для клиентов во второй партии
                        for (int j = 1; j <= batchSize && j < customersCount; j++)
                        {
                            serviceStartTimes.Add(firstBatchReturnTime);
                            waitTimes.Add(firstBatchReturnTime - arrivals[j]);
                            serviceEndTimes.Add(secondReturnTime);
                            servedInFirstBatch++;
                        }

                        string secondReturnTimeStr = new DateTime(1, 1, 1, 10, 0, 0).AddMinutes(secondReturnTime).ToString("HH:mm");
                        logCallback?.Invoke(secondReturnTimeStr, "СКЛАД", "Клерк 1 вернулся со склада",
                            $"Обслужено {batchSize} клиентов за {secondServiceTime:F1} мин");
                    }
                }
            }

            // Расчёт итогов
            int servedCustomers = (int)serviceEndTimes.Count;
            double totalWaitTime = waitTimes.Sum();
            double totalServiceTime = serviceEndTimes.Sum() - arrivals.Sum();
            double avgWaitTime = servedCustomers > 0 ? totalWaitTime / servedCustomers : 0;
            double avgTotalTime = servedCustomers > 0 ? totalServiceTime / servedCustomers : 0;
            double loadFactor = (serviceEndTimes.Count > 0 ? serviceEndTimes.Last() : 0) > 0
                ? (serviceTime + (servedCustomers - 1 > 0 ? (travelTime + (productsCount * (servedCustomers - 1)) + checkoutTime) : 0)) / (serviceEndTimes.Last() > 0 ? serviceEndTimes.Last() : 1)
                : 0;

            logCallback?.Invoke("", "ИТОГИ_РУЧНОГО_РАСЧЁТА", $"Всего клиентов: {customersCount}", "");
            logCallback?.Invoke("", "ИТОГИ_РУЧНОГО_РАСЧЁТА", $"Обслужено: {servedCustomers}", "");
            logCallback?.Invoke("", "ИТОГИ_РУЧНОГО_РАСЧЁТА", $"Отказов: {customersCount - servedCustomers}", "");
            logCallback?.Invoke("", "ИТОГИ_РУЧНОГО_РАСЧЁТА", $"Среднее время ожидания: {avgWaitTime:F2} мин", "");
            logCallback?.Invoke("", "ИТОГИ_РУЧНОГО_РАСЧЁТА", $"Среднее время в магазине: {avgTotalTime:F2} мин", "");
            logCallback?.Invoke("", "ИТОГИ_РУЧНОГО_РАСЧЁТА", $"Загрузка клерка 1: {loadFactor:P0}", "");

            logCallback?.Invoke("", "РУЧНОЙ_РАСЧЁТ", "Ручной расчёт завершён", "Результаты являются теоретическими и не зависят от алгоритма модели");
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
        public void Enqueue(T item)
        {
            _data.Add(item);
            int ci = _data.Count - 1;
            while (ci > 0)
            {
                int pi = (ci - 1) / 2;
                if (_data[ci].CompareTo(_data[pi]) >= 0) break;
                T tmp = _data[ci]; _data[ci] = _data[pi]; _data[pi] = tmp;
                ci = pi;
            }
        }
        public T Dequeue()
        {
            int li = _data.Count - 1;
            T frontItem = _data[0];
            _data[0] = _data[li];
            _data.RemoveAt(li);
            li--;
            int pi = 0;
            while (true)
            {
                int ci = pi * 2 + 1;
                if (ci > li) break;
                int rc = ci + 1;
                if (rc <= li && _data[rc].CompareTo(_data[ci]) < 0) ci = rc;
                if (_data[pi].CompareTo(_data[ci]) <= 0) break;
                T tmp = _data[pi]; _data[pi] = _data[ci]; _data[ci] = tmp;
                pi = ci;
            }
            return frontItem;
        }
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
        public int ClerksCount { get; set; }
        public double AverageLoadFactor => ClerkLoadFactors?.Average() ?? 0;
    }
}