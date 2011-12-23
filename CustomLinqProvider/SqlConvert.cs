using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;

namespace CustomLinqProvider
{
    /// <summary>
    /// This class is an extension to the standard Convert class by providing support for DBNull and nullable value types.
    /// </summary>
    public static class SqlConvert
    {
        public static object ChangeType(object value, Type conversionType)
        {
            return ChangeType(value, conversionType, Thread.CurrentThread.CurrentCulture);
        }

        public static object ChangeType(object value, Type conversionType, IFormatProvider provider)
        {
            if (ShouldConsiderNull(value))
            {
                if (IsNullableValueType(conversionType))
                    return CreateNullableValueTypeWithNull(conversionType);

                return Convert.ChangeType(null, conversionType, provider);
            }

            if (IsNullableValueType(conversionType))
                return CreateNullableValueTypeWithValue(value, conversionType, provider);

            return Convert.ChangeType(value, conversionType, provider);
        }

        private static bool ShouldConsiderNull(object value)
        {
            return value == null || value is DBNull;
        }

        private static bool IsNullableValueType(Type conversionType)
        {
            return !conversionType.IsClass && conversionType.IsGenericType && !conversionType.IsGenericTypeDefinition &&
                conversionType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static object CreateNullableValueTypeWithNull(Type conversionType)
        {
            return Activator.CreateInstance(conversionType);
        }

        private static object CreateNullableValueTypeWithValue(object value, Type conversionType, IFormatProvider provider)
        {
            var actualValueType = conversionType.GetGenericArguments()[0];
            var convertedValue = Convert.ChangeType(value, actualValueType, provider);
            var value2 = Activator.CreateInstance(conversionType, convertedValue);
            return value2;
        }
    }
}
