using UnityEngine;
using Photon.Realtime;
using TMPro;
using Photon.Pun.Demo.PunBasics;

public class RoomButtonInfo : MonoBehaviour
{
    public TMP_Text buttonText;
    private RoomInfo info;

    public void SetButtonInfo(RoomInfo inputInfo)
    {
        info = inputInfo;

        buttonText.text = info.Name + info.PlayerCount;
    }
    public void ButtonOpenRoom()
    {
        Laucher.Instance.JoinRoom(info);
    }

}
