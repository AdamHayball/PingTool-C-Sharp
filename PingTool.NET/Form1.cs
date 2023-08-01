﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
 
namespace PingTool.NET
{
    public partial class Form1 : Form
    {
        private TextBox outputTextBox;
        private TextBox textBoxGatewayIP;
        private TextBox textBoxUsableIP;
        private TextBox textBoxNumPings; // Added the textBoxNumPings field
        private Button buttonRunPing;
        private Button buttonReset;
        private Button buttonCancel; // Add the buttonCancel field
        private Label labelUsableIP;
        private Label labelGatewayIP;
        private Label labelNumPings; // Added the labelNumPings field
        private CancellationTokenSource cancellationTokenSource;
        private CheckBox checkBoxSlowPings; // Add the checkBoxSlowPings field
        private CheckBox checkBoxSaveToLogFile; // Add the checkBoxSaveToLogFile field
        private Chart pingChart; // Add the pingChart field
        private List<Task<string>> pingTasks = new List<Task<string>>(); // Moved pingTasks to class level

        public Form1()
        {
            InitializeComponent();
            textBoxNumPings.Text = "200"; // Set the default value of the number of pings

            // Set version number and icon
            this.Text = $"Ping Tool v1.2.0";
            this.Icon = Properties.Resources.SPTMulti;

            // Initialize and set up the pingChart control
            pingChart = new Chart();
            pingChart.Location = new Point(20, this.ClientSize.Height - 210); // Set the chart location at the bottom
            pingChart.Size = new Size(380, 200); // Set the size of the chart
            pingChart.ChartAreas.Add("PingChartArea");

            // Add the first series for Usable IP with blue color
            AddSeriesToChart("UsableIP", Color.Blue);

            // Add the second series for Gateway IP with green color
            AddSeriesToChart("GatewayIP", Color.Green);

            pingChart.ChartAreas["PingChartArea"].AxisX.Title = "Ping Number";
            pingChart.ChartAreas["PingChartArea"].AxisY.Title = "Response Time (ms)";
            this.Controls.Add(pingChart); // Add the chart control to the form
        }

        private void DisplayResultsAfterCancellation()
        {
            // Check if any ping task was canceled
            bool pingCancelled = pingTasks.Any(task => task.Status == TaskStatus.Canceled);

            // Check if any valid IP address was provided
            bool validIPProvided = pingTasks.Any(task => task.Status == TaskStatus.RanToCompletion && !string.IsNullOrEmpty(task.Result));

            // Display the appropriate message in the outputTextBox
            if (pingCancelled)
            {
                outputTextBox.Text = "Ping canceled by user.";
            }
            else if (!validIPProvided)
            {
                outputTextBox.Text = "Please enter valid IP addresses.";
            }
            else
            {
                // Concatenate the ping results
                StringBuilder resultBuilder = new StringBuilder();

                string ipAddress;
                string previousIPAddress = null;

                foreach (var task in pingTasks)
                {
                    if (!string.IsNullOrEmpty(task.Result))
                    {
                        ipAddress = (task == pingTasks[0]) ? textBoxUsableIP.Text.Trim() : textBoxGatewayIP.Text.Trim();

                        if (previousIPAddress != null)
                        {
                            // Add separator only if there are more valid IP addresses to display
                            resultBuilder.AppendLine("=================");
                        }

                        resultBuilder.AppendLine($"Results for {(task == pingTasks[0] ? "Usable" : "Gateway")} IP ({ipAddress}):");
                        resultBuilder.AppendLine(task.Result);

                        previousIPAddress = ipAddress;
                    }
                }

                if (pingTasks.All(task => string.IsNullOrEmpty(task.Result)))
                {
                    resultBuilder.AppendLine("Request Timed Out (all pings failed).");
                }

                if (checkBoxSaveToLogFile.Checked)
                {
                    SavePingOutputToLogFile(resultBuilder.ToString());
                }

                outputTextBox.Text = resultBuilder.ToString();
            }
        }
        private void AddSeriesToChart(string seriesName, Color color)
        {
            if (!pingChart.Series.IsUniqueName(seriesName))
                return;

            var series = new Series(seriesName);
            series.ChartType = SeriesChartType.Line;
            series.Color = color;
            pingChart.Series.Add(series);
        }
        private void InitializeChartSeries()
        {
            // Initialize and set up the pingChart control
            pingChart = new Chart();
            pingChart.Location = new Point(20, this.ClientSize.Height - 210); // Set the chart location at the bottom
            pingChart.Size = new Size(380, 200); // Set the size of the chart
            pingChart.ChartAreas.Add("PingChartArea");

            // Add the first series for Usable IP with blue color
            var seriesUsableIP = new Series("UsableIP");
            seriesUsableIP.ChartType = SeriesChartType.Line;
            seriesUsableIP.Color = Color.Blue;
            pingChart.Series.Add(seriesUsableIP);

            // Add the second series for Gateway IP with green color
            var seriesGatewayIP = new Series("GatewayIP");
            seriesGatewayIP.ChartType = SeriesChartType.Line;
            seriesGatewayIP.Color = Color.Green;
            pingChart.Series.Add(seriesGatewayIP);

            pingChart.ChartAreas["PingChartArea"].AxisX.Title = "Ping Number";
            pingChart.ChartAreas["PingChartArea"].AxisY.Title = "Response Time (ms)";
            this.Controls.Add(pingChart); // Add the chart control to the form
        }

