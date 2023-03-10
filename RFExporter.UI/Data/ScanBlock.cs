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

        public enum BlockStatus
        {
            Unscanned,
            InProgress,
            Scanned
        }
    }


}
