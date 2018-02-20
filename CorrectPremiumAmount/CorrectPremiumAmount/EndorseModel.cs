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

        public decimal PreviousPolicyPremiumBeforeFee { get; set; }
        public decimal PreviousPolicyDuty { get; set; }

        public string PolicyAgreementId { get; set; }
        public string InAppItemIdforDel { get; set; }
        public string InAppItemIdforAdd { get; set; }

        public PremiumModel ActualPremiumForPolicy = new PremiumModel();
        public PremiumModel ExpectedPremiumForPolicy = new PremiumModel();
        public PremiumModel ActualPremiumForEndorsement = new PremiumModel();
        public PremiumModel ExpectedPremiumForEndorsement = new PremiumModel();
        public PremiumModel ActualPremiumForDel = new PremiumModel();
        public PremiumModel ActualPremiumForAdd = new PremiumModel();
        public PremiumModel ExpectedPremiumForDel = new PremiumModel();
        public PremiumModel ExpectedPremiumForAdd = new PremiumModel();

        public bool IsAdding { get; set; }
        public bool IsDeleting { get; set; }
        public bool HaveOrtherTypeEnd { get; set; }

        public EndorseModel(string polNo, string apeNo, string endNo)
        {
            this.PolicyNumber = polNo;
            this.APENumber    = apeNo;
            this.ENDNumber    = endNo;
        }

        public EndorseModel(string apeNo)
        {
            this.APENumber = apeNo;
        }

        public void InitialPremium(PremiumModel premium, string premiumBeforeFee, string duty, string premiumAfterFee)
        {
            premium.PremiumBeforeFee = premiumBeforeFee;
            premium.PremiumAfterFee = premiumAfterFee;
            premium.Duty = duty;
        }

    }
}
