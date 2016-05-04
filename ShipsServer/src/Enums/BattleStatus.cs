namespace ShipsServer.Enums
{
    public enum BattleStatus : uint
    {
        BATTLE_STATUS_NONE                    = 0,
        BATTLE_STATUS_INITIAL                 = 1,
        BATTLE_STATUS_BATTLE                  = 2,
        BATTLE_STATUS_WAIT_OPONENT            = 3,
        BATTLE_STATUS_DONE                    = 4,
    }
}
