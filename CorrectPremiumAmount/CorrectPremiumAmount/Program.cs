using System;

namespace CorrectPremiumAmount
{
    public static class ConvertHelper
    {
        public static Decimal ToDecimal(this string input)
        {
            var output = Decimal.MinValue;
            Decimal.TryParse(input, out output);
            return output;
        }

    }

    public class Program
    {

        static void Main(string[] args)
        {

            var VISconn = new DbConnection("Server='VISDBCentral';Database=VIS_DB;User=vissa;Password=visdbpassword;MultipleActiveResultSets=True");
            //DbConnection VISconn = new DbConnection("Server='.';Database=VIS_DB;Integrated Security=True; Trusted_Connection=true");
            //var VISconn = new DbConnection("Server='VISHQDB';Database=VIS_DB;User=vissa;Password=visdbpassword;MultipleActiveResultSets=True");
         
           // string polNo = "17181/POL/000017-563";
            string apeNo = "18181/APE/000029-574";
            //string endNo = "17181/END/000055-563";

            string addingInsuredTypeGUID = "A446F001-873E-4439-9397-9D95609143B5";
            string deletingInsuredTypeGUID = "C59823FA-4ADF-439B-A281-D7E759CAE307";

           // var objEndModel = new EndorseModel(polNo, apeNo, endNo);
            var objEndModel = new EndorseModel(apeNo);
            FindEndorsementAndPolicyNumber(apeNo, VISconn, objEndModel);

            Console.WriteLine("Policy Number : {0}\n" +
                              "APE    Number : {1}\n"+
                              "END    Number : {2}\n\n", objEndModel.PolicyNumber, objEndModel.APENumber, objEndModel.ENDNumber);
            //objEndModel.InitialPremium(objEndModel.ExpectedPremiumForEndorsement, "235047.22", "941", "235988.22");
            //objEndModel.InitialPremium(objEndModel.ExpectedPremiumForEndorsement, "16163.64", "65", "16228.64");
            objEndModel.InitialPremium(objEndModel.ExpectedPremiumForAdd, "3508.66", "15", "3523.66");
            objEndModel.InitialPremium(objEndModel.ExpectedPremiumForDel, "-21618.24", "0", "-21618.24");
            //Console.Write("Confirm? (Y/N): ");
            //var result = Console.ReadLine();
            var result = "Y";
            if (result == "Y")
            {

                FindEndorseSequence(VISconn, objEndModel);
                FindPreviousEndPremiumAndDuty(VISconn, objEndModel);
                CheckTypesOfThisEndorsement(VISconn, objEndModel, addingInsuredTypeGUID, deletingInsuredTypeGUID);
                // 1. Update premium amount of insured that was added

                if (objEndModel.IsAdding)
                {
                    //CallInsertInsuredSP(VISconn, objEndModel);
                    // 2. Update premium amount for Endorsement
                    UpdateInAppItem(VISconn, objEndModel.ExpectedPremiumForAdd,objEndModel.InAppItemIdforAdd);
                }

                if (objEndModel.IsDeleting)
                {
                    if (objEndModel.AuctualEndorseSeq == objEndModel.CurrPolicyEndSeq)
                    {
                       // CallDeletedInsuredSP(VISconn, objEndModel);
                        UpdateInAppItem(VISconn, objEndModel.ExpectedPremiumForDel, objEndModel.InAppItemIdforDel);
                    }
                    else
                    {
                        Console.WriteLine("\n==============\n Can't update the premium of insured who are deleted from the policy!\n b/c the last endorse is not the current endorse \n========================");
                    }
                }
                CallPremiumEndorse(objEndModel);
                if (!String.IsNullOrEmpty(objEndModel.ExpectedPremiumForEndorsement.PremiumBeforeFee))
                {
                    // 3. Update the endorsement premium
                    UpdateEndorsePremium(VISconn, objEndModel);

                    // 3.2 Update the endorsment fee
                    UpdateEndorsementDuty(VISconn, objEndModel);

                    //4. Modify the reciept
                    //4.1 Update the PremiumSchedule
                    UpdatePremiumSchedule(VISconn, objEndModel);
                    //4.2 Update the PolicyItemPremium (only use for have 1 reciept)
                    UpdateEndorseItemPremium(VISconn, objEndModel);
                    //4.3 Update the Payment
                    UpdatePayment(VISconn, objEndModel);
                }

                /*if (objEndModel.AuctualEndorseSeq != objEndModel.CurrPolicyEndSeq)
                {
                    Console.WriteLine(
                        "!!! You need to run method interrupt b/c the current End_sequnece doen't match with in the policy !!!");
                    var ans = String.Empty;
                    do
                    {
                        Console.Write("Have you run the interupt method? (Y/N): ");
                        ans = Console.ReadLine();
                    } while (ans != "Y");

                }*/

                CalculatePolicyPremium(objEndModel);

                //6. Update  Policy Premium
                UpdatePolicyItemPremium(VISconn, objEndModel);

                //7. Update Duty
                UpdatePolicyDuty(VISconn, objEndModel);

                //8. Update overview of Endorsement's Premium
                UpdateEndorsementOverview(VISconn, objEndModel);

                    Console.ReadLine();
            }
            else
            {
                Console.WriteLine("You cancel the process!");
            }
        }

