using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    private const string GameVersion = "0.1";
    private const string FixedRegion = "asia";

    // 준비 단계에서 필요한 기본 정보 등록용 PhotonKeys
    private const string PropUserId = "UserId";
    private const string PropPlayerName = "PlayerName";
    private const string PropFireLv = "FireLv";
    private const string PropWaterLv = "WaterLv";
    private const string PropGrassLv = "GrassLv";
    private const string PropAuroraLv = "AuroraLv";
    private const string PropHp = "Hp";
    private const string PropIsReady = "IsReady";

    // BattleUnit 생성을 위한 정보 등록 용 PhotonKeys
    private const string PropBattleMaxHp = "BattleMaxHp";
    private const string PropBattleHp = "BattleHp";
    private const string PropBattleAtk = "BattleAtk";
    private const string PropBattleDef = "BattleDef";
    private const string PropBattleSpd = "BattleSpd";
    private const string PropBattleCrit = "BattleCrit";
    private const string PropBattleAttr = "BattleAttr";
    private const string PropBattleAttrLevel = "BattleAttrLevel";

    void Start()
    {
        PhotonNetwork.GameVersion = GameVersion;
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = FixedRegion;
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        PhotonNetwork.JoinLobby();
        Debug.Log($"Photon connected to master. Region: {PhotonNetwork.CloudRegion}, GameVersion: {PhotonNetwork.GameVersion}");
    }

    public bool StartMatchmaking()
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby)
        {
            Debug.LogWarning($"Photon is not ready for matchmaking. State: {PhotonNetwork.NetworkClientState}");
            return false;
        }

        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        return PhotonNetwork.JoinRandomOrCreateRoom(null, 2, MatchmakingMode.FillRoom, TypedLobby.Default, null, null, options);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);
        Debug.LogWarning($"JoinRandom failed. Code: {returnCode}, Message: {message}");
        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(null, options);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined room. Name: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}, Region: {PhotonNetwork.CloudRegion}");
        UpdateUI();
        RegisterLocalPlayerProperties();
        RefreshMyInfoUI();
        ScheduleReadyPanelCheck();
    }

    private void RegisterLocalPlayerProperties()
    {
        var data = CharacterManager.Instance.characterStatusData;
        long userId = data != null ? data.UserId : PhotonNetwork.LocalPlayer.ActorNumber;
        string playerName = GetLocalPlayerName(userId);

        Hashtable myProps = new Hashtable();
        myProps.Add(PropUserId, userId);
        myProps.Add(PropPlayerName, playerName);
        myProps.Add(PropFireLv, data != null ? data.FireLv : 0);
        myProps.Add(PropWaterLv, data != null ? data.WaterLv : 0);
        myProps.Add(PropGrassLv, data != null ? data.GrassLv : 0);
        myProps.Add(PropAuroraLv, data != null ? data.AuroraLv : 0);
        myProps.Add(PropHp, data != null ? (int)data.Hp : 0);
        myProps.Add(PropIsReady, false);

        PhotonNetwork.LocalPlayer.SetCustomProperties(myProps);
        Debug.Log("Local player properties registered.");
    }

    private string GetLocalPlayerName(long userId)
    {
        if (!string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            return PhotonNetwork.NickName;
        }

        if (!string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.UserId))
        {
            return PhotonNetwork.LocalPlayer.UserId;
        }

        return $"Player {userId}";
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player entered room. Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        UpdateUI();
        ScheduleReadyPanelCheck();
    }

    private void UpdateUI()
    {
        if (PhotonNetwork.InRoom)
        {
            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            BattleUIManager.Instance.UpdatePlayerCount(current);
        }
    }

    private bool ContainsBattleUnitProperty(Hashtable props)
    {
        return props.ContainsKey(PropBattleMaxHp)
            || props.ContainsKey(PropBattleHp)
            || props.ContainsKey(PropBattleAtk)
            || props.ContainsKey(PropBattleDef)
            || props.ContainsKey(PropBattleSpd)
            || props.ContainsKey(PropBattleCrit)
            || props.ContainsKey(PropBattleAttr)
            || props.ContainsKey(PropBattleAttrLevel);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        RefreshOpponentInfoUI();
        ScheduleReadyPanelCheck();

        if (PhotonNetwork.IsMasterClient && (changedProps.ContainsKey(PropIsReady) || ContainsBattleUnitProperty(changedProps)))
        {
            CheckAllPlayersReady();
        }
    }

    private void CheckAllPlayersReady()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue(PropIsReady, out object isReady))
            {
                if (!(bool)isReady) return;
            }
            else return;

            if (!HasRequiredBattleUnitData(p)) return;
        }

        Debug.Log("All players are ready. Starting battle.");
        BattleManager.Instance.BroadcastStartBattle();
    }

    private void ScheduleReadyPanelCheck()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount != 2) return;
        if (!BattleUIManager.Instance.IsMatching) return;

        CancelInvoke(nameof(TryGoToReadyPanel));
        Invoke(nameof(TryGoToReadyPanel), 1.0f);
    }

    private void TryGoToReadyPanel()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount != 2) return;
        if (!BattleUIManager.Instance.IsMatching) return;

        RefreshOpponentInfoUI();

        if (HasAllPlayerReadyData())
        {
            BattleUIManager.Instance.LoadingComplete();
        }
        else
        {
            Invoke(nameof(TryGoToReadyPanel), 0.5f);
        }
    }

    private bool HasAllPlayerReadyData()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!HasRequiredReadyData(player)) return false;
        }

        return true;
    }

    private bool HasRequiredReadyData(Player player)
    {
        Hashtable props = player.CustomProperties;

        return props.ContainsKey(PropUserId)
            && props.ContainsKey(PropPlayerName)
            && props.ContainsKey(PropFireLv)
            && props.ContainsKey(PropWaterLv)
            && props.ContainsKey(PropGrassLv)
            && props.ContainsKey(PropAuroraLv)
            && props.ContainsKey(PropHp)
            && props.ContainsKey(PropIsReady);
    }

    private bool HasRequiredBattleUnitData(Player player)
    {
        Hashtable props = player.CustomProperties;

        return props.ContainsKey(PropBattleMaxHp)
            && props.ContainsKey(PropBattleHp)
            && props.ContainsKey(PropBattleAtk)
            && props.ContainsKey(PropBattleDef)
            && props.ContainsKey(PropBattleSpd)
            && props.ContainsKey(PropBattleCrit)
            && props.ContainsKey(PropBattleAttr)
            && props.ContainsKey(PropBattleAttrLevel);
    }
    private void RefreshMyInfoUI()
    {
        var data = CharacterManager.Instance.characterStatusData;
        long userId = data != null ? data.UserId : PhotonNetwork.LocalPlayer.ActorNumber;
        BattleUIManager.Instance.SetMyInfoUI(GetLocalPlayerName(userId));
    }

    private void RefreshOpponentInfoUI()
    {
        if (!PhotonNetwork.InRoom) return;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player == PhotonNetwork.LocalPlayer) continue;
            if (!HasRequiredReadyData(player)) continue;

            Hashtable cp = player.CustomProperties;
            string name = cp.TryGetValue(PropPlayerName, out object n) ? n.ToString() : "Unknown";
            int fire = cp.TryGetValue(PropFireLv, out object f) ? (int)f : 0;
            int water = cp.TryGetValue(PropWaterLv, out object w) ? (int)w : 0;
            int grass = cp.TryGetValue(PropGrassLv, out object g) ? (int)g : 0;
            int aurora = cp.TryGetValue(PropAuroraLv, out object a) ? (int)a : 0;

            BattleUIManager.Instance.SetOpponentInfoUI(name, fire, water, grass, aurora);
            return;
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        CancelInvoke(nameof(TryGoToReadyPanel));
        BattleUIManager.Instance.ReturnToLobby();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        CancelInvoke(nameof(TryGoToReadyPanel));

        if (BattleManager.Instance.IsBattleFinished)
        {
            UpdateUI();
            return;
        }

        Hashtable props = new Hashtable();
        props.Add(PropIsReady, false);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateUI();
        BattleUIManager.Instance.BackToMatchingAfterOpponentLeft();
    }

    public void PrepareBattlePanelUI(BattleUnit myUnit, BattleUnit oppUnit)
    {
        RefreshMyInfoUI();
        RefreshOpponentInfoUI();
        BattleUIManager.Instance.SetBattleHpUI(myUnit.maxHp, oppUnit.maxHp);
    }

    public void RegisterBattleUnitProperties(BattleUnit unit)
    {
        Hashtable props = new Hashtable();

        props.Add(PropBattleMaxHp, unit.maxHp);
        props.Add(PropBattleHp, unit.hp);
        props.Add(PropBattleAtk, unit.atk);
        props.Add(PropBattleDef, unit.def);
        props.Add(PropBattleSpd, unit.spd);
        props.Add(PropBattleCrit, unit.crtk);
        props.Add(PropBattleAttr, (int)unit.attribute);
        props.Add(PropBattleAttrLevel, unit.attributeLevel);

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"BattleUnit registered. HP:{unit.hp}/{unit.maxHp}, ATK:{unit.atk}, DEF:{unit.def}, SPD:{unit.spd}, CRIT:{unit.crtk}, ATTR:{unit.attribute}, ATTR_LV:{unit.attributeLevel}");
    }

    public BattleUnit CreateBattleUnitFromPlayer(Player player)
    {
        Hashtable props = player.CustomProperties;

        string name = props.TryGetValue(PropPlayerName, out object n) ? n.ToString() : "Unknown";

        return new BattleUnit
        {
            name = name,
            maxHp = props.TryGetValue(PropBattleMaxHp, out object maxHp) ? (int)maxHp : 0,
            hp = props.TryGetValue(PropBattleHp, out object hp) ? (int)hp : 0,
            atk = props.TryGetValue(PropBattleAtk, out object atk) ? (float)atk : 0f,
            def = props.TryGetValue(PropBattleDef, out object def) ? (float)def : 0f,
            spd = props.TryGetValue(PropBattleSpd, out object spd) ? (float)spd : 0f,
            crtk = props.TryGetValue(PropBattleCrit, out object crit) ? (float)crit : 0f,
            attribute = props.TryGetValue(PropBattleAttr, out object attr) ? (AttributeType)(int)attr : AttributeType.Fire,
            attributeLevel = props.TryGetValue(PropBattleAttrLevel, out object attrLv) ? (int)attrLv : 0
        };
    }

    // 상대방 Player 찾기
    private Player GetOpponentPlayer()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player != PhotonNetwork.LocalPlayer)
            {
                return player;
            }
        }

        return null;
    }

    public bool TryCreateBattleUnits(out BattleUnit myUnit, out BattleUnit oppUnit)
    {
        myUnit = null;
        oppUnit = null;

        if (!PhotonNetwork.InRoom) return false;

        Player opponent = GetOpponentPlayer();
        if (opponent == null) return false;

        if (!HasRequiredBattleUnitData(PhotonNetwork.LocalPlayer)) return false;
        if (!HasRequiredBattleUnitData(opponent)) return false;

        myUnit = CreateBattleUnitFromPlayer(PhotonNetwork.LocalPlayer);
        oppUnit = CreateBattleUnitFromPlayer(opponent);

        Debug.Log($"My BattleUnit: {myUnit.name}, HP:{myUnit.hp}/{myUnit.maxHp}, ATK:{myUnit.atk}, DEF:{myUnit.def}, SPD:{myUnit.spd}");
        Debug.Log($"Opp BattleUnit: {oppUnit.name}, HP:{oppUnit.hp}/{oppUnit.maxHp}, ATK:{oppUnit.atk}, DEF:{oppUnit.def}, SPD:{oppUnit.spd}");

        return true;
    }

    public void ReturnToLobby()
    {
        CancelInvoke(nameof(TryGoToReadyPanel));

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            BattleUIManager.Instance.ReturnToLobby();
        }
    }
}
