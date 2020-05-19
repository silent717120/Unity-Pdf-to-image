using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System;

//匯入PDF
public void f_ImportPDF(string inputPath)
{
   //為了要顯示進度
   StartCoroutine(MergePdfImg(inputPath));
}

public void MergePdfImg(string inputPath){
   //利用API轉換PDF檔案成jpeg圖片 convert PDF to Imgage--------------------
   var fileName = path + "/P%03d.jpeg";
   //將pdf輸出成單頁圖片
   CSPDFConvertor.instance.f_ConvertAllPDF(inputPath, fileName);
   //取得單面的圖片
   FileInfo[] pdfImages = PDFData.GetFiles("*.*");
  
   //將雙頁合併，算出最後的頁數
   string AllPdfLength = (pdfImages.Length / 2).ToString();
   //累積頁數的參數
   int NowPdfIndex = 0;
  
   //製作一個介面負責顯示進度內容給使用者觀看
   loadTip = "合併PDF  "+ NowPdfIndex.ToString() + " / "+ AllPdfLength;
   CSPrompt.instance.showMessage(loadTip, false);
   yield return null;
  
   //進行合成
   if (pdfImages.Length > 0){
      
      for (int i = 0; i < pdfImages.Length-1; i +=2){
            //計算第幾張
            NowPdfIndex++;
            //顯示進度-合併兩張圖階段
            loadTip = "合併PDF  " + NowPdfIndex.ToString() + " / " + AllPdfLength;
            CSPrompt.instance.showMessage(loadTip, false);
            yield return null;
            //載入兩張圖後合併
            Texture2D img1 = LoadTexture(pdfImages[i].FullName); //左圖
            Texture2D img2 = LoadTexture(pdfImages[i+1].FullName); //右圖
            //建立空白貼圖，設定寬，高，用於合併兩張圖
            Texture2D texture = new Texture2D(img1.width + img2.width, img1.height, TextureFormat.RGB24, false);
            texture.Apply();

            //第一種方法，取出兩張圖的每一個像素顏色，依序存入一個完整的陣列中，利用SetPixels一次渲染圖片 (聽說速度較快)
            Color[] imgColorArray1 = img1.GetPixels();
            Color[] imgColorArray2 = img2.GetPixels();
            Color[] imgColorArray3 = texture.GetPixels();
            for (int k = 0; k < img1.height; k++)
            {
                for(int s = 0; s < img1.width * 2; s++)
                {
                    if(s < img1.width)
                    {
                        imgColorArray3[s+ (k * img1.width*2)] = imgColorArray1[s+(k*img1.width)];
                    }
                    else
                    {
                        imgColorArray3[s + (k * img1.width*2)] = imgColorArray2[(s - img1.width) + (k * img1.width)];
                    }
                }
            }
            texture.SetPixels(imgColorArray3);
            /*
            //第二種方法，計算位置，依序用SetPixel將每一個像素點放到正確位置(聽說速度較慢)
            //第一張圖
            for (int x = 0; x < img1.width; x++)
            {
                for (int y = 0; y < img1.height; y++)
                {
                    texture.SetPixel(x, y, img1.GetPixel(x, y));
                }
            }
            //第二張圖
            for (int x = 0; x < img2.width; x++)
            {
                for (int y = 0; y < img2.height; y++)
                {
                    texture.SetPixel(texture.width / 2 + x, y, img2.GetPixel(x, y));
                }
            }
            */
            //生成貼圖
            texture.Apply();
       
            //顯示進度-壓縮尺寸
            loadTip = "壓縮PDF  " + NowPdfIndex.ToString() + " / " + AllPdfLength;
            CSPrompt.instance.showMessage(loadTip, false);
            yield return null;
       
            //將圖片寬跟高限制在2000內，重新利用RenderTexture壓縮貼圖
            float width = img1.width * 2;
            float height = img1.height;
            float scale = 1;
            if(width > 2000)
            {
                scale = 1920 / width;
            }
            else if(height > 2000)
            {
                scale = 1920 / height;
            }
            int newWidth = Convert.ToInt32((float)width * scale);
            int newHeight = Convert.ToInt32((float)height * scale);
            //生成RenderTexture貼圖，利用Graphics.Blit()重新渲染貼圖
            RenderTexture renderTexture = new RenderTexture(newWidth, newHeight, 0);
            RenderTexture.active = renderTexture;
            Graphics.Blit(texture, renderTexture);

            Texture2D texture_small = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            texture_small.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
            texture_small.Apply();

            texture = texture_small;
            RenderTexture.active = null;
            renderTexture.Release(); //完成後需釋放記憶體
            
            //顯示進度-輸出檔案
            loadTip = "輸出PDF  " + NowPdfIndex.ToString() + " / " + AllPdfLength;
            CSPrompt.instance.showMessage(loadTip, false);
            yield return null;
       
            //輸出已融合的雙頁圖片
            System.IO.File.WriteAllBytes(path + "/" + NowPdfIndex.ToString() + ".png", texture.EncodeToPNG());
      }
    
      //若頁數為奇數頁，最後一張右方則為空白 (基本上做一樣的事)
      if(pdfImages.Length%2 != 0)
      {
            loadTip = "合併奇數頁PDF...";
            CSPrompt.instance.showMessage(loadTip, false);
            yield return null;
            //將單頁與空白合併
            Texture2D img1 = ResourceManager.instance.LoadTexture(pdfImages[pdfImages.Length - 1].FullName); //左圖
            Texture2D texture = new Texture2D(img1.width + img1.width, img1.height, TextureFormat.RGB24, false); //畫在這張貼圖上
            //換成白色底
            Color fillColor = Color.white;
            var fillColorArray = texture.GetPixels();
            for (var i = 0; i < fillColorArray.Length; ++i)
            {
                fillColorArray[i] = fillColor;
            }
            texture.SetPixels(fillColorArray);
            texture.Apply();
            //設置左圖顏色
            for (int x = 0; x < img1.width; x++)
            {
                for (int y = 0; y < img1.height; y++)
                {
                    texture.SetPixel(x, y, img1.GetPixel(x, y));
                }
            }
       
            //生成貼圖
            texture.Apply();
       
            //顯示進度-壓縮尺寸
            loadTip = "壓縮奇數頁PDF";
            CSPrompt.instance.showMessage(loadTip, false);
            yield return null; //完成後需釋放記憶體
       
            //將圖片寬跟高限制在2000內，重新利用RenderTexture壓縮貼圖
            float width = img1.width * 2;
            float height = img1.height;
            float scale = 1;
            if (width > 2000)
            {
                scale = 1920 / width;
            }
            else if (height > 2000)
            {
                scale = 1920 / height;
            }
            int newWidth = Convert.ToInt32((float)width * scale);
            int newHeight = Convert.ToInt32((float)height * scale);
            //生成RenderTexture貼圖，利用Graphics.Blit()重新渲染貼圖
            RenderTexture renderTexture = new RenderTexture(newWidth, newHeight, 0);
            RenderTexture.active = renderTexture;
            Graphics.Blit(texture, renderTexture);

            Texture2D texture_small = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            texture_small.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
            texture_small.Apply();

            texture = texture_small;
            RenderTexture.active = null;
            renderTexture.Release(); //完成後需釋放記憶體

            //顯示進度-輸出檔案
            loadTip = "輸出奇數頁PDF";
            CSPrompt.instance.showMessage(loadTip, false);
            yield return null; //完成後需釋放記憶體

            //計算最後一張奇數
            NowPdfIndex++;
            //輸出已融合的雙頁圖片
            System.IO.File.WriteAllBytes(path + "/" + NowPdfIndex.ToString() + ".png", texture.EncodeToPNG());
            }
            //刪除一開始輸出的單頁圖片
            AllPdfLength = pdfImages.Length.ToString();
            NowPdfIndex = 0;
            for (int i = pdfImages.Length - 1; i >= 0; i --)
            {
                //計算頁數
                NowPdfIndex++;
                //顯示刪除進度
                loadTip = "移除單頁圖片中  " + NowPdfIndex + " / " + AllPdfLength;
                CSPrompt.instance.showMessage(loadTip, false);
                //刪除檔案
                File.Delete(pdfImages[i].FullName);
                yield return null;
            }
        }
 
        //關閉進度介面
        CSPrompt.instance.hideMessage();
    }


public Texture2D LoadTexture(string loadPath){
   //檔案不存在則跳出
   if (!File.Exists(loadPath)){
       Debug.Log("File not exists : " + loadPath);
       return null;
   }
   //創建文件讀取流
   using (FileStream fileStream = new FileStream(loadPath, FileMode.Open, FileAccess.Read)){
       fileStream.Seek(0, SeekOrigin.Begin);
       //創建文件長度緩衝區
       byte[] bytes = new byte[fileStream.Length];
       //讀取文件
       fileStream.Read(bytes, 0, (int)fileStream.Length);
       //釋放文件讀取流
       fileStream.Close();
       fileStream.Dispose();
       //將文件轉為貼圖
       Texture2D texture = new Texture2D(2, 2, TextureFormat.DXT5, false);                
       texture.LoadImage(bytes);
       texture.wrapMode = TextureWrapMode.Clamp;
       texture.filterMode = FilterMode.Bilinear;
    }
    return texture;
}
