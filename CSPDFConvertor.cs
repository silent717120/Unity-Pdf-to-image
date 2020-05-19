using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GhostscriptSharp;

public class CSPDFConvertor : MonoBehaviour
{
    public static CSPDFConvertor instance;
    // Use this for initialization
    void Start()
    {
        instance = this;
    }

    //用來轉換PDF成一張一張的jpeg圖片
    public void f_ConvertAllPDF(string pdfPath, string savePath)
    {
        GhostscriptSettings ghostscriptSettings = new GhostscriptSettings();
        ghostscriptSettings.Device = GhostscriptSharp.Settings.GhostscriptDevices.jpeg; //圖片類型
        ghostscriptSettings.Size.Native = GhostscriptSharp.Settings.GhostscriptPageSizes.legal; //legal為原尺寸
        //ghostscriptSettings.Size.Manual = new System.Drawing.Size(2552, 3579); //此為設定固定尺寸
        ghostscriptSettings.Resolution = new System.Drawing.Size(150, 150); //此為解析度
        ghostscriptSettings.Page.AllPages = true;
        GhostscriptWrapper.GenerateOutput(pdfPath, savePath, ghostscriptSettings);

        Debug.Log("Complete!");
    }
}
