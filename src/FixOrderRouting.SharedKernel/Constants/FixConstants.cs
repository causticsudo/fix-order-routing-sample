namespace FixOrderRouting.SharedKernel.Constants;

public static class FixConstants
{
    public static class ExecType
    {
        public const string New = "0";
        public const string Rejected = "8";
    }

    public static class OrdType
    {
        public const char Market = '1';
        public const char Limit = '2';
    }

    public static class Side
    {
        public const char Buy = '1';
        public const char Sell = '2';
    }

    public static class OrdStatus
    {
        public const char New = '0';
        public const char PartiallyFilled = '1';
        public const char Filled = '2';
        public const char DoneForDay = '3';
        public const char Canceled = '4';
        public const char Rejected = '8';
    }
}
