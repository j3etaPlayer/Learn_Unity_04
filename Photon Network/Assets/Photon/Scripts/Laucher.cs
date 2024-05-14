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
    // MonoBehaviourPunCallbacks : Photon Network���¿� ���� Callback Interface�Լ��� �ڵ����� ����ϰ� ����Ҽ� �ְ� ���ִ� Ŭ����

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
    public GameObject errorPanel;                           // ������ �߻��Ǿ��� �� Ȱ��ȭ �Ǵ� ������Ʈ
    public TMP_Text errorText;                              // ������ �ش�Ǵ� ������ ����ϴ� ����

    [Header("[Create Nickname]")]
    public GameObject nicknamePanel;                        // �г��� ���� ������Ʈ
    public TMP_InputField nicknameInput;                    // �г����� �ۼ��ϴ� ����
    private bool hasSetNickname = false;                    // �г����� ������ �Ǿ� ������ �ݺ��� �����ֱ� ���� ����
    private const string PLAYERNAMEKEY = "playerName";      // playerPrefabs �� ���, ������ ������ ���� ���

    [Header("[Photon RoomInfo]")]
    // �� ���� �� ���� �̸��� �����ͷ� �Ľ��ϴ� Ŭ����
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
        currentStatus.text = PhotonNetwork.NetworkClientState.ToString() + $"\n�г��� : {PhotonNetwork.NickName}";
    }

    /// <summary>
    /// �ǳڵ��� �ݾ��ִ� �Լ�(���ο� �ǳ��� �����ɽ� �ۼ� �ʿ�)
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
        loadingText.text = "������ ���� ��...";

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        CloseMenu();
        menuButtons.SetActive(true);

        // ������ ����ʰ� ���ÿ� �κ� ����
        PhotonNetwork.JoinLobby();

        loadingText.text = "�κ� ���� ��...";

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
    /// �� ���� �г��� Ȱ��ȭ
    /// </summary>
    public void CreateRoomPanel()
    {
        CloseMenu();
        createRoomPanel.SetActive(true);
    }

    /// <summary>
    /// �� ���� ��ư�� Ȱ��ȭ
    /// </summary>
    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInput.text))
        {
            // �˾�â, �� ���� ��� �˾�
            Debug.LogWarning("���� ������ �ۼ��� �ּ���");
        }
        else
        {
            // ���� ����, �濡 ���� �� �ִ� �ο���, ���� ȣ��Ʈ
            RoomOptions option = new RoomOptions();
            option.MaxPlayers = 8;

            PhotonNetwork.CreateRoom(roomNameInput.text, option);

            CloseMenu();
            loadingText.text = "�� ���� ��...";
            loadingPanel.SetActive(true);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CloseMenu();
        errorText.text = $"�� ������ �����Ͽ����ϴ�.\n���� : {message}";
        errorPanel.SetActive(true);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CloseMenu();
        errorText.text = $"���� ������ �����Ͽ����ϴ�.\n���� : {message}";
        errorPanel.SetActive(true);
    }

    public override void OnJoinedRoom()
    {
        CloseMenu();
        roomNameText.text = $"�� ���� : {PhotonNetwork.CurrentRoom.Name}";
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
        loadingText.text = "�� ���� ��...";
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
        pV.RPC(nameof(ChatRPC), RpcTarget.All, $"<color=blue>{newPlayer.NickName}</color>���� �濡 �����̽��ϴ�.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ShowListAllPlayer();
        pV.RPC(nameof(ChatRPC), RpcTarget.All, $"<color=red>{otherPlayer.NickName}</color>���� �濡 �����̽��ϴ�.");
    }

    public void ButtonLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenu();

        loadingText.text = "���� ������ ��...";
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

    // �̻�� ���
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
        // �������� ���� �ϳ��� �����ϸ� �ش� �濡 �������� ����
        if (PhotonNetwork.CountOfRooms <= 0)
        {
            JoinOrCreateRoom();
        }
        // �������� ���� ���ٸ� ���� ���� �����.
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
        // �濡 ���� �� ä�� â �αװ� ����ִ� ���·� ����
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
    private void ChatRPC(string message)    // �Լ��� RPC�� �ۼ��ϸ� RPC ��Ʈ����Ʈ�� ����ϰڴٴ� ����̴�.
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
