using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WholesaleStoreSimulation
{
    public partial class MainForm : Form
    {
        private TextBox txtSimulationTime;
        private TextBox txtPrecision;
        private Button btnRun;
        private Button btnRunMultiple;
        private Button btnManualCalc;
        private DataGridView dgvResults;
        private Chart chartLoad;
        private Label lblStatus;
        private NumericUpDown nudIterations;
        private DataGridView dgvLogs;
        private NumericUpDown nudManualCustomers; // Для выбора количества клиентов в ручном расчёте
        private Label lblManualCustomers;

        public MainForm()
        {
            InitializeComponent();
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Имитационная модель: Оптовый магазин (с ручным расчётом)";
        }

        private void InitializeComponent()
        {
            txtSimulationTime = new TextBox();
            txtPrecision = new TextBox();
            btnRun = new Button();
            btnRunMultiple = new Button();
            btnManualCalc = new Button();
            nudIterations = new NumericUpDown();
            dgvResults = new DataGridView();
            chartLoad = new Chart();
            lblStatus = new Label();
            dgvLogs = new DataGridView();
            nudManualCustomers = new NumericUpDown();
            lblManualCustomers = new Label();

            ((System.ComponentModel.ISupportInitialize)(nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(dgvResults)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(chartLoad)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(dgvLogs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudManualCustomers)).BeginInit();
            SuspendLayout();

            Label lblTime = new Label() { Text = "Время моделирования (мин):", Location = new Point(12, 15), Size = new Size(180, 23) };
            txtSimulationTime.Location = new Point(200, 13);
            txtSimulationTime.Size = new Size(100, 23);
            txtSimulationTime.Text = "600";

            Label lblPrecision = new Label() { Text = "Точность (%):", Location = new Point(12, 45), Size = new Size(180, 23) };
            txtPrecision.Location = new Point(200, 43);
            txtPrecision.Size = new Size(100, 23);
            txtPrecision.Text = "20";

            Label lblIter = new Label() { Text = "Кол-во прогонов:", Location = new Point(12, 75), Size = new Size(180, 23) };
            nudIterations.Location = new Point(200, 73);
            nudIterations.Size = new Size(100, 23);
            nudIterations.Minimum = 1;
            nudIterations.Maximum = 100;
            nudIterations.Value = 30;

            // Элементы для ручного расчёта
            lblManualCustomers.Text = "Клиентов для ручного расчёта:";
            lblManualCustomers.Location = new Point(320, 45);
            lblManualCustomers.Size = new Size(180, 23);

            nudManualCustomers.Location = new Point(510, 43);
            nudManualCustomers.Size = new Size(60, 23);
            nudManualCustomers.Minimum = 1;
            nudManualCustomers.Maximum = 50;
            nudManualCustomers.Value = 4;

            btnRun.Location = new Point(320, 12);
            btnRun.Size = new Size(130, 30);
            btnRun.Text = "Одиночный прогон";
            btnRun.Click += BtnRun_Click;

            btnRunMultiple.Location = new Point(460, 12);
            btnRunMultiple.Size = new Size(150, 30);
            btnRunMultiple.Text = "Многократный прогон";
            btnRunMultiple.Click += BtnRunMultiple_Click;

            btnManualCalc.Location = new Point(600, 12);
            btnManualCalc.Size = new Size(130, 30);
            btnManualCalc.Text = "Ручной расчёт";
            btnManualCalc.Click += BtnManualCalc_Click;

            CheckBox chkFixedMode = new CheckBox();
            chkFixedMode.Text = "Фиксированные значения";
            chkFixedMode.Location = new Point(760, 15);
            chkFixedMode.Size = new Size(110, 40);
            chkFixedMode.Checked = false;
            chkFixedMode.CheckedChanged += ChkDeterministic_CheckedChanged;
            this.Controls.Add(chkFixedMode);

            dgvResults.Location = new Point(12, 110);
            dgvResults.Size = new Size(850, 250);
            dgvResults.AllowUserToAddRows = false;
            dgvResults.ReadOnly = true;
            dgvResults.RowHeadersVisible = false;
            dgvResults.Columns.Add("Parameter", "Показатель");
            dgvResults.Columns.Add("Value", "Значение");
            dgvResults.Columns[0].Width = 300;
            dgvResults.Columns[1].Width = 200;
            
            chartLoad.Location = new Point(12, 370);
            chartLoad.Size = new Size(850, 380);
            chartLoad.ChartAreas.Add(new ChartArea());
            chartLoad.Series.Add("Загрузка клерков");
            chartLoad.Series[0].ChartType = SeriesChartType.Column;
            chartLoad.Series[0].IsValueShownAsLabel = true;

            dgvLogs.Location = new Point(880, 12);
            dgvLogs.Size = new Size(500, 738);
            dgvLogs.AllowUserToAddRows = false;
            dgvLogs.ReadOnly = true;
            dgvLogs.RowHeadersVisible = false;
            dgvLogs.Columns.Add("Time", "Время");
            dgvLogs.Columns.Add("EventType", "Событие");
            dgvLogs.Columns.Add("Description", "Описание");
            dgvLogs.Columns.Add("Extra", "Дополнительно");
            dgvLogs.Columns[0].Width = 60;
            dgvLogs.Columns[1].Width = 100;
            dgvLogs.Columns[2].Width = 220;
            dgvLogs.Columns[3].Width = 100;

            lblStatus.Location = new Point(12, 760);
            lblStatus.Size = new Size(1360, 25);
            lblStatus.BackColor = Color.LightGray;
            lblStatus.Text = "Готов к работе";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            Controls.Add(lblTime);
            Controls.Add(txtSimulationTime);
            Controls.Add(lblPrecision);
            Controls.Add(txtPrecision);
            Controls.Add(lblIter);
            Controls.Add(nudIterations);
            Controls.Add(lblManualCustomers);
            Controls.Add(nudManualCustomers);
            Controls.Add(btnRun);
            Controls.Add(btnRunMultiple);
            Controls.Add(btnManualCalc);
            Controls.Add(dgvResults);
            Controls.Add(chartLoad);
            Controls.Add(dgvLogs);
            Controls.Add(lblStatus);

            ((System.ComponentModel.ISupportInitialize)(nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(dgvResults)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(chartLoad)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(dgvLogs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudManualCustomers)).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            try
            {
                dgvLogs.Rows.Clear();
                lblStatus.Text = "Выполняется одиночный прогон...";
                Application.DoEvents();

                double simTime = double.Parse(txtSimulationTime.Text);
                var model = new WholesaleStoreSimulation(simTime);
                model.OnLog += AddLogEntry;
                model.Run();

                DisplayResults(model.Results);
                UpdateChart(model.Results);
                lblStatus.Text = $"Одиночный прогон завершен. Обслужено: {model.Results.ServedCustomers} клиентов";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Ошибка при выполнении";
            }
        }

        private void ChkDeterministic_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            WholesaleStoreSimulation.FixedMode = chk.Checked;

            if (chk.Checked)
            {
                WholesaleStoreSimulation.ResetDeterministicIndex();
                lblStatus.Text = "Включен режим ТЕСТА (неслучайные значения). Все интервалы фиксированы.";
                AddLogEntry(DateTime.Now.ToString("HH:mm:ss"), "ТЕСТ_РЕЖИМ",
                    "Включен детерминированный режим",
                    "Интервал прихода = 2 мин, время поиска = число товаров, путь и расчёт = средние значения");
            }
            else
            {
                lblStatus.Text = "Обычный режим (случайные значения)";
                AddLogEntry(DateTime.Now.ToString("HH:mm:ss"), "ТЕСТ_РЕЖИМ",
                    "Выключен детерминированный режим",
                    "Включена генерация случайных значений");
            }
        }

        private void BtnManualCalc_Click(object sender, EventArgs e)
        {
            try
            {
                dgvLogs.Rows.Clear();
                int customersCount = (int)nudManualCustomers.Value;
                lblStatus.Text = $"Выполняется ручной расчёт (теоретический) для {customersCount} клиентов...";
                Application.DoEvents();

                WholesaleStoreSimulation.RunManualCalculation(AddLogEntry, customersCount);

                dgvResults.Rows.Clear();
                dgvResults.Rows.Add("Ручной расчёт", $"Теоретические значения для {customersCount} клиентов");

                // Простые расчёты для итоговой таблицы
                double travelTime = 3.5;
                double checkoutTime = 3.0;
                int productsCount = 5;
                double firstServiceTime = travelTime + productsCount + checkoutTime; // 11.5
                double firstReturnTime = 2.0 + firstServiceTime;

                int remaining = customersCount - 1;
                int batchSize = Math.Min(remaining, 3);
                double secondServiceTime = 0;
                if (batchSize > 0)
                    secondServiceTime = travelTime + (productsCount * batchSize) + checkoutTime;

                double totalSimTime = firstReturnTime + secondServiceTime;
                double loadFactor = (firstServiceTime + secondServiceTime) / totalSimTime;

                // Расчёт среднего времени ожидания
                double totalWaitTime = 0;
                for (int i = 1; i < customersCount && i <= batchSize + 1; i++)
                {
                    totalWaitTime += firstReturnTime - (2.0 + i * 2.0);
                }
                double avgWaitTime = customersCount > 1 ? totalWaitTime / (customersCount - 1) : 0;

                // Расчёт среднего времени в магазине
                double totalStoreTime = firstServiceTime;
                for (int i = 1; i < customersCount && i <= batchSize + 1; i++)
                {
                    totalStoreTime += (firstReturnTime + secondServiceTime) - (2.0 + i * 2.0);
                }
                double avgStoreTime = customersCount > 0 ? totalStoreTime / customersCount : 0;

                dgvResults.Rows.Add("Всего клиентов", customersCount);
                dgvResults.Rows.Add("Обслужено клиентов", customersCount);
                dgvResults.Rows.Add("Отказов", 0);
                dgvResults.Rows.Add("Вероятность отказа", "0%");
                dgvResults.Rows.Add("Среднее время ожидания (мин)", $"{avgWaitTime:F2}");
                dgvResults.Rows.Add("Среднее время в магазине (мин)", $"{avgStoreTime:F2}");
                dgvResults.Rows.Add("Загрузка клерка 1", $"{loadFactor:P0}");

                lblStatus.Text = $"Ручной расчёт для {customersCount} клиентов завершён (теоретические значения)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при ручном расчёте: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Ошибка при ручном расчёте";
            }
        }

        private void BtnRunMultiple_Click(object sender, EventArgs e)
        {
            try
            {
                dgvLogs.Rows.Clear();
                lblStatus.Text = "Выполняется многократный прогон...";
                Application.DoEvents();

                double simTime = double.Parse(txtSimulationTime.Text);
                int iterations = (int)nudIterations.Value;
                double targetPrecision = double.Parse(txtPrecision.Text) / 100.0;

                var aggregatedResults = RunMultipleSimulations(simTime, iterations, targetPrecision);
                DisplayAggregatedResults(aggregatedResults);

                if (aggregatedResults.AverageLoadFactors.Length > 0)
                    UpdateAggregatedChart(aggregatedResults);
                else
                    chartLoad.Series[0].Points.Clear();

                lblStatus.Text = $"Многократный прогон ({iterations} итераций) завершен. Точность: {targetPrecision * 100}%";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Ошибка при выполнении";
            }
        }

        private void AddLogEntry(string time, string eventType, string description, string extra)
        {
            if (dgvLogs.InvokeRequired)
                dgvLogs.Invoke(new Action(() => dgvLogs.Rows.Add(time, eventType, description, extra)));
            else
                dgvLogs.Rows.Add(time, eventType, description, extra);
        }

        private AggregatedResults RunMultipleSimulations(double simTime, int iterations, double targetPrecision)
        {
            targetPrecision = 1-targetPrecision;
            List<double> a = new List<double>();
           
                int newCountIterations = iterations, actualClerksCount = 0;
                var servedList = new List<int>();
                var rejectedList = new List<int>();
                var waitTimeList = new List<double>();
                var totalTimeList = new List<double>();
                var avgLoadFactors = new double[0];
                double meanWait = -1, stdDevWait = -1;
                do
                {
                    iterations = newCountIterations;
                    servedList = new List<int>();
                    rejectedList = new List<int>();
                    waitTimeList = new List<double>();
                    totalTimeList = new List<double>();
                    var clerkLoadLists = new List<List<double>>();

                    for (int i = 0; i < iterations; i++)
                    {
                        var model = new WholesaleStoreSimulation(simTime);
                        model.Run();

                        if (i == 0)
                            actualClerksCount = model.Results.ClerksCount;

                        servedList.Add(model.Results.ServedCustomers);
                        rejectedList.Add(model.Results.RejectedCustomers);
                        waitTimeList.Add(model.Results.AverageWaitTime);
                        totalTimeList.Add(model.Results.AverageTotalTime);

                        for (int c = 0; c < model.Results.ClerkLoadFactors.Count; c++)
                        {
                            if (clerkLoadLists.Count <= c)
                                clerkLoadLists.Add(new List<double>());
                            clerkLoadLists[c].Add(model.Results.ClerkLoadFactors[c]);
                        }

                        if (i % 10 == 0)
                        {
                            lblStatus.Text = $"Прогон {i + 1} из {iterations}...";
                            Application.DoEvents();
                        }
                    }
                    avgLoadFactors = new double[actualClerksCount];
                    for (int i = 0; i < actualClerksCount && i < clerkLoadLists.Count; i++)
                    {
                        avgLoadFactors[i] = clerkLoadLists[i].Count > 0 ? clerkLoadLists[i].Average() : 0;
                    }
                    meanWait = waitTimeList.Count > 0 ? waitTimeList.Average() : 0;
                    stdDevWait = waitTimeList.Count > 0 ? Math.Sqrt(waitTimeList.Sum(w => Math.Pow(w - meanWait, 2)) / waitTimeList.Count) : 0;
                    newCountIterations = (int)Math.Pow(GetNormalQuantile(targetPrecision) * stdDevWait / (1 - targetPrecision), 2);
                }
                while (newCountIterations > iterations);
                a.Add(meanWait);
            

         
            return new AggregatedResults
            {
                Iterations = iterations,
                TargetPrecision = targetPrecision,
                ActualPrecision = iterations,
                AverageServed = servedList.Count > 0 ? servedList.Average() : 0,
                AverageRejected = rejectedList.Count > 0 ? rejectedList.Average() : 0,
                AverageWaitTime = meanWait,
                AverageTotalTime = totalTimeList.Count > 0 ? totalTimeList.Average() : 0,
                RejectionProbability = (servedList.Count > 0 && rejectedList.Count > 0) ? servedList.Zip(rejectedList, (s, r) => (double)r / (s + r)).Average() : 0,
                AverageLoadFactors = avgLoadFactors,
                MinServed = servedList.Count > 0 ? servedList.Min() : 0,
                MaxServed = servedList.Count > 0 ? servedList.Max() : 0,
                StdDevWait = stdDevWait,
                ClerksCount = actualClerksCount
            };
        }
        public static double GetNormalQuantile(double confidenceLevel)
        {
            if (confidenceLevel <= 0.5 || confidenceLevel >= 0.9999)
                throw new ArgumentException("Доверительная вероятность должна быть в интервале (0.5, 0.9999)");

            // Таблица: доверительная вероятность → квантиль
            double[] levels = { 0.50, 0.60, 0.70, 0.80, 0.85, 0.90, 0.91, 0.95, 0.96, 0.97, 0.98, 0.99, 0.995, 0.999 };
            double[] quantiles = { 0.000, 0.524, 1.036, 1.282, 1.440, 1.645, 1.695, 1.960, 2.054, 2.170, 2.326, 2.576, 2.807, 3.291 };

            // Линейная интерполяция между табличными значениями
            for (int i = 0; i < levels.Length - 1; i++)
            {
                if (confidenceLevel <= levels[i + 1])
                {
                    double t = (confidenceLevel - levels[i]) / (levels[i + 1] - levels[i]);
                    return quantiles[i] + t * (quantiles[i + 1] - quantiles[i]);
                }
            }

            return quantiles[quantiles.Length - 1];
        }
        private void DisplayResults(SimulationResults results)
        {
            dgvResults.Rows.Clear();
            dgvResults.Rows.Add("Всего клиентов", results.TotalCustomers);
            dgvResults.Rows.Add("Обслужено клиентов", results.ServedCustomers);
            dgvResults.Rows.Add("Отказов", results.RejectedCustomers);
            dgvResults.Rows.Add("Вероятность отказа", $"{results.RejectionProbability:P2}");
            dgvResults.Rows.Add("Среднее время ожидания (мин)", $"{results.AverageWaitTime:F2}");
            dgvResults.Rows.Add("Среднее время в магазине (мин)", $"{results.AverageTotalTime:F2}");
            dgvResults.Rows.Add("Очередь после моделирования", results.QueueLength);
            for (int i = 0; i < results.ClerkLoadFactors.Count; i++)
                dgvResults.Rows.Add($"Загрузка клерка {i + 1}", $"{results.ClerkLoadFactors[i]:P2}");
            dgvResults.Rows.Add("Средняя загрузка", $"{results.AverageLoadFactor:P2}");
        }

        private void DisplayAggregatedResults(AggregatedResults results)
        {
            dgvResults.Rows.Clear();
            dgvResults.Rows.Add("Количество прогонов", results.Iterations);
            dgvResults.Rows.Add("Целевая точность", $"{results.TargetPrecision:P2}");
            dgvResults.Rows.Add("Среднее кол-во клиентов", $"{results.AverageServed:F2}");
            dgvResults.Rows.Add("Итераций", results.ActualPrecision);
            dgvResults.Rows.Add("Мин. обслужено", results.MinServed);
            dgvResults.Rows.Add("Макс. обслужено", results.MaxServed);
            dgvResults.Rows.Add("Среднее кол-во отказов", $"{results.AverageRejected:F2}");
            dgvResults.Rows.Add("Вероятность отказа", $"{results.RejectionProbability:P2}");
            dgvResults.Rows.Add("Среднее время ожидания (мин)", $"{results.AverageWaitTime:F2}");
            dgvResults.Rows.Add("СКО времени ожидания", $"{results.StdDevWait:F2}");
            dgvResults.Rows.Add("Среднее время в магазине (мин)", $"{results.AverageTotalTime:F2}");
            dgvResults.Rows.Add($"Количество клерков в модели", results.ClerksCount);
        }

        private void UpdateChart(SimulationResults results)
        {
            chartLoad.Series[0].Points.Clear();
            for (int i = 0; i < results.ClerkLoadFactors.Count; i++)
            {
                int idx = chartLoad.Series[0].Points.AddXY(i, results.ClerkLoadFactors[i] * 100);
                chartLoad.Series[0].Points[idx].AxisLabel = $"Клерк {i + 1}";
                chartLoad.Series[0].Points[idx].Label = $"{results.ClerkLoadFactors[i]:P1}";
            }
            chartLoad.ChartAreas[0].AxisX.Minimum = -0.5;
            chartLoad.ChartAreas[0].AxisX.Maximum = results.ClerkLoadFactors.Count - 0.5;
            chartLoad.ChartAreas[0].AxisX.Interval = 1;
            chartLoad.ChartAreas[0].AxisY.Title = "Загрузка (%)";
            chartLoad.ChartAreas[0].AxisY.Minimum = 0;
            chartLoad.ChartAreas[0].AxisY.Maximum = 100;
            chartLoad.Invalidate();
        }

        private void UpdateAggregatedChart(AggregatedResults results)
        {
            chartLoad.Series[0].Points.Clear();
            for (int i = 0; i < results.AverageLoadFactors.Length; i++)
            {
                int idx = chartLoad.Series[0].Points.AddXY(i, results.AverageLoadFactors[i] * 100);
                chartLoad.Series[0].Points[idx].AxisLabel = $"Клерк {i + 1}";
                chartLoad.Series[0].Points[idx].Label = $"{results.AverageLoadFactors[i]:P1}";
            }
            chartLoad.ChartAreas[0].AxisX.Minimum = -0.5;
            chartLoad.ChartAreas[0].AxisX.Maximum = results.AverageLoadFactors.Length - 0.5;
            chartLoad.ChartAreas[0].AxisX.Interval = 1;
            chartLoad.ChartAreas[0].AxisY.Title = "Средняя загрузка (%)";
            chartLoad.ChartAreas[0].AxisY.Minimum = 0;
            chartLoad.ChartAreas[0].AxisY.Maximum = 100;
            chartLoad.Invalidate();
        }
    }

    public class AggregatedResults
    {
        public int Iterations { get; set; }
        public double TargetPrecision { get; set; }
        public double ActualPrecision { get; set; }
        public double AverageServed { get; set; }
        public double AverageRejected { get; set; }
        public double AverageWaitTime { get; set; }
        public double AverageTotalTime { get; set; }
        public double RejectionProbability { get; set; }
        public double[] AverageLoadFactors { get; set; }
        public int MinServed { get; set; }
        public int MaxServed { get; set; }
        public double StdDevWait { get; set; }
        public int ClerksCount { get; set; }
    }
}