        private static void FindEndorsementAndPolicyNumber(string apeNo, DbConnection visConn, EndorseModel objEndModel)
        {

            var query = "select a.ReferenceNumber as APENumber, b.ReferenceNumber as ENDNumber, c.PolicyNumber from Agreement a left join Agreement b on (a.InsuranceApplication_Id = b.InsuranceApplication_Id AND b.Discriminator = 'InsuranceEndorsement')  left join InsuranceApplication c on a.InsuranceApplication_Id = c.Id where a.ReferenceNumber = '" + apeNo + "'";
            var reader = visConn.ExecutrQueryReader(query);
            if (reader.Read())
            {
                objEndModel.ENDNumber = reader["ENDNumber"].ToString();
                objEndModel.PolicyNumber = reader["PolicyNumber"].ToString();
            }
            visConn.CloseConnection();
        }

  
        private static void CalculatePolicyPremium(EndorseModel objEndModel)
        {
            objEndModel.ExpectedPremiumForPolicy.PremiumBeforeFee =
                (objEndModel.PreviousPolicyPremiumBeforeFee + objEndModel.ExpectedPremiumForEndorsement.PremiumBeforeFee.ToDecimal()).ToString();
            objEndModel.ExpectedPremiumForPolicy.Duty =
                (objEndModel.PreviousPolicyDuty + objEndModel.ExpectedPremiumForEndorsement.Duty.ToDecimal()).ToString();
            objEndModel.ExpectedPremiumForPolicy.PremiumAfterFee =
                (objEndModel.ExpectedPremiumForPolicy.PremiumBeforeFee.ToDecimal() + objEndModel.ExpectedPremiumForPolicy.Duty.ToDecimal()).ToString();
        }

        private static void UpdateEndorsementOverview(DbConnection visConn, EndorseModel objEndModel)
        {
            Console.WriteLine("\n========================");
            Console.WriteLine("8. Update Endorsement's overview");
            Console.WriteLine("========================\n");
            var updateQuery = "UPDATE iai " +
                              "SET iai.Amount = " + objEndModel.ExpectedPremiumForPolicy.PremiumBeforeFee +
                              " ,iai.TotalBeforeFee = " + objEndModel.ExpectedPremiumForPolicy.PremiumBeforeFee +
                              " ,iai.TotalDuty = " + objEndModel.ExpectedPremiumForPolicy.Duty +
                              " ,iai.TotalAfterFee  = " + objEndModel.ExpectedPremiumForPolicy.PremiumAfterFee +
                              " from Agreement a " +
                              " inner join insuranceapplication ia on a.insuranceapplication_id=ia.id " +
                              " inner join insuranceapplicationitem iai on iai.insuranceapplication_id=ia.id " +
                              " where a.ReferenceNumber = '" + objEndModel.ENDNumber +
                              "' and InsuranceApplicationItemType_Id='F89579DB-7C44-409F-8D50-A0061D29D04E' ";

            var reader = visConn.ExecutrQueryReader(updateQuery);
            visConn.CloseConnection();
            //var result = reader.Read();
        }

