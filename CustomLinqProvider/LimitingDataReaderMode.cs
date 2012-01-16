using System;

namespace CustomLinqProvider
{
    [Flags]
    public enum LimitingDataReaderMode
    {
        None = 0,
        AtLeastOneRow = 1,
        AtMostOneRow = 2
    }
}
