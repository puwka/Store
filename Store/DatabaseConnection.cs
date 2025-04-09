using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Store
{
    internal class DatabaseConnection
    {
        private string connectionString = "Server=localhost;Port=5432;Database=Store;User Id=postgres;Password=12345";

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(connectionString);
        }
    }
}
