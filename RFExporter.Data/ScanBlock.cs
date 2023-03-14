using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RFExporter.Data
{
    public class ScanBlock
    {
        public ScanBlock()
        {
            AmplitudeData = new List<float[]>();
            Status = BlockStatus.Unscanned;
        }

        public double StartFrequency;
        public double EndFrequency;

        public List<float[]> AmplitudeData;
       
        public BlockStatus Status;

        public ScanData[] ScanData
        { 
            get
            {
                return GetScanData();
            }
        }

        public float[] Averaged 
        {
            get
            {
                return GetAveraged();
            }
        }
        public double[] Frequencies
        {
            get
            {
                return GetFrequencies();
            }
        }

        private ScanData[] GetScanData()
        {

            float[] Averaged = GetAveraged();
            double[] Frequencies = GetFrequencies();

            List<ScanData> scanData = new List<ScanData>();

            for(int i = 0; i < Frequencies.Length; i++)
            {
                scanData.Add(new Data.ScanData()
                {
                    AverageAmplitudeDBM = Averaged[i],
                    Frequency = Frequencies[i]
                });
            }

            return scanData.ToArray();
        }

        private double[] GetFrequencies()
        {
            if (AmplitudeData.Count < 1)
            {
                return new double[0];
            }

            List<double> frequencyList = new List<double>();
            double step = (EndFrequency - StartFrequency) / AmplitudeData[0].Length;
            for (int f = 0; f < AmplitudeData[0].Length - 1; f++)
            {
                frequencyList.Add(StartFrequency + f * step);
            }

            return frequencyList.ToArray();
        }

        private float[] GetAveraged()
        {
            if(AmplitudeData.Count < 1)
            {
                return new float[0];
            }

            float[] buffer = new float[AmplitudeData[0].Length];

            // Add each of the values into the buffer
            foreach (var data in AmplitudeData)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    buffer[i] += data[i];
                }
            }

            // Divide by the sample count to create an average
            for(int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = buffer[i] / AmplitudeData.Count;
            }

            return buffer;
        }

        public enum BlockStatus
        {
            Unscanned,
            InProgress,
            Scanned
        }
    }


}
