using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace PingTool.NET
{
    public partial class Form1 : Form
    {
        private TextBox outputTextBox;
        private TextBox textBoxGatewayIP;
        private TextBox textBoxUsableIP;
        private Button buttonRunPing;
        private Button buttonReset;
        // Declare the labels as class-level fields
        private Label labelUsableIP;
        private Label labelGatewayIP;
        public Form1()
        {
            InitializeComponent(); // Initialize the components on the form
        }

        private void runPingButton_Click(object sender, EventArgs e) // Changed the event handler name to 'buttonRunPing_Click'
        {
            string usableIP = textBoxUsableIP.Text.Trim();
            string gatewayIP = textBoxGatewayIP.Text.Trim();

            // Clear the output text box
            outputTextBox.Text = "Pinging IPs. Please Wait...";

            // Validate the input to ensure at least one valid IP address is provided
            bool validIPProvided = false;
            if (System.Net.IPAddress.TryParse(usableIP, out _))
            {
                validIPProvided = true;
                // Run the script for the usable IP
                outputTextBox.Text += Environment.NewLine + $"Results for Usable IP ({usableIP}):" + Environment.NewLine;
                outputTextBox.Text += RunPing(usableIP);
            }

            if (System.Net.IPAddress.TryParse(gatewayIP, out _))
            {
                validIPProvided = true;
                // Add a separator
                if (outputTextBox.Text.Length > 0)
                {
                    outputTextBox.Text += Environment.NewLine + "=================";
                }
                // Run the script for the gateway IP
                outputTextBox.Text += Environment.NewLine + $"Results for Gateway IP ({gatewayIP}):" + Environment.NewLine;
                outputTextBox.Text += RunPing(gatewayIP);
            }

            if (!validIPProvided)
            {
                // No valid IP address provided
                outputTextBox.Text = "Please enter at least one valid IP address.";
            }
        }

        private string RunPing(string ipAddress)
        {
            string pingResults = "";
            var pingSender = new Ping();
            var pingOptions = new PingOptions();
            var latencies = new List<long>();
            bool timeoutMessageAdded = false;

            for (int i = 0; i < 200; i++)
            {
                var reply = pingSender.Send(ipAddress, 1000, new byte[32], pingOptions);

                if (reply.Status == IPStatus.Success)
                {
                    latencies.Add(reply.RoundtripTime);
                }
                else
                {
                    if (!timeoutMessageAdded)
                    {
                        pingResults += "Request timed out." + Environment.NewLine;
                        timeoutMessageAdded = true;
                    }
                }
            }

            // Calculate and display statistics
            int received = latencies.Count;
            int lost = 200 - received;
            double lossPercentage = (lost / 200.0) * 100.0;

            pingResults += Environment.NewLine + $"Ping statistics for {ipAddress}:" + Environment.NewLine;
            pingResults += $"    Packets: Sent = 200, Received = {received}, Lost = {lost} (Loss Percentage = {lossPercentage:F2}%)" + Environment.NewLine;

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

        private void resetButton_Click(object sender, EventArgs e) // Changed the event handler name to 'buttonReset_Click'
        {
            // Clear the text boxes and output
            textBoxUsableIP.Text = "";
            textBoxGatewayIP.Text = "";
            outputTextBox.Text = "";
        }

        // No changes made to the Form1_Load event handler, so it remains as is

        private void InitializeComponent()
        {
            this.textBoxUsableIP = new System.Windows.Forms.TextBox();
            this.textBoxGatewayIP = new System.Windows.Forms.TextBox();
            this.buttonRunPing = new System.Windows.Forms.Button();
            this.buttonReset = new System.Windows.Forms.Button();
            this.outputTextBox = new System.Windows.Forms.TextBox();
            this.labelUsableIP = new System.Windows.Forms.Label(); // New Label for Usable IP
            this.labelGatewayIP = new System.Windows.Forms.Label(); // New Label for Gateway IP
            this.SuspendLayout();
            // 
            // labelUsableIP
            // 
            this.labelUsableIP.AutoSize = true;
            this.labelUsableIP.Location = new System.Drawing.Point(10, 20);
            this.labelUsableIP.Name = "labelUsableIP";
            this.labelUsableIP.Size = new System.Drawing.Size(64, 13);
            this.labelUsableIP.Text = "Usable IP:";
            // 
            // labelGatewayIP
            // 
            this.labelGatewayIP.AutoSize = true;
            this.labelGatewayIP.Location = new System.Drawing.Point(10, 60);
            this.labelGatewayIP.Name = "labelGatewayIP";
            this.labelGatewayIP.Size = new System.Drawing.Size(75, 13);
            this.labelGatewayIP.Text = "Gateway IP:";
            // 
            // textBoxUsableIP
            // 
            this.textBoxUsableIP.Location = new System.Drawing.Point(200, 20);
            this.textBoxUsableIP.Name = "textBoxUsableIP";
            this.textBoxUsableIP.Size = new System.Drawing.Size(150, 20);
            this.textBoxUsableIP.TabIndex = 0;
            // 
            // textBoxGatewayIP
            // 
            this.textBoxGatewayIP.Location = new System.Drawing.Point(200, 60);
            this.textBoxGatewayIP.Name = "textBoxGatewayIP";
            this.textBoxGatewayIP.Size = new System.Drawing.Size(150, 20);
            this.textBoxGatewayIP.TabIndex = 1;
            // 
            // buttonRunPing
            // 
            this.buttonRunPing.Location = new System.Drawing.Point(10, 100);
            this.buttonRunPing.Name = "buttonRunPing";
            this.buttonRunPing.Size = new System.Drawing.Size(100, 30);
            this.buttonRunPing.TabIndex = 2;
            this.buttonRunPing.Text = "Run Ping";
            this.buttonRunPing.UseVisualStyleBackColor = true;
            this.buttonRunPing.Click += new System.EventHandler(this.runPingButton_Click); // Changed the event handler reference to 'runPingButton_Click'
                                                                                           // 
                                                                                           // buttonReset
                                                                                           // 
            this.buttonReset.Location = new System.Drawing.Point(120, 100);
            this.buttonReset.Name = "buttonReset";
            this.buttonReset.Size = new System.Drawing.Size(100, 30);
            this.buttonReset.TabIndex = 3;
            this.buttonReset.Text = "Reset";
            this.buttonReset.UseVisualStyleBackColor = true;
            this.buttonReset.Click += new System.EventHandler(this.resetButton_Click); // Changed the event handler reference to 'resetButton_Click'
                                                                                       // 
                                                                                       // outputTextBox
                                                                                       // 
            this.outputTextBox.Location = new System.Drawing.Point(10, 140);
            this.outputTextBox.Multiline = true;
            this.outputTextBox.Name = "outputTextBox";
            this.outputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.outputTextBox.Size = new System.Drawing.Size(380, 200);
            this.outputTextBox.TabIndex = 4;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 400);
            this.Controls.Add(this.outputTextBox);
            this.Controls.Add(this.buttonReset);
            this.Controls.Add(this.buttonRunPing);
            this.Controls.Add(this.textBoxGatewayIP);
            this.Controls.Add(this.textBoxUsableIP);
            this.Controls.Add(this.labelGatewayIP); // Add the labelGatewayIP control to the Form1
            this.Controls.Add(this.labelUsableIP); // Add the labelUsableIP control to the Form1
            this.Name = "Form1";
            this.Text = "Ping Tool";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}