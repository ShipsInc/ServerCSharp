namespace ShipsServer.Common
{
    public static class Constants
    {
        public static readonly byte MAX_BATTLE_PLAYERS      = 2;
        public static readonly byte BOARD_SIZE              = 10;

        public static readonly int SAVE_INTERVAL            = 60000; // 1 минута

        // База
        public static readonly string DB_HOST               = "127.0.0.1";
        //public static readonly string DB_USERNAME           = "ships";
        //public static readonly string DB_PASSWORD           = "vxkAVUfdfX";
        public static readonly string DB_USERNAME           = "root";
        public static readonly string DB_PASSWORD           = "root";
        public static readonly string DB_DBNAME             = "ships";
        public static readonly int DB_PORT                  = 3306;

        // Криптография
        public static readonly string CRYPTOGRAPHY_PASSWORD = "13W_1q_ew3e$%213ASe";
    }
}
