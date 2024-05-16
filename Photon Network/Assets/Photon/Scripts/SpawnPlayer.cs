using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPlayer : MonoBehaviour
{
    public GameObject playerPrefab;     // ���ӿ� ���� �÷��̾� ������
    public Transform spawnPosition;


    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {

        }

        Spawn();
    }

    public void Spawn()                 // project�� �ִ� asset�� load�ϴ� ������ �ν��Ͻ�ȭ ���
    {
        // Instantiate(playerPrefab, spawnPosition.position, Quaternion.identity);                  // ��Ʈ��ũ �󿡼��� �۵����� ����
        PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition.position, Quaternion.identity);  // ��Ʈ��ũ ������Ʈ�� �ν��Ͻ� ȭ
        // ���� : ������ PlayerPrefab�� photon view ������Ʈ�� ������ �Ǿ��־�� �Ѵ�.
    }
}