        private static void UpdatePolicyDuty(DbConnection visConn, EndorseModel objEndModel)
        {
            Console.WriteLine("\n========================");
            Console.WriteLine("7. Update Duty of Policy");
            Console.WriteLine("========================\n");
            var updateQuery = "UPDATE iaf " +
                              " SET iaf.Amount = " + objEndModel.ExpectedPremiumForPolicy.Duty +
                              " from Agreement a " +
                              " inner join insuranceapplication ia on a.insuranceapplication_id=ia.id " +
                              " inner join insuranceapplicationitem iai on iai.insuranceapplication_id=ia.id " +
                              " inner join InsuranceApplicationItemFee iaf on iaf.InsuranceApplicationPackageItem_Id=iai.Id " +
                              " where a.id = '" + objEndModel.PolicyAgreementId +
                              "' and insuranceapplicationitemtype_id='f89579db-7c44-409f-8d50-a0061d29d04e' and iaf.InsuranceProductCategoryRateConfiguration_Id='FCCC43A8-4EEC-40C7-85C3-A2B7C58264CA'";

            var reader = visConn.ExecutrQueryReader(updateQuery);
            visConn.CloseConnection();
            //var result = reader.Read();
        }

        private static void UpdatePolicyItemPremium(DbConnection visConn, EndorseModel objEndModel)
        {
            Console.WriteLine("\n========================");
            Console.WriteLine("6. Update Premium of Policy");
            Console.WriteLine("========================\n");
            var updateQuery = "UPDATE iai " +
                              "SET totalbeforefee = " + objEndModel.ExpectedPremiumForPolicy.PremiumBeforeFee +
                              " ,amount = " + objEndModel.ExpectedPremiumForPolicy.PremiumBeforeFee +
                              " ,totalduty = " + objEndModel.ExpectedPremiumForPolicy.Duty +
                              " ,totalafterfee  = " + objEndModel.ExpectedPremiumForPolicy.PremiumAfterFee +
                              " from Agreement a " +
                              " inner join insuranceapplication ia on a.insuranceapplication_id=ia.id " +
                              " inner join insuranceapplicationitem iai on iai.insuranceapplication_id=ia.id " +
                              " where a.id = '" + objEndModel.PolicyAgreementId +
                              "' and insuranceapplicationitemtype_id='f89579db-7c44-409f-8d50-a0061d29d04e'";

            var reader = visConn.ExecutrQueryReader(updateQuery);
            visConn.CloseConnection();
            //var result = reader.Read();
        }

        private static void UpdatePayment(DbConnection visConn, EndorseModel objEndModel)
        {
            Console.WriteLine("\n========================");
            Console.WriteLine("4.3 Update Payment of Endorsement");
            Console.WriteLine("========================\n");
            var updateQuery = "UPDATE ps " +
                              "SET ps.PremiumAmount = " + objEndModel.ExpectedPremiumForEndorsement.PremiumBeforeFee +
                              " ,ps.StampsDutyAmount = " + objEndModel.ExpectedPremiumForEndorsement.Duty +
                              " ,ps.TotalAmount  = " + objEndModel.ExpectedPremiumForEndorsement.PremiumAfterFee +
                              " from Agreement a " +
                              " inner join PremiumSchedule ps on ps.InsuranceApplication_Id=a.InsuranceApplication_Id "+
                              " where a.ReferenceNumber = '" + objEndModel.APENumber + "'";

            var reader = visConn.ExecutrQueryReader(updateQuery);
            visConn.CloseConnection();
            //var result = reader.Read();
        }

