﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace CustomLinqProvider
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var connection = new SqlConnection(@"Server=.;Database=AdventureWorks;Integrated Security=True"))
            using (var context = new DataContext(connection))
            {
                var result = context.GetTable<Contact>().Select(a => a).ToArray();//.FirstOrDefault();
            }
        }
    }
}