namespace ShipsServer.Server.Battle.Enums
{
    public enum BattleResponse : uint
    {
        BATTLE_RESPONSE_NONE                    = 0,
        BATTLE_RESPONSE_SUCCESS                 = 1,
        BATTLE_RESPONSE_WAIT_OPPONENT           = 2,
        BATTLE_RESPONSE_YOU_IN_BATTLE           = 3,
    }
}
