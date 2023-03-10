using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RFExporter.UI.Data
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

        private double[] GetFrequencies()
        {
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
