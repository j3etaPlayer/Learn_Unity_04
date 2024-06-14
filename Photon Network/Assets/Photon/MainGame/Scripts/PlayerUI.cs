using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("���� �ý��� UI")]
    public GameObject overHeatTextObject;  // Canvas UI���� ������� �ʴ� ������Ʈ�� ��Ȱ��ȭ �ϴ� ���� ���� �� �� ȿ�����̴�.
    public Slider currentWeaponSlider;

    public WeaponSlot[] allWeaponSlots;
    private int currentWeaponIndex;

    [Header("���� ȭ��")]
    public GameObject deathScreenObject;
    public TMP_Text deathText;

    [Header("�÷��̾�")]
    public TMP_Text playerHPText;          // Scene���� HP_txt �������� ��

    [Header("player Options")]
    public GameObject optionPanel;

    public void ShowDeathMessage(string killer)
    {
        deathScreenObject.SetActive(true);
        deathText.text = $"�÷��̾ {killer}���� �׾����ϴ�.";
    }

    public void SetWeaponSlot(int weaponIndex) // PlayerController���� Index �Ѱ� �ְ�
    {
        currentWeaponIndex = weaponIndex;

        for (int i = 0; i < allWeaponSlots.Length; i++)
        {
            //SetImageAlpha(allWeaponSlots[i].weaponImage, 0.5f);
            SetWeaponSlot(allWeaponSlots[i].weaponImage, allWeaponSlots[i].weaponNumber, 0.5f);
        }

        SetWeaponSlot(allWeaponSlots[currentWeaponIndex].weaponImage, allWeaponSlots[currentWeaponIndex].weaponNumber, 1f);
    }

    private void SetWeaponSlot(Image image, TMP_Text text, float alphaValue)
    {
        SetImageAlpha(image, alphaValue);
        SetTextImageAlpha(text, alphaValue);
    }

    private void SetImageAlpha(Image image, float alphaValue)
    {
        Color color = image.color;
        color.a = alphaValue;
        image.color = color;
    }

    private void SetTextImageAlpha(TMP_Text text, float alphaValue)
    {
        Color color = text.color;
        color.a = alphaValue;
        text.color = color;
    }

    public void ShowOptions()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionPanel.activeInHierarchy)
            {
                optionPanel.SetActive(false);
            }
            else
            {
                optionPanel.SetActive(true);
            }
        }
        if (optionPanel.activeInHierarchy && !Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

        }
        else if (!optionPanel.activeInHierarchy && Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

}

[System.Serializable]
public struct WeaponSlot
{

    public Image weaponImage;
    public TMP_Text weaponNumber;
}
