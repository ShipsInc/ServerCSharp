using ShipsServer.Database;

namespace ShipsServer.Server
{
    public class Statistics
    {
        public Statistics(uint accountId)
        {
            AccountId = accountId;
            Load();
        }

        public long LastBattle { get; set; }
        public uint Wins { get; set; }
        public uint Loose { get; set; }
        public uint AccountId { get; private set; }

        public void SaveToDB()
        {
            var mysql = MySQL.Instance();
            mysql.BeginTransaction();
            mysql.PExecute($"DELETE FROM `statistics` WHERE `Id` = {AccountId}");
            mysql.PExecute($"INSERT INTO `statistics` (`Id`, `lastBattle`, `wins`, `loose`) VALUES ({AccountId}, {LastBattle}, {Wins}, {Loose})");
            mysql.CommitTransaction();;
        }

        private void Load()
        {
            var mysql = MySQL.Instance();
            using (var reader = mysql.Execute($"SELECT `lastBattle`, `wins`, `loose` FROM `statistics` WHERE `Id` = '{AccountId}'"))
            {
                if (reader == null || !reader.Read())
                    return;

                LastBattle = reader.GetUInt32(0);
                Wins = reader.GetUInt16(1);
                Loose = reader.GetUInt16(2);
            }
        }
    }
}
