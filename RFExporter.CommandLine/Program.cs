using RFExporter.Data;

namespace RFExporter.CommandLine
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RFExporter.Data.ScanningService scanningService = new Data.ScanningService();

            Console.WriteLine("RFExporter");
            Console.WriteLine("----------\n\n");

            // Get Serial ports
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();

            // If there are no serial ports, exit.
            if(ports.Length <=0 ) {
                Console.WriteLine("No Serial ports found");
                return;
            }


            for(int i = 0; i < ports.Length; i++)
            {
                Console.WriteLine("    [{0}] {1}",i+1, ports[i]);
            }
            Console.WriteLine("");
            Console.Write("Select Port [1]: ");


            // Port Selection
            int num = 1;          
            if(!int.TryParse(Console.ReadKey().KeyChar.ToString(), out num))
            {
                Console.WriteLine("Invalid selection");
            }
            if(num < 1 || num > ports.Length)
            {
                Console.WriteLine("Invalid selection");
            }
            string portName = ports[num-1];
            

            // Connect and reset the RFE
            Console.WriteLine("\nConnecting...");
            scanningService.Connect(portName);

            while(scanningService.Status == Data.ScanningService.ScanningStatus.Connecting)
            {
                Thread.Sleep(100);
            }

            Console.WriteLine("Connected!");
            Console.WriteLine("Waiting for ready...");

            while(scanningService.Status != Data.ScanningService.ScanningStatus.ConnectedAndReady)
            {
                Thread.Sleep(100);
            }

            Console.WriteLine("\nRFExplorer Details");
            Console.WriteLine("Model: " + scanningService.DeviceModel);
            Console.WriteLine("Firmware: " + scanningService.DeviceFirmware);
            Console.WriteLine("Serial: " + scanningService.DeviceSerialNumber);


            Console.Write("Is this correct? [y/n]");

            if(Console.ReadKey().Key != ConsoleKey.Y)
            {
                scanningService.Disconnect();
                return;
            }
            Console.WriteLine("\n-----------\n");

            // Get scan bounds:
            double start = DoStartFrequency();
            double end = DoStopFrequency();
            double width = DoScanWidth();
            int samples = DoSampleCount();


            Console.WriteLine("\nStarting Scan...");
            Thread.Sleep(2000);

            scanningService.Start(start, end, width, samples);

            while(scanningService.Status != Data.ScanningService.ScanningStatus.ScanComplete)
            {
                Thread.Sleep(1000);
                Console.Clear();
                Console.WriteLine("Scanning, Block {0} of {1}", scanningService.ScannedBlocks, scanningService.TotalBlocks);
                Console.WriteLine("Scanning, Sample {0} of {1}", scanningService.ScannedSamples, scanningService.TotalSamples);

                ScanBlock? currentBlock = scanningService.ScanBlocks.FirstOrDefault(b => b.Status == ScanBlock.BlockStatus.InProgress);

                if(currentBlock != null)
                {
                    Console.WriteLine("Current Block: {0} -> {1}", currentBlock.StartFrequency, currentBlock.EndFrequency);
                }
                
            }

            Console.WriteLine("Writing CSV...");
            StreamWriter strw = new StreamWriter("output.csv");
            strw.Write(scanningService.GetCSVData());
            strw.Close();
            Console.WriteLine("Done!");
            
        }

        private static double DoStartFrequency()
        {
            Console.Write("Start of sweep [Default: 450.000]: ");

            string input = Console.ReadLine();
            if (input == null || input == "")
                return 450.0d;

            try
            {
                return double.Parse(input);
            }
            catch (Exception ex)
            {
                return 450.00d;
            }
        }

        private static double DoStopFrequency()
        {
            Console.Write("End of sweep [Default: 900.000]: ");

            string input = Console.ReadLine();
            if (input == null || input == "")
                return 900.0d;

            try
            {
                return double.Parse(input);
            }
            catch (Exception ex)
            {
                return 900.00d;
            }
        }

        private static double DoScanWidth()
        {
            Console.Write("Width of scan [Default: 2]: ");

            string input = Console.ReadLine();
            if (input == null || input == "")
                return 2.0d;

            try
            {
                return double.Parse(input);
            }
            catch (Exception ex)
            {
                return 2.00d;
            }
        }

        private static int  DoSampleCount()
        {
            Console.Write("How many samples [Default: 10]: ");

            string input = Console.ReadLine();
            if (input == null || input == "")
                return 10;

            try
            {
                return int.Parse(input);
            }
            catch (Exception ex)
            {
                return 10;
            }
        }

    }
}