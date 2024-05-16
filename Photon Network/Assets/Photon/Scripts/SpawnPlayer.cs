using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPlayer : MonoBehaviour
{
    public GameObject playerPrefab;     // 게임에 사용될 플레이어 프리팹
    public Transform spawnPosition;


    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {

        }

        Spawn();
    }

    public void Spawn()                 // project에 있는 asset을 load하는 프리팹 인스턴스화 방식
    {
        // Instantiate(playerPrefab, spawnPosition.position, Quaternion.identity);                  // 네트워크 상에서는 작동하지 않음
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition.position, Quaternion.identity);  // 네트워크 오브젝트를 인스턴스 화
        // 조건 : 생성할 PlayerPrefab에 photon view 컴포넌트가 부착이 되어있어야 한다.
    }
}
