using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class Laucher : MonoBehaviourPunCallbacks
{
    public static Laucher Instance;
    // MonoBehaviourPunCallbacks : Photon Network상태에 따라 Callback Interface함수를 자동으로 등록하고 사용할수 있게 해주는 클래스

    [Header("[Main]")]
    public GameObject menuButtons;
    public TMP_Text currentStatus;

    [Header("[Lading]")]
    public GameObject loadingPanel;
    public TMP_Text loadingText;

    [Header("[Room Create]")]
    public GameObject createRoomPanel;
    public TMP_InputField roomNameInput;

    [Header("[Room Info]")]
    public GameObject roomPanel;
    public GameObject startButton;
    public TMP_Text roomNameText;
    public TMP_Text playerNicknameText;

    [Header("[Room Search]")]
    public GameObject roomBroswerPanel;
    // public TMP_InputField roomBroswerNameText;

    [Header("[Room Error]")]
    public GameObject errorPanel;                           // 에러가 발생되었을 때 활성화 되는 오브젝트
    public TMP_Text errorText;                              // 에러에 해당되는 내용을 출력하는 변수

    [Header("[Create Nickname]")]
    public GameObject nicknamePanel;                        // 닉네임 생성 오브젝트
    public TMP_InputField nicknameInput;                    // 닉네임을 작성하는 공간
    private bool hasSetNickname = false;                    // 닉네임이 저장이 되어 있으면 반복을 피해주기 위한 변수
    private const string PLAYERNAMEKEY = "playerName";      // playerPrefabs 를 사용, 간단한 데이터 저장 방식

    [Header("[Photon RoomInfo]")]
    // 방 생성 시 방의 이름을 데이터로 파싱하는 클래스
    public RoomButtonInfo theRoomButtonInfo;
    private List<RoomButtonInfo> roomButtonList = new List<RoomButtonInfo>();
    private List<TMP_Text> allPlayerName = new List<TMP_Text>();

    [Header("[Photon Chat]")]
    public TMP_Text[] chatText;
    public TMP_InputField chatInput;
    public PhotonView pV;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        SetResolution();
    }

    private void SetResolution() => Screen.SetResolution(1920/2, 1080/2, false);

    private void Start()
    {
        
    }

    private void Update()
    {
        currentStatus.text = PhotonNetwork.NetworkClientState.ToString() + $"\n닉네임 : {PhotonNetwork.NickName}";
    }

    /// <summary>
    /// 판넬들을 닫아주는 함수(새로운 판넬이 생성될시 작성 필요)
    /// </summary>
    private void CloseMenu()
    {
        menuButtons.SetActive(false);
        loadingPanel.SetActive(false);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);
        roomBroswerPanel.SetActive(false);
        errorPanel.SetActive(false);
        nicknamePanel.SetActive(false);
    }

    #region Photon Network Function
    public void Connect()
    {
        CloseMenu();
        menuButtons.SetActive(true);
        loadingPanel.SetActive(true);
        loadingText.text = "서버에 접속 중...";

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        CloseMenu();
        menuButtons.SetActive(true);

        // 서버와 연결됨과 동시에 로비에 접속
        PhotonNetwork.JoinLobby();

        loadingText.text = "로비에 연결 중...";

        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void Disconnect() => PhotonNetwork.Disconnect();

    public void JoinLobby() => PhotonNetwork.JoinLobby();

    public override void OnJoinedLobby()
    {
        CloseMenu();
        menuButtons.SetActive(true);

        // PhotonNetwork.NickName = Random.Range(0, 1000).ToString();
        if (!hasSetNickname)
        {
            CloseMenu();
            nicknamePanel.SetActive(true);

            if (PlayerPrefs.HasKey(PLAYERNAMEKEY))
            {
                nicknameInput.text = PlayerPrefs.GetString(PLAYERNAMEKEY);
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString(PLAYERNAMEKEY);
        }
    }

    /// <summary>
    /// 방 생성 패널을 활성화
    /// </summary>
    public void CreateRoomPanel()
    {
        CloseMenu();
        createRoomPanel.SetActive(true);
    }

    /// <summary>
    /// 방 생성 버튼을 활성화
    /// </summary>
    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
        {
            // 팝업창, 방 생성 경고 팝업
            Debug.LogWarning("방의 제목을 작성해 주세요");
        }
        else
        {
            // 방의 제목, 방에 들어올 수 있는 인원수, 방장 호스트
            RoomOptions option = new RoomOptions();
            option.MaxPlayers = 8;

            PhotonNetwork.CreateRoom(roomNameInput.text, option);

            CloseMenu();
            loadingText.text = "방 생성 중...";
            loadingPanel.SetActive(true);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CloseMenu();
        errorText.text = $"방 생성에 실패하였습니다.\n내용 : {message}";
        errorPanel.SetActive(true);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CloseMenu();
        errorText.text = $"빠른 참가에 실패하였습니다.\n내용 : {message}";
        errorPanel.SetActive(true);
    }

    public override void OnJoinedRoom()
    {
        CloseMenu();
        roomNameText.text = $"방 제목 : {PhotonNetwork.CurrentRoom.Name}";
        roomPanel.SetActive(true);

        ShowListAllPlayer();
        ChatClear();

        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                startButton.SetActive(true);
            }
            else
            { 
                startButton.SetActive(false); 
            }
        }
    }

    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);

        CloseMenu();
        loadingText.text = "방 접속 중...";
        loadingPanel.SetActive(true);
    }

    private void ShowListAllPlayer()
    {
        foreach (var player in allPlayerName)
        {
            Destroy(player.gameObject);
        }
        allPlayerName.Clear();
        playerNicknameText.gameObject.SetActive(false);
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            TMP_Text newPlayerNickname = Instantiate(playerNicknameText, playerNicknameText.transform.parent);
            newPlayerNickname.text = players[i].NickName;
            newPlayerNickname.gameObject.SetActive(true);

            allPlayerName.Add(newPlayerNickname);
        }

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerNickname = Instantiate(playerNicknameText, playerNicknameText.transform.parent);
        newPlayerNickname.text = newPlayer.NickName;
        newPlayerNickname.gameObject.SetActive(true);

        allPlayerName.Add(newPlayerNickname);
        pV.RPC(nameof(ChatRPC), RpcTarget.All, $"<color=blue>{newPlayer.NickName}</color>님이 방에 들어오셨습니다.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ShowListAllPlayer();
        pV.RPC(nameof(ChatRPC), RpcTarget.All, $"<color=red>{otherPlayer.NickName}</color>님이 방에 나가셨습니다.");
    }

    public void ButtonLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenu();

        loadingText.text = "방을 나가는 중...";
        loadingPanel.SetActive(true);
    }
    public override void OnLeftRoom()
    {
        ButtonReturnLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var roomButton in roomButtonList)
        {
            Destroy(roomButton.gameObject);
        }
        roomButtonList.Clear();
        theRoomButtonInfo.gameObject.SetActive(false);

        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
            {
                RoomButtonInfo newButton = Instantiate(theRoomButtonInfo, theRoomButtonInfo.transform.parent);

                newButton.SetButtonInfo(roomList[i]);
                newButton.gameObject.SetActive(true);

                roomButtonList.Add(newButton);
            }
        }
    }

    public void OpenRoomBroswer()
    {
        CloseMenu();
        roomBroswerPanel.SetActive(true);
    }

    // 미사용 기능
    public void CloseRoomBroswer()
    {
        CloseMenu();
        menuButtons.SetActive(true);
    }

    #endregion

    private void JoinOrCreateRoom()
    {
        string roomName = $"No.{Random.Range(0,1000).ToString()}";
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions { MaxPlayers = 8 }, null);
    }

    #region Button
    public void ButtonStartGame()
    {
        SceneManager.LoadScene("Photon MainGame");
    }

    public void ButtonJoinRandomRoom()
    {
        // 서버내에 방이 하나라도 존재하면 해당 방에 랜덤으로 참여
        if (PhotonNetwork.CountOfRooms <= 0)
        {
            JoinOrCreateRoom();
        }
        // 서버내에 방이 없다면 내가 방을 만든다.
        else
        {
            PhotonNetwork.JoinRandomRoom();
        }
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
    public void ButtonReturnLobby()
    {
        CloseMenu();
        menuButtons.SetActive(true);
    }
    public void ButtonSetNickname()
    {
        if (!string.IsNullOrEmpty(nicknameInput.text))
        {
            PhotonNetwork.NickName = nicknameInput.text;

            PlayerPrefs.SetString(PLAYERNAMEKEY, nicknameInput.text);

            CloseMenu();
            menuButtons.SetActive(true);
            hasSetNickname = true;
        }
    }

    #endregion
    #region Photon Chat
    private void ChatClear()
    {
        // 방에 들어갔을 때 채팅 창 로그가 비어있는 상태로 접속
        chatInput.text = string.Empty;

        for (int i = 0; i < chatText.Length; i++)
        {
            chatText[i].text = string.Empty;
        }
    }

    public void Send()
    {
        string message = $"{PhotonNetwork.NickName} : {chatInput.text}";
        pV.RPC(nameof(ChatRPC), RpcTarget.All, message);

        chatInput.text = string.Empty;
    }

    [PunRPC]
    private void ChatRPC(string message)    // 함수에 RPC를 작성하면 RPC 애트리뷰트를 사용하겠다는 약속이다.
    {
        bool isChatFull = false;

        for (int i = 0; i < chatText.Length; i++)
        {
            if (chatText[i].text == string.Empty)
            {
                chatText[i].text = message;
                isChatFull = true;
                break;
            }
        }
        if(!isChatFull)
        {
            for(int i = 1; i<chatText.Length; i++)
            {
                chatText[i - 1].text = chatText[i].text;
            }
            chatText[chatText.Length - 1].text = message;
        }
    }
    #endregion
}
