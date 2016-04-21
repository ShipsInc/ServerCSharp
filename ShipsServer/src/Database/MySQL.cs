using System;
using MySql.Data.MySqlClient;

namespace ShipsServer.Database
{
    public class MySQL : IDisposable
    {
        private const string Host = "127.0.0.1";
        private const string Username = "root";
        private const string Password = "root";
        private const int Port = 3306;

        private MySQL() { }

        private string DatabaseName { get; set; } = "ships";

        private MySqlConnection connection = null;

        private MySqlConnection Connection
        {
            get { return connection; }
        }

        private static MySQL _instance = null;
        public static MySQL Instance()
        {
            if (_instance == null)
                _instance = new MySQL();

            return _instance;
        }

        public int PExecute(string query)
        {
            var cmd = new MySqlCommand(query, connection);
            try
            {
                return cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return -1;
        }

        public MySqlDataReader Execute(string query)
        {
            if (!IsConnect())
                return null;

            var cmd = new MySqlCommand(query, connection);
            try
            {
                return cmd.ExecuteReader();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return null;
        }

        private bool IsConnect()
        {
            if (Connection != null)
                return true;

            if (string.IsNullOrEmpty(DatabaseName))
                return false;

            bool result = false;
            try
            {
                connection = new MySqlConnection($"Server={Host}; database={DatabaseName}; UID={Username}; password={Password}; port={Port}");
                connection.Open();
                result = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"MySQL Exeption: {0}", e.Message);
            }

            return result;
        }

        public void BeginTransaction()
        {
            PExecute("START TRANSACTION");
        }

        public void CommitTransaction()
        {
            PExecute("COMMIT");
        }

        public void RollbackTransaction()
        {
            PExecute("ROLLBACK");
        }

        public void Close()
        {
            if (connection == null)
                return;

            connection.Close();
            connection = null;
        }

        public override string ToString()
        {
            return !IsConnect() ? string.Format($"MySQL: Failed connect to {0}", DatabaseName) : string.Format($"MySQL: Connect to {0}. Server version: {1}", DatabaseName, connection.ServerVersion);
        }

        public void Dispose()
        {
            if (IsConnect())
                Close();
        }
    }
}
