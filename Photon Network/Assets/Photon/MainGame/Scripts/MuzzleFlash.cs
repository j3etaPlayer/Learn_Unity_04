using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public float muzzleCoolTime = 0.015f;
    private float muzzleCounter;

    private void OnEnable()   // �÷��̾� ���� Ȱ��ȭ ���� ��
    {
        MuzzleReset();
    }

    private void Update()
    {
        // Muzzle �ð��� ����ϴ� ����
        if (gameObject.activeSelf) // Ȱ��ȭ �Ǿ� ���� ���� �ڵ� ����
        {
            muzzleCounter -= Time.deltaTime;

            if (muzzleCounter <= 0)
                gameObject.SetActive(false);
        }
    }

    // Counter�� ���� Cooltime���� �ʱ�ȭ
    private void MuzzleReset()
    {
        muzzleCounter = muzzleCoolTime;
    }
}
