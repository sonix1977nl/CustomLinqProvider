using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomLinqProvider
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
