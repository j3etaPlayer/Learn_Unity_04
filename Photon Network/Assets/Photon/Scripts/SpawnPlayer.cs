using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnPlayer : MonoBehaviour
{
    public static SpawnPlayer Instance;

    public GameObject playerPrefab;      // ���ӿ� ���� �÷��̾� ������
    public Transform[] spawnPositions;   
    private GameObject player;           // �÷��̾ ����, �ı��� �� ����� ���� ����

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
        if (PhotonNetwork.IsConnected)  // MainLobby���� Connect()�Լ� ���� ������ �Ǿ� �ִ� ����
        {
            Spawn();
        }
    }

    private Transform GetSpawnPosition()
    {
        int randomIndex = Random.Range(0, spawnPositions.Length);

        return spawnPositions[randomIndex];
    }

    public void Spawn()  //  Project�� �ִ� ������ Load�ϴ� ������ �ν��Ͻ�ȭ ���
    {
        //Instantiate(playerPrefab, spawnPosition.position, Quaternion.identity); ��Ʈ��ũ ������ �۵��� ���� ����.
        // ���� : ������ playerPrefab�� ������Ʈ�� photonView�� �����ϰ� �־�� �Ѵ�.
        player = PhotonNetwork.Instantiate(playerPrefab.name, GetSpawnPosition().position, Quaternion.identity); // ��Ʈ��ũ ������Ʈ �ν��Ͻ�ȭ
    } 

    public void Die()
    {
        // ���� PhotonView. Actor Number, Id�� ������ �ؼ�.. �� �÷��̾� �����Ͱ� 1������ �Ͽ���.
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
