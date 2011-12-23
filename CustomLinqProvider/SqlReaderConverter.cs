using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Data.SqlClient;
using System.Reflection;
using System.Globalization;

namespace CustomLinqProvider
{
    internal class SqlReaderConverter<T> : IEnumerable<T> where T : class, new()
    {
        private readonly SqlDataReader mReader;
        private readonly PropertyInfo[] mProperties;

        public SqlReaderConverter(SqlDataReader reader, IEnumerable<PropertyInfo> properties)
        {
            mReader = reader;
            mProperties = properties.ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            int rowCount = 0;
            while (mReader.Read())
            {
                var @object = new T();

                for (int i = 0; i < mProperties.Length; ++i)
                {
                    var property = mProperties[i];
                    var value = mReader.GetValue(i);
                    var convertedValue = SqlConvert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture);
                    property.SetValue(@object, convertedValue, null);
                }

                yield return @object;
            }

            mReader.Dispose();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
