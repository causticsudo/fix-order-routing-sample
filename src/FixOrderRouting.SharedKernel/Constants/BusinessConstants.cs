namespace FixOrderRouting.SharedKernel.Constants;

public static class BusinessConstants
{
    public static class Symbols
    {
        public static readonly IReadOnlySet<string> Valid = new HashSet<string> { "PETR4", "VALE3", "VIIA4" };
    }

    public static class Orders
    {
        public const decimal ExposureLimit = 100_000_000m;
        public const long MinQuantity = 1;
        public const long MaxQuantity = 99_999;
        public const decimal MinPrice = 0.01m;
        public const decimal MaxPrice = 999.99m;
    }

    public static class Sides
    {
        public const string Buy = "BUY";
        public const string Sell = "SELL";
    }
}
