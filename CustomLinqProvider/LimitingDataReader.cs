using System;
using System.Data;

namespace CustomLinqProvider
{
    public class LimitingDataReader : IDataReader
    {
        private readonly IDataReader mInnerReader;
        private readonly LimitingDataReaderMode mMode;
        private bool mFirstRow = true;

        public LimitingDataReader(IDataReader innerReader, LimitingDataReaderMode mode)
        {
            if (innerReader == null) 
                throw new ArgumentNullException("innerReader");
            mInnerReader = innerReader;
            mMode = mode;
        }


        public void Dispose()
        {
            mInnerReader.Dispose();
        }

        public string GetName(int i)
        {
            return mInnerReader.GetName(i);
        }

        public string GetDataTypeName(int i)
        {
            return mInnerReader.GetDataTypeName(i);
        }

        public Type GetFieldType(int i)
        {
            return mInnerReader.GetFieldType(i);
        }

        public object GetValue(int i)
        {
            return mInnerReader.GetValue(i);
        }

        public int GetValues(object[] values)
        {
            return mInnerReader.GetValues(values);
        }

        public int GetOrdinal(string name)
        {
            return mInnerReader.GetOrdinal(name);
        }

        public bool GetBoolean(int i)
        {
            return mInnerReader.GetBoolean(i);
        }

        public byte GetByte(int i)
        {
            return mInnerReader.GetByte(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return mInnerReader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        }

        public char GetChar(int i)
        {
            return mInnerReader.GetChar(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return mInnerReader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        }

        public Guid GetGuid(int i)
        {
            return mInnerReader.GetGuid(i);
        }

        public short GetInt16(int i)
        {
            return mInnerReader.GetInt16(i);
        }

        public int GetInt32(int i)
        {
            return mInnerReader.GetInt32(i);
        }

        public long GetInt64(int i)
        {
            return mInnerReader.GetInt64(i);
        }

        public float GetFloat(int i)
        {
            return mInnerReader.GetFloat(i);
        }

        public double GetDouble(int i)
        {
            return mInnerReader.GetDouble(i);
        }

        public string GetString(int i)
        {
            return mInnerReader.GetString(i);
        }

        public decimal GetDecimal(int i)
        {
            return mInnerReader.GetDecimal(i);
        }

        public DateTime GetDateTime(int i)
        {
            return mInnerReader.GetDateTime(i);
        }

        public IDataReader GetData(int i)
        {
            return mInnerReader.GetData(i);
        }

        public bool IsDBNull(int i)
        {
            return mInnerReader.IsDBNull(i);
        }

        public int FieldCount
        {
            get { return mInnerReader.FieldCount; }
        }

        public object this[int i]
        {
            get { return mInnerReader[i]; }
        }

        public object this[string name]
        {
            get { return mInnerReader[name]; }
        }

        public void Close()
        {
            mInnerReader.Close();
        }

        public DataTable GetSchemaTable()
        {
            return mInnerReader.GetSchemaTable();
        }

        public bool NextResult()
        {
            mFirstRow = true; 
            return mInnerReader.NextResult();
        }

        public bool Read()
        {
            var result = mInnerReader.Read();
            if (IsModeAtLeastOneRow && mFirstRow && !result)
                throw new InvalidOperationException("Sequence contains no elements");
            if (IsModeAtMostOneRow && !mFirstRow && result)
                throw new InvalidOperationException("Sequence contains more than one element");
            mFirstRow = false;
            return result;
        }

        private bool IsModeAtLeastOneRow
        {
            get { return (mMode & LimitingDataReaderMode.AtLeastOneRow) != 0; }
        }

        private bool IsModeAtMostOneRow
        {
            get { return (mMode & LimitingDataReaderMode.AtMostOneRow) != 0; }
        }

        public int Depth
        {
            get { return mInnerReader.Depth; }
        }

        public bool IsClosed
        {
            get { return mInnerReader.IsClosed; }
        }

        public int RecordsAffected
        {
            get { return mInnerReader.RecordsAffected; }
        }
    }
}
