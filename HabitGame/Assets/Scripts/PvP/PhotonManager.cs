using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    private const string PropUserId = "UserId";
    private const string PropPlayerName = "PlayerName";
    private const string PropFireLv = "FireLv";
    private const string PropWaterLv = "WaterLv";
    private const string PropGrassLv = "GrassLv";
    private const string PropAuroraLv = "AuroraLv";
    private const string PropIsReady = "IsReady";

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        PhotonNetwork.JoinLobby();
        Debug.Log("Photon connected to master.");
    }

    public bool StartMatchmaking()
    {
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby)
        {
            Debug.LogWarning($"Photon is not ready for matchmaking. State: {PhotonNetwork.NetworkClientState}");
            return false;
        }

        return PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);
        RoomOptions options = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(null, options);
    }

    public override void OnJoinedRoom()
    {
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

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        RefreshOpponentInfoUI();
        ScheduleReadyPanelCheck();

        if (PhotonNetwork.IsMasterClient && changedProps.ContainsKey(PropIsReady))
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
            && props.ContainsKey(PropIsReady);
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

    public void CancelMatchmaking()
    {
        CancelInvoke(nameof(TryGoToReadyPanel));

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            BattleUIManager.Instance.CancelMatchingComplete();
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        CancelInvoke(nameof(TryGoToReadyPanel));
        BattleUIManager.Instance.CancelMatchingComplete();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        CancelInvoke(nameof(TryGoToReadyPanel));
        Hashtable props = new Hashtable();
        props.Add(PropIsReady, false);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateUI();
        BattleUIManager.Instance.BackToMatchingAfterOpponentLeft();
    }
}
