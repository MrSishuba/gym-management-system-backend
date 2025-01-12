using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SendGrid;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.IO;
using iText.Layout.Borders;
using iText.IO.Font.Constants;
using iText.Kernel.Font;

namespace av_motion_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EmailController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        [Route("Send3MonthContract")]
        public async Task<IActionResult> Send3MonthContract([FromBody] ContractRequestViewModel model)
        {
            string contractType = "3-month Contract";
            string contractFileName = "3-month-contract.pdf";
            string monthlyFee = "600";
            string duration = "3 months";

            var result = await GenerateAndSendContractAndConsentForm(model.Email, contractType, contractFileName, monthlyFee, duration);
            return result;
        }

        [HttpPost]
        [Route("Send6MonthContract")]
        public async Task<IActionResult> Send6MonthContract([FromBody] ContractRequestViewModel model)
        {
            string contractType = "6-month Contract";
            string contractFileName = "6-month-contract.pdf";
            string monthlyFee = "500";
            string duration = "6 months";

            var result = await GenerateAndSendContractAndConsentForm(model.Email, contractType, contractFileName, monthlyFee, duration);
            return result;
        }

        [HttpPost]
        [Route("Send12MonthContract")]
        public async Task<IActionResult> Send12MonthContract([FromBody] ContractRequestViewModel model)
        {
            string contractType = "12-month Contract";
            string contractFileName = "12-month-contract.pdf";
            string monthlyFee = "400";
            string duration = "12 months";

            var result = await GenerateAndSendContractAndConsentForm(model.Email, contractType, contractFileName, monthlyFee, duration);
            return result;
        }

        private async Task<IActionResult> GenerateAndSendContractAndConsentForm(string email, string contractType, string contractFileName, string monthlyFee, string duration)
        {
            string contractsDirectory = _configuration["SendGrid:ContractsDirectory"];

            // Generate Contract
            string contractFilePath = Path.Combine(contractsDirectory, contractFileName);
            DateTime date = DateTime.Now;
            ContractGenerator.GenerateContract(contractFilePath, contractType, monthlyFee, duration, date);

            // Generate Debit Order Consent Form
            string consentFormFileName = "debit-order-consent-form.pdf";
            string consentFormFilePath = Path.Combine(contractsDirectory, consentFormFileName);
            ContractGenerator.GenerateDebitOrderConsentForm(consentFormFilePath, date);

            var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
            var from = new EmailAddress(_configuration["SendGrid:SenderEmail"], _configuration["SendGrid:SenderName"]);
            var subject = $"{contractType} Request";
            var to = new EmailAddress(email);
            var plainTextContent = $"You requested a {contractType}. Please find the attached contract and Debit Order Consent Form.";
            var htmlContent = $"<strong>You requested a {contractType}.</strong><br><br>Please find the attached contract and Debit Order Consent Form.";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            // Attach the contract PDF
            if (System.IO.File.Exists(contractFilePath))
            {
                var contractBytes = await System.IO.File.ReadAllBytesAsync(contractFilePath);
                var contractFileBase64 = Convert.ToBase64String(contractBytes);
                msg.AddAttachment(contractFileName, contractFileBase64);
            }
            else
            {
                return NotFound("Contract file not found.");
            }

            // Attach the Debit Order Consent Form PDF
            if (System.IO.File.Exists(consentFormFilePath))
            {
                var consentFormBytes = await System.IO.File.ReadAllBytesAsync(consentFormFilePath);
                var consentFormFileBase64 = Convert.ToBase64String(consentFormBytes);
                msg.AddAttachment(consentFormFileName, consentFormFileBase64);
            }
            else
            {
                return NotFound("Debit Order Consent Form file not found.");
            }

            var response = await client.SendEmailAsync(msg);

            // Check for 200 OK or 202 Accepted
            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                return Ok($"{contractType} email with Debit Order Consent Form sent.");
            }
            else
            {
                return StatusCode((int)response.StatusCode, "Error sending email.");
            }
        }

        public class ContractRequestViewModel
        {
            public string Email { get; set; }
        }
    }

    public static class ContractGenerator
    {
        public static void GenerateContract(string filePath, string contractType, string monthlyFee, string duration, DateTime date)
        {
            using (PdfWriter writer = new PdfWriter(filePath))
            {
                using (PdfDocument pdf = new PdfDocument(writer))
                {
                    Document document = new Document(pdf);

                    // Set the font to Times Roman
                    PdfFont timesRoman = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);

                    // Add title (bold and centered)
                    Paragraph title = new Paragraph("Membership Contract Template - " + duration)
                        .SetFont(timesRoman)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(20)
                        .SetBold();
                    document.Add(title);

                    // Add contract header (bold and centered)
                    Paragraph contractHeader = new Paragraph("Membership Contract")
                        .SetFont(timesRoman)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(16)
                        .SetBold();
                    document.Add(contractHeader);

                    // Add effective date with space for input
                    Paragraph dateInfo = new Paragraph($"This agreement is made effective as of Date [YYYY/MM/DD] __________________________ between AVS Fitness (hereinafter referred to as \"the Gym\") and [Member's Name] __________________________ (hereinafter referred to as \"the Member\").")
                        .SetFont(timesRoman)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12);
                    document.Add(dateInfo);

                    // Add contract term with space for dates
                    Paragraph termInfo = new Paragraph($"Term: This membership is valid for a period of {duration}, starting from Start Date [YYYY/MM/DD] __________________________ to End Date [YYYY/MM/DD] __________________________.")
                        .SetFont(timesRoman)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12)
                        .SetBold();
                    document.Add(termInfo);

                    // Add membership fee (bold)
                    Paragraph feeInfo = new Paragraph($"Membership Fee: The Member agrees to pay the sum of [R{monthlyFee} in ZAR] (South African Rand) for the {duration} membership period.")
                        .SetFont(timesRoman)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12)
                        .SetBold();
                    document.Add(feeInfo);

                    // Add terms and conditions (bold)
                    Paragraph termsHeader = new Paragraph("Terms and Conditions:")
                        .SetFont(timesRoman)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12)
                        .SetBold();
                    document.Add(termsHeader);

                    // Add terms and conditions text
                    string terms = @"
                1. The Member agrees to abide by the rules and regulations of AVS Fitness during the membership period.
                2. Payment must be made in full at the commencement of the membership.
                3. The membership fee is non-refundable and non-transferable.
                4. The Gym reserves the right to amend membership rules and facility schedules.
            ";
                    document.Add(new Paragraph(terms)
                        .SetFont(timesRoman)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12));

                    // Add signatures section (bold)
                    Paragraph signaturesHeader = new Paragraph("Signatures:")
                        .SetFont(timesRoman)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12)
                        .SetBold();
                    document.Add(signaturesHeader);

                    // Add space for signatures
                    string signatures = @"
                [Signature of Member] __________________________________________
                Date: [YYYY/MM/DD] __________________________________________
                [Signature of Gym Representative] _______________________________
                Date: [YYYY/MM/DD] __________________________________________
                [Printed Name of Member] _____________________________________
                [Printed Name of Gym Representative] ___________________________
            ";
                    document.Add(new Paragraph(signatures)
                        .SetFont(timesRoman)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12));

                    // Add a fake logo (text-based)
                    Paragraph logo = new Paragraph("AVS Fitness")
                        .SetFont(timesRoman)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(12);
                    document.Add(logo);

                    document.Close();
                }
            }
        }

        public static void GenerateDebitOrderConsentForm(string filePath, DateTime date)
        {
            using (PdfWriter writer = new PdfWriter(filePath))
            {
                using (PdfDocument pdf = new PdfDocument(writer))
                {
                    Document document = new Document(pdf);
                    PdfFont font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
                    document.SetFont(font);

                    // Add title
                    Paragraph title = new Paragraph("AVS FITNESS DEBIT ORDER MANDATE AND CONSENT FORM")
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetFontSize(20)
                        .SetBold();
                    document.Add(title);

                    // Add banking details
                    string bankingDetails = @"
                                            AVS Fitness Banking Details:

                                            Bank Name: First National Bank (FNB)
                                            Branch Name: Sandton
                                            Branch Code: 250655
                                            Account Number: 12345678901
                                            Account Type: Business Account
                                            ";
                    document.Add(new Paragraph(bankingDetails).SetMarginTop(20).SetTextAlignment(TextAlignment.LEFT).SetFontSize(12));

                    // Add placeholders for member's banking details
                    string placeholders = @"
                                            Account Holder Name: ___________________________
                                            Bank Name: ___________________________
                                            Branch Name: ___________________________
                                            Branch Code: ___________________________
                                            Account Number: ___________________________
                                            Account Type: ___________________________
                                            ";
                    document.Add(new Paragraph(placeholders).SetMarginTop(20).SetTextAlignment(TextAlignment.LEFT).SetFontSize(12));

                    // Add the DEBIT ORDER MANDATE section
                    Paragraph mandateHeader = new Paragraph("A. DEBIT ORDER MANDATE")
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12)
                        .SetBold();
                    document.Add(mandateHeader);

                    string mandateText = @"
                                        I/We hereby authorize AVS Fitness to collect the agreed monthly membership fee from my/our above-mentioned bank account. This authorization allows for the collection of R600 for a 3-month contract, R500 for a 6-month contract, or R400 for a 12-month contract. The first payment is expected exactly 30 days from the subscription date, which is the day the member becomes active in the system. This mandate will remain in place until the total amount due for my/our membership is fully paid.
                                        ";
                    document.Add(new Paragraph(mandateText).SetTextAlignment(TextAlignment.LEFT).SetFontSize(12));

                    // Add the AUTHORITY section
                    Paragraph authorityHeader = new Paragraph("B. AUTHORITY")
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12)
                        .SetBold();
                    document.Add(authorityHeader);

                    string authorityText = @"
                                            I/We authorize AVS Fitness to issue payment instructions to my/our bank for collection against my/our account as specified above. The amount of such payment instructions will never exceed my/our obligations as agreed to in the membership contract. This mandate will begin on the date I/we sign the membership contract and will continue until the mandate is terminated by me/us in writing.
                                            ";
                    document.Add(new Paragraph(authorityText).SetTextAlignment(TextAlignment.LEFT).SetFontSize(12));

                    // Add the TERMS AND CONDITIONS section
                    Paragraph termsHeader = new Paragraph("C. TERMS AND CONDITIONS")
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12)
                        .SetBold();
                    document.Add(termsHeader);

                    string termsText = @"
                                        Payment Timing: If the due date falls on a weekend or public holiday, the payment will be collected on the next business day.

                                        Insufficient Funds: Should there be insufficient funds in my/our account to meet the debit order obligation, AVS Fitness is authorized to re-present the instruction for payment as soon as sufficient funds are available.

                                        Late Fee: Failure to make a payment on the due date will result in an additional late fee of R30, which will be added to the expected payment amount.

                                        Bank Statement: I/We understand that all details of this debit order will be reflected in my/our bank statement or accompanying voucher.

                                        Cancellation: I/We agree that this mandate may only be canceled with a 20-day written notice to AVS Fitness. However, such cancellation will not cancel the membership contract or the obligation to pay the full contractual amount owed by the date of the last payment, which is 30 days from the contract expiry date.

                                        No Refund: In the event of a cancellation, I/we will not be entitled to a refund of any amounts legally withdrawn while this mandate was in effect.
                                    ";
                    document.Add(new Paragraph(termsText).SetTextAlignment(TextAlignment.LEFT).SetFontSize(12));

                    // Add the MANDATE section
                    Paragraph mandateAckHeader = new Paragraph("D. MANDATE")
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12)
                        .SetBold();
                    document.Add(mandateAckHeader);

                    string mandateAckText = @"
                                            I/We acknowledge that payment instructions issued by AVS Fitness will be treated by my/our bank as if they were issued by me/us personally.
                                            ";
                    document.Add(new Paragraph(mandateAckText).SetTextAlignment(TextAlignment.LEFT).SetFontSize(12));

                    // Add the CANCELLATION section
                    Paragraph cancellationHeader = new Paragraph("E. CANCELLATION")
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12)
                        .SetBold();
                    document.Add(cancellationHeader);

                    string cancellationText = @"
                                            I/We understand that this mandate can be canceled by giving AVS Fitness 20 ordinary working days' written notice. However, such cancellation will not cancel my/our membership contract or any obligations thereunder, including the obligation to pay the full contractual amount by the date of the last payment.
                                            ";
                    document.Add(new Paragraph(cancellationText).SetTextAlignment(TextAlignment.LEFT).SetFontSize(12));

                    // Add the ASSIGNMENT section
                    Paragraph assignmentHeader = new Paragraph("F. ASSIGNMENT")
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFontSize(12)
                        .SetBold();
                    document.Add(assignmentHeader);

                    string assignmentText = @"
                                            I/We acknowledge that this mandate may be ceded or assigned to a third party if my/our membership contract is also ceded or assigned to that third party. Without such assignment, this mandate cannot be transferred to any third party.
                                            ";
                    document.Add(new Paragraph(assignmentText).SetTextAlignment(TextAlignment.LEFT).SetFontSize(12));

                    // Add placeholders for signatures
                    string signatures = @"
                                        Signed at ______________________ on this ____ day of _______________ 20____

                                        Account Holder's Full Name: ___________________________
                                        Signature: ___________________________
                                        Date: ___________________________
                                        ";
                    document.Add(new Paragraph(signatures).SetMarginTop(20).SetTextAlignment(TextAlignment.LEFT).SetFontSize(12));

                    document.Close();
                }
            }
        }
    }

}