        private static void UpdateEndorseItemPremium(DbConnection visConn, EndorseModel objEndModel)
        {

            Console.WriteLine("\n========================");
            Console.WriteLine("4.2 Update PolicyItemPremium of Endorsement");
            Console.WriteLine("========================\n");
            var updateQuery = "UPDATE pip " +
                              "SET pip.premiumamount = " + objEndModel.ExpectedPremiumForEndorsement.PremiumBeforeFee +
                              " ,pip.stampsdutyamount = " + objEndModel.ExpectedPremiumForEndorsement.Duty +
                              " ,pip.subtotalamount = " + objEndModel.ExpectedPremiumForEndorsement.PremiumAfterFee +
                              " from Agreement a " +
                              " inner join PremiumSchedule ps on ps.InsuranceApplication_Id=a.InsuranceApplication_Id " +
                              " inner join policyitempremium pip on ps.id=pip.premiumschedule_id " +
                              " where a.ReferenceNumber = '" + objEndModel.APENumber + "'";

            var reader = visConn.ExecutrQueryReader(updateQuery);
            visConn.CloseConnection();
            //var result = reader.Read();
        }

        private static void UpdatePremiumSchedule(DbConnection visConn, EndorseModel objEndModel)
        {

            Console.WriteLine("\n========================");
            Console.WriteLine("4.1 Update PremiumSchedule of Endorsement");
            Console.WriteLine("========================\n");
            var query = "SELECT p.TotalBeforefee,p.totalDuty ,p.TotalAfterFee  " +
                              " from Agreement a " +
                              " inner join PremiumSchedule ps on ps.InsuranceApplication_Id=a.InsuranceApplication_Id " +
                              " inner join PaymentApplication pa on pa.PremiumSchedule_Id=ps.Id " +
                              " inner join Payment p on p.Id = pa.Payment_Id " +
                              " where a.ReferenceNumber = '" + objEndModel.APENumber + "'";
            var reader = visConn.ExecutrQueryReader(query);
            if (reader.Read())
            {
                var premiumTemp = new PremiumModel()
                {
                    PremiumBeforeFee = reader["TotalBeforefee"].ToString(),
                    Duty = reader["totalDuty"].ToString(),
                    PremiumAfterFee = reader["TotalAfterFee"].ToString()
                };

                PrintPremiumDetail(premiumTemp);
            }
            visConn.CloseConnection();
            if (objEndModel.IsAdding)
            {
                var updateQuery = "UPDATE p " +
                                  "SET p.TotalBeforefee = " + objEndModel.ExpectedPremiumForEndorsement.PremiumBeforeFee +
                                  " ,p.totalDuty = " + objEndModel.ExpectedPremiumForEndorsement.Duty +
                                  " ,p.TotalAfterFee = " + objEndModel.ExpectedPremiumForEndorsement.PremiumAfterFee +
                                  " from Agreement a " +
                                  " inner join PremiumSchedule ps on ps.InsuranceApplication_Id=a.InsuranceApplication_Id " +
                                  " inner join PaymentApplication pa on pa.PremiumSchedule_Id=ps.Id " +
                                  " inner join Payment p on p.Id = pa.Payment_Id " +
                                  " where a.ReferenceNumber = '" + objEndModel.APENumber + "'";
                PrintPremiumDetail(objEndModel.ExpectedPremiumForEndorsement, false);
                reader = visConn.ExecutrQueryReader(updateQuery);
                visConn.CloseConnection();
                
            }
            //var result = reader.Read();
        }

