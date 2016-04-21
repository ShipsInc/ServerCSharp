﻿namespace ShipsServer.Protocol
{
    public enum Opcodes : uint
    {
        CMSG_AUTH                   = 0x001,
        CMSG_REGISTRATION           = 0x002,
        SMSG_AUTH_RESPONSE          = 0x003,
        SMSG_REGISTRATION_RESPONSE  = 0x004,
        CMSG_LOGOUT                 = 0x005,
        CMSG_KEEP_ALIVE             = 0x006,
        SMSG_KEEP_ALIVE             = 0x007,
        CMSG_PROFILE                = 0x008,
        SMSG_PROFILE_RESPONSE       = 0x009,
        CMSG_GET_GAMES              = 0x00A,
        SMSG_GET_GAMES_RESPONSE     = 0x00B,
        CMSG_BATTLE_INITIALIZATION  = 0x00C, // Battle
        CMSG_BATTLE_JOIN            = 0x00D,
        SMSG_BATTLE_RESPONSE        = 0x00E,
        SMSG_BATTLE_INITIAL_BATTLE  = 0x00F,
        MAX_OPCODE,
    }
}