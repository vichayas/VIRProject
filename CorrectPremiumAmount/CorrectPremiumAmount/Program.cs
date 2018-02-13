using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorrectPremiumAmount
{
    class Program
    {
        static void Main(string[] args)
        {
            //var VISconn = new DbConnection("Server='VISDBCentral';Database=VIS_DB;User=vissa;Password=visdbpassword;MultipleActiveResultSets=True");
            var VISconn = new DbConnection("Server='.';Database=VIS_DB;Integrated Security=True; Trusted_Connection=true");
            //var Mamaconn = new DbConnection("Server='VISCON';Database=ComplianceDB;Integrated Security=True; Trusted_Connection=true");

            var polNo = "17181/POL/000017-563";
            var apeNo = "17181/APE/000054-563";
            var endNo = "17181/END/000055-563";

            var addingInsuredTypeGUID = "A446F001-873E-4439-9397-9D95609143B5";
            var deletingInsuredTypeGUID = "C59823FA-4ADF-439B-A281-D7E759CAE307";

            var objEndModel = new EndorseModel(polNo, apeNo, endNo);
            Console.WriteLine("Policy Number : {0}\n" +
                              "APE    Number : {1}\n"+
                              "END    Number : {2}\n\n", objEndModel.PolicyNumber, objEndModel.APENumber, objEndModel.ENDNumber);
            //Console.Write("Confirm? (Y/N): ");
            //var result = Console.ReadLine();
            var result = "Y";
            if (result == "Y")
            {

                FindEndorseSequence(VISconn, objEndModel);
                FindPreviousEndPremiumAndDuty(VISconn, objEndModel);
                CheckTypesOfThisEndorsement(VISconn, objEndModel, addingInsuredTypeGUID, deletingInsuredTypeGUID);
                // 1. Update premium of insured that was added
                if (objEndModel.IsAdding)
                {
                    //CallInsertInsuredSP(VISconn);
                }

                UpdateInAppItemForAdd(VISconn, objEndModel);
                if (objEndModel.IsDeleting)
                {
                    //CallDeletedInsuredSP(VISconn);
                }

                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("You cancel the process.");
            }
        }

        private static void UpdateInAppItemForAdd(DbConnection viSconn, EndorseModel objEndModel)
        {
            var updateQuery = "UPDATE InsuranceApplicationItem "+
                               "SET TotalBeforeFee = "+objEndModel.ExpectPremiumForAdd.PremiumBeforeFee+
                               " ,TotalDuty = "+objEndModel.ExpectPremiumForAdd.Duty+
                               " ,TotalAfterFee = "+objEndModel.ExpectPremiumForAdd.PremiumAfterFee+
                               " WHERE Id = '"+objEndModel.InAppItemIdforAdd+"'";


        }

        private static void CallDeletedInsuredSP(DbConnection visConn)
        {
            throw new NotImplementedException();
        }

        private static void CallInsertInsuredSP(DbConnection visConn)
        {
            throw new NotImplementedException();
        }

        private static void CheckTypesOfThisEndorsement(DbConnection visConn, EndorseModel endorseModel, string addingInsuredTypeGuid, string deletingInsuredTypeGuid)
        {
            var query = "select InsuranceApplicationItem.Id, InsuranceApplicationItem.TotalBeforeFee,InsuranceApplicationItem.TotalDuty,InsuranceApplicationItem.TotalAfterFee,ApplicantEndorsementItem.EndorsementType_Id from Agreement inner join ApplicantEndorsementItem on Agreement.Id = ApplicantEndorsementItem.ApplicantEndorsement_Id inner join InsuranceApplicationItem on ApplicantEndorsementItem.InsuranceApplication_Id = InsuranceApplicationItem.InsuranceApplication_Id where Agreement.ReferenceNumber= '" + endorseModel.APENumber + "' and InsuranceApplicationItem.InsuranceApplicationItemType_Id='213F8708-FCC2-4430-A804-A1D115F718C5'";
            var reader = visConn.ExecutrQueryReader(query);
            var output = String.Empty;
            while (reader.Read())
            {
                if (reader["EndorsementType_Id"].ToString() == addingInsuredTypeGuid.ToLower())
                {
                    endorseModel.IsAdding = true;
                    endorseModel.ActualPremiumForAdd.PremiumBeforeFee = reader["TotalBeforeFee"].ToString();
                    endorseModel.ActualPremiumForAdd.PremiumAfterFee = reader["TotalAfterFee"].ToString();
                    endorseModel.ActualPremiumForAdd.Duty = reader["TotalDuty"].ToString();
                    endorseModel.InAppItemIdforAdd = reader["Id"].ToString();
                    output +=  "Have => Adding Insured Endorsement\n";
                    

                }
                else if (reader["EndorsementType_Id"].ToString() == deletingInsuredTypeGuid.ToLower())
                {
                    endorseModel.IsDeleting = true;
                    endorseModel.ActualPremiumForDel.PremiumBeforeFee = reader["TotalBeforeFee"].ToString();
                    endorseModel.ActualPremiumForDel.PremiumAfterFee = reader["TotalAfterFee"].ToString();
                    endorseModel.ActualPremiumForDel.Duty = reader["TotalDuty"].ToString();
                    endorseModel.InAppItemIdforDel = reader["Id"].ToString();
                    output +=  "Have => Deleting Insured Endorsement\n";
                }
            }

            Console.WriteLine(output);
            Console.WriteLine("\n\n");
            visConn.CloseConnection();
        }

        private static void FindEndorseSequence(DbConnection visConn, EndorseModel endorseModel)
        {
            //Find the End_sequnce of the policy 
            var stringCheckEndSequence = "select a.Id as AgreementId, a.ReferenceNumber, b.END_Sequence, a.InsuranceApplication_Id from Agreement a left join InsuranceApplication b on (a.InsuranceApplication_Id = b.Id) where ReferenceNumber = '" + endorseModel.PolicyNumber + "'  and Discriminator = 'InsurancePolicy'";
            var reader = visConn.ExecutrQueryReader(stringCheckEndSequence);
            while (reader.Read())
            {
                Console.WriteLine("Reference Number: " + reader["ReferenceNumber"]);
                Console.WriteLine("Last End_seq: " + reader["END_Sequence"]);
                Console.WriteLine("InsuranceApplication Id: " + reader["InsuranceApplication_Id"]);
                endorseModel.CurrPolicyEndSeq = Int32.Parse(reader["END_Sequence"].ToString());
                endorseModel.PolicyInAppId = reader["InsuranceApplication_Id"].ToString();
            }

            visConn.CloseConnection();

            //Find the number of endorsement
            var query = "SELECT max(IA1.END_Sequence) as LastEndSeq  FROM InsuranceApplication IA1   INNER JOIN InsuranceApplication IA2 ON IA2.Id = IA1.ParentId  WHERE IA1.PolicyNumber = '" + endorseModel.PolicyNumber + "'";
            reader = visConn.ExecutrQueryReader(query);
            while (reader.Read())
            {
                Console.WriteLine("\n\nLast End_seq: " + reader["LastEndSeq"]);
                endorseModel.AuctualEndorseSeq = Int32.Parse(reader["LastEndSeq"].ToString());
            }
            visConn.CloseConnection();
        }

        private static void FindPreviousEndPremiumAndDuty(DbConnection visConn, EndorseModel endorseModel)
        {
            var query = "SELECT IA1.END_Sequence,iai.TotalBeforeFee,iai.TotalDuty FROM InsuranceApplication IA1   INNER JOIN InsuranceApplication IA2 ON IA2.Id = IA1.ParentId  INNER JOIN InsuranceApplicationItem iai on iai.InsuranceApplication_Id=IA1.Id WHERE IA1.PolicyNumber = '" + endorseModel.PolicyNumber + "' AND IA1.END_Sequence = '" + (endorseModel.AuctualEndorseSeq-1)+ "' AND iai.InsuranceApplicationItemType_Id like 'F8%'";
            var reader = visConn.ExecutrQueryReader(query);
            while (reader.Read())
            {       
                endorseModel.PreviousPolicyPremiumBeforeFee = Double.Parse(reader["TotalBeforeFee"].ToString());
                endorseModel.PreviousPolicyDuty  = Double.Parse(reader["TotalDuty"].ToString());
                Console.WriteLine("\n\nPrevious End_seq: {0} , TotalBeforeFee: {1}, Duty: {2}", reader["END_Sequence"], reader["TotalBeforeFee"], reader["TotalDuty"]);
            }
            visConn.CloseConnection();
        }

    }
}
