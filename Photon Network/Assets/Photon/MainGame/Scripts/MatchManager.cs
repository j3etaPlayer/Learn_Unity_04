using ExitGames.Client.Photon;
using JetBrains.Annotations;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MatchManager;

[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor, kill, death;

    public PlayerInfo(string name, int actor, int kill, int death)
    {
        this.name = name;
        this.actor = actor;
        this.kill = kill;
        this.death = death;
    }
}

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
#region �̱���
    public static MatchManager Instance;
    public void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    #endregion

    public TMP_Text killText, deathText;

    // 3���� �̺�Ʈ. �÷��̾ �濡 ���� ���� ��.. ��� �÷��̾����� ����...  ������ Update �����ϴ�.
    public enum EventCodes : byte // byte�� �̺�Ʈ �ڵ带 �ۼ��ϸ�, ���� ����ȯ�� ���� �ʱ� ������ ������ �� �߻��Ѵ�.
    {
       NewPlayer,ListPlayers,UpdateStats
    }

    public enum GameState
    {
        Waiting,Playing,Ending
    }

    private EventCodes eventCodes;

    [SerializeField] List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;            // PhotonView.IsMine �� �ڽ��� Index�� ����.

    [Header("���� ����")]
    public GameObject LeaderBoardPanel;          // TabŰ�� ������ Ȱ��ȭ, ��Ȱ��ȭ
    public LeaderBoardPlayer instantLeaderBoard; // �ش��ϴ� leaderBoadr ��ü�� �ν��Ͻ�ȭ�� �����.
    private List<LeaderBoardPlayer> leaderBoardPlayers = new List<LeaderBoardPlayer>();

    [Header("���ѽð�")]
    public GameObject matchTimePanel;
    public TMP_Text timeText;
    public float matchEndTime = 180f;
    private float currentMatchTime;

    [Header("���� ����")]
    public int killToWin = 1;
    public float waitForEnding = 3f;
    public GameState gameState = GameState.Waiting;
    public GameObject matchEndScene;
    public GameObject EndCamera;


    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // ���� ���� Scene â -> Lobby ȣ��
    // Start is called before the first frame update
    void Start()
    {
        // ����.. ������ �ȵǾ� ���� ���� LoadScene������
        if (!PhotonNetwork.IsConnected)
            SceneManager.LoadScene(0);
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);
            SetUpGameSetting();
        }
    }

    private void SetUpGameSetting()
    {
        matchEndScene.SetActive(false);
        EndCamera.gameObject.SetActive(false);
        currentMatchTime = matchEndTime;
        UpdateTimerDisplay();
        gameState = GameState.Playing;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (LeaderBoardPanel.activeInHierarchy)
            {
                LeaderBoardPanel.SetActive(false);
            }
            else
            {
                // LeaderBoard ������ �����ִ� �Լ�
                ShowLeaderBoard();
            }
        }
        if (currentMatchTime > 0 && gameState == GameState.Playing)
        {
            currentMatchTime -= Time.deltaTime;
            
            if (currentMatchTime <= 0)
            {
                currentMatchTime = 0;
                gameState = GameState.Ending;

                if (PhotonNetwork.IsMasterClient)
                {
                    ListPlayersSend();
                    StateCheck();
                }
            }
        }
        UpdateTimerDisplay();
    }

    public void OnEvent(EventData photonEvent)
    {
        if(photonEvent.Code < 200) // ���� ���ڴ� Custom Event
        {
            EventCodes eventCode = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            eventCodes = (EventCodes)photonEvent.Code;
            Debug.Log("���Ź��� �̺�Ʈ�� ���� " + eventCodes);

            switch (eventCode)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                    break;
                case EventCodes.ListPlayers:
                    ListPlayersReceive(data);
                    break;
                case EventCodes.UpdateStats:
                    UpdateStatsReceive(data);
                    break;
            }
        }
    }

    // Send �Լ����� Photon Raise Event  
    // Receive �Լ����� �Ű� ������ ���� Event Class�� �ΰ��ӿ� �����ϴ� �ڵ�.

    public void NewPlayerSend(string username)
    {
        // �г��� - ���� �α���, Actor - LcaolPlayer.ActorNumber, Kill = 0, Death = 0

        object[] playerInfo = new object[4] {username, PhotonNetwork.LocalPlayer.ActorNumber, 0, 0};

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.MasterClient
        };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent((byte)EventCodes.NewPlayer, playerInfo, raiseEventOptions, sendOptions);
    }
    public void NewPlayerReceive(object[] data) 
    {
        PlayerInfo playerInfo = new PlayerInfo((string)data[0], (int)data[1], (int)data[2], (int)data[3]);

        allPlayers.Add(playerInfo);

        ListPlayersSend();
    }
    public void ListPlayersSend() // masterClient <- ���ο� �÷��̾ ������ �ʰ� �� ������ �����. ������ �ٸ� Client���� �������ִ� ���. PlayerInfo ��Ŷȭ�ؼ� ������ȴ�.
    {
        object[] packet = new object[allPlayers.Count + 1];

        // gameState object ��ŷ�� �ؼ� ��Ʈ��ũ ���
        // ������ ����Ǵ� ListPlayersSend ���Ź�����.. gameState.Ending;
        packet[0] = gameState;

        for (int i = 0; i < allPlayers.Count; i++)
        {
            object[] info = new object[4];

            info[0] = allPlayers[i].name;
            info[1] = allPlayers[i].actor;
            info[2] = allPlayers[i].kill;
            info[3] = allPlayers[i].death;

            packet[i + 1] = info;
        }

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent((byte)EventCodes.ListPlayers, packet, raiseEventOptions, sendOptions);
    }
    public void ListPlayersReceive(object[] data) // room <1,2,3,4... 1 12 123 1234
    {
        allPlayers.Clear();

        gameState = (GameState)data[0];

        for (int i = 1; i< data.Length; i++)
        {
            object[] info = (object[])data[i];

            PlayerInfo player = new PlayerInfo((string)info[0], (int)info[1], (int)info[2], (int)info[3]);

            allPlayers.Add(player);

            if(PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i - 1;
                UpdateStatsDisPlay();
            }
        }

        StateCheck();
    }

    // PlayerController. //TakeDamage // Spawner : Die
    /// <summary>
    /// statToUpdate 0�̸� ų , 1�̸� ����
    /// </summary>
    public void UpdateStatsSend(int actorIndex, int statToUpdate, int amountToChange) 
    {
        object[] packet = new object[] { actorIndex, statToUpdate, amountToChange };

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent((byte)EventCodes.UpdateStats, packet, raiseEventOptions, sendOptions);
    }
    public void UpdateStatsReceive(object[] data) 
    {
        int actor = (int)data[0];
        int stat = (int)data[1];
        int amount = (int)data[2];

        for(int i =0; i< allPlayers.Count; i++)
        {
            if (allPlayers[i].actor == actor)
            {
                switch (stat)
                {
                    case 0: // kills
                        allPlayers[i].kill += amount;
                        break;
                    case 1: // Deaths
                        allPlayers[i].death += amount;
                        break;
                }
                if (i == index)
                {
                    // UpdateView Kill, Death text ��ȭ�ϴ� �Լ�
                    UpdateStatsDisPlay();
                }

                if(LeaderBoardPanel.activeInHierarchy) // ���� ���尡 Ȱ���� �Ǿ� ���� ���� ShowLeaderBoard ����
                {
                    ShowLeaderBoard();
                }
                break;
            }
        }

        ScoreCheck();
    }

    private void UpdateStatsDisPlay()
    {
        if(allPlayers.Count > index)
        {
            killText.text = $"ų �� : {allPlayers[index].kill}";
            deathText.text = $"���� �� : {allPlayers[index].death}";
        }
        else
        {
            killText.text = $"ų �� : 0";
            deathText.text = $"���� �� : 0";
        }
    }

    void ShowLeaderBoard() // ���� �״�.. �ݺ� ����
    {
        // LeaderBoardPanel Ȱ��ȭ
        LeaderBoardPanel.SetActive(true);
        // ����(�����͸� 0 ����ִ� �۾�)
        foreach(var leaderBoardPlayer in leaderBoardPlayers)
        {
            Destroy(leaderBoardPlayer.gameObject);
        }
        leaderBoardPlayers.Clear();
        // ������ ����

        instantLeaderBoard.gameObject.SetActive(false);   // �ν��Ͻ��� ���� ����� �� Default ���� ������Ʈ.. ��Ȱ��ȭ �Ѵ�.

        // ���ĵ� playerinfo List ��ü�� ��ȯ�� ���ָ�..
        List<PlayerInfo> sorted = SortPlayers(allPlayers);

        foreach (var player in sorted)
        {
            // LeaderBoardPlayer Ŭ���� <-  Instantiate �Լ��� ����ؼ� ��ü�� �����Ѵ�. 
            LeaderBoardPlayer leaderBoardPlayer = Instantiate(instantLeaderBoard, instantLeaderBoard.transform.parent);
            // LeaderBoardPlayer ��� �Լ�. allPlayers�ȿ� �ִ� ������ -> SetPlayerInfo ����
            leaderBoardPlayer.SetPlayerInfo(player.name, player.kill, player.death);
            // LeaderBoardPlayer ��ü�� leaderBoardPlayers ����Ʈ �߰��Ѵ�.
            leaderBoardPlayers.Add(leaderBoardPlayer);
            // ��ü�� SetActvie Ȱ��ȭ �Ѵ�.
            leaderBoardPlayer.gameObject.SetActive(true);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> allPlayers)
    {
        List<PlayerInfo> sortedList = new List<PlayerInfo>();

        // �޾ƿ� ����Ʈ�� ų ���� ���� ������� �����Ѵ�.
            
        while(sortedList.Count < allPlayers.Count)
        {
            // �ʱ� ��
            PlayerInfo selectedPlayer = allPlayers[0];
            int highest = -1;

            // Loop ����
            foreach(PlayerInfo player in allPlayers)
            {
                if (!sortedList.Contains(player))
                {
                    if(player.kill > highest)
                    {
                        selectedPlayer = player;
                        highest = player.kill;
                    }
                }
            }
            sortedList.Add(selectedPlayer);
        }

        return sortedList;
    }

    public void UpdateTimerDisplay()
    {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);
        timeText.text = timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00");
    }

    #region ��ġ ����
    /// <summary>
    /// allPlayers�� �÷����� ų ���� ��ǥ ų���� �����ߴ��� üũ�ϴ� �Լ�
    /// </summary>
    void ScoreCheck()
    {
        // �� �ȿ� �ִ� ��� �÷��̾� �߿� ��ǥ ų ���� �����ߴ��� üũ�Ѵ�.
        bool isExistWinner = false;
        foreach(var player in allPlayers)
        {
            if(player.kill >= killToWin && killToWin > 0)
            {
                isExistWinner = true;
                break;
            }
        }
        // üũ�� ������ �ִٸ�, ��� �������� ������ ��������� �˸���.
        if (isExistWinner)
        {
            if(PhotonNetwork.IsMasterClient && gameState != GameState.Ending)
            {
                gameState = GameState.Ending;
                // ��� �÷��̾ ���� GameState�� ����Ǿ����� �˸���.
                ListPlayersSend();
            }
        }
    }

    // GameState�� Ending�̸� ������ �����Ѵ�.
    void StateCheck()
    {
        if(gameState == GameState.Ending)
        {
            EndMatch();
        }
    }

    void EndMatch()
    {
        gameState = GameState.Ending;

        if (PhotonNetwork.IsMasterClient)
        {
            //PhotonNetwork.DestroyAll();
            // ���� �ȿ� �ִ� �����͸� ���� ���� ��.. Room ������ �ʰ� ������ϱ� ���� ����մϴ�.
        }

        // ���� ���带 �����ְ�, Mouse Cursor ���¸� ������ �� �ְ� ��ȭ���ش�.
        ShowLeaderBoard();
        matchEndScene.SetActive(true);
        EndCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ��ġ ���� �ð��� ������ Lobby�� ���ư��� ���
        StartCoroutine(nameof(MatchEndCo));
    }

    // �ڷ�ƾ, �Լ�_�����ƾ(Invoke)
    private IEnumerator MatchEndCo()
    {
        yield return new WaitForSeconds(waitForEnding);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.LoadLevel(0);
        // PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.LoadLevel(0);
    }

    #endregion


}
