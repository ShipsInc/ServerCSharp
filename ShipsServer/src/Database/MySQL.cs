using System;
using MySql.Data.MySqlClient;
using ShipsServer.Common;

namespace ShipsServer.Database
{
    public class MySQL : IDisposable
    {
        public MySQL() { }

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

        public bool Initialization()
        {
            if (string.IsNullOrEmpty(Constants.DB_DBNAME))
                return false;

            try
            {
                connection = new MySqlConnection($"Server={Constants.DB_HOST}; database={Constants.DB_DBNAME}; UID={Constants.DB_USERNAME}; password={Constants.DB_PASSWORD}; port={Constants.DB_PORT}");
                connection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine($"MySQL initialization error: {e.Message}");
                return false;
            }

            return true;
        }

        public long PExecute(string query)
        {
            var cmd = new MySqlCommand(query, connection);
            try
            {
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
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

            return Initialization();
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
            return !IsConnect() ? string.Format($"MySQL: Failed connect to {0}", Constants.DB_DBNAME) : string.Format($"MySQL: Connect to {0}. Server version: {1}", Constants.DB_DBNAME, connection.ServerVersion);
        }

        public void Dispose()
        {
            if (IsConnect())
                Close();
        }
    }
}
