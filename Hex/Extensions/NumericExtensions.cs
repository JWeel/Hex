namespace Hex.Extensions
{
    public static class NumericExtensions
    {
        public static float AddWithLowerLimit(this float source, float amount, float lowerLimit) =>
            source.AddWithLimitsImplementation(amount, lowerLimit: lowerLimit);

        public static float AddWithUpperLimit(this float source, float amount, float upperLimit) =>
            source.AddWithLimitsImplementation(amount, upperLimit: upperLimit);
            
        public static float AddWithLimits(this float source, float amount, float lowerLimit, float upperLimit) =>
            source.AddWithLimitsImplementation(amount, lowerLimit, upperLimit);

        private static float AddWithLimitsImplementation(this float source, float amount, float? lowerLimit = default, float? upperLimit = default)
        {
            var value = source + amount;
            if (lowerLimit.HasValue && (value < lowerLimit.Value))
                return lowerLimit.Value;
            if (upperLimit.HasValue && (value > upperLimit.Value))
                return upperLimit.Value;
            return value;
        }

        public static int AddWithLowerLimit(this int source, int amount, int lowerLimit) =>
            source.AddWithLimitsImplementation(amount, lowerLimit: lowerLimit);

        public static int AddWithUpperLimit(this int source, int amount, int upperLimit) =>
            source.AddWithLimitsImplementation(amount, upperLimit: upperLimit);

        public static int AddWithLimits(this int source, int amount, int lowerLimit, int upperLimit) =>
            source.AddWithLimitsImplementation(amount, lowerLimit, upperLimit);

        private static int AddWithLimitsImplementation(this int source, int amount, int? lowerLimit = default, int? upperLimit = default)
        {
            var value = source + amount;
            if (lowerLimit.HasValue && (value < lowerLimit.Value))
                return lowerLimit.Value;
            if (upperLimit.HasValue && (value > upperLimit.Value))
                return upperLimit.Value;
            return value;
        }

        public static int IfOddAddOne(this int value) =>
            value % 2 == 0 ? value : value + 1;

        public static int IfOddSubtractOne(this int value) =>
            value % 2 == 0 ? value : value - 1;
    }
}