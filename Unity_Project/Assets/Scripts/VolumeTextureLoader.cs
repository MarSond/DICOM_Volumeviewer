using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class VolumeTextureLoader : MonoBehaviour
{
    public TextAsset dataFile;
    private char separator = '|';
    private char block_separator = '#';
    private String texAssetPath="Assets/BildAssets";
    private int expectedImageCount;
    private int rows;
    private int colls;
    private int sliceThick;
    private int[,] currImage;
    private List<int[,]> images;
    private int imagesLoaded = 0;
    private int lowestInt = 0;
    private int highestInt = 0;
    private Texture3D _texture;
    private bool _fileLoaded = false;
    private int[] arr;
    public int getLoadedCount() {
        return imagesLoaded;
    }

    private Texture3D loadTexture() {
        string[] assId = AssetDatabase.FindAssets(dataFile.name+"", new[] { texAssetPath });
        arr = new int[rows * colls * imagesLoaded];
        Debug.Log(assId.Length + " Assets gerfunden.");
        if (assId.Length == 1) {
            String assetPath = AssetDatabase.GUIDToAssetPath(assId[0]);
            Debug.Log(assetPath);
            Texture3D textureFromAsset=(Texture3D) AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture3D));
            _texture = textureFromAsset;
            //return textureFromAsset;
        }
        int loaded = getLoadedCount();
        Debug.Log(loaded + " Bilder geladen");
        Texture3D tex = new Texture3D(colls, rows, loaded, TextureFormat.R16, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 0;
        Color[] colorArr = new Color[colls * rows * loaded];
        int colorCounter = 0;
        for (int currImage = 0; currImage < loaded; currImage++)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int collum = 0; collum < colls; collum++)
                {
                    int intens = (int)images[currImage][collum, row];
                    float alpha = 1.0f;
                    float returnInt = map(intens, lowestInt, highestInt, 0.0f, 1.0f);
                    colorArr[colorCounter] = new Color(returnInt, returnInt, returnInt, alpha);
                    arr[colorCounter] = intens;
                    colorCounter++;
                }
            }
        }
        tex.SetPixels(colorArr, 0);
        tex.Apply();
        _texture = tex;
        saveAsset(); // Als .asset speichern
        return tex;
    }

    public int[] get1DArray()
    {

        int[] arr = new int[rows * colls * imagesLoaded];
        for (int im = 0; im < imagesLoaded; im++)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int collum = 0; collum < colls; collum++)
                {
                    //arr[im + row + collum] = (int)_texture.GetPixel(row, collum, im).r;
                }
            }
        }
        return arr;
    }

    private void saveAsset() {
        AssetDatabase.CreateAsset(_texture, texAssetPath +"/"+ dataFile.name + ".asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    internal bool isFileLoaded() {
        return _fileLoaded;
    }

    IEnumerator LoadFileWork() {
        string[] linesInFile = dataFile.text.Split('\n');
        string firstLine = linesInFile[0];
        Debug.Assert(firstLine.StartsWith("#"), "Not valid Data File");
        string[] headerArr = firstLine.Split(separator);
        expectedImageCount = int.Parse(headerArr[0].Substring(1));
        rows = int.Parse(headerArr[1]);
        colls = int.Parse(headerArr[2]);
        sliceThick= int.Parse(headerArr[3]);
        Debug.Log(expectedImageCount + " " + rows + " x " + colls + " Images which thickness " + sliceThick +" Expected");
        string lineTwo = linesInFile[1];
        images = new List<int[,]>();
        int counter = -1;

        int[,] tempImage = new int[rows, colls];
        int tempLine = 0;
        foreach (string line in linesInFile)
        {

            if (line == "" || counter == -1) {
                counter++;
                continue;
            }

            if (line.StartsWith(block_separator.ToString()))
            {
                if (imagesLoaded > 0) {
                    images.Add(tempImage);
                }
                tempImage = new int[rows, colls];
                tempLine = 0;
                imagesLoaded++;
                counter++;
                if (imagesLoaded % 80 == 0) {
                    yield return null; // Einen Frame freigeben
                }
                continue;
            }
            string[] line_split = line.Split(separator);
            int colCount = 0;
            foreach (string p in line_split) {
                int pi = System.Convert.ToInt32(p);
                lowestInt = Math.Min(pi, lowestInt);
                highestInt = Math.Max(pi, highestInt);
                tempImage[tempLine, colCount] = pi;
                colCount++;
            }
            tempLine++;
            counter++;
        }
        images.Add(tempImage);
        _fileLoaded = true;
        Debug.Log(imagesLoaded+" files Loaded");
    }

    void Start() {
        StartCoroutine(LoadFileWork());
    }

    internal float getLowestHound() {
        return this.lowestInt;
    }

    internal float getHighestHound() {
        return this.highestInt;
    }

    public Texture3D getTexture()
    {
        if (!_fileLoaded) {
            return null;
        }
        //StartCoroutine(GetTextureWork());
        return loadTexture();
    }

    void Update() {

    }

    internal int[] getArr() {
        return arr;
    }
    private float map(float value, float fromLow, float fromHigh, float toLow, float toHigh) {
        return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
    }
}