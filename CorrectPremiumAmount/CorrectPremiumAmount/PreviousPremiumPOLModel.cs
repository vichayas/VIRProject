using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorrectPremiumAmount
{
    public class PreviousPremiumPOLModel
    {
        public string ReferenceNo { get; set; }
        public double PreviousPremiumBeforeFee { get; set; }
        public double PreviousPremiumAfterFee { get; set; }
        public int Duty { get; set; }
        public int LastEndorse { get; set; }
    }
}
