namespace PaymentGateway.Api.Constants;

public static class PaymentConstants
{
    public static class AllowedCurrencies
    {
        public const string GBP = "GBP";
        public const string USD = "USD";
        public const string EUR = "EUR";

        public static readonly string[] All = { GBP, USD, EUR };
    }
}
