using RFExplorerNET.RFExplorerCommunicator;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFExporter.UI.Data
{
    public class ScanningService
    {
        public List<string> Log { get; private set; }


        private RFExplorerNET.RFExplorerCommunicator.RFECommunicator rfe;
        private CancellationTokenSource cancellationToken;

        public bool IsConnecting { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsScanning { get; private set; }
        public bool HasCompleteScan { get; private set; }

        private bool IsReady = false;
        private bool collectingData = false;
        private int sampleCount = 2;

        public List<ScanBlock> ScanBlocks = new List<ScanBlock>();
        private int currentBlock = 0;

        public int TotalBlocks
        { 
            get
            {
                return ScanBlocks.Count();
            }
        }
        public int ScannedBlocks 
        {  
            get
            {
                return ScanBlocks.Count(b => b.Status == ScanBlock.BlockStatus.Scanned);
            }
        }

        private List<float[]> AmplitudeData = new List<float[]>();

        // Device Info
        public string DeviceFirmware { get; private set; }
        public string DeviceSerialNumber { get; private set; }
        public string DeviceModel { get; private set; }

        public ScanningService()
        { 
            Log = new List<string>();

            IsConnected = false;
            IsScanning = false;
            HasCompleteScan = false;

            WriteLog("Scanning service initialised");
        }

        public string GetCSVData()
        {
            string output = "";
            foreach(var sb in ScanBlocks)
            {
                foreach(var sd in sb.ScanData)
                {
                    output += sd.Frequency.ToString() + "," + sd.AverageAmplitudeDBM.ToString() + Environment.NewLine;
                }
            }    
            return output;
        }

        public Task<string[]> GetAvailablePorts()
        {
            WriteLog("Finding available ports");
            string[] ports = SerialPort.GetPortNames();

            WriteLog("Found: " + String.Join(",", ports));

            return Task.FromResult(ports);
        }

        private void PollingTask()
        {
            WriteLog("Starting Polling");
            string dummy;
            while (!cancellationToken.Token.IsCancellationRequested)
            {
                rfe.ProcessReceivedString(true, out dummy);
                Thread.Sleep(10);
            }
            WriteLog("Stopping Polling");
        }

        public Task Connect(string port)
        {
            WriteLog("Connecting...");
            IsConnecting = true;

            rfe = new RFExplorerNET.RFExplorerCommunicator.RFECommunicator(true);
            rfe.ConnectPort(port, 500000);

            // Spin off a thread to watch for serial data
            cancellationToken = new CancellationTokenSource();
            new Task(() => PollingTask(), cancellationToken.Token, TaskCreationOptions.LongRunning).Start();
        
            // Set up events
            rfe.DeviceReset += Rfe_DeviceReset;
            rfe.ReceivedConfigurationDataEvent += Rfe_ReceivedConfigurationDataEvent;
            rfe.UpdateDataEvent += Rfe_UpdateDataEvent;

            WriteLog("Sending Reset...");
            rfe.SendCommand("r");

            return Task.CompletedTask;
        }

        private void Rfe_UpdateDataEvent(object sender, EventArgs e)
        {

            if (IsScanning && collectingData)
            {
                // Get the sweep data object
                RFESweepData sweepData = rfe.SweepData.GetData(rfe.SweepData.Count - 1);

                // Sanity check that this scan data is from the range requested
                if (
                    Math.Round(ScanBlocks[currentBlock].StartFrequency) ==
                    Math.Round(sweepData.StartFrequencyMHZ)
                    )
                {
                    // Build the amplitude data
                    List<float> ampData = new List<float>();
                    for (ushort p = 0; p < sweepData.TotalSteps - 1; p++)
                    {
                        ampData.Add(sweepData.GetAmplitudeDBM(p));
                    }

                    // Add it to the block data
                    ScanBlocks[currentBlock].AmplitudeData.Add(ampData.ToArray());

                    // If we have enough scans, move on
                    if(ScanBlocks[currentBlock].AmplitudeData.Count >= sampleCount)
                    {
                        ScanBlocks[currentBlock].Status = ScanBlock.BlockStatus.Scanned;
                        WriteLog("Block " + currentBlock + " complete");

                        WriteLog("Count: " + ScanBlocks.Count);
                        // Complete scan
                        if (currentBlock >= ScanBlocks.Count - 1)
                        {
                            WriteLog("Scan Complete");
                            IsScanning = false;
                            collectingData = false;
                            HasCompleteScan = true;
                        } else {
                            currentBlock++;
                            RangeAndScanBlock(currentBlock);
                        }
                    }
                }
            }
        }

        private void Rfe_DeviceReset(object sender, EventArgs e)
        {
            WriteLog("Received Reset ACK");
            WriteLog("Waiting for boot to complete...");
            // Wait for boot
            for (int i = 10; i > 0; i--)
            {
                Thread.Sleep(1000);
                WriteLog(i.ToString() + "...");
            }
            WriteLog("Connected");
            WriteLog("Requesting Config");
          
            rfe.SendCommand_RequestConfigData();

            IsReady = true;
            IsConnected = true;
            IsConnecting = false;
        }

        private void Rfe_ReceivedConfigurationDataEvent(object sender, EventArgs e)
        {
            WriteLog("Got Config Data");
            DeviceSerialNumber = rfe.SerialNumber;
            DeviceModel = rfe.ActiveModel.ToString();
            DeviceFirmware = rfe.FullModelText;
        }

        public Task Disconnect()
        {
            if(IsScanning)
            {
                WriteLog("Can't disconnect, please stop the scan");
                return Task.CompletedTask;
            }

            rfe.ClosePort();
            rfe.Close();

            WriteLog("Disconnecting...");

            IsConnected = false;
            IsReady = false;
            return Task.CompletedTask;
        }

        public Task Start(double start, double end, double step, int samples)
        {
            sampleCount = samples;

            HasCompleteScan = false;

            double currentStartFrequency = start;

            // Work out the blocks for scanning
            ScanBlocks = new List<ScanBlock>();
            while(currentStartFrequency < end)
            {
                ScanBlocks.Add(new ScanBlock()
                {
                    StartFrequency = currentStartFrequency,
                    EndFrequency = currentStartFrequency + step
                });

                currentStartFrequency += step;
            }

         
            // Set the scanning block and start the scan
            IsScanning = true;

            RangeAndScanBlock(0);

            return Task.CompletedTask;
        }

        private void RangeAndScanBlock(int block)
        {
            currentBlock = block;

            rfe.UpdateDeviceConfig(
                ScanBlocks[block].StartFrequency,
                ScanBlocks[block].EndFrequency);

            ScanBlocks[block].AmplitudeData.Clear();
            ScanBlocks[block].Status = ScanBlock.BlockStatus.InProgress;

            collectingData = true;
        }

        public Task Stop()
        {
            WriteLog("Stopping Scan");
            IsScanning = false;
            return Task.CompletedTask;
        }

        private void WriteLog(string message)
        {
            if (Log.Count > 128)
                Log.RemoveAt(0);

            Log.Add(message);
        }
    }
}
