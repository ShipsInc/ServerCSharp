using System;
using System.Collections.Generic;
using System.Linq;
using ShipsServer.Enums;

namespace ShipsServer.Server.Battle
{
    class BattleMgr
    {
        public List<Battle> BattleList { get; private set; }

        private static BattleMgr _instance;

        public BattleMgr()
        {
            BattleList = new List<Battle>();
        }

        public static BattleMgr Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BattleMgr();
                return _instance;
            }
        }

        public void AddBattle(Battle battle)
        {
            BattleList.RemoveAll(bt => bt.Id == battle.Id);

            if (BattleList.Count != 0)
                battle.Id = BattleList.Where(bt => bt.Id > 0).OrderByDescending(bt => bt.Id).First().Id + 1;
            else
                battle.Id = 1;

            BattleList.Add(battle);
        }

        public void RemoveBattle(Battle battle)
        {
            BattleList.Remove(battle);
        }

        public Battle GetBattle(int id)
        {
            return BattleList.Find(x => x.Id == id);
        }

        public void Update()
        {
            // Очистка завершённых игар
            BattleList.RemoveAll(battle => battle.Status == BattleStatus.BATTLE_STATUS_DONE);
        }
    }
}