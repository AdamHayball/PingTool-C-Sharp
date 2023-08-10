using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
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
        private TextBox textBoxPacketSize; // Add the textBoxPacketSize field
        private Button buttonRunPing;
        private Button buttonReset;
        private Button buttonCancel; // Add the buttonCancel field
        private Label labelUsableIP;
        private Label labelGatewayIP;
        private Label labelNumPings; // Added the labelNumPings field
        private Label labelPacketSize; // Add the labelPacketSize field
        private CancellationTokenSource cancellationTokenSource;
        private CheckBox checkBoxSlowPings; // Add the checkBoxSlowPings field
        private CheckBox checkBoxSaveToLogFile; // Add the checkBoxSaveToLogFile field
        private Chart pingChart; // Add the pingChart field
        private LinkLabel linkLabel;
        private List<Task<string>> pingTasks = new List<Task<string>>(); // Moved pingTasks to class level

        public Form1()
        {
            InitializeComponent();

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MinimumSize = new Size(440, 695);
            this.MaximumSize = new Size(440, 695);

            textBoxNumPings.Text = "200"; // Set the default value of the number of pings

            // Set version number in the title bar without the revision number
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string versionNumber = $"{version.Major}.{version.Minor}.{version.Build}";
            this.Text = $"Ping Tool v{versionNumber}";

            // Set titlebar icon
            this.Icon = Properties.Resources.SPTMulti;

            // Add the first series for Usable IP with blue color
            AddSeriesToChart("UsableIP", Color.Blue);

            // Add the second series for Gateway IP with green color
            AddSeriesToChart("GatewayIP", Color.Green);

            pingChart.ChartAreas["PingChartArea"].AxisX.Title = "Ping Number";
            pingChart.ChartAreas["PingChartArea"].AxisY.Title = "Response Time (ms)";

            // Add the color legend at the bottom of the chart
            var legend = new Legend("Legend");
            legend.Docking = Docking.Bottom;
            legend.Alignment = StringAlignment.Center;
            legend.LegendStyle = LegendStyle.Row;
            pingChart.Legends.Add(legend);
            this.Controls.Add(pingChart); // Add the chart control to the form
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                // Use the default web browser to open the URL
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/AdamHayball/PingTool-C-Sharp",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            // Check the platform at runtime and add the series only for Windows
            if (OperatingSystem.IsWindows())
            {
                if (!pingChart.Series.IsUniqueName(seriesName))
                    return;

                var series = new Series(seriesName);
                series.ChartType = SeriesChartType.Line;
                series.Color = color;
                series.BorderWidth = 2; // Set the line thickness to 2
                pingChart.Series.Add(series);
            }
        }
        // Add the "Run Ping" button click event handler
        private async void runPingButton_Click(object sender, EventArgs e)
        {
            // Disable the "Run Ping" button to prevent multiple clicks while pinging
            buttonRunPing.Enabled = false;

            // Get the packet size from the textBoxPacketSize control
            if (!int.TryParse(textBoxPacketSize.Text, out int packetSize))
            {
                MessageBox.Show("Please enter a valid number for the packet size.");
                buttonRunPing.Enabled = true; // Enable the button again
                return;
            }

            string usableIP = textBoxUsableIP.Text.Trim();
            string gatewayIP = textBoxGatewayIP.Text.Trim();

            if (!int.TryParse(textBoxNumPings.Text, out int numPings))
            {
                MessageBox.Show("Please enter a valid number for the number of pings.");
                buttonRunPing.Enabled = true; // Enable the button again
                return;
            }

            // Clear any previous ping results and chart data
            outputTextBox.Text = "Pinging IPs. Please Wait...";
            ClearPingChart();
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            // Execute ping tasks concurrently when both IPs are provided
            bool validUsableIP = !string.IsNullOrEmpty(usableIP);
            bool validGatewayIP = !string.IsNullOrEmpty(gatewayIP);

            StringBuilder resultBuilder = new StringBuilder();

            if (validUsableIP && validGatewayIP)
            {
                var usableIpTask = RunPingAsync(usableIP, numPings, packetSize, cancellationTokenSource.Token, pingChart.Series["UsableIP"]);
                var gatewayIpTask = RunPingAsync(gatewayIP, numPings, packetSize, cancellationTokenSource.Token, pingChart.Series["GatewayIP"]);

                await Task.WhenAll(usableIpTask, gatewayIpTask);

                resultBuilder.AppendLine($"Results for Usable IP ({usableIP}):");
                resultBuilder.AppendLine(usableIpTask.Result);
                resultBuilder.AppendLine("=================");
                resultBuilder.AppendLine($"Results for Gateway IP ({gatewayIP}):");
                resultBuilder.AppendLine(gatewayIpTask.Result);
            }
            else if (validUsableIP)
            {
                string usableIpResults = await RunPingAsync(usableIP, numPings, packetSize, cancellationTokenSource.Token, pingChart.Series["UsableIP"]);
                resultBuilder.AppendLine($"Results for Usable IP ({usableIP}):");
                resultBuilder.AppendLine(usableIpResults);
            }
            else if (validGatewayIP)
            {
                string gatewayIpResults = await RunPingAsync(gatewayIP, numPings, packetSize, cancellationTokenSource.Token, pingChart.Series["GatewayIP"]);
                resultBuilder.AppendLine($"Results for Gateway IP ({gatewayIP}):");
                resultBuilder.AppendLine(gatewayIpResults);
            }
            else
            {
                resultBuilder.AppendLine("No valid IP addresses provided.");
            }

            if (checkBoxSaveToLogFile.Checked)
            {
                SavePingOutputToLogFile(resultBuilder.ToString());
            }

            outputTextBox.Text = resultBuilder.ToString();

            // Enable the "Run Ping" button after ping tasks are completed or canceled
            buttonRunPing.Enabled = true;
        }

        private async Task<string> RunPingAsync(string ipAddress, int numPings, int packetSize, CancellationToken cancellationToken, Series series)
        {
            return await Task.Run(() => RunPing(ipAddress, numPings, packetSize, cancellationToken, series), cancellationToken);
        }
        private string RunPing(string ipAddress, int numPings, int packetSize, CancellationToken cancellationToken, Series series)
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

                var reply = pingSender.Send(ipAddress, 1000, new byte[packetSize], pingOptions);

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
            // Check the platform at runtime and update the chart only for Windows
            if (OperatingSystem.IsWindows())
            {
                // Get the appropriate series based on the ipAddress
                series = series == null ? (ipAddress == textBoxUsableIP.Text.Trim() ? pingChart.Series["UsableIP"] : pingChart.Series["GatewayIP"]) : series;

                // Create a new data point with the given X and Y values and add it to the chart series
                DataPoint dataPoint = new DataPoint(pingNumber, responseTime);
                series.Points.Add(dataPoint);
            }
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
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            // Cancel ongoing ping tasks if they exist
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            // Clear the text boxes and output
            textBoxUsableIP.Text = "";
            textBoxGatewayIP.Text = "";
            outputTextBox.Text = "";

            // Clear the graph series data points
            ClearPingChart();
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

        private void InitializeComponent()
        {
            ChartArea chartArea1 = new ChartArea();
            pingChart = new Chart();
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
            textBoxPacketSize = new TextBox();
            labelPacketSize = new Label();
            linkLabel = new LinkLabel();
            ((System.ComponentModel.ISupportInitialize)pingChart).BeginInit();
            SuspendLayout();
            // 
            // pingChart
            // 
            chartArea1.Name = "PingChartArea";
            pingChart.ChartAreas.Add(chartArea1);
            pingChart.Location = new Point(12, 400);
            pingChart.Name = "pingChart";
            pingChart.Size = new Size(400, 245);
            pingChart.TabIndex = 0;
            // 
            // textBoxUsableIP
            // 
            textBoxUsableIP.Location = new Point(125, 7);
            textBoxUsableIP.Margin = new Padding(4, 3, 4, 3);
            textBoxUsableIP.Name = "textBoxUsableIP";
            textBoxUsableIP.Size = new Size(287, 23);
            textBoxUsableIP.TabIndex = 0;
            // 
            // textBoxGatewayIP
            // 
            textBoxGatewayIP.Location = new Point(125, 37);
            textBoxGatewayIP.Margin = new Padding(4, 3, 4, 3);
            textBoxGatewayIP.Name = "textBoxGatewayIP";
            textBoxGatewayIP.Size = new Size(287, 23);
            textBoxGatewayIP.TabIndex = 1;
            // 
            // buttonRunPing
            // 
            buttonRunPing.Location = new Point(13, 163);
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
            buttonReset.Location = new Point(155, 163);
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
            outputTextBox.Location = new Point(12, 204);
            outputTextBox.Margin = new Padding(4, 3, 4, 3);
            outputTextBox.Multiline = true;
            outputTextBox.Name = "outputTextBox";
            outputTextBox.ScrollBars = ScrollBars.Vertical;
            outputTextBox.Size = new Size(400, 184);
            outputTextBox.TabIndex = 4;
            // 
            // labelUsableIP
            // 
            labelUsableIP.AutoSize = true;
            labelUsableIP.Location = new Point(12, 10);
            labelUsableIP.Margin = new Padding(4, 0, 4, 0);
            labelUsableIP.Name = "labelUsableIP";
            labelUsableIP.Size = new Size(58, 15);
            labelUsableIP.TabIndex = 10;
            labelUsableIP.Text = "Usable IP:";
            // 
            // labelGatewayIP
            // 
            labelGatewayIP.AutoSize = true;
            labelGatewayIP.Location = new Point(12, 40);
            labelGatewayIP.Margin = new Padding(4, 0, 4, 0);
            labelGatewayIP.Name = "labelGatewayIP";
            labelGatewayIP.Size = new Size(68, 15);
            labelGatewayIP.TabIndex = 9;
            labelGatewayIP.Text = "Gateway IP:";
            // 
            // labelNumPings
            // 
            labelNumPings.AutoSize = true;
            labelNumPings.Location = new Point(12, 70);
            labelNumPings.Margin = new Padding(4, 0, 4, 0);
            labelNumPings.Name = "labelNumPings";
            labelNumPings.Size = new Size(100, 15);
            labelNumPings.TabIndex = 4;
            labelNumPings.Text = "Number of Pings:";
            // 
            // textBoxNumPings
            // 
            textBoxNumPings.Location = new Point(125, 67);
            textBoxNumPings.Margin = new Padding(4, 3, 4, 3);
            textBoxNumPings.Name = "textBoxNumPings";
            textBoxNumPings.Size = new Size(40, 23);
            textBoxNumPings.TabIndex = 5;
            // 
            // buttonCancel
            // 
            buttonCancel.Location = new Point(295, 163);
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
            checkBoxSlowPings.Location = new Point(13, 138);
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
            checkBoxSaveToLogFile.Location = new Point(116, 138);
            checkBoxSaveToLogFile.Margin = new Padding(4, 3, 4, 3);
            checkBoxSaveToLogFile.Name = "checkBoxSaveToLogFile";
            checkBoxSaveToLogFile.Size = new Size(46, 19);
            checkBoxSaveToLogFile.TabIndex = 8;
            checkBoxSaveToLogFile.Text = "Log";
            checkBoxSaveToLogFile.UseVisualStyleBackColor = true;
            // 
            // textBoxPacketSize
            // 
            textBoxPacketSize.Location = new Point(125, 97);
            textBoxPacketSize.Name = "textBoxPacketSize";
            textBoxPacketSize.Size = new Size(40, 23);
            textBoxPacketSize.TabIndex = 11;
            textBoxPacketSize.Text = "32";
            // 
            // labelPacketSize
            // 
            labelPacketSize.AutoSize = true;
            labelPacketSize.Location = new Point(12, 100);
            labelPacketSize.Name = "labelPacketSize";
            labelPacketSize.Size = new Size(107, 15);
            labelPacketSize.TabIndex = 12;
            labelPacketSize.Text = "Packet Size (bytes):";
            // 
            // linkLabel
            // 
            linkLabel.AutoSize = true;
            linkLabel.Location = new Point(225, 138);
            linkLabel.Name = "linkLabel";
            linkLabel.Size = new Size(185, 15);
            linkLabel.TabIndex = 13;
            linkLabel.TabStop = true;
            linkLabel.Text = "Like it? Donate to the coffee fund!";
            linkLabel.LinkClicked += LinkLabel1_LinkClicked;
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
            Controls.Add(textBoxPacketSize);
            Controls.Add(labelGatewayIP);
            Controls.Add(labelUsableIP);
            Controls.Add(labelNumPings);
            Controls.Add(labelPacketSize);
            Controls.Add(linkLabel);
            Controls.Add(textBoxNumPings);
            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "Ping Tool";
            ((System.ComponentModel.ISupportInitialize)pingChart).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}