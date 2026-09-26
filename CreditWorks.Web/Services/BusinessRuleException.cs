namespace CreditWorks.Web.Services;

public sealed class BusinessRuleException(string message) : Exception(message);
