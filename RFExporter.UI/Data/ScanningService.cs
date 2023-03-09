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

        public bool IsConnected { get; private set; }
        public bool IsScanning { get; private set; }
        public bool HasCompleteScan { get; private set; }

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

        public Task Connect()
        {
            WriteLog("Connecting...");


            rfe = new RFExplorerNET.RFExplorerCommunicator.RFECommunicator(true);
            rfe.ConnectPort("COM8", 500000);

            // Spin off a thread to watch for serial data
            cancellationToken = new CancellationTokenSource();
            new Task(() => PollingTask(), cancellationToken.Token, TaskCreationOptions.LongRunning).Start();

            
            // Set up events
            rfe.DeviceReset += Rfe_DeviceReset;
            rfe.ReceivedConfigurationDataEvent += Rfe_ReceivedConfigurationDataEvent;

            WriteLog("Sending Reset...");
            rfe.SendCommand("r");

            return Task.CompletedTask;
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

            IsConnected = true;

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

            WriteLog("Disconnecting...");

            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task Start()
        {
            WriteLog("Starting Scan");
            IsScanning = true;
            return Task.CompletedTask;
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
