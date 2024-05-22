using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("과열 시스템 UI")]
    public GameObject overheatTextObject;
    public Slider currentWeaponSlider;
    public TMP_Text heatText;

    public WeaponSlot[] allWeaponSlots;
    private int currentWeaponIndex;
    
    public void SetWeaponSlot(int weaponIndex)
    {
        currentWeaponIndex = weaponIndex;

        for (int i = 0; i < allWeaponSlots.Length; i++)
        {
            //SetImageAlpha(allWeaponSlots[i].weaponImage);
            SetWeaponSlot(allWeaponSlots[i].weaponImage, allWeaponSlots[i].weaponText);
        }

        // SetImageAlpha(allWeaponSlots[currentWeaponIndex].weaponImage, 1f);
        SetWeaponSlot(allWeaponSlots[currentWeaponIndex].weaponImage, allWeaponSlots[currentWeaponIndex].weaponText, 1f);

    }

    private void SetWeaponSlot(Image image, TMP_Text text, float alhpaValue = 0.5f)
    {
        SetImageAlpha(image, alhpaValue);
        SetTextImageAlpha(text, alhpaValue);    
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
}

[System.Serializable]
public struct WeaponSlot
{
    public Image weaponImage;
    public TMP_Text weaponText;
}