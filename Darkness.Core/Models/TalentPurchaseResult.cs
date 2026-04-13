namespace Darkness.Core.Models;

public class TalentPurchaseResult 
{
    public bool Success { get; set; }
    public string? FailureReason { get; set; }

    public static TalentPurchaseResult Succeeded() => new() { Success = true };
    public static TalentPurchaseResult Failed(string reason) => new() { Success = false, FailureReason = reason };
}
