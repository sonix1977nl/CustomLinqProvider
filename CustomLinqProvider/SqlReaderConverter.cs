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
        private readonly SqlReaderConverterFlags mFlags;

        public SqlReaderConverter(SqlDataReader reader, IEnumerable<PropertyInfo> properties, SqlReaderConverterFlags flags)
        {
            mReader = reader;
            mProperties = properties.ToArray();
            mFlags = flags;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int rowCount = 0;
            while (mReader.Read())
            {
                if ((mFlags & SqlReaderConverterFlags.AtMostOneRow) != 0 && rowCount > 1)
                    throw new InvalidOperationException("Sequence contains more than one element");

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

            if ((mFlags & SqlReaderConverterFlags.AtLeastOneRow) != 0 && rowCount < 1)
                throw new InvalidOperationException("Sequence contains no elements");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
