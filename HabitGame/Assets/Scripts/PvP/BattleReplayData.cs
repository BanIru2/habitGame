using System;
using System.Collections.Generic;

[Serializable]
public class BattleTurnLog
{
    public int turn;
    public bool attackerIsMaster; // 공격자가 마스터 클라이언트인지 여부
    public int damage;
    public bool isCrit;
    public int defenderHp;        // 피격 후 방어자의 남은 HP
}

[Serializable]
public class BattleReplay
{
    public int winnerMasterResult; // 0: 무승부, 1: 마스터 승, 2: 마스터 패
    public List<BattleTurnLog> turns = new List<BattleTurnLog>();
}
