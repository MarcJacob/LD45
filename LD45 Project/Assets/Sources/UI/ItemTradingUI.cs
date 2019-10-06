﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using System;
using UnityEngine.UI;

public class ItemTradingUI : MonoBehaviour
{
    [SerializeField]
    private GameObject playerTradingPanelContent;
    [SerializeField]
    private GameObject stationTradingPanelContent;
    [SerializeField]
    private GameObject tradeItemLinePrefab;

    private Cargo playerShipCargo;
    private Cargo stationCargo;
    private Trading stationTradingComponent;

    private List<TradePanelLine> playerTradingPanelLines = new List<TradePanelLine>();
    private List<TradePanelLine> stationTradingPanelLines = new List<TradePanelLine>();

    private bool selling;
    TradePanelLine selectedLine;

    public void SetDockedStation(Dock dock)
    {
        stationCargo = dock.GetComponent<Cargo>();
        stationTradingComponent = dock.GetComponent<Trading>(); // TODO handle when station does not have trading component. Do not show the panels.
    }

    private void OnEnable()
    {
        playerShipCargo = PlayerInput.CurrentShip.GetComponent<Cargo>();
        PlayerInput.OnPlayerShipChanged += PlayerInput_OnPlayerShipChanged;
        InvokeRepeating("RefreshTradePanels", 0f, 3f);
    }

    private void OnDisable()
    {
        CancelInvoke("RefreshTradePanels");
    }

    private void PlayerInput_OnPlayerShipChanged(GameObject obj)
    {
        if (obj == null) playerShipCargo = null;
        else playerShipCargo = PlayerInput.CurrentShip.GetComponent<Cargo>();
        RefreshTradePanels();
    }

    private void RefreshTradePanels()
    {
        RefreshTradePanel(playerTradingPanelContent, playerShipCargo, playerTradingPanelLines, true);
        RefreshTradePanel(stationTradingPanelContent, stationCargo, stationTradingPanelLines, false);
    }

    private void RefreshTradePanel(GameObject panel, Cargo cargo, List<TradePanelLine> existingLines, bool selling)
    {

        float lineHeight = tradeItemLinePrefab.GetComponent<RectTransform>().rect.height;
        int lineAmount = 0;
        if (cargo != null) lineAmount = cargo.GetStoredResourceTypes().Length;

        var delta = panel.GetComponent<RectTransform>().sizeDelta;
        delta.y = lineHeight * lineAmount;
        panel.GetComponent<RectTransform>().sizeDelta = delta;

        if (lineAmount > existingLines.Count)
        {
            int lineAmountToInstantiate = lineAmount - existingLines.Count;
            for (int i = 0; i < lineAmountToInstantiate; i++)
            {
                GameObject newLine = CreateTradeLine(panel, existingLines, lineHeight, selling);
                existingLines.Add(newLine.GetComponent<TradePanelLine>());
            }
        }
        else if (lineAmount < existingLines.Count)
        {
            for(int i = lineAmount; i < existingLines.Count; i++)
            {
                var line = existingLines[i];
                existingLines.RemoveAt(i);
                Destroy(line.gameObject);
            }
        }

        if (lineAmount > 0)
        {
            int lineID = 0;
            for (int i = 0; i < cargo.StoredResources.Length; i++)
            {
                if (cargo.StoredResources[i] > 0)
                {
                    RESOURCE_TYPE t = (RESOURCE_TYPE)i;
                    existingLines[lineID].SetLineInfo(t, cargo.StoredResources[i], t.GetBasePrice());
                    lineID++;
                }
            }
        }

    }

    private GameObject CreateTradeLine(GameObject panel, List<TradePanelLine> existingLines, float lineHeight, bool selling)
    {
        var newLine = Instantiate(tradeItemLinePrefab);
        var linePos = newLine.GetComponent<RectTransform>().position;
        linePos.y = (existingLines.Count + 0.5f) * -lineHeight;

        newLine.GetComponent<RectTransform>().position = linePos;
        newLine.GetComponent<RectTransform>().SetParent(panel.transform, false);

        newLine.GetComponent<Button>().onClick.AddListener(() => SelectLine(newLine, selling));

        return newLine;
    }

    public void Sell()
    {
        int amount = 1;
        if (Input.GetKey(KeyCode.LeftShift)) amount = 10;
        else if (Input.GetKey(KeyCode.LeftControl)) amount = 100;
        if (selling && selectedLine != null)
        {
            stationTradingComponent.BuyFrom(playerShipCargo, (RESOURCE_TYPE)selectedLine.ResourceID, (uint)amount, selectedLine.PricePerUnit);
            RefreshTradePanels();
        }
    }

    public void Buy()
    {
        int amount = 1;
        if (Input.GetKey(KeyCode.LeftShift)) amount = 10;
        else if (Input.GetKey(KeyCode.LeftControl)) amount = 100;
        if (!selling && selectedLine != null)
        {
            stationTradingComponent.SellTo(playerShipCargo, (RESOURCE_TYPE)selectedLine.ResourceID, (uint)amount, selectedLine.PricePerUnit);
            RefreshTradePanels();
        }
    }

    public void SelectLine(GameObject obj, bool selling)
    {
        var line = obj.GetComponent<TradePanelLine>();
        if (selectedLine != null) selectedLine.OnLineDeselected();

        selectedLine = line;
        this.selling = selling;
        line.OnLineSelected();
    }
}