        // Add the "Run Ping" button click event handler
        private async void runPingButton_Click(object sender, EventArgs e)
        {
            string usableIP = textBoxUsableIP.Text.Trim();
            string gatewayIP = textBoxGatewayIP.Text.Trim();

            if (!int.TryParse(textBoxNumPings.Text, out int numPings))
            {
                MessageBox.Show("Please enter a valid number for the number of pings.");
                return;
            }

            outputTextBox.Text = "Pinging IPs. Please Wait...";

            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            bool validIPProvided = false;
            int validIpCount = 0; // Variable to track the number of valid IP addresses found
            StringBuilder resultBuilder = new StringBuilder();

            ClearPingChart();
            pingTasks.Clear(); // Clear any existing tasks before adding new ones

            if (!string.IsNullOrEmpty(usableIP))
            {
                Task<string> usableIpTask = RunPingAsync(usableIP, numPings, cancellationTokenSource.Token, pingChart.Series["UsableIP"]);
                pingTasks.Add(usableIpTask);
            }

            if (!string.IsNullOrEmpty(gatewayIP))
            {
                Task<string> gatewayIpTask = RunPingAsync(gatewayIP, numPings, cancellationTokenSource.Token, pingChart.Series["GatewayIP"]);
                pingTasks.Add(gatewayIpTask);
            }

            try
            {
                if (pingTasks.Count > 0)
                {

                    await Task.WhenAll(pingTasks);

                    foreach (var task in pingTasks)
                    {
                        if (!string.IsNullOrEmpty(task.Result))
                        {
                            validIPProvided = true;
                            validIpCount++; // Increment the count for each valid IP address found
                            string ipType = (task == pingTasks[0]) ? "Usable" : "Gateway";
                            resultBuilder.AppendLine($"Results for {ipType} IP ({(task == pingTasks[0] ? usableIP : gatewayIP)}):");
                            resultBuilder.AppendLine(task.Result);
                            if (validIpCount < pingTasks.Count)
                            {
                                // Add separator only if there are more valid IP addresses to display
                                resultBuilder.AppendLine("=================");
                            }
                        }
                    }

                    if (validIPProvided && pingTasks.All(task => string.IsNullOrEmpty(task.Result)))
                    {
                        resultBuilder.AppendLine("Request Timed Out (all pings failed).");
                    }

                    if (checkBoxSaveToLogFile.Checked)
                    {
                        SavePingOutputToLogFile(resultBuilder.ToString());
                    }
                }
            }
            catch (OperationCanceledException)
            {
                resultBuilder.AppendLine("Ping canceled by user.");
            }
            catch (Exception ex)
            {
                resultBuilder.AppendLine($"Error occurred during ping: {ex.Message}");
            }
            finally
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    outputTextBox.Text = resultBuilder.ToString();
                }
            }
        }

        private async Task<string> RunPingAsync(string ipAddress, int numPings, CancellationToken cancellationToken, Series series)
        {
            return await Task.Run(() => RunPing(ipAddress, numPings, cancellationToken, series), cancellationToken);
        }
        private string RunPing(string ipAddress, int numPings, CancellationToken cancellationToken, Series series)
        {
            string pingResults = "";
            var pingSender = new Ping();
            var pingOptions = new PingOptions();
            var latencies = new List<long>();
            bool timeoutMessageAdded = false;

            for (int i = 0; i < numPings; i++)
            {
                // Add a delay between pings if the checkbox is checked
                int delay = checkBoxSlowPings.Checked ? 500 : 0;
                Thread.Sleep(delay); // Add the delay

                // Check if the cancellation has been requested
                if (cancellationToken.IsCancellationRequested)
                {
                    pingResults += "Ping canceled by user.";
                    break;
                }

                var reply = pingSender.Send(ipAddress, 1000, new byte[32], pingOptions);

                if (reply.Status == IPStatus.Success)
                {
                    latencies.Add(reply.RoundtripTime);

                    // Update the chart with the new ping result on the main UI thread
                    this.Invoke(new Action(() => UpdatePingChart(ipAddress, i + 1, reply.RoundtripTime, series)));
                }
                else
                {
                    if (!timeoutMessageAdded)
                    {
                        pingResults += "Request timed out." + Environment.NewLine;
                        timeoutMessageAdded = true;
                    }
                }

                // Update the chart with the new ping result on the main UI thread
                this.Invoke(new Action(() => UpdatePingChart(ipAddress, i + 1, reply.RoundtripTime, series)));

            }

            // Calculate and display statistics based on the actual 'numPings'
            int received = latencies.Count;
            int lost = numPings - received;
            double lossPercentage = (lost / (double)numPings) * 100.0;

            pingResults += Environment.NewLine + $"Ping statistics for {ipAddress}:" + Environment.NewLine;
            pingResults += $"    Packets: Sent = {numPings}, Received = {received}, Lost = {lost} (Loss Percentage = {lossPercentage:F2}%)" + Environment.NewLine;


            if (received > 0)
            {
                double avgLatency = 0;
                long minLatency = latencies[0];
                long maxLatency = latencies[0];
                foreach (long latency in latencies)
                {
                    avgLatency += latency;
                    if (latency < minLatency) minLatency = latency;
                    if (latency > maxLatency) maxLatency = latency;
                }
                avgLatency /= received;

                pingResults += Environment.NewLine + "Approximate round trip times in milliseconds:" + Environment.NewLine;
                pingResults += $"    Minimum = {minLatency} ms" + Environment.NewLine;
                pingResults += $"    Maximum = {maxLatency} ms" + Environment.NewLine;
                pingResults += $"    Average = {avgLatency:F2} ms" + Environment.NewLine;
            }
            else
            {
                // No successful pings
                pingResults += Environment.NewLine + "No successful pings." + Environment.NewLine;
            }

            return pingResults;
        }

        // Update the UpdatePingChart method to take ipAddress, pingNumber, responseTime, and series as parameters
        private void UpdatePingChart(string ipAddress, int pingNumber, long responseTime, Series series)
        {
            // Get the appropriate series based on the ipAddress
            series = series == null ? (ipAddress == textBoxUsableIP.Text.Trim() ? pingChart.Series["UsableIP"] : pingChart.Series["GatewayIP"]) : series;

            // Create a new data point with the given X and Y values and add it to the chart series
            DataPoint dataPoint = new DataPoint(pingNumber, responseTime);
            series.Points.Add(dataPoint);
        }
        private void ClearPingChart()
        {
            // Clear the chart series data points
            pingChart.Series["UsableIP"].Points.Clear();
            pingChart.Series["GatewayIP"].Points.Clear();
        }

        // Add the "Cancel" button click event handler
        private async void cancelButton_Click(object sender, EventArgs e)
        {
            // Check if there are any ongoing ping tasks
            if (pingTasks.Any(task => !task.IsCompleted))
            {
                // Cancel the ongoing pings when the cancel button is clicked
                cancellationTokenSource?.Cancel();

                try
                {
                    // Wait for all tasks to complete or be canceled
                    await Task.WhenAll(pingTasks);
                }
                catch (OperationCanceledException)
                {
                    // The tasks were canceled, which is expected
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error occurred during ping cancellation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Display the results after cancellation
            DisplayResultsAfterCancellation();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            textBoxUsableIP.Text = "";
            textBoxGatewayIP.Text = "";
            outputTextBox.Text = "";
            ClearPingChart(); // Clear the graph series data points
        }

        private void SavePingOutputToLogFile(string pingResults)
        {
            string logFileName = $"PingLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);

            try
            {
                File.WriteAllText(logFilePath, pingResults);
            }
            catch (Exception ex)
            {
                // Handle the exception if there's an issue saving to the log file.
                MessageBox.Show($"Error saving to log file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // No changes made to the Form1_Load event handler, so it remains as is

        private void InitializeComponent()
        {
            textBoxUsableIP = new TextBox();
            textBoxGatewayIP = new TextBox();
            buttonRunPing = new Button();
            buttonReset = new Button();
            outputTextBox = new TextBox();
            labelUsableIP = new Label();
            labelGatewayIP = new Label();
            labelNumPings = new Label();
            textBoxNumPings = new TextBox();
            buttonCancel = new Button();
            checkBoxSlowPings = new CheckBox();
            checkBoxSaveToLogFile = new CheckBox();
            SuspendLayout();
            // 
            // textBoxUsableIP
            // 
            textBoxUsableIP.Location = new Point(233, 23);
            textBoxUsableIP.Margin = new Padding(4, 3, 4, 3);
            textBoxUsableIP.Name = "textBoxUsableIP";
            textBoxUsableIP.Size = new Size(174, 23);
            textBoxUsableIP.TabIndex = 0;
            // 
            // textBoxGatewayIP
            // 
            textBoxGatewayIP.Location = new Point(233, 69);
            textBoxGatewayIP.Margin = new Padding(4, 3, 4, 3);
            textBoxGatewayIP.Name = "textBoxGatewayIP";
            textBoxGatewayIP.Size = new Size(174, 23);
            textBoxGatewayIP.TabIndex = 1;
            // 
            // buttonRunPing
            // 
            buttonRunPing.Location = new Point(12, 208);
            buttonRunPing.Margin = new Padding(4, 3, 4, 3);
            buttonRunPing.Name = "buttonRunPing";
            buttonRunPing.Size = new Size(117, 35);
            buttonRunPing.TabIndex = 2;
            buttonRunPing.Text = "Run Ping";
            buttonRunPing.UseVisualStyleBackColor = true;
            buttonRunPing.Click += runPingButton_Click;
            // 
            // buttonReset
            // 
            buttonReset.Location = new Point(140, 208);
            buttonReset.Margin = new Padding(4, 3, 4, 3);
            buttonReset.Name = "buttonReset";
            buttonReset.Size = new Size(117, 35);
            buttonReset.TabIndex = 3;
            buttonReset.Text = "Reset";
            buttonReset.UseVisualStyleBackColor = true;
            buttonReset.Click += resetButton_Click;
            // 
            // outputTextBox
            // 
            outputTextBox.Location = new Point(12, 254);
            outputTextBox.Margin = new Padding(4, 3, 4, 3);
            outputTextBox.Multiline = true;
            outputTextBox.Name = "outputTextBox";
            outputTextBox.ScrollBars = ScrollBars.Vertical;
            outputTextBox.Size = new Size(395, 184);
            outputTextBox.TabIndex = 4;
            // 
            // labelUsableIP
            // 
            labelUsableIP.AutoSize = true;
            labelUsableIP.Location = new Point(12, 23);
            labelUsableIP.Margin = new Padding(4, 0, 4, 0);
            labelUsableIP.Name = "labelUsableIP";
            labelUsableIP.Size = new Size(58, 15);
            labelUsableIP.TabIndex = 10;
            labelUsableIP.Text = "Usable IP:";
            // 
            // labelGatewayIP
            // 
            labelGatewayIP.AutoSize = true;
            labelGatewayIP.Location = new Point(12, 69);
            labelGatewayIP.Margin = new Padding(4, 0, 4, 0);
            labelGatewayIP.Name = "labelGatewayIP";
            labelGatewayIP.Size = new Size(68, 15);
            labelGatewayIP.TabIndex = 9;
            labelGatewayIP.Text = "Gateway IP:";
            // 
            // labelNumPings
            // 
            labelNumPings.AutoSize = true;
            labelNumPings.Location = new Point(12, 115);
            labelNumPings.Margin = new Padding(4, 0, 4, 0);
            labelNumPings.Name = "labelNumPings";
            labelNumPings.Size = new Size(100, 15);
            labelNumPings.TabIndex = 4;
            labelNumPings.Text = "Number of Pings:";
            // 
            // textBoxNumPings
            // 
            textBoxNumPings.Location = new Point(233, 115);
            textBoxNumPings.Margin = new Padding(4, 3, 4, 3);
            textBoxNumPings.Name = "textBoxNumPings";
            textBoxNumPings.Size = new Size(174, 23);
            textBoxNumPings.TabIndex = 5;
            // 
            // buttonCancel
            // 
            buttonCancel.Location = new Point(268, 208);
            buttonCancel.Margin = new Padding(4, 3, 4, 3);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(117, 35);
            buttonCancel.TabIndex = 6;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += cancelButton_Click;
            // 
            // checkBoxSlowPings
            // 
            checkBoxSlowPings.AutoSize = true;
            checkBoxSlowPings.Location = new Point(12, 162);
            checkBoxSlowPings.Margin = new Padding(4, 3, 4, 3);
            checkBoxSlowPings.Name = "checkBoxSlowPings";
            checkBoxSlowPings.Size = new Size(83, 19);
            checkBoxSlowPings.TabIndex = 7;
            checkBoxSlowPings.Text = "Slow Pings";
            checkBoxSlowPings.UseVisualStyleBackColor = true;
            // 
            // checkBoxSaveToLogFile
            // 
            checkBoxSaveToLogFile.AutoSize = true;
            checkBoxSaveToLogFile.Location = new Point(117, 162);
            checkBoxSaveToLogFile.Margin = new Padding(4, 3, 4, 3);
            checkBoxSaveToLogFile.Name = "checkBoxSaveToLogFile";
            checkBoxSaveToLogFile.Size = new Size(46, 19);
            checkBoxSaveToLogFile.TabIndex = 8;
            checkBoxSaveToLogFile.Text = "Log";
            checkBoxSaveToLogFile.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(424, 660);
            Controls.Add(buttonCancel);
            Controls.Add(checkBoxSlowPings);
            Controls.Add(checkBoxSaveToLogFile);
            Controls.Add(outputTextBox);
            Controls.Add(buttonReset);
            Controls.Add(buttonRunPing);
            Controls.Add(textBoxGatewayIP);
            Controls.Add(textBoxUsableIP);
            Controls.Add(labelGatewayIP);
            Controls.Add(labelUsableIP);
            Controls.Add(labelNumPings);
            Controls.Add(textBoxNumPings);
            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "Ping Tool";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}