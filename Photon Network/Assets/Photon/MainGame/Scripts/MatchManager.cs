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
#region 싱글톤
    public static MatchManager Instance;
    public void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    #endregion

    public TMP_Text killText, deathText;

    // 3가지 이벤트. 플레이어가 방에 접속 했을 때.. 모든 플레이어한테 전송...  정보를 Update 갱신하다.
    public enum EventCodes : byte // byte로 이벤트 코드를 작성하면, 따로 형변환을 하지 않기 때문에 에러가 덜 발생한다.
    {
       NewPlayer,ListPlayers,UpdateStats
    }

    public enum GameState
    {
        Waiting,Playing,Ending
    }

    private EventCodes eventCodes;

    [SerializeField] List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;            // PhotonView.IsMine 나 자신의 Index를 저장.

    [Header("리더 보드")]
    public GameObject LeaderBoardPanel;          // Tab키를 눌러서 활성화, 비활성화
    public LeaderBoardPlayer instantLeaderBoard; // 해당하는 leaderBoadr 객체를 인스턴스화로 사용함.
    private List<LeaderBoardPlayer> leaderBoardPlayers = new List<LeaderBoardPlayer>();

    [Header("제한시간")]
    public GameObject matchTimePanel;
    public TMP_Text timeText;
    public float matchEndTime = 180f;
    private float currentMatchTime;

    [Header("게임 엔딩")]
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

    // 메인 게임 Scene 창 -> Lobby 호출
    // Start is called before the first frame update
    void Start()
    {
        // 포톤.. 연결이 안되어 있을 때만 LoadScene보내줘
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
                // LeaderBoard 정보를 보여주는 함수
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
        if(photonEvent.Code < 200) // 작은 숫자는 Custom Event
        {
            EventCodes eventCode = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            eventCodes = (EventCodes)photonEvent.Code;
            Debug.Log("수신받은 이벤트의 정보 " + eventCodes);

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

    // Send 함수에는 Photon Raise Event  
    // Receive 함수에는 매개 변수로 받은 Event Class를 인게임에 적용하는 코드.

    public void NewPlayerSend(string username)
    {
        // 닉네임 - 포톤 로그인, Actor - LcaolPlayer.ActorNumber, Kill = 0, Death = 0

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
    public void ListPlayersSend() // masterClient <- 새로운 플레이어가 들어오면 너가 그 정보를 기억해. 정보를 다른 Client한테 전달해주는 기능. PlayerInfo 패킷화해서 보내면된다.
    {
        object[] packet = new object[allPlayers.Count + 1];

        // gameState object 패킹을 해서 네트워크 통신
        // 게임이 종료되는 ListPlayersSend 수신받으면.. gameState.Ending;
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
    /// statToUpdate 0이면 킬 , 1이면 데스
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
                    // UpdateView Kill, Death text 변화하는 함수
                    UpdateStatsDisPlay();
                }

                if(LeaderBoardPanel.activeInHierarchy) // 리더 보드가 활성하 되어 있을 때만 ShowLeaderBoard 실행
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
            killText.text = $"킬 수 : {allPlayers[index].kill}";
            deathText.text = $"데스 수 : {allPlayers[index].death}";
        }
        else
        {
            killText.text = $"킬 수 : 0";
            deathText.text = $"데스 수 : 0";
        }
    }

    void ShowLeaderBoard() // 껐다 켰다.. 반복 실행
    {
        // LeaderBoardPanel 활성화
        LeaderBoardPanel.SetActive(true);
        // 리셋(데이터를 0 비워주는 작업)
        foreach(var leaderBoardPlayer in leaderBoardPlayers)
        {
            Destroy(leaderBoardPlayer.gameObject);
        }
        leaderBoardPlayers.Clear();
        // 데이터 갱신

        instantLeaderBoard.gameObject.SetActive(false);   // 인스턴스을 위해 만들어 둔 Default 설정 오브젝트.. 비활성화 한다.

        // 정렬된 playerinfo List 객체로 변환을 해주면..
        List<PlayerInfo> sorted = SortPlayers(allPlayers);

        foreach (var player in sorted)
        {
            // LeaderBoardPlayer 클래스 <-  Instantiate 함수를 사용해서 객체를 생성한다. 
            LeaderBoardPlayer leaderBoardPlayer = Instantiate(instantLeaderBoard, instantLeaderBoard.transform.parent);
            // LeaderBoardPlayer 멤버 함수. allPlayers안에 있는 데이터 -> SetPlayerInfo 실행
            leaderBoardPlayer.SetPlayerInfo(player.name, player.kill, player.death);
            // LeaderBoardPlayer 객체를 leaderBoardPlayers 리스트 추가한다.
            leaderBoardPlayers.Add(leaderBoardPlayer);
            // 객체를 SetActvie 활성화 한다.
            leaderBoardPlayer.gameObject.SetActive(true);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> allPlayers)
    {
        List<PlayerInfo> sortedList = new List<PlayerInfo>();

        // 받아온 리스트를 킬 수가 높은 순서대로 정렬한다.
            
        while(sortedList.Count < allPlayers.Count)
        {
            // 초기 값
            PlayerInfo selectedPlayer = allPlayers[0];
            int highest = -1;

            // Loop 세팅
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

    #region 매치 종료
    /// <summary>
    /// allPlayers의 플레이의 킬 수가 목표 킬수에 도달했는지 체크하는 함수
    /// </summary>
    void ScoreCheck()
    {
        // 방 안에 있는 모든 플레이어 중에 목표 킬 수를 도달했는지 체크한다.
        bool isExistWinner = false;
        foreach(var player in allPlayers)
        {
            if(player.kill >= killToWin && killToWin > 0)
            {
                isExistWinner = true;
                break;
            }
        }
        // 체크한 유저가 있다면, 모든 유저에게 게임이 종료됬음을 알린다.
        if (isExistWinner)
        {
            if(PhotonNetwork.IsMasterClient && gameState != GameState.Ending)
            {
                gameState = GameState.Ending;
                // 모든 플레이어가 현재 GameState가 종료되었음을 알린다.
                ListPlayersSend();
            }
        }
    }

    // GameState가 Ending이면 게임을 종료한다.
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
            // 게임 안에 있는 데이터를 전부 삭제 후.. Room 나가지 않고 재시작하기 위해 사용합니다.
        }

        // 리더 보드를 보여주고, Mouse Cursor 상태를 움직일 수 있게 변화해준다.
        ShowLeaderBoard();
        matchEndScene.SetActive(true);
        EndCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 매치 종료 시간이 끝나면 Lobby로 돌아가는 기능
        StartCoroutine(nameof(MatchEndCo));
    }

    // 코루틴, 함수_서브루틴(Invoke)
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