        private static void UpdateEndorsementDuty(DbConnection visConn, EndorseModel objEndModel)
        {
            Console.WriteLine("\n========================");
            Console.WriteLine("3.2 Update Duty of Endorsement");
            Console.WriteLine("========================\n");
            var query = "select f.Amount, f.ID  from Agreement a inner join InsuranceApplication ia on a.InsuranceApplication_Id=ia.Id inner join InsuranceApplicationItem iai on iai.InsuranceApplication_Id=ia.Id inner join InsuranceApplicationItemFee f on f.InsuranceApplicationPackageItem_Id = iai.Id  where a.referencenumber ='"+objEndModel.APENumber+ 
                        "' and InsuranceApplicationItemType_Id='213F8708-FCC2-4430-A804-A1D115F718C5' and InsuranceProductCategoryRateConfiguration_Id = 'FCCC43A8-4EEC-40C7-85C3-A2B7C58264CA'";
            var reader = visConn.ExecutrQueryReader(query);
            var inAppItemFeeId = String.Empty;
            if (reader.Read())
            {
                inAppItemFeeId = reader["ID"].ToString();
            } 
            visConn.CloseConnection();
            if (inAppItemFeeId != String.Empty)
            {
                var updateQuery = "UPDATE InsuranceApplicationItemFee " +
                                  "SET Amount = " + objEndModel.ExpectedPremiumForEndorsement.Duty +
                                  " WHERE Id = '" + inAppItemFeeId + "'";

                reader = visConn.ExecutrQueryReader(updateQuery);
                visConn.CloseConnection();
                //var result = reader.Read();
            }
        }

        private static void UpdateEndorsePremium(DbConnection visConn, EndorseModel objEndModel)
        {
            Console.WriteLine("\n========================");
            Console.WriteLine("3. Update InsuranceApplicationItem of Endorsement");
            Console.WriteLine("========================\n");
            var query = "select iai.TotalBeforeFee, iai.TotalDuty, iai.TotalAfterFee, iai.ID from Agreement a inner join InsuranceApplication ia on a.InsuranceApplication_Id=ia.Id inner join InsuranceApplicationItem iai on iai.InsuranceApplication_Id=ia.Id where a.referencenumber = '"+objEndModel.APENumber+"'  and InsuranceApplicationItemType_Id='213F8708-FCC2-4430-A804-A1D115F718C5'";

            var reader = visConn.ExecutrQueryReader(query);
            var inAppItemId = String.Empty;
            if (reader.Read())
            {
                objEndModel.InitialPremium(objEndModel.ActualPremiumForEndorsement, 
                                            reader["TotalBeforeFee"].ToString(), 
                                            reader["TotalDuty"].ToString(), 
                                            reader["TotalAfterFee"].ToString());

                inAppItemId = reader["ID"].ToString();
                PrintPremiumDetail(objEndModel.ActualPremiumForEndorsement);
            }
            visConn.CloseConnection();
            if (inAppItemId != String.Empty)
            {
                UpdateInAppItem(visConn, objEndModel.ExpectedPremiumForEndorsement, inAppItemId);
            }
        }

        private static void PrintPremiumDetail(PremiumModel actualPremium,bool isActualpreium = true)
        {
            if (isActualpreium)
            {
                Console.WriteLine("Actual Premium (Before Update)");
            }
            else
            {
                Console.WriteLine("Expected Premium (After Update)");
            }
            Console.WriteLine(
                               "\tBefore Fee = {0}\n" +
                               "\tDuty = {1}\n"+
                               "\tAfter Fee = {2}\n",actualPremium.PremiumBeforeFee,actualPremium.Duty,actualPremium.PremiumAfterFee);
        }

        private static void UpdateInAppItem(DbConnection visConn, PremiumModel premium, string inAppItemId)
        {

            PrintPremiumDetail(premium, false);
            var updateQuery = "UPDATE InsuranceApplicationItem "+
                               "SET TotalBeforeFee = "+premium.PremiumBeforeFee+
                               " ,TotalDuty = "+premium.Duty+
                               " ,TotalAfterFee = " + premium.PremiumAfterFee +
                              " ,ModifiedUsername = 'RSTOWER/vichayas'"+
                               " WHERE Id = '" + inAppItemId + "'";

            var reader = visConn.ExecutrQueryReader(updateQuery);
            visConn.CloseConnection();
            //var result = reader.Read();
        }

