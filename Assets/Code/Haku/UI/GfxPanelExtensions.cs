using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class GfxPanelExtensions
{
    private const string LEVEL_TEXT = "Lv.";

    private const string LEVEL_MAX = "MAX";

    public static void SetWeaponData(this GfxPanel aItemPanel, WeaponOwnedData aWeaponData, bool aShowCountWeapons, bool aShowName, bool aSnapToDesiredState = false, HorizontalAlignmentOptions aXAlignmentLeft = HorizontalAlignmentOptions.Left, VerticalAlignmentOptions aYAlignmentLeft = VerticalAlignmentOptions.Middle, HorizontalAlignmentOptions aXAlignmentRight = HorizontalAlignmentOptions.Right, VerticalAlignmentOptions aYAlignmentRight = VerticalAlignmentOptions.Middle)
    {
        aItemPanel.SetDisabled(false);
        aItemPanel.SetIcon(GfUiTools.GetIconWeapon(), true);
        aItemPanel.SetIconSizeDefault();
        aItemPanel.SetBlessed(aWeaponData.Blessed);

        var tierColor = aWeaponData.TierColor;
        GfxPanelHightlightState defaultState = aItemPanel.GetHightlightStateDefault();
        defaultState.ColorPanel = aWeaponData.TierColorComplementary;
        defaultState.ColorIcon = defaultState.ColorLeftText = tierColor;
        aItemPanel.SetHightlightStateDefault(defaultState, aSnapToDesiredState);

        if (!aShowName)
        {
            aItemPanel.SetLeftTextOnly("", aXAlignmentLeft, aYAlignmentLeft);
            aItemPanel.SetLeftTextLength(0);
        }
        else if (aShowCountWeapons)
        {
            aItemPanel.SetLeftText(aWeaponData.GetEffectiveName(), aXAlignmentLeft, aYAlignmentLeft);
            aItemPanel.SetRightText("x" + aWeaponData.CountInInventory, aXAlignmentRight, aYAlignmentRight);
            aItemPanel.SetLeftTextLengthRatio(0.8f);
        }
        else
        {
            aItemPanel.SetLeftTextOnly(aWeaponData.GetEffectiveName(), aXAlignmentLeft, aYAlignmentLeft);
        }
    }

    public static void SetCharmData(this GfxPanel aItemPanel, EquipCharmData aCharmData, bool aSnapToDesiredState, HorizontalAlignmentOptions aXAlignmentLeft = HorizontalAlignmentOptions.Left, VerticalAlignmentOptions aYAlignmentLeft = VerticalAlignmentOptions.Middle, HorizontalAlignmentOptions aXAlignmentRight = HorizontalAlignmentOptions.Left, VerticalAlignmentOptions aYAlignmentRight = VerticalAlignmentOptions.Middle)
    {
        aItemPanel.SetDisabled(false);
        aItemPanel.SetIcon(GfUiTools.GetIconCharm(), true);
        aItemPanel.SetIconSizeDefault();
        aItemPanel.SetLeftTextLengthRatio(0.7f);
        aItemPanel.SetBlessed(aCharmData.Blessed);

        GfxPanelHightlightState defaultState = aItemPanel.GetHightlightStateDefault();
        defaultState.ColorPanel = aCharmData.RankColor;
        defaultState.ColorIcon = defaultState.ColorLeftText = aCharmData.CategoryColor;
        aItemPanel.SetHightlightStateDefault(defaultState, aSnapToDesiredState);

        if (aSnapToDesiredState)
            aItemPanel.SnapToDesiredState();
        else
            aItemPanel.StartTransitionToNewState();

        string levelText = LEVEL_MAX;
        if (!aCharmData.IsMaxLevel)
        {
            GfcStringBuffer stringBuffer = GfcPooling.GfcStringBuffer;
            stringBuffer.Concatenate(LEVEL_TEXT);
            stringBuffer.Concatenate(aCharmData.Level + 1);
            levelText = stringBuffer.GetStringClone();
            stringBuffer.Clear();
        }

        aItemPanel.SetLeftText(aCharmData.Name, aXAlignmentLeft, aYAlignmentLeft);
        aItemPanel.SetRightText(levelText, aXAlignmentRight, aYAlignmentRight);
    }
}