using System;
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
        private DataGridView dgvResults;
        private Chart chartLoad;
        private Label lblStatus;
        private NumericUpDown nudIterations;
        private DataGridView dgvLogs;

        public MainForm()
        {
            InitializeComponent();
            this.Size = new Size(1400, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Имитационная модель: Оптовый магазин (с логами)";
        }

        private void InitializeComponent()
        {
            // Создание элементов
            txtSimulationTime = new TextBox();
            txtPrecision = new TextBox();
            btnRun = new Button();
            btnRunMultiple = new Button();
            nudIterations = new NumericUpDown();
            dgvResults = new DataGridView();
            chartLoad = new Chart();
            lblStatus = new Label();
            dgvLogs = new DataGridView();

            ((System.ComponentModel.ISupportInitialize)(nudIterations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(dgvResults)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(chartLoad)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(dgvLogs)).BeginInit();
            SuspendLayout();

            // Label для времени
            Label lblTime = new Label() { Text = "Время моделирования (мин):", Location = new Point(12, 15), Size = new Size(180, 23) };
            txtSimulationTime.Location = new Point(200, 13);
            txtSimulationTime.Size = new Size(100, 23);
            txtSimulationTime.Text = "600";

            // Точность
            Label lblPrecision = new Label() { Text = "Точность (%):", Location = new Point(12, 45), Size = new Size(180, 23) };
            txtPrecision.Location = new Point(200, 43);
            txtPrecision.Size = new Size(100, 23);
            txtPrecision.Text = "20";

            // Количество прогонов
            Label lblIter = new Label() { Text = "Кол-во прогонов:", Location = new Point(12, 75), Size = new Size(180, 23) };
            nudIterations.Location = new Point(200, 73);
            nudIterations.Size = new Size(100, 23);
            nudIterations.Minimum = 1;
            nudIterations.Maximum = 100;
            nudIterations.Value = 30;

            // Кнопки
            btnRun.Location = new Point(320, 12);
            btnRun.Size = new Size(130, 30);
            btnRun.Text = "Одиночный прогон";
            btnRun.Click += BtnRun_Click;

            btnRunMultiple.Location = new Point(460, 12);
            btnRunMultiple.Size = new Size(150, 30);
            btnRunMultiple.Text = "Многократный прогон";
            btnRunMultiple.Click += BtnRunMultiple_Click;

            // Таблица результатов
            dgvResults.Location = new Point(12, 110);
            dgvResults.Size = new Size(850, 250);
            dgvResults.AllowUserToAddRows = false;
            dgvResults.ReadOnly = true;
            dgvResults.RowHeadersVisible = false;
            dgvResults.Columns.Add("Parameter", "Показатель");
            dgvResults.Columns.Add("Value", "Значение");
            dgvResults.Columns[0].Width = 300;
            dgvResults.Columns[1].Width = 200;

            // График
            chartLoad.Location = new Point(12, 370);
            chartLoad.Size = new Size(850, 380);
            chartLoad.ChartAreas.Add(new ChartArea());
            chartLoad.Series.Add("Загрузка клерков");
            chartLoad.Series[0].ChartType = SeriesChartType.Column;
            chartLoad.Series[0].IsValueShownAsLabel = true;

            // Таблица логов
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

            // Строка статуса
            lblStatus.Location = new Point(12, 760);
            lblStatus.Size = new Size(1360, 25);
            lblStatus.BackColor = Color.LightGray;
            lblStatus.Text = "Готов к работе";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            // Добавление элементов на форму
            Controls.Add(lblTime);
            Controls.Add(txtSimulationTime);
            Controls.Add(lblPrecision);
            Controls.Add(txtPrecision);
            Controls.Add(lblIter);
            Controls.Add(nudIterations);
            Controls.Add(btnRun);
            Controls.Add(btnRunMultiple);
            Controls.Add(dgvResults);
            Controls.Add(chartLoad);
            Controls.Add(dgvLogs);
            Controls.Add(lblStatus);

            ((System.ComponentModel.ISupportInitialize)(nudIterations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(dgvResults)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(chartLoad)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(dgvLogs)).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        // ========== Обработчики и логика (копируем из предыдущего ответа) ==========
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
                UpdateAggregatedChart(aggregatedResults);
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
            var servedList = new System.Collections.Generic.List<int>();
            var rejectedList = new System.Collections.Generic.List<int>();
            var waitTimeList = new System.Collections.Generic.List<double>();
            var totalTimeList = new System.Collections.Generic.List<double>();
            var loadC1 = new System.Collections.Generic.List<double>();
            var loadC2 = new System.Collections.Generic.List<double>();
            var loadC3 = new System.Collections.Generic.List<double>();
            var loadC4 = new System.Collections.Generic.List<double>();
            var loadC5 = new System.Collections.Generic.List<double>();

            for (int i = 0; i < iterations; i++)
            {
                var model = new WholesaleStoreSimulation(simTime);
                model.Run();

                servedList.Add(model.Results.ServedCustomers);
                rejectedList.Add(model.Results.RejectedCustomers);
                waitTimeList.Add(model.Results.AverageWaitTime);
                totalTimeList.Add(model.Results.AverageTotalTime);
                if (model.Results.ClerkLoadFactors.Count >= 5)
                {
                    loadC1.Add(model.Results.ClerkLoadFactors[0]);
                    loadC2.Add(model.Results.ClerkLoadFactors[1]);
                    loadC3.Add(model.Results.ClerkLoadFactors[2]);
                    loadC4.Add(model.Results.ClerkLoadFactors[3]);
                    loadC5.Add(model.Results.ClerkLoadFactors[4]);
                }

                if (i % 10 == 0)
                {
                    lblStatus.Text = $"Прогон {i + 1} из {iterations}...";
                    Application.DoEvents();
                }
            }

            double meanWait = waitTimeList.Average();
            double stdDevWait = Math.Sqrt(waitTimeList.Sum(w => Math.Pow(w - meanWait, 2)) / iterations);
            double actualPrecision = 1.96 * stdDevWait / Math.Sqrt(iterations) / meanWait;

            return new AggregatedResults
            {
                Iterations = iterations,
                TargetPrecision = targetPrecision,
                ActualPrecision = actualPrecision,
                AverageServed = servedList.Average(),
                AverageRejected = rejectedList.Average(),
                AverageWaitTime = meanWait,
                AverageTotalTime = totalTimeList.Average(),
                RejectionProbability = servedList.Zip(rejectedList, (s, r) => (double)r / (s + r)).Average(),
                AverageLoadFactors = new double[] { loadC1.Average(), loadC2.Average(), loadC3.Average(), loadC4.Average(), loadC5.Average() },
                MinServed = servedList.Min(),
                MaxServed = servedList.Max(),
                StdDevWait = stdDevWait
            };
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
            dgvResults.Rows.Add("Фактическая точность", $"{results.ActualPrecision:P2}");
            dgvResults.Rows.Add("Точность достигнута", results.ActualPrecision <= results.TargetPrecision ? "ДА" : "НЕТ");
            dgvResults.Rows.Add("", "");
            dgvResults.Rows.Add("Среднее кол-во клиентов", $"{results.AverageServed:F2}");
            dgvResults.Rows.Add("Мин. обслужено", results.MinServed);
            dgvResults.Rows.Add("Макс. обслужено", results.MaxServed);
            dgvResults.Rows.Add("Среднее кол-во отказов", $"{results.AverageRejected:F2}");
            dgvResults.Rows.Add("Вероятность отказа", $"{results.RejectionProbability:P2}");
            dgvResults.Rows.Add("Среднее время ожидания (мин)", $"{results.AverageWaitTime:F2}");
            dgvResults.Rows.Add("СКО времени ожидания", $"{results.StdDevWait:F2}");
            dgvResults.Rows.Add("Среднее время в магазине (мин)", $"{results.AverageTotalTime:F2}");
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
    }
}