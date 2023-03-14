using RFExplorerNET.RFExplorerCommunicator;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFExporter.Data
{
    public class ScanningService
    {
        public List<string> Log { get; private set; }


        private RFExplorerNET.RFExplorerCommunicator.RFECommunicator rfe;
        private CancellationTokenSource cancellationToken;

        //public bool IsConnecting { get; private set; }
        //public bool IsConnected { get; private set; }
        //public bool IsScanning { get; private set; }
        //public bool HasCompleteScan { get; private set; }

        //private bool IsReady = false;

        public ScanningStatus Status { get; private set; }
        
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

        public int TotalSamples
        {
            get
            {
                return ScanBlocks.Count * sampleCount;
            }
        }
        public int ScannedSamples
        {
            get
            {
                int scannedSamples = 0;
                // Only scanned or in progress count
                foreach (var block in ScanBlocks.FindAll(p => p.Status == ScanBlock.BlockStatus.Scanned || p.Status == ScanBlock.BlockStatus.InProgress)) {
                    scannedSamples += block.AmplitudeData.Count();
                }
                return scannedSamples;
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

            Status = ScanningStatus.Idle;

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
            // TOOD: Any way to identify ports provided by an RFE?
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
            Status = ScanningStatus.Connecting;

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

            if (Status == ScanningStatus.Scanning && collectingData)
            {
                // Get the latest sweep data object
                RFESweepData sweepData = rfe.SweepData.GetData(rfe.SweepData.Count - 1);

                // Sanity check that this scan data is from the range currently
                // being scanned, this just checks the start frequency
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

                    // Add it to the block data, AmplitudeData holds multiple samples for averaging
                    ScanBlocks[currentBlock].AmplitudeData.Add(ampData.ToArray());

                    // Keep scanning this block until the required number of samples
                    if(ScanBlocks[currentBlock].AmplitudeData.Count >= sampleCount)
                    {
                        // We're done with this block, mark as complete and move
                        // to the next one or complete the scan
                        ScanBlocks[currentBlock].Status = ScanBlock.BlockStatus.Scanned;
                        WriteLog("Block " + currentBlock + " complete");

                        if (currentBlock >= ScanBlocks.Count - 1)
                        {
                            WriteLog("Scan Complete");
                            Status = ScanningStatus.ScanComplete;
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
            // The only time this should be received is just after
            // the initial connection
            WriteLog("Received Reset ACK");
            WriteLog("Waiting for boot to complete...");

            // Wait for boot, ~4 seconds
            for (int i = 5; i > 0; i--)
            {
                Thread.Sleep(1000);
                WriteLog(i.ToString() + "...");
            }
            WriteLog("Connected");
            WriteLog("Requesting Config");
          
            // Once config is received the RFE is ready for use.
            Status = ScanningStatus.Connected;
            rfe.SendCommand_RequestConfigData();
           
        }

        private void Rfe_ReceivedConfigurationDataEvent(object sender, EventArgs e)
        {
            // Grab some details about the device
            DeviceSerialNumber = rfe.SerialNumber;
            DeviceModel = rfe.ActiveModel.ToString();
            DeviceFirmware = rfe.FullModelText;

            // If the configuration data was received during
            // the connection phase then we are now ready.
            if(Status == ScanningStatus.Connected)
            {
                WriteLog("Got Config Data, Ready.");
                Status = ScanningStatus.ConnectedAndReady;
            }
        }

        public Task Disconnect()
        {
            if(Status == ScanningStatus.Scanning)
            {
                WriteLog("Can't disconnect, please stop the scan");
                return Task.CompletedTask;
            }

            rfe.ClosePort();
            rfe.Close();

            WriteLog("Disconnecting...");

            Status = ScanningStatus.Idle;

            return Task.CompletedTask;
        }

        public Task Start(double start, double end, double step, int samples)
        {
            if(!(Status == ScanningStatus.ConnectedAndReady || Status == ScanningStatus.ScanComplete))
            {
                WriteLog("Not ConnectedAndReady or at ScanComplete, can't start scan");
                return Task.CompletedTask;
            }

            sampleCount = samples;
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
            Status = ScanningStatus.Scanning;
            RangeAndScanBlock(0);

            return Task.CompletedTask;
        }

        private void RangeAndScanBlock(int block)
        {
            // Set the currently active block
            currentBlock = block;

            // Retune the RFExplorer to this block
            rfe.UpdateDeviceConfig(
                ScanBlocks[block].StartFrequency,
                ScanBlocks[block].EndFrequency);

            // Clear all pending sweep data and mark the block as in progress
            ScanBlocks[block].AmplitudeData.Clear();
            ScanBlocks[block].Status = ScanBlock.BlockStatus.InProgress;

            // Control will now hand over to the UpdateDataEvent
            collectingData = true;
        }

        public Task Stop()
        {
            WriteLog("Stopping Scan");
            Status = ScanningStatus.ConnectedAndReady;
            return Task.CompletedTask;
        }

        private void WriteLog(string message)
        {
            if (Log.Count > 128)
                Log.RemoveAt(0);

            Log.Add(message);
        }

        public enum ScanningStatus
        {
            Idle,
            Connecting,
            Connected,
            ConnectedAndReady,
            Scanning,
            ScanComplete
        }
    }
}
