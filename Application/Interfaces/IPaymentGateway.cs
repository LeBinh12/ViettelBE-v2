namespace Application.Interfaces;

public interface IPaymentGateway
{
    string GeneratePaymentLink(Guid invoiceId, decimal amount);
    bool VerifyCallback(Dictionary<string, string> callbackParams);
}
