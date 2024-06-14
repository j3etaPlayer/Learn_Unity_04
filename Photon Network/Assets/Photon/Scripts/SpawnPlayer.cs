using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayer : MonoBehaviour
{
    public static SpawnPlayer Instance;

    public GameObject playerPrefab;      // 게임에 사용될 플레이어 프리팹
    public Transform[] spawnPositions;   
    private GameObject player;           // 플레이어가 생성, 파괴될 때 사용할 변수 저장

    [Header("Respawn")]
    public GameObject DeathEffect;
    public float respawnTime = 2f;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (PhotonNetwork.IsConnected)  // MainLobby에서 Connect()함수 서버 연결이 되어 있는 상태
        {
            Spawn();
        }
    }

    private Transform GetSpawnPosition()
    {
        int randomIndex = Random.Range(0, spawnPositions.Length);

        return spawnPositions[randomIndex];
    }

    public void Spawn()  //  Project에 있는 에셋을 Load하는 프리팹 인스턴스화 방식
    {
        //Instantiate(playerPrefab, spawnPosition.position, Quaternion.identity); 네트워크 에서는 작동을 하지 않음.
        // 조건 : 생성할 playerPrefab의 컴포넌트로 photonView를 소유하고 있어야 한다.
        player = PhotonNetwork.Instantiate(playerPrefab.name, GetSpawnPosition().position, Quaternion.identity); // 네트워크 오브젝트 인스턴스화
    } 

    public void Die()
    {
        // 나의 PhotonView. Actor Number, Id에 접근을 해서.. 내 플레이어 데이터가 1데스를 하였다.
        MatchManager.Instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

        StartCoroutine(nameof(DieCo));
    }

    private IEnumerator DieCo()
    {
        PhotonNetwork.Instantiate(DeathEffect.name, player.transform.position, Quaternion.identity);

        yield return new WaitForSeconds(respawnTime);

        PhotonNetwork.Destroy(player);
        Spawn();
    }
}
