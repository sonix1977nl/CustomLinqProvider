using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CustomLinqProvider
{
    public static class SqlHelper
    {
        public static string ToSqlConstant(this object value)
        {
            if (value == null)
            {
                return "NULL";
            }

            var type = value.GetType();
            if (type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                value = typeof(Nullable<>).GetProperty("Value").GetValue(value, null);
                type = value.GetType();
            }
                
            var typeCode = Type.GetTypeCode(type);

            if (typeCode == TypeCode.Object)
                throw new InvalidOperationException(string.Format("Constants of type [{0}] are unsupported.", value.GetType().FullName));

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return ((bool)value) ? "1" : "0";

                case TypeCode.String:
                    return ((string)value).ToSqlConstant();

                case TypeCode.DateTime:
                    return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
                    break;

                default:
                    return string.Format(CultureInfo.InvariantCulture, "{0}", value);
            }
        }

        public static string ToSqlConstant(this string value)
        {
            value = value.Replace("'", "''");
            return "'" + value + "'";
        }
    }
}
