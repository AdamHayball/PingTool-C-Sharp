using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Newtonsoft.Json.Linq;

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
        private TabControl tabControl1;
        private TabPage pingToolTab;
        private TabPage cellSignalTab;
        private CheckBox checkBoxLTE;
        private CheckBox checkBox4G;
        private CheckBox checkBox3G;
        private TextBox ssOutputTextbox;
        private TextBox SINRtextBox;
        private TextBox RSRPtextBox;
        private TextBox RSRQtextBox;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private TextBox CINRtextBox;
        private Button runViaButton;
        private TextBox RSSItextBox;
        private TextBox SigStrTextBox;
        private Label label6;
        private Button resetViaButton;
        private TabPage dslSignalTab;
        private Label snrMarginLabel;
        private Button dsResetButton;
        private Button dsRunButton;
        private TextBox dslSignalOutputTextBox;
        private Label loopLengthLabel;
        private Label lineAttLabel;
        private Label transmitPowerLabel;
        private TextBox loopLengthTextBox;
        private TextBox snrMarginTextBox;
        private TextBox transmitPowerTextBox;
        private TextBox lineAttTextBox;
        private Label dslWarningLabel1;
        private Label dslWarningLabel2;
        private TabPage cableSignalTab;
        private Label usPowerLabel;
        private Label dsPowerLabel;
        private Label dsnrLabel;
        private Label snrLabel;
        private TextBox cblSigResultsTextBox;
        private Label merLabel;
        private TextBox merInputTextBox;
        private TextBox usPowerInputTextBox;
        private TextBox dsPowerInputTextBox;
        private TextBox dsnrInputTextBox;
        private TextBox snrPowerInputTextBox;
        private Button cblSigResetButton;
        private Button cblSigRunButton;
        private Label label7;
        private Label label8;
        private TabPage macOuiTab;
        private TextBox macAddrOutputTextBox;
        private TextBox macAddrInputTextbox;
        private Label macAddrLabel;
        private Button macAddrResetButton;
        private Button lookupMacButton;
        private List<Task<string>> pingTasks = new List<Task<string>>(); // Moved pingTasks to class level
        private const string OUIApiBaseUrl = "http://www.macvendorlookup.com/api/v2/";

        public Form1()
        {
            InitializeComponent();

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MinimumSize = new Size(444, 684);
            this.MaximumSize = new Size(444, 684);

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
            try
            {
                return await Task.Run(() => RunPing(ipAddress, numPings, packetSize, cancellationToken, series), cancellationToken);
            }
            catch (PingException ex)
            {
                return $"Error pinging {ipAddress}: {ex.Message}{Environment.NewLine}";
            }
            catch (Exception ex)
            {
                return $"An error occurred while pinging {ipAddress}: {ex.Message}{Environment.NewLine}";
            }
        }
        private string RunPing(string ipAddress, int numPings, int packetSize, CancellationToken cancellationToken, Series series)
        {
            string pingResults = "";
            var pingSender = new Ping();
            var pingOptions = new PingOptions();
            var latencies = new List<long>();
            bool timeoutMessageAdded = false;

            bool errorMessageAdded = false;

            for (int i = 0; i < numPings; i++)
            {
                // Add a delay between pings if the checkbox is checked
                int delay = checkBoxSlowPings.Checked ? 1000 : 0;
                Thread.Sleep(delay); // Add the delay

                // Check if the cancellation has been requested
                if (cancellationToken.IsCancellationRequested)
                {
                    pingResults += "Ping canceled by user.";
                    break;
                }

                try
                {
                    var reply = pingSender.Send(ipAddress, 2000, new byte[packetSize], pingOptions);

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
                catch (PingException ex)
                {
                    if (!errorMessageAdded)
                    {
                        pingResults += $"Error pinging {ipAddress}: {ex.Message}" + Environment.NewLine;
                        errorMessageAdded = true;
                    }
                }
                catch (SocketException ex)
                {
                    if (!errorMessageAdded)
                    {
                        pingResults += $"Error resolving or connecting to {ipAddress}: {ex.Message}" + Environment.NewLine;
                        errorMessageAdded = true;
                    }
                }
                catch (Exception ex)
                {
                    if (!errorMessageAdded)
                    {
                        pingResults += $"An error occurred while pinging {ipAddress}: {ex.Message}" + Environment.NewLine;
                        errorMessageAdded = true;
                    }
                }
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

        private void RunViability_Click(object sender, EventArgs e)
        {
            if (double.TryParse(SigStrTextBox.Text, out double signalStrength) &&
                double.TryParse(RSSItextBox.Text, out double rssi) &&
                double.TryParse(RSRPtextBox.Text, out double rsrp) &&
                double.TryParse(RSRQtextBox.Text, out double rsrq))
            {
                string signalStrengthResult = GetSignalStrengthResult(signalStrength);
                string rssiResult = GetRssiResult(rssi, checkBox3G.Checked, checkBox4G.Checked, checkBoxLTE.Checked);
                string rsrpResult = GetRsrpResult(rsrp);
                string rsrqResult = GetRsrqResult(rsrq);

                string sinrResult = "";
                string cinrResult = "";

                if (!checkBox3G.Checked)
                {
                    if (!checkBox4G.Checked)
                    {
                        double sinrValue;
                        if (double.TryParse(SINRtextBox.Text, out sinrValue))
                        {
                            sinrResult = $"{sinrValue} dB {GetSinrResult(sinrValue)}";
                        }
                    }

                    if (!checkBoxLTE.Checked)
                    {
                        double cinrValue;
                        if (double.TryParse(CINRtextBox.Text, out cinrValue))
                        {
                            cinrResult = $"{cinrValue} dB {GetCinrResult(cinrValue)}";
                        }
                    }
                }

                string technologyText = "Type: ";
                if (checkBox3G.Checked) technologyText += "3G, ";
                if (checkBox4G.Checked) technologyText += "4G, ";
                if (checkBoxLTE.Checked) technologyText += "LTE, ";
                technologyText = technologyText.TrimEnd(',', ' ');

                string output = $"{technologyText}{Environment.NewLine}" +
                                $"Signal Strength: {signalStrength}% {signalStrengthResult}{Environment.NewLine}" +
                                $"RSSI: {rssi} dBm {rssiResult}{Environment.NewLine}" +
                                $"RSRP: {rsrp} dBm {rsrpResult}{Environment.NewLine}" +
                                $"RSRQ: {rsrq} dB {rsrqResult}";

                if (!string.IsNullOrEmpty(sinrResult) && sinrResult != "N/A")
                {
                    output += $"{Environment.NewLine}SINR: {sinrResult}";
                }

                if (!string.IsNullOrEmpty(cinrResult) && cinrResult != "N/A")
                {
                    output += $"{Environment.NewLine}CINR: {cinrResult}";
                }

                ssOutputTextbox.Text = output;
            }
            else
            {
                ssOutputTextbox.Text = "Invalid input. Please enter numeric values.";
            }
        }

        private void CheckBox3G_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3G.Checked)
            {
                checkBox4G.Enabled = false;
                checkBoxLTE.Enabled = false;
                SINRtextBox.Enabled = false;
                CINRtextBox.Enabled = false;
            }
            else
            {
                checkBox4G.Enabled = true;
                checkBoxLTE.Enabled = true;
                SINRtextBox.Enabled = true;
                CINRtextBox.Enabled = true;
            }
        }

        private void CheckBox4G_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4G.Checked)
            {
                checkBox3G.Enabled = false;
                checkBoxLTE.Enabled = false;
                SINRtextBox.Enabled = false;
                CINRtextBox.Enabled = true;
            }
            else
            {
                checkBox3G.Enabled = true;
                checkBoxLTE.Enabled = true;
                SINRtextBox.Enabled = true;
            }
        }

        private void CheckBoxLTE_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLTE.Checked)
            {
                checkBox3G.Enabled = false;
                checkBox4G.Enabled = false;
                SINRtextBox.Enabled = true;
                CINRtextBox.Enabled = false;
            }
            else
            {
                checkBox3G.Enabled = true;
                checkBox4G.Enabled = true;
                CINRtextBox.Enabled = true;
            }
        }

        private string GetSignalStrengthResult(double value)
        {
            if (value <= 0) return "Error! Enter positive number";
            if (value >= 1 && value <= 25) return "Unacceptable";
            if (value >= 26 && value <= 50) return "Borderline";
            if (value >= 51 && value <= 75) return "Good";
            if (value >= 76 && value <= 100) return "Excellent";
            return "Invalid value";
        }

        private string GetRssiResult(double value, bool is3GChecked, bool is4GChecked, bool isLTEChecked)
        {
            if (!is3GChecked && !is4GChecked && !isLTEChecked)
            {
                // Default to 4G/LTE logic
                if (value >= -100 && value <= -80) return "Unacceptable";
                if (value >= -79 && value <= -70) return "Borderline";
                if (value >= -69 && value <= -50) return "Good";
                if (value >= -49 && value <= -40) return "Excellent";
                if (value >= 0) return "Error! Enter negative number";
            }
            else if (is3GChecked)
            {
                if (value >= -100 && value <= -90) return "Unacceptable";
                if (value >= -89 && value <= -80) return "Borderline";
                if (value >= -79 && value <= -60) return "Good";
                if (value >= -59 && value <= -50) return "Excellent";
                if (value >= 0) return "Error! Enter negative number";
            }
            else if (is4GChecked || isLTEChecked)
            {
                if (value >= -100 && value <= -80) return "Unacceptable";
                if (value >= -79 && value <= -70) return "Borderline";
                if (value >= -69 && value <= -50) return "Good";
                if (value >= -49 && value <= -40) return "Excellent";
                if (value >= 0) return "Error! Enter negative number";
            }

            return "Invalid value";
        }

        private string GetRsrpResult(double value)
        {
            if (value >= 0) return "Error! Enter a negative number";
            if (value <= -100) return "Cell Edge";
            if (value <= -90) return "Mid Cell";
            if (value <= -80) return "Good";
            if (value > -80) return "Excellent";
            return "Invalid value";
        }

        private string GetRsrqResult(double value)
        {
            if (value >= 0) return "Error! Enter negative number";
            if (value <= -20) return "Cell Edge";
            if (value >= -15 && value <= -19) return "Mid Cell";
            if (value >= -10 && value <= -14) return "Good";
            if (value >= -10) return "Excellent";
            return "Invalid value";
        }

        private string GetSinrResult(double value)
        {
            if (value <= 0) return "High Interference Noise";
            if (value < 5) return "Unstable";
            if (value >= 6 && value <= 9) return "Signal Dependent";
            if (value >= 10 && value <= 14) return "Good";
            if (value >= 15) return "Excellent";
            return "Invalid value";
        }

        private string GetCinrResult(double value)
        {
            if (value <= 0) return "High Interference Noise";
            if (value < 5) return "Unstable";
            if (value >= 6 && value <= 9) return "Signal Dependent";
            if (value >= 10 && value <= 14) return "Good";
            if (value >= 15) return "Excellent";
            return "Invalid value";
        }

        private void resetViaButton_Click(object sender, EventArgs e)
        {
            // Reset checkboxes
            checkBox3G.Checked = false;
            checkBox4G.Checked = false;
            checkBoxLTE.Checked = false;

            // Clear textboxes
            SigStrTextBox.Clear();
            RSSItextBox.Clear();
            RSRPtextBox.Clear();
            RSRQtextBox.Clear();
            SINRtextBox.Clear();
            CINRtextBox.Clear();

            // Clear output
            ssOutputTextbox.Clear();
        }

        //
        // ADSL Form
        //
        private void dsResetButton_Click(object sender, EventArgs e)
        {
            ClearInputFields();
            ClearOutputField();
        }

        private void ClearInputFields()
        {
            snrMarginTextBox.Clear();
            loopLengthTextBox.Clear();
            lineAttTextBox.Clear();
            transmitPowerTextBox.Clear();
        }
        private void ClearOutputField()
        {
            dslSignalOutputTextBox.Clear();
        }
        private void dsRunButton_Click(object sender, EventArgs e)
        {
            string snrMarginInput = snrMarginTextBox.Text;
            string loopLengthInput = loopLengthTextBox.Text;
            string lineAttenuationInput = lineAttTextBox.Text;
            string transmitPowerInput = transmitPowerTextBox.Text;

            if (string.IsNullOrWhiteSpace(snrMarginInput) ||
            string.IsNullOrWhiteSpace(loopLengthInput) ||
            string.IsNullOrWhiteSpace(lineAttenuationInput) ||
            string.IsNullOrWhiteSpace(transmitPowerInput))
            {
                dslSignalOutputTextBox.Text = "Please enter valid input values.";
                return;
            }

            string snrMarginOutput = GetSNRMarginOutput(snrMarginInput);
            string loopLengthOutput = GetLoopLengthOutput(loopLengthInput);
            string lineAttenuationOutput = GetLineAttenuationOutput(lineAttenuationInput);
            string transmitPowerOutput = GetTransmitPowerOutput(transmitPowerInput);

            string outputText = $"SNR Margin: {snrMarginOutput}\r\n" +
                                $"Loop Length: {loopLengthOutput}\r\n" +
                                $"Line Attenuation: {lineAttenuationOutput}\r\n" +
                                $"Transmit Power: {transmitPowerOutput}";

            dslSignalOutputTextBox.Text = outputText;
        }

        private string GetSNRMarginOutput(string input)
        {
            int snrMarginValue = int.Parse(input);

            string snrMarginUnit = "dB";
            string snrMarginLevel = "";

            if (snrMarginValue < 6)
                snrMarginLevel = "Bad";
            else if (snrMarginValue >= 6 && snrMarginValue <= 10)
                snrMarginLevel = "Fair";
            else if (snrMarginValue > 10 && snrMarginValue <= 15)
                snrMarginLevel = "Good";
            else
                snrMarginLevel = "Excellent";

            return $"{snrMarginValue} {snrMarginUnit} {snrMarginLevel}";
        }

        private string GetLoopLengthOutput(string input)
        {
            double loopLengthValue = double.Parse(input);

            string loopLengthUnit = "mile";
            string loopLengthLevel = "";

            if (loopLengthValue < 1.6)
                loopLengthLevel = "Excellent";
            else if (loopLengthValue >= 1.6 && loopLengthValue <= 3.2)
                loopLengthLevel = "Good";
            else if (loopLengthValue > 3.2)
                loopLengthLevel = "Fair to Poor";
            else
                loopLengthLevel = "Poor";

            return $"{loopLengthValue} {loopLengthUnit} {loopLengthLevel}";
        }

        private string GetLineAttenuationOutput(string input)
        {
            int lineAttenuationValue = int.Parse(input);

            string lineAttenuationUnit = "dB";
            string lineAttenuationLevel = "";

            if (lineAttenuationValue > 60)
                lineAttenuationLevel = "Bad";
            else if (lineAttenuationValue >= 40 && lineAttenuationValue <= 60)
                lineAttenuationLevel = "Fair";
            else if (lineAttenuationValue >= 25 && lineAttenuationValue <= 40)
                lineAttenuationLevel = "Good";
            else
                lineAttenuationLevel = "Excellent";

            return $"{lineAttenuationValue} {lineAttenuationUnit} {lineAttenuationLevel}";
        }

        private string GetTransmitPowerOutput(string input)
        {
            int transmitPowerValue = int.Parse(input);

            string transmitPowerUnit = "dBm";
            string transmitPowerLevel = "";

            if (transmitPowerValue < -20)
                transmitPowerLevel = "Bad";
            else if (transmitPowerValue >= -20 && transmitPowerValue <= -10)
                transmitPowerLevel = "Fair";
            else if (transmitPowerValue >= -10 && transmitPowerValue <= 0)
                transmitPowerLevel = "Good";
            else
                transmitPowerLevel = "Excellent";

            return $"{transmitPowerValue} {transmitPowerUnit} {transmitPowerLevel}";
        }

        //
        // Cable Signal Tab
        //
        private void cblSigRunButton_Click(object sender, EventArgs e)
        {
            // Clear the results textbox
            cblSigResultsTextBox.Clear();

            // Initialize error message
            string errorMessage = "Invalid values entered for the following fields:\r\n";

            // Process SNR
            if (!string.IsNullOrEmpty(snrPowerInputTextBox.Text))
            {
                if (double.TryParse(snrPowerInputTextBox.Text, out double snr))
                {
                    string snrCategory = GetSignalCategory(snr, 25, 30, 35);
                    cblSigResultsTextBox.AppendText($"SNR (US): {snr} dB - {snrCategory}\r\n");
                }
                else
                {
                    errorMessage += "- SNR (US)\r\n";
                }
            }

            // Process DSNR
            if (!string.IsNullOrEmpty(dsnrInputTextBox.Text))
            {
                if (double.TryParse(dsnrInputTextBox.Text, out double dsnr))
                {
                    string dsnrCategory = GetSignalCategory(dsnr, 15, 25, 35);
                    cblSigResultsTextBox.AppendText($"DSNR: {dsnr} dB - {dsnrCategory}\r\n");
                }
                else
                {
                    errorMessage += "- DSNR\r\n";
                }
            }

            // Process Downstream Signal Power
            if (!string.IsNullOrEmpty(dsPowerInputTextBox.Text))
            {
                if (double.TryParse(dsPowerInputTextBox.Text, out double dsPower))
                {
                    string dsPowerCategory = GetSignalCategory(dsPower, -15, 10, 15);
                    cblSigResultsTextBox.AppendText($"Downstream Power: {dsPower} dBmV - {dsPowerCategory}\r\n");
                }
                else
                {
                    errorMessage += "- Downstream Power\r\n";
                }
            }

            // Process Upstream Signal Power
            if (!string.IsNullOrEmpty(usPowerInputTextBox.Text))
            {
                if (double.TryParse(usPowerInputTextBox.Text, out double usPower))
                {
                    string usPowerCategory = GetSignalCategory(usPower, -45, -32, -25);
                    cblSigResultsTextBox.AppendText($"Upstream Power: {usPower} dBmV - {usPowerCategory}\r\n");
                }
                else
                {
                    errorMessage += "- Upstream Power\r\n";
                }
            }

            // Process MER
            if (!string.IsNullOrEmpty(merInputTextBox.Text))
            {
                if (double.TryParse(merInputTextBox.Text, out double mer))
                {
                    string merCategory = GetSignalCategory(mer, 20, 25, 30);
                    cblSigResultsTextBox.AppendText($"MER: {mer} dB - {merCategory}\r\n");
                }
                else
                {
                    errorMessage += "- MER\r\n";
                }
            }

            // Display error message if needed
            if (errorMessage != "Invalid values entered for the following fields:\r\n")
            {
                MessageBox.Show(errorMessage, "Invalid Values", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private string GetSignalCategory(double value, double badThreshold, double fairThreshold, double goodThreshold)
        {
            if (value < badThreshold)
            {
                return "Bad";
            }
            else if (value < fairThreshold)
            {
                return "Fair";
            }
            else if (value < goodThreshold)
            {
                return "Good";
            }
            else
            {
                return "Excellent";
            }
        }

        private void cblSigResetButton_Click(object sender, EventArgs e)
        {
            // Clear input fields and results textbox
            snrPowerInputTextBox.Clear();
            dsnrInputTextBox.Clear();
            dsPowerInputTextBox.Clear();
            usPowerInputTextBox.Clear();
            merInputTextBox.Clear();
            cblSigResultsTextBox.Clear();
        }
        //
        //MAC OUI Lookup Tab
        //
        private async void LookupMacButton_Click(object sender, EventArgs e)
        {
            string macAddress = macAddrInputTextbox.Text;

            if (!string.IsNullOrWhiteSpace(macAddress))
            {
                macAddrOutputTextBox.Text = "Please wait...";
                macAddrOutputTextBox.Refresh(); // Force immediate update

                string result = await PerformOuiLookupAsync(macAddress);

                DisplayLookupResult(result);
            }
            else
            {
                macAddrOutputTextBox.Text = "Please enter a valid MAC address.";
            }
        }
        private async Task<string> PerformOuiLookupAsync(string macAddress)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string apiUrl = $"{OUIApiBaseUrl}{macAddress}";
                    string response = await client.GetStringAsync(apiUrl);
                    return response;
                }
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }

        private void MacAddrResetButton_Click(object sender, EventArgs e)
        {
            macAddrInputTextbox.Text = string.Empty;
            macAddrOutputTextBox.Text = string.Empty;
        }

        private void DisplayLookupResult(string jsonResult)
        {
            try
            {
                // Check for 204 No Content response
                if (string.IsNullOrEmpty(jsonResult))
                {
                    macAddrOutputTextBox.Text = "No information found for the given MAC address.";
                    return;
                }

                JArray jsonArray = JArray.Parse(jsonResult);

                if (jsonArray.Count > 0)
                {
                    JObject jsonObject = (JObject)jsonArray[0];

                    string startHex = jsonObject["startHex"].ToString();
                    string endHex = jsonObject["endHex"].ToString();
                    string company = jsonObject["company"].ToString();
                    string addressL1 = jsonObject["addressL1"].ToString();
                    string addressL2 = jsonObject["addressL2"].ToString();
                    string addressL3 = jsonObject["addressL3"].ToString();
                    string country = jsonObject["country"].ToString();
                    string type = jsonObject["type"].ToString();

                    string formattedResult =
                        $"{company}{Environment.NewLine}" +
                        $"{addressL1}{Environment.NewLine}" +
                        $"{addressL2}{Environment.NewLine}" +
                        $"{addressL3}{Environment.NewLine}" +
                        $"Country: {country}{Environment.NewLine}" +
                        $"Type: {type}{Environment.NewLine}" +
                        $"Start Hex: {startHex}{Environment.NewLine}" +
                        $"End Hex: {endHex}";

                    macAddrOutputTextBox.Text = formattedResult;
                }
                else
                {
                    macAddrOutputTextBox.Text = "No information found for the given MAC address.";
                }
            }
            catch (Exception ex)
            {
                macAddrOutputTextBox.Text = $"Error parsing JSON: {ex.Message}";
            }
        }
        private void InitializeComponent()
        {
            ChartArea chartArea1 = new ChartArea();
            Legend legend1 = new Legend();
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
            tabControl1 = new TabControl();
            pingToolTab = new TabPage();
            cellSignalTab = new TabPage();
            resetViaButton = new Button();
            SigStrTextBox = new TextBox();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            CINRtextBox = new TextBox();
            runViaButton = new Button();
            RSSItextBox = new TextBox();
            ssOutputTextbox = new TextBox();
            SINRtextBox = new TextBox();
            RSRPtextBox = new TextBox();
            RSRQtextBox = new TextBox();
            checkBoxLTE = new CheckBox();
            checkBox4G = new CheckBox();
            checkBox3G = new CheckBox();
            dslSignalTab = new TabPage();
            dslWarningLabel2 = new Label();
            dslWarningLabel1 = new Label();
            transmitPowerTextBox = new TextBox();
            lineAttTextBox = new TextBox();
            loopLengthTextBox = new TextBox();
            snrMarginTextBox = new TextBox();
            transmitPowerLabel = new Label();
            lineAttLabel = new Label();
            loopLengthLabel = new Label();
            snrMarginLabel = new Label();
            dsResetButton = new Button();
            dsRunButton = new Button();
            dslSignalOutputTextBox = new TextBox();
            cableSignalTab = new TabPage();
            label7 = new Label();
            label8 = new Label();
            cblSigResetButton = new Button();
            cblSigRunButton = new Button();
            merInputTextBox = new TextBox();
            usPowerInputTextBox = new TextBox();
            dsPowerInputTextBox = new TextBox();
            dsnrInputTextBox = new TextBox();
            snrPowerInputTextBox = new TextBox();
            merLabel = new Label();
            usPowerLabel = new Label();
            dsPowerLabel = new Label();
            dsnrLabel = new Label();
            snrLabel = new Label();
            cblSigResultsTextBox = new TextBox();
            macOuiTab = new TabPage();
            macAddrResetButton = new Button();
            lookupMacButton = new Button();
            macAddrOutputTextBox = new TextBox();
            macAddrInputTextbox = new TextBox();
            macAddrLabel = new Label();
            ((System.ComponentModel.ISupportInitialize)pingChart).BeginInit();
            tabControl1.SuspendLayout();
            pingToolTab.SuspendLayout();
            cellSignalTab.SuspendLayout();
            dslSignalTab.SuspendLayout();
            cableSignalTab.SuspendLayout();
            macOuiTab.SuspendLayout();
            SuspendLayout();
            // 
            // pingChart
            // 
            chartArea1.Name = "PingChartArea";
            pingChart.ChartAreas.Add(chartArea1);
            legend1.Alignment = StringAlignment.Center;
            legend1.Docking = Docking.Bottom;
            legend1.LegendStyle = LegendStyle.Row;
            legend1.Name = "Legend";
            pingChart.Legends.Add(legend1);
            pingChart.Location = new Point(10, 367);
            pingChart.Name = "pingChart";
            pingChart.Size = new Size(400, 245);
            pingChart.TabIndex = 0;
            // 
            // textBoxUsableIP
            // 
            textBoxUsableIP.Location = new Point(121, 19);
            textBoxUsableIP.Margin = new Padding(4, 3, 4, 3);
            textBoxUsableIP.Name = "textBoxUsableIP";
            textBoxUsableIP.Size = new Size(287, 23);
            textBoxUsableIP.TabIndex = 0;
            // 
            // textBoxGatewayIP
            // 
            textBoxGatewayIP.Location = new Point(122, 48);
            textBoxGatewayIP.Margin = new Padding(4, 3, 4, 3);
            textBoxGatewayIP.Name = "textBoxGatewayIP";
            textBoxGatewayIP.Size = new Size(287, 23);
            textBoxGatewayIP.TabIndex = 1;
            // 
            // buttonRunPing
            // 
            buttonRunPing.Location = new Point(10, 137);
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
            buttonReset.Location = new Point(152, 137);
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
            outputTextBox.Location = new Point(9, 178);
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
            labelUsableIP.Location = new Point(10, 22);
            labelUsableIP.Margin = new Padding(4, 0, 4, 0);
            labelUsableIP.Name = "labelUsableIP";
            labelUsableIP.Size = new Size(58, 15);
            labelUsableIP.TabIndex = 10;
            labelUsableIP.Text = "Usable IP:";
            // 
            // labelGatewayIP
            // 
            labelGatewayIP.AutoSize = true;
            labelGatewayIP.Location = new Point(10, 51);
            labelGatewayIP.Margin = new Padding(4, 0, 4, 0);
            labelGatewayIP.Name = "labelGatewayIP";
            labelGatewayIP.Size = new Size(68, 15);
            labelGatewayIP.TabIndex = 9;
            labelGatewayIP.Text = "Gateway IP:";
            // 
            // labelNumPings
            // 
            labelNumPings.AutoSize = true;
            labelNumPings.Location = new Point(9, 80);
            labelNumPings.Margin = new Padding(4, 0, 4, 0);
            labelNumPings.Name = "labelNumPings";
            labelNumPings.Size = new Size(100, 15);
            labelNumPings.TabIndex = 4;
            labelNumPings.Text = "Number of Pings:";
            // 
            // textBoxNumPings
            // 
            textBoxNumPings.Location = new Point(122, 77);
            textBoxNumPings.Margin = new Padding(4, 3, 4, 3);
            textBoxNumPings.Name = "textBoxNumPings";
            textBoxNumPings.Size = new Size(40, 23);
            textBoxNumPings.TabIndex = 5;
            // 
            // buttonCancel
            // 
            buttonCancel.Location = new Point(292, 137);
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
            checkBoxSlowPings.Location = new Point(170, 79);
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
            checkBoxSaveToLogFile.Location = new Point(261, 77);
            checkBoxSaveToLogFile.Margin = new Padding(4, 3, 4, 3);
            checkBoxSaveToLogFile.Name = "checkBoxSaveToLogFile";
            checkBoxSaveToLogFile.Size = new Size(46, 19);
            checkBoxSaveToLogFile.TabIndex = 8;
            checkBoxSaveToLogFile.Text = "Log";
            checkBoxSaveToLogFile.UseVisualStyleBackColor = true;
            // 
            // textBoxPacketSize
            // 
            textBoxPacketSize.Location = new Point(122, 104);
            textBoxPacketSize.Name = "textBoxPacketSize";
            textBoxPacketSize.Size = new Size(40, 23);
            textBoxPacketSize.TabIndex = 11;
            textBoxPacketSize.Text = "32";
            // 
            // labelPacketSize
            // 
            labelPacketSize.AutoSize = true;
            labelPacketSize.Location = new Point(9, 112);
            labelPacketSize.Name = "labelPacketSize";
            labelPacketSize.Size = new Size(107, 15);
            labelPacketSize.TabIndex = 12;
            labelPacketSize.Text = "Packet Size (bytes):";
            // 
            // linkLabel
            // 
            linkLabel.AutoSize = true;
            linkLabel.Location = new Point(314, 78);
            linkLabel.Name = "linkLabel";
            linkLabel.Size = new Size(96, 15);
            linkLabel.TabIndex = 13;
            linkLabel.TabStop = true;
            linkLabel.Text = "Buy Me A Coffee";
            linkLabel.LinkClicked += LinkLabel1_LinkClicked;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(pingToolTab);
            tabControl1.Controls.Add(cellSignalTab);
            tabControl1.Controls.Add(dslSignalTab);
            tabControl1.Controls.Add(cableSignalTab);
            tabControl1.Controls.Add(macOuiTab);
            tabControl1.Location = new Point(1, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(426, 648);
            tabControl1.TabIndex = 1;
            // 
            // pingToolTab
            // 
            pingToolTab.Controls.Add(pingChart);
            pingToolTab.Controls.Add(textBoxUsableIP);
            pingToolTab.Controls.Add(buttonCancel);
            pingToolTab.Controls.Add(textBoxNumPings);
            pingToolTab.Controls.Add(checkBoxSlowPings);
            pingToolTab.Controls.Add(linkLabel);
            pingToolTab.Controls.Add(checkBoxSaveToLogFile);
            pingToolTab.Controls.Add(labelPacketSize);
            pingToolTab.Controls.Add(outputTextBox);
            pingToolTab.Controls.Add(labelNumPings);
            pingToolTab.Controls.Add(buttonReset);
            pingToolTab.Controls.Add(labelUsableIP);
            pingToolTab.Controls.Add(buttonRunPing);
            pingToolTab.Controls.Add(labelGatewayIP);
            pingToolTab.Controls.Add(textBoxGatewayIP);
            pingToolTab.Controls.Add(textBoxPacketSize);
            pingToolTab.Location = new Point(4, 24);
            pingToolTab.Name = "pingToolTab";
            pingToolTab.Padding = new Padding(3);
            pingToolTab.Size = new Size(418, 620);
            pingToolTab.TabIndex = 0;
            pingToolTab.Text = "Ping";
            pingToolTab.UseVisualStyleBackColor = true;
            // 
            // cellSignalTab
            // 
            cellSignalTab.Controls.Add(resetViaButton);
            cellSignalTab.Controls.Add(SigStrTextBox);
            cellSignalTab.Controls.Add(label6);
            cellSignalTab.Controls.Add(label5);
            cellSignalTab.Controls.Add(label4);
            cellSignalTab.Controls.Add(label3);
            cellSignalTab.Controls.Add(label2);
            cellSignalTab.Controls.Add(label1);
            cellSignalTab.Controls.Add(CINRtextBox);
            cellSignalTab.Controls.Add(runViaButton);
            cellSignalTab.Controls.Add(RSSItextBox);
            cellSignalTab.Controls.Add(ssOutputTextbox);
            cellSignalTab.Controls.Add(SINRtextBox);
            cellSignalTab.Controls.Add(RSRPtextBox);
            cellSignalTab.Controls.Add(RSRQtextBox);
            cellSignalTab.Controls.Add(checkBoxLTE);
            cellSignalTab.Controls.Add(checkBox4G);
            cellSignalTab.Controls.Add(checkBox3G);
            cellSignalTab.Location = new Point(4, 24);
            cellSignalTab.Name = "cellSignalTab";
            cellSignalTab.Padding = new Padding(3);
            cellSignalTab.Size = new Size(418, 620);
            cellSignalTab.TabIndex = 1;
            cellSignalTab.Text = "Cell";
            cellSignalTab.UseVisualStyleBackColor = true;
            // 
            // resetViaButton
            // 
            resetViaButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            resetViaButton.Location = new Point(253, 155);
            resetViaButton.Name = "resetViaButton";
            resetViaButton.Size = new Size(147, 32);
            resetViaButton.TabIndex = 10;
            resetViaButton.Text = "Reset";
            resetViaButton.UseVisualStyleBackColor = true;
            resetViaButton.Click += resetViaButton_Click;
            // 
            // SigStrTextBox
            // 
            SigStrTextBox.Location = new Point(103, 23);
            SigStrTextBox.Name = "SigStrTextBox";
            SigStrTextBox.Size = new Size(100, 23);
            SigStrTextBox.TabIndex = 3;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(6, 23);
            label6.Name = "label6";
            label6.Size = new Size(87, 15);
            label6.TabIndex = 15;
            label6.Text = "Signal Strength";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(6, 166);
            label5.Name = "label5";
            label5.Size = new Size(87, 15);
            label5.TabIndex = 14;
            label5.Text = "CINR (4G Only)";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(6, 137);
            label4.Name = "label4";
            label4.Size = new Size(88, 15);
            label4.TabIndex = 13;
            label4.Text = "SINR (LTE Only)";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(6, 108);
            label3.Name = "label3";
            label3.Size = new Size(34, 15);
            label3.TabIndex = 12;
            label3.Text = "RSRP";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 79);
            label2.Name = "label2";
            label2.Size = new Size(36, 15);
            label2.TabIndex = 11;
            label2.Text = "RSRQ";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 52);
            label1.Name = "label1";
            label1.Size = new Size(29, 15);
            label1.TabIndex = 10;
            label1.Text = "RSSI";
            // 
            // CINRtextBox
            // 
            CINRtextBox.Location = new Point(103, 166);
            CINRtextBox.Name = "CINRtextBox";
            CINRtextBox.Size = new Size(100, 23);
            CINRtextBox.TabIndex = 8;
            // 
            // runViaButton
            // 
            runViaButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            runViaButton.Location = new Point(253, 120);
            runViaButton.Name = "runViaButton";
            runViaButton.Size = new Size(147, 32);
            runViaButton.TabIndex = 9;
            runViaButton.Text = "Run";
            runViaButton.UseVisualStyleBackColor = true;
            runViaButton.Click += RunViability_Click;
            // 
            // RSSItextBox
            // 
            RSSItextBox.AccessibleName = "RSSI";
            RSSItextBox.Location = new Point(103, 52);
            RSSItextBox.Name = "RSSItextBox";
            RSSItextBox.Size = new Size(100, 23);
            RSSItextBox.TabIndex = 4;
            // 
            // ssOutputTextbox
            // 
            ssOutputTextbox.Location = new Point(6, 210);
            ssOutputTextbox.Multiline = true;
            ssOutputTextbox.Name = "ssOutputTextbox";
            ssOutputTextbox.Size = new Size(406, 404);
            ssOutputTextbox.TabIndex = 11;
            // 
            // SINRtextBox
            // 
            SINRtextBox.Location = new Point(103, 137);
            SINRtextBox.Name = "SINRtextBox";
            SINRtextBox.Size = new Size(100, 23);
            SINRtextBox.TabIndex = 7;
            // 
            // RSRPtextBox
            // 
            RSRPtextBox.Location = new Point(103, 108);
            RSRPtextBox.Name = "RSRPtextBox";
            RSRPtextBox.Size = new Size(100, 23);
            RSRPtextBox.TabIndex = 6;
            // 
            // RSRQtextBox
            // 
            RSRQtextBox.Location = new Point(103, 79);
            RSRQtextBox.Name = "RSRQtextBox";
            RSRQtextBox.Size = new Size(100, 23);
            RSRQtextBox.TabIndex = 5;
            // 
            // checkBoxLTE
            // 
            checkBoxLTE.AutoSize = true;
            checkBoxLTE.Location = new Point(253, 83);
            checkBoxLTE.Name = "checkBoxLTE";
            checkBoxLTE.Size = new Size(43, 19);
            checkBoxLTE.TabIndex = 2;
            checkBoxLTE.Text = "LTE";
            checkBoxLTE.UseVisualStyleBackColor = true;
            checkBoxLTE.CheckedChanged += CheckBoxLTE_CheckedChanged;
            // 
            // checkBox4G
            // 
            checkBox4G.AutoSize = true;
            checkBox4G.Location = new Point(253, 56);
            checkBox4G.Name = "checkBox4G";
            checkBox4G.Size = new Size(80, 19);
            checkBox4G.TabIndex = 1;
            checkBox4G.Text = "4G WiMax";
            checkBox4G.UseVisualStyleBackColor = true;
            checkBox4G.CheckedChanged += CheckBox4G_CheckedChanged;
            // 
            // checkBox3G
            // 
            checkBox3G.AutoSize = true;
            checkBox3G.Location = new Point(253, 27);
            checkBox3G.Name = "checkBox3G";
            checkBox3G.Size = new Size(73, 19);
            checkBox3G.TabIndex = 0;
            checkBox3G.Text = "3G EVDO";
            checkBox3G.UseVisualStyleBackColor = true;
            checkBox3G.CheckedChanged += CheckBox3G_CheckedChanged;
            // 
            // dslSignalTab
            // 
            dslSignalTab.Controls.Add(dslWarningLabel2);
            dslSignalTab.Controls.Add(dslWarningLabel1);
            dslSignalTab.Controls.Add(transmitPowerTextBox);
            dslSignalTab.Controls.Add(lineAttTextBox);
            dslSignalTab.Controls.Add(loopLengthTextBox);
            dslSignalTab.Controls.Add(snrMarginTextBox);
            dslSignalTab.Controls.Add(transmitPowerLabel);
            dslSignalTab.Controls.Add(lineAttLabel);
            dslSignalTab.Controls.Add(loopLengthLabel);
            dslSignalTab.Controls.Add(snrMarginLabel);
            dslSignalTab.Controls.Add(dsResetButton);
            dslSignalTab.Controls.Add(dsRunButton);
            dslSignalTab.Controls.Add(dslSignalOutputTextBox);
            dslSignalTab.Location = new Point(4, 24);
            dslSignalTab.Name = "dslSignalTab";
            dslSignalTab.Padding = new Padding(3);
            dslSignalTab.Size = new Size(418, 620);
            dslSignalTab.TabIndex = 2;
            dslSignalTab.Text = "DSL";
            dslSignalTab.UseVisualStyleBackColor = true;
            // 
            // dslWarningLabel2
            // 
            dslWarningLabel2.AutoSize = true;
            dslWarningLabel2.Location = new Point(76, 161);
            dslWarningLabel2.Name = "dslWarningLabel2";
            dslWarningLabel2.Size = new Size(271, 15);
            dslWarningLabel2.TabIndex = 12;
            dslWarningLabel2.Text = "Actual values will vary by carrier and specification.";
            // 
            // dslWarningLabel1
            // 
            dslWarningLabel1.AutoSize = true;
            dslWarningLabel1.Location = new Point(50, 146);
            dslWarningLabel1.Name = "dslWarningLabel1";
            dslWarningLabel1.Size = new Size(319, 15);
            dslWarningLabel1.TabIndex = 11;
            dslWarningLabel1.Text = "***WARNING*** These values are for a generic ADSL circuit.";
            // 
            // transmitPowerTextBox
            // 
            transmitPowerTextBox.Location = new Point(108, 110);
            transmitPowerTextBox.Name = "transmitPowerTextBox";
            transmitPowerTextBox.Size = new Size(100, 23);
            transmitPowerTextBox.TabIndex = 10;
            // 
            // lineAttTextBox
            // 
            lineAttTextBox.Location = new Point(108, 78);
            lineAttTextBox.Name = "lineAttTextBox";
            lineAttTextBox.Size = new Size(100, 23);
            lineAttTextBox.TabIndex = 9;
            // 
            // loopLengthTextBox
            // 
            loopLengthTextBox.Location = new Point(108, 49);
            loopLengthTextBox.Name = "loopLengthTextBox";
            loopLengthTextBox.Size = new Size(100, 23);
            loopLengthTextBox.TabIndex = 8;
            // 
            // snrMarginTextBox
            // 
            snrMarginTextBox.Location = new Point(108, 21);
            snrMarginTextBox.Name = "snrMarginTextBox";
            snrMarginTextBox.Size = new Size(100, 23);
            snrMarginTextBox.TabIndex = 7;
            // 
            // transmitPowerLabel
            // 
            transmitPowerLabel.AutoSize = true;
            transmitPowerLabel.Location = new Point(7, 113);
            transmitPowerLabel.Name = "transmitPowerLabel";
            transmitPowerLabel.Size = new Size(88, 15);
            transmitPowerLabel.TabIndex = 6;
            transmitPowerLabel.Text = "Transmit Power";
            // 
            // lineAttLabel
            // 
            lineAttLabel.AutoSize = true;
            lineAttLabel.Location = new Point(7, 81);
            lineAttLabel.Name = "lineAttLabel";
            lineAttLabel.Size = new Size(95, 15);
            lineAttLabel.TabIndex = 5;
            lineAttLabel.Text = "Line Attenuation";
            // 
            // loopLengthLabel
            // 
            loopLengthLabel.AutoSize = true;
            loopLengthLabel.Location = new Point(7, 52);
            loopLengthLabel.Name = "loopLengthLabel";
            loopLengthLabel.Size = new Size(74, 15);
            loopLengthLabel.TabIndex = 4;
            loopLengthLabel.Text = "Loop Length";
            // 
            // snrMarginLabel
            // 
            snrMarginLabel.AutoSize = true;
            snrMarginLabel.Location = new Point(7, 24);
            snrMarginLabel.Name = "snrMarginLabel";
            snrMarginLabel.Size = new Size(70, 15);
            snrMarginLabel.TabIndex = 3;
            snrMarginLabel.Text = "SNR Margin";
            // 
            // dsResetButton
            // 
            dsResetButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            dsResetButton.Location = new Point(238, 89);
            dsResetButton.Name = "dsResetButton";
            dsResetButton.Size = new Size(147, 32);
            dsResetButton.TabIndex = 2;
            dsResetButton.Text = "Reset";
            dsResetButton.UseVisualStyleBackColor = true;
            dsResetButton.Click += dsResetButton_Click;
            // 
            // dsRunButton
            // 
            dsRunButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            dsRunButton.Location = new Point(238, 34);
            dsRunButton.Name = "dsRunButton";
            dsRunButton.Size = new Size(147, 32);
            dsRunButton.TabIndex = 1;
            dsRunButton.Text = "Run";
            dsRunButton.UseVisualStyleBackColor = true;
            dsRunButton.Click += dsRunButton_Click;
            // 
            // dslSignalOutputTextBox
            // 
            dslSignalOutputTextBox.Location = new Point(7, 183);
            dslSignalOutputTextBox.Multiline = true;
            dslSignalOutputTextBox.Name = "dslSignalOutputTextBox";
            dslSignalOutputTextBox.Size = new Size(400, 431);
            dslSignalOutputTextBox.TabIndex = 0;
            // 
            // cableSignalTab
            // 
            cableSignalTab.Controls.Add(label7);
            cableSignalTab.Controls.Add(label8);
            cableSignalTab.Controls.Add(cblSigResetButton);
            cableSignalTab.Controls.Add(cblSigRunButton);
            cableSignalTab.Controls.Add(merInputTextBox);
            cableSignalTab.Controls.Add(usPowerInputTextBox);
            cableSignalTab.Controls.Add(dsPowerInputTextBox);
            cableSignalTab.Controls.Add(dsnrInputTextBox);
            cableSignalTab.Controls.Add(snrPowerInputTextBox);
            cableSignalTab.Controls.Add(merLabel);
            cableSignalTab.Controls.Add(usPowerLabel);
            cableSignalTab.Controls.Add(dsPowerLabel);
            cableSignalTab.Controls.Add(dsnrLabel);
            cableSignalTab.Controls.Add(snrLabel);
            cableSignalTab.Controls.Add(cblSigResultsTextBox);
            cableSignalTab.Location = new Point(4, 24);
            cableSignalTab.Name = "cableSignalTab";
            cableSignalTab.Padding = new Padding(3);
            cableSignalTab.Size = new Size(418, 620);
            cableSignalTab.TabIndex = 3;
            cableSignalTab.Text = "Cable";
            cableSignalTab.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(77, 174);
            label7.Name = "label7";
            label7.Size = new Size(271, 15);
            label7.TabIndex = 26;
            label7.Text = "Actual values will vary by carrier and specification.";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(60, 159);
            label8.Name = "label8";
            label8.Size = new Size(306, 15);
            label8.TabIndex = 25;
            label8.Text = "***WARNING*** These values are for DOCIS 3.1 modems.";
            // 
            // cblSigResetButton
            // 
            cblSigResetButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            cblSigResetButton.Location = new Point(236, 95);
            cblSigResetButton.Name = "cblSigResetButton";
            cblSigResetButton.Size = new Size(147, 32);
            cblSigResetButton.TabIndex = 24;
            cblSigResetButton.Text = "Reset";
            cblSigResetButton.UseVisualStyleBackColor = true;
            cblSigResetButton.Click += cblSigResetButton_Click;
            // 
            // cblSigRunButton
            // 
            cblSigRunButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            cblSigRunButton.Location = new Point(236, 40);
            cblSigRunButton.Name = "cblSigRunButton";
            cblSigRunButton.Size = new Size(147, 32);
            cblSigRunButton.TabIndex = 23;
            cblSigRunButton.Text = "Run";
            cblSigRunButton.UseVisualStyleBackColor = true;
            cblSigRunButton.Click += cblSigRunButton_Click;
            // 
            // merInputTextBox
            // 
            merInputTextBox.Location = new Point(97, 133);
            merInputTextBox.Name = "merInputTextBox";
            merInputTextBox.Size = new Size(100, 23);
            merInputTextBox.TabIndex = 22;
            // 
            // usPowerInputTextBox
            // 
            usPowerInputTextBox.Location = new Point(97, 104);
            usPowerInputTextBox.Name = "usPowerInputTextBox";
            usPowerInputTextBox.Size = new Size(100, 23);
            usPowerInputTextBox.TabIndex = 21;
            // 
            // dsPowerInputTextBox
            // 
            dsPowerInputTextBox.Location = new Point(97, 75);
            dsPowerInputTextBox.Name = "dsPowerInputTextBox";
            dsPowerInputTextBox.Size = new Size(100, 23);
            dsPowerInputTextBox.TabIndex = 20;
            // 
            // dsnrInputTextBox
            // 
            dsnrInputTextBox.Location = new Point(97, 46);
            dsnrInputTextBox.Name = "dsnrInputTextBox";
            dsnrInputTextBox.Size = new Size(100, 23);
            dsnrInputTextBox.TabIndex = 19;
            // 
            // snrPowerInputTextBox
            // 
            snrPowerInputTextBox.Location = new Point(97, 17);
            snrPowerInputTextBox.Name = "snrPowerInputTextBox";
            snrPowerInputTextBox.Size = new Size(100, 23);
            snrPowerInputTextBox.TabIndex = 18;
            // 
            // merLabel
            // 
            merLabel.AutoSize = true;
            merLabel.Location = new Point(7, 136);
            merLabel.Name = "merLabel";
            merLabel.Size = new Size(31, 15);
            merLabel.TabIndex = 17;
            merLabel.Text = "MER";
            // 
            // usPowerLabel
            // 
            usPowerLabel.AutoSize = true;
            usPowerLabel.Location = new Point(7, 107);
            usPowerLabel.Name = "usPowerLabel";
            usPowerLabel.Size = new Size(79, 15);
            usPowerLabel.TabIndex = 16;
            usPowerLabel.Text = "US Power (Tx)";
            // 
            // dsPowerLabel
            // 
            dsPowerLabel.AutoSize = true;
            dsPowerLabel.Location = new Point(7, 78);
            dsPowerLabel.Name = "dsPowerLabel";
            dsPowerLabel.Size = new Size(81, 15);
            dsPowerLabel.TabIndex = 15;
            dsPowerLabel.Text = "DS Power (Rx)";
            // 
            // dsnrLabel
            // 
            dsnrLabel.AutoSize = true;
            dsnrLabel.Location = new Point(7, 49);
            dsnrLabel.Name = "dsnrLabel";
            dsnrLabel.Size = new Size(54, 15);
            dsnrLabel.TabIndex = 14;
            dsnrLabel.Text = "SNR (DS)";
            // 
            // snrLabel
            // 
            snrLabel.AutoSize = true;
            snrLabel.Location = new Point(7, 20);
            snrLabel.Name = "snrLabel";
            snrLabel.Size = new Size(54, 15);
            snrLabel.TabIndex = 13;
            snrLabel.Text = "SNR (US)";
            // 
            // cblSigResultsTextBox
            // 
            cblSigResultsTextBox.Location = new Point(6, 192);
            cblSigResultsTextBox.Multiline = true;
            cblSigResultsTextBox.Name = "cblSigResultsTextBox";
            cblSigResultsTextBox.Size = new Size(406, 425);
            cblSigResultsTextBox.TabIndex = 12;
            // 
            // macOuiTab
            // 
            macOuiTab.Controls.Add(macAddrResetButton);
            macOuiTab.Controls.Add(lookupMacButton);
            macOuiTab.Controls.Add(macAddrOutputTextBox);
            macOuiTab.Controls.Add(macAddrInputTextbox);
            macOuiTab.Controls.Add(macAddrLabel);
            macOuiTab.Location = new Point(4, 24);
            macOuiTab.Name = "macOuiTab";
            macOuiTab.Padding = new Padding(3);
            macOuiTab.Size = new Size(418, 620);
            macOuiTab.TabIndex = 4;
            macOuiTab.Text = "MAC OUI";
            macOuiTab.UseVisualStyleBackColor = true;
            // 
            // macAddrResetButton
            // 
            macAddrResetButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            macAddrResetButton.Location = new Point(250, 48);
            macAddrResetButton.Name = "macAddrResetButton";
            macAddrResetButton.Size = new Size(147, 32);
            macAddrResetButton.TabIndex = 26;
            macAddrResetButton.Text = "Reset";
            macAddrResetButton.UseVisualStyleBackColor = true;
            macAddrResetButton.Click += MacAddrResetButton_Click;
            // 
            // lookupMacButton
            // 
            lookupMacButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            lookupMacButton.Location = new Point(97, 48);
            lookupMacButton.Name = "lookupMacButton";
            lookupMacButton.Size = new Size(147, 32);
            lookupMacButton.TabIndex = 25;
            lookupMacButton.Text = "Lookup MAC";
            lookupMacButton.UseVisualStyleBackColor = true;
            lookupMacButton.Click += LookupMacButton_Click;
            // 
            // macAddrOutputTextBox
            // 
            macAddrOutputTextBox.Location = new Point(6, 86);
            macAddrOutputTextBox.Multiline = true;
            macAddrOutputTextBox.Name = "macAddrOutputTextBox";
            macAddrOutputTextBox.Size = new Size(406, 528);
            macAddrOutputTextBox.TabIndex = 13;
            // 
            // macAddrInputTextbox
            // 
            macAddrInputTextbox.Location = new Point(88, 19);
            macAddrInputTextbox.Name = "macAddrInputTextbox";
            macAddrInputTextbox.Size = new Size(324, 23);
            macAddrInputTextbox.TabIndex = 1;
            // 
            // macAddrLabel
            // 
            macAddrLabel.AutoSize = true;
            macAddrLabel.Location = new Point(3, 22);
            macAddrLabel.Name = "macAddrLabel";
            macAddrLabel.Size = new Size(79, 15);
            macAddrLabel.TabIndex = 0;
            macAddrLabel.Text = "MAC Address";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(424, 660);
            Controls.Add(tabControl1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "Ping Tool";
            ((System.ComponentModel.ISupportInitialize)pingChart).EndInit();
            tabControl1.ResumeLayout(false);
            pingToolTab.ResumeLayout(false);
            pingToolTab.PerformLayout();
            cellSignalTab.ResumeLayout(false);
            cellSignalTab.PerformLayout();
            dslSignalTab.ResumeLayout(false);
            dslSignalTab.PerformLayout();
            cableSignalTab.ResumeLayout(false);
            cableSignalTab.PerformLayout();
            macOuiTab.ResumeLayout(false);
            macOuiTab.PerformLayout();
            ResumeLayout(false);
        }
    }
}