        private static void CallDeletedInsuredSP(DbConnection visConn, EndorseModel objEndModel)
        {
            var reader = visConn.UpdateInsuredPremium("DivPersonCalculate", objEndModel.PolicyNumber, objEndModel.APENumber);
            if (reader.Read())
            {
                Console.WriteLine(" Adding Inseured premium : " + (reader["TotalNetPremiumEndos"].ToString()));
                var amount = reader["TotalNetPremiumEndos"].ToString().ToDecimal();
                objEndModel.ExpectedPremiumForDel.PremiumBeforeFee = amount.ToString();
                objEndModel.ExpectedPremiumForDel.PremiumAfterFee = objEndModel.ExpectedPremiumForDel.PremiumBeforeFee;
                objEndModel.ExpectedPremiumForDel.Duty = "0";
            }
            visConn.CloseConnection();
        }


        private static void CallPremiumEndorse(EndorseModel objEndModel)
        {

            objEndModel.ExpectedPremiumForEndorsement.PremiumBeforeFee = (objEndModel.ExpectedPremiumForAdd.PremiumBeforeFee.ToDecimal() +
                                                                            objEndModel.ExpectedPremiumForDel.PremiumBeforeFee.ToDecimal()).ToString();
            var duty = (Math.Floor(objEndModel.ExpectedPremiumForEndorsement.PremiumBeforeFee.ToDecimal() * (0.4M / 100)))+1;
            objEndModel.ExpectedPremiumForEndorsement.Duty = (duty < 0) ? "0" : duty.ToString();
           objEndModel.ExpectedPremiumForEndorsement.PremiumAfterFee = (objEndModel.ExpectedPremiumForEndorsement.PremiumBeforeFee.ToDecimal() + objEndModel.ExpectedPremiumForEndorsement.Duty.ToDecimal()).ToString();


        }

        private static void CallInsertInsuredSP(DbConnection visConn, EndorseModel objEndModel)
        {
            Console.WriteLine("\n================================================");
            Console.WriteLine("1. Update Insured's premium ");
            Console.WriteLine("================================================\n");
            var reader = visConn.UpdateInsuredPremium("InsertPersonCalculate", objEndModel.PolicyNumber, objEndModel.APENumber);
            if (reader.Read())
            {
                var premiumAmount = reader["TotalNetPremiumEndos"].ToString().ToDecimal();
                Console.Write("premiumAmount = " + premiumAmount);
                premiumAmount = Math.Round(premiumAmount/2, 2);
                Console.WriteLine(" =>  Math.Round(premiumAmount/2) =" + premiumAmount);
                var duty = premiumAmount * (0.4M / 100);
                Console.Write("Duty = " + duty);
                duty = Math.Ceiling(duty);
                Console.WriteLine(" =>  Math.Ceiling(duty) =" + duty);
                //objEndModel.ExpectedPremiumForAdd.PremiumBeforeFee = premiumAmount.ToString();
                //objEndModel.ExpectedPremiumForAdd.PremiumAfterFee = (premiumAmount+duty).ToString();
                //objEndModel.ExpectedPremiumForAdd.Duty = (duty).ToString();
            }
            visConn.CloseConnection();
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

                    endorseModel.InitialPremium(endorseModel.ActualPremiumForAdd,
                                                reader["TotalBeforeFee"].ToString(),
                                                reader["TotalDuty"].ToString(),
                                                reader["TotalAfterFee"].ToString());

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
                else
                {
                    endorseModel.HaveOrtherTypeEnd = true;
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
                Console.WriteLine("================================================\nReference Number: " + reader["ReferenceNumber"]);
                Console.WriteLine("the number of End_seq in policy: " + reader["END_Sequence"]);
                Console.WriteLine("InsuranceApplication Id: " + reader["InsuranceApplication_Id"]);
                Console.WriteLine("================================================");
                endorseModel.PolicyAgreementId = reader["AgreementId"].ToString();
                endorseModel.CurrPolicyEndSeq = Int32.Parse(reader["END_Sequence"].ToString());
                endorseModel.PolicyInAppId = reader["InsuranceApplication_Id"].ToString();
            }

            visConn.CloseConnection();

            //Find the number of endorsement
            var query = "SELECT IA1.END_Sequence as LastEndSeq, A.ReferenceNumber as ReferenceNumber FROM InsuranceApplication IA1   INNER JOIN InsuranceApplication IA2 ON IA2.Id = IA1.ParentId  Inner join Agreement A on (IA1.Id = A.InsuranceApplication_Id)  WHERE IA1.PolicyNumber = '" + endorseModel.PolicyNumber + "' ORDER BY IA1.END_Sequence DESC";
            reader = visConn.ExecutrQueryReader(query);
            if (reader.Read())
            {
                Console.WriteLine("current number of End_seq: " + reader["LastEndSeq"]);
                if (String.IsNullOrEmpty(reader["ReferenceNumber"].ToString()))
                {
                    Console.WriteLine("Some one hasn't done creating an application (Insurance/Endorsement)!!");
                }
                Console.WriteLine("================================================");
            }
            visConn.CloseConnection();

            //Find the sequence of this endorsement
            query = "SELECT IA1.END_Sequence,iai.TotalBeforeFee,iai.TotalDuty  FROM InsuranceApplication IA1   INNER JOIN InsuranceApplication IA2 ON IA2.Id = IA1.ParentId Inner join Agreement A on (IA1.Id = A.InsuranceApplication_Id AND A.ReferenceNumber = '" + endorseModel.APENumber + "')  INNER JOIN InsuranceApplicationItem iai on iai.InsuranceApplication_Id=IA1.Id WHERE IA1.PolicyNumber = '" + endorseModel.PolicyNumber + "' AND iai.InsuranceApplicationItemType_Id like 'F8%'";
            reader = visConn.ExecutrQueryReader(query);
            while (reader.Read())
            {
                Console.WriteLine("current End_seq of This endorsement: " + reader["END_Sequence"]);
                Console.WriteLine("\nCurrent Endorsement \n\tEnd_seq: {0} , \n\tTotalBeforeFee: {1}, \n\tDuty: {2}", reader["END_Sequence"], reader["TotalBeforeFee"], reader["TotalDuty"]);
                Console.WriteLine("================================================");
                endorseModel.AuctualEndorseSeq = Int32.Parse(reader["END_Sequence"].ToString());
            }
            visConn.CloseConnection();
        }

        private static void FindPreviousEndPremiumAndDuty(DbConnection visConn, EndorseModel endorseModel)
        {
            var query = "SELECT IA1.END_Sequence,iai.TotalBeforeFee,iai.TotalDuty FROM InsuranceApplication IA1   INNER JOIN InsuranceApplication IA2 ON IA2.Id = IA1.ParentId  INNER JOIN InsuranceApplicationItem iai on iai.InsuranceApplication_Id=IA1.Id WHERE IA1.PolicyNumber = '" + endorseModel.PolicyNumber + "' AND IA1.END_Sequence = '" + (endorseModel.AuctualEndorseSeq-1)+ "' AND iai.InsuranceApplicationItemType_Id like 'F8%'";
            var reader = visConn.ExecutrQueryReader(query);
            while (reader.Read())
            {       
                endorseModel.PreviousPolicyPremiumBeforeFee = reader["TotalBeforeFee"].ToString().ToDecimal();
                endorseModel.PreviousPolicyDuty  = reader["TotalDuty"].ToString().ToDecimal();
                Console.WriteLine("\nPrevious Endorsement \n\tEnd_seq: {0} , \n\tTotalBeforeFee: {1}, \n\tDuty: {2}", reader["END_Sequence"], reader["TotalBeforeFee"], reader["TotalDuty"]);
                Console.WriteLine("================================================");
            }
            visConn.CloseConnection();
        }

    }
}
