 //點擊資源庫'匯入PDF'按鈕
    public void f_ImportPDF(string inputPath)
    {
        string[] paths = inputPath.Split(',');

        for (int j = 0; j < paths.Length; j++)
        {
            //先產生一個資料夾,之後要用來裝jpeg圖片
            GameObject PDFfolder = f_AddFolder();
            CSResourceFolder resourceFolder = PDFfolder.GetComponent<CSResourceFolder>();

            //在指定路徑創建一個名為PDFData的資料夾
           //var path = Application.dataPath + "/../Resource/PDFData" + (j + 1) + " " + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var path = Application.dataPath + "/../Resource/" + Path.GetFileNameWithoutExtension(paths[j]);
            if (Directory.Exists(path)) //若不移除會導致合併圖片時出問題
            {
                Debug.Log("该路径已存在转换过的PDF资料夹，进行移除");
                Directory.Delete(path,true);
            }
            DirectoryInfo PDFData = Directory.CreateDirectory(path);
            resourceFolder.SetLoadFolder(path);

            StartCoroutine(MergePdfImg(paths,j, path, PDFData, PDFfolder));
        }
    }

    public IEnumerator MergePdfImg(string[] paths,int j, string path, DirectoryInfo PDFData, GameObject PDFfolder)
    {
        string loadTip = "建立单页PDF图片  ";
        //CSPrompt.instance.showMessage(loadTip, false);
        yield return null;

        //利用API轉換PDF檔案成jpeg圖片 convert PDF to Imgage--------------------
        var fileName = path + "/P%03d.jpeg";
        //將pdf輸出成單頁圖片
        CSPDFConvertor.instance.f_ConvertAllPDF(paths[j], fileName);
        //取得單面的圖片
        FileInfo[] pdfImages = PDFData.GetFiles("*.*");

        string AllPdfLength = (pdfImages.Length / 2).ToString();
        int NowPdfIndex = 0;
        loadTip = "合并PDF  "+ NowPdfIndex.ToString() + " / "+ AllPdfLength;
        CSPrompt.instance.showMessage(loadTip, false);
        yield return null;
        //進行合成
        if (pdfImages.Length > 0)
        {
            for (int i = 0; i < pdfImages.Length-1; i +=2)
            {
                //計算第幾張
                NowPdfIndex++;
                loadTip = "合并PDF  " + NowPdfIndex.ToString() + " / " + AllPdfLength;
                CSPrompt.instance.showMessage(loadTip, false);
                yield return null;
                //載入兩張圖後合併
                Texture2D img1 = ResourceManager.instance.LoadTexture(pdfImages[i].FullName); //左圖
                Texture2D img2 = ResourceManager.instance.LoadTexture(pdfImages[i+1].FullName); //右圖
                Texture2D texture = new Texture2D(img1.width + img2.width, img1.height, TextureFormat.RGB24, false); //畫在這張貼圖上
                Debug.Log(img1.width);
                Debug.Log(img1.height);
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

                texture.Apply();

                loadTip = "压缩PDF  " + NowPdfIndex.ToString() + " / " + AllPdfLength;
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
                RenderTexture renderTexture = new RenderTexture(newWidth, newHeight, 0);
                RenderTexture.active = renderTexture;
                Graphics.Blit(texture, renderTexture);

                Texture2D texture_small = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
                texture_small.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
                texture_small.Apply();

                texture = texture_small;
                RenderTexture.active = null;
                renderTexture.Release(); //完成後需釋放記憶體

                loadTip = "输出PDF  " + NowPdfIndex.ToString() + " / " + AllPdfLength;
                CSPrompt.instance.showMessage(loadTip, false);
                yield return null;
                //輸出已融合的雙頁圖片
                System.IO.File.WriteAllBytes(path + "/" + NowPdfIndex.ToString() + ".png", texture.EncodeToPNG());
            }
            //若頁數為奇數頁，最後一張右方則為空白
            if(pdfImages.Length%2 != 0)
            {
                loadTip = "合并奇数页PDF...";
                CSPrompt.instance.showMessage(loadTip, false);
                yield return null;
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

                texture.Apply();

                loadTip = "压缩奇数页PDF";
                CSPrompt.instance.showMessage(loadTip, false);
                yield return null;
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
                RenderTexture renderTexture = new RenderTexture(newWidth, newHeight, 0);
                RenderTexture.active = renderTexture;
                Graphics.Blit(texture, renderTexture);

                Texture2D texture_small = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
                texture_small.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
                texture_small.Apply();

                texture = texture_small;
                RenderTexture.active = null;
                renderTexture.Release();

                loadTip = "输出奇数页PDF";
                CSPrompt.instance.showMessage(loadTip, false);
                yield return null; //完成後需釋放記憶體

                //計算最後一張奇數
                NowPdfIndex++;
                //輸出已融合的雙頁圖片
                System.IO.File.WriteAllBytes(path + "/" + NowPdfIndex.ToString() + ".png", texture.EncodeToPNG());
            }
            //刪除單頁圖片
            AllPdfLength = pdfImages.Length.ToString();
            NowPdfIndex = 0;
            for (int i = pdfImages.Length - 1; i >= 0; i --)
            {
                NowPdfIndex++;
                loadTip = "移除单页图片中  " + NowPdfIndex + " / " + AllPdfLength;
                CSPrompt.instance.showMessage(loadTip, false);
                File.Delete(pdfImages[i].FullName);
                yield return null;
            }
        }

        //重新獲取新的圖片
        pdfImages = PDFData.GetFiles("*.*");

        //load PDFData to project--------------------
        if (pdfImages.Length > 0)
        {
            for (int i = 0; i < pdfImages.Length; i++)
            {
                GameObject file = f_AddFile();
                CSResourceFile resourceFile = file.GetComponent<CSResourceFile>();

                resourceFile.SetTargetFolder(PDFfolder.transform);
                resourceFile.f_MoveFileToFolder();

                resourceFile.SetLoadFile(pdfImages[i].FullName);
                resourceFile.SetIsActual(true);
            }
        }
        CSPrompt.instance.hideMessage();
    }
