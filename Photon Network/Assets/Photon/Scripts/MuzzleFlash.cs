using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public float muzzleCoolTime = 0.015f;
    private float muzzleCounter;

    private void OnEnable()
    {
        MuzzleReset();
    }

    void Update()
    {
        if (gameObject.activeSelf)
        {
            muzzleCounter -= Time.deltaTime;

            if (muzzleCounter <= 0)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void MuzzleReset()
    {
        muzzleCounter = muzzleCoolTime;
    }
}
