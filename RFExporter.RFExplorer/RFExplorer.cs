using System.IO.Ports;

namespace RFExporter.RFExplorer
{
    public class RFExplorer
    {
        private SerialPort serialPort;

        public void Connect(string portName)
        {
            serialPort = new SerialPort(portName, 500000);
            serialPort.Open();

            SendCommand(Command.Request_Reboot);
        }

        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        private void SendCommand(Command command)
        {
            switch (command)
            {
                case Command.Request_Reboot:
                    serialPort.Write("#3r");
                    break;
            }

            
        }

        private enum Command
        {
            Request_Reboot
        }
    }
}