using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomLinqProvider
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public string SchemaName { get; set; }

        public string Name { get; set; }
    }
}
