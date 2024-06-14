using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public float muzzleCoolTime = 0.015f;
    private float muzzleCounter;

    private void OnEnable()   // 플레이어 의해 활성화 됬을 때
    {
        MuzzleReset();
    }

    private void Update()
    {
        // Muzzle 시간을 계산하는 로직
        if (gameObject.activeSelf) // 활성화 되어 있을 때만 코드 실행
        {
            muzzleCounter -= Time.deltaTime;

            if (muzzleCounter <= 0)
                gameObject.SetActive(false);
        }
    }

    // Counter의 값을 Cooltime으로 초기화
    private void MuzzleReset()
    {
        muzzleCounter = muzzleCoolTime;
    }
}
