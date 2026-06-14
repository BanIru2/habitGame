using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.CodeDom.Compiler;    // 모호성 방지

public class PhotonManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        PhotonNetwork.JoinLobby();
        Debug.Log("마스터 서버 접속 완료");
    }

    public void StartMatchmaking()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Photon 서버 접속이 아직 완료되지 않았습니다");
            BattleUIManager.Instance.CancelMatchingComplete();
            return;
        }

        PhotonNetwork.JoinRandomRoom();
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

        var data = CharacterManager.Instance.characterStatusData;
        if(data != null)
        {
            Hashtable myProps = new Hashtable();
            myProps.Add("FireLv", data.FireLv);
            myProps.Add("WaterLv", data.WaterLv);
            myProps.Add("GrassLv", data.GrassLv);
            myProps.Add("AuroraLv", data.AuroraLv);
            myProps.Add("IsReady", false);

            PhotonNetwork.LocalPlayer.SetCustomProperties(myProps);
            Debug.Log("서버 프로필 등록 완료");
        }


        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Invoke("GoToReadyPanel", 1.0f);
        }
    }

    private void GoToReadyPanel()
    {
        BattleUIManager.Instance.LoadingComplete();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateUI();
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Invoke("GoToReadyPanel", 1.0f);
        }
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
        // UI 갱신
        if(targetPlayer != PhotonNetwork.LocalPlayer)
        {
            var cp = targetPlayer.CustomProperties;

            // string name = cp.TryGetValue("NickName", out object n) ? n.ToString() : "Unknown";
            string name = targetPlayer.UserId;
            int fire = cp.TryGetValue("FireLv", out object f) ? (int)f : 0;
            int water = cp.TryGetValue("WaterLv", out object w) ? (int)w : 0;
            int grass = cp.TryGetValue("GrassLv", out object g) ? (int)g : 0;
            int aurora = cp.TryGetValue("AuroraLv", out object a) ? (int)a : 0;
            BattleUIManager.Instance.SetOppoentInfoUI(name, fire, water, grass, aurora);
        }

        if (PhotonNetwork.IsMasterClient &&changedProps.ContainsKey("IsReady"))
        {
            CheckAllPlayersReady();
        }
    }

    private void CheckAllPlayersReady()
    {
        foreach(Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("IsReady", out object isReady))
            {
                if (!(bool)isReady) return;
            }
            else return;
        }
        Debug.Log("모든 인원 준비 완료. 전투 시작 신호 발생");
        BattleManager.Instance.BroadcastStartBattle();
    }
    
    public void CancelMatchmaking()
    {
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
        BattleUIManager.Instance.CancelMatchingComplete();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        UpdateUI();
        BattleUIManager.Instance.BackToMatchingAfterOpponentLeft();
    }
}
