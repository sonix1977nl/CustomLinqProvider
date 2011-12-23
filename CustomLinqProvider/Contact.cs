using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomLinqProvider
{
    [Table(SchemaName = "Person")]
    public class Contact
    {
        [Column(Name="ContactID")]
        public int Id { get; set; }

        public string Title { get; set; }

        public string FirstName { get; set; }

        public string MiddleName { get; set; }

        public string LastName { get; set; }

        public Guid RowGuid { get; set; }

        public DateTime ModifiedDate { get; set; }
    }
}
