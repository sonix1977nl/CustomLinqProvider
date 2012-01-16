using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomLinqProvider
{
    [Flags]
    public enum SqlReaderConverterFlags
    {
        None = 0,
        AtLeastOneRow = 1,
        AtMostOneRow = 2
    }
}
