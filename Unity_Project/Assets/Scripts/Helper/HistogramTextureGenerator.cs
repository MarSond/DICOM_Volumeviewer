using UnityEngine;

public class HistogramTextureGenerator
{
    public static Texture2D GenerateHistogramTexture(VolumeTextureLoader dataset)
    {
        int numSamples = (int)dataset.getHighestHound() + (int)Mathf.Abs(dataset.getLowestHound()) + 1;
        int[] values = new int[numSamples];
        Color[] cols = new Color[numSamples];
        Texture2D texture = new Texture2D(numSamples, 1, TextureFormat.RGBAFloat, false);
        int[] data = dataset.getArr();
        int maxFreq = 0;
        for (int iData = 0; iData < data.Length; iData++)
        {
            int value = data[iData] + (int)Mathf.Abs(dataset.getLowestHound());
            values[value] += 1;
            maxFreq = System.Math.Max(values[value], maxFreq);
        }

        for (int iSample = 0; iSample < numSamples; iSample++)
            cols[iSample] = new Color(Mathf.Log10((float)values[iSample]) / Mathf.Log10((float)maxFreq), 0.0f, 0.0f, 1.0f);

        texture.SetPixels(cols);
        texture.Apply();

        return texture;
    }
}
