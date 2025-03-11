using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

[ApiController]
[Route("api/submittrxmessage")]
public class TransactionController : ControllerBase
{
    private static readonly Dictionary<string, string> AllowedPartners = new()
    {
        { "FAKEGOOGLE", "FAKEPASSWORD1234" },
        { "FAKEPEOPLE", "FAKEPASSWORD4578" }
    };

    [HttpPost]
    public IActionResult SubmitTransaction([FromBody] TransactionRequest request)
    {
        var validationErrors = ValidateRequest(request);
        if (validationErrors.Any())
        {
            return BadRequest(new { result = 0, resultmessage = string.Join(", ", validationErrors) });
        }

        string generatedSig = GenerateSignature(request);
        if (generatedSig != request.Sig)
        {
            return BadRequest(new { result = 0, resultmessage = "Access Denied!" });
        }

        decimal discountPercentage = CalculateDiscount(request.TotalAmount);
        decimal totalDiscount = request.TotalAmount * discountPercentage;
        decimal finalAmount = request.TotalAmount - totalDiscount;

        return Ok(new { result = 1, totalamount = request.TotalAmount, totaldiscount = totalDiscount, finalamount = finalAmount });
    }

    private List<string> ValidateRequest(TransactionRequest request)
    {
        var errors = new List<string>();

        // Mandatory Field Checks
        if (string.IsNullOrEmpty(request.PartnerKey)) errors.Add("partnerkey is required.");
        if (string.IsNullOrEmpty(request.PartnerRefNo)) errors.Add("partnerrefno is required.");
        if (string.IsNullOrEmpty(request.PartnerPassword)) errors.Add("partnerpassword is required.");
        if (string.IsNullOrEmpty(request.Timestamp)) errors.Add("timestamp is required.");
        if (string.IsNullOrEmpty(request.Sig)) errors.Add("sig is required.");
        if (request.TotalAmount <= 0) errors.Add("Invalid Total Amount.");

        // Partner Authentication
        if (!AllowedPartners.ContainsKey(request.PartnerKey)) errors.Add("Access Denied!");

        string decodedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(request.PartnerPassword));
        if (AllowedPartners.TryGetValue(request.PartnerKey, out string correctPassword) && decodedPassword != correctPassword)
        {
            errors.Add("Access Denied!");
        }

        // Timestamp Validation (must be within ±5 minutes of server time)
        if (DateTime.TryParse(request.Timestamp, null, DateTimeStyles.RoundtripKind, out DateTime requestTime))
        {
            DateTime serverTime = DateTime.UtcNow;
            if (Math.Abs((serverTime - requestTime).TotalMinutes) > 5)
            {
                errors.Add("Expired.");
            }
        }
        else
        {
            errors.Add("Invalid timestamp format.");
        }

        // Validate Total Amount Matches Items
        if (request.Items != null && request.Items.Any())
        {
            long calculatedTotal = request.Items.Sum(i => i.Qty * i.UnitPrice);
            if (calculatedTotal != request.TotalAmount)
            {
                errors.Add("Invalid Total Amount.");
            }
        }

        return errors;
    }

    private string GenerateSignature(TransactionRequest request)
    {
        if (!DateTime.TryParse(request.Timestamp, null, DateTimeStyles.AdjustToUniversal, out DateTime timestamp))
        {
            return "Invalid Timestamp";
        }

        string formattedTimestamp = timestamp.ToUniversalTime().ToString("yyyyMMddHHmmss");
        string rawString = $"{formattedTimestamp}{request.PartnerKey}{request.PartnerRefNo}{request.TotalAmount}{request.PartnerPassword}";

        Console.WriteLine($"Corrected Raw String: {rawString}"); // for debug

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawString));
            return Convert.ToBase64String(hashBytes);
        }
    }

    private decimal CalculateDiscount(long totalAmount)
    {
        decimal baseDiscount = 0;
        if (totalAmount >= 20000 && totalAmount <= 50000) baseDiscount = 0.05m;
        else if (totalAmount >= 50100 && totalAmount <= 80000) baseDiscount = 0.07m;
        else if (totalAmount >= 80100 && totalAmount <= 120000) baseDiscount = 0.10m;
        else if (totalAmount > 120000) baseDiscount = 0.15m;

        decimal conditionalDiscount = 0;
        if (IsPrime(totalAmount) && totalAmount > 50000) conditionalDiscount += 0.08m;
        if (totalAmount > 90000 && (totalAmount / 100) % 10 == 5) conditionalDiscount += 0.10m;

        decimal totalDiscount = baseDiscount + conditionalDiscount;
        decimal maxDiscount = 0.20m;
        if (totalDiscount > maxDiscount) totalDiscount = maxDiscount;

        return totalDiscount;
    }

    private bool IsPrime(long number)
    {
        if (number < 2) return false;
        for (long i = 2; i * i <= number; i++)
        {
            if (number % i == 0) return false;
        }
        return true;
    }
}
