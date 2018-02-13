using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorrectPremiumAmount
{
    public class EndorseModel
    {
        public string PolicyNumber { get; set; }
        public string APENumber { get; set; }
        public string ENDNumber { get; set; }

        public string PolicyInAppId { get; set; }

        public int AuctualEndorseSeq { get; set; }
        public int CurrPolicyEndSeq { get; set; }

        public double PreviousPolicyPremiumBeforeFee { get; set; }
        public double PreviousPolicyDuty { get; set; }

        public string InAppItemIdforDel { get; set; }
        public string InAppItemIdforAdd { get; set; }

        public PremiumModel ActualPremium = new PremiumModel();
        public PremiumModel ExpectPremium = new PremiumModel();
        public PremiumModel ActualPremiumForDel = new PremiumModel();
        public PremiumModel ActualPremiumForAdd = new PremiumModel();
        public PremiumModel ExpectPremiumForDel = new PremiumModel();
        public PremiumModel ExpectPremiumForAdd = new PremiumModel();

        public bool IsAdding { get; set; }
        public bool IsDeleting { get; set; }

        public EndorseModel(string polNo, string apeNo, string endNo)
        {
            this.PolicyNumber = polNo;
            this.APENumber    = apeNo;
            this.ENDNumber    = endNo;
        }

    }
}
