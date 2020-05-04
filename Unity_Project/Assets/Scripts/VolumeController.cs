using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
using System;
using System.IO;

public class VolumeController : MonoBehaviour {
    [SerializeField] protected Material material;
    [SerializeField] public VolumeTextureLoader textLoader;
    public Texture3D currTexture;
    public Texture2D noiseTexture ;
    [Range(0f, 1f)] public float sliceXMin = 0.0f, sliceXMax = 1.0f;
    [Range(0f, 1f)] public float sliceYMin = 0.0f, sliceYMax = 1.0f;
    [Range(0f, 1f)] public float sliceZMin = 0.0f, sliceZMax = 1.0f;
    private Renderer render;
    [SerializeField] public Light sceneLight;
    private bool firstLoad = false;
    private RenderMode renderMode;
    private TransferFunction transferFunction;
    private String tfPath;
    private bool oldLightSetting = true;

    void Start() {
        tfPath = Application.dataPath + "/BildExport/TransferFunction.png";
        render = GetComponent<Renderer>();
        TransferFunction tf = new TransferFunction();
        tf.reset();
        tf.GenerateTexture();
        Texture2D tfTexture = tf.GetTexture();
        this.transferFunction = tf;
        if (File.Exists(tfPath)) {
            Texture2D tex = new Texture2D(512, 4);
            tex.LoadImage(File.ReadAllBytes(tfPath));
            render.material.SetTexture("_TFTex", tex);
            Debug.Log("Transferfunktion von " + tfPath + " geladen");
        } else {
            Debug.LogError("Keine TF bei " + tfPath + " gefudnen");
        }
        const int noiseDimX = 512;
        const int noiseDimY = 512;
        noiseTexture = NoiseTextureGenerator.GenerateNoiseTexture(noiseDimX, noiseDimY);
    }

    public TransferFunction getTransferFunction() {
        return this.transferFunction;
    }

    public void reloadTexture() {
        //int[,,] volume = textLoader.getImageMatrice();
        currTexture = textLoader.getTexture();
        if (currTexture == null) {
            Debug.LogError("Texture not ready");
            return;
        }
        firstLoad = true;
        float scaleZ = ((currTexture.depth+0.0f) / currTexture.height) ;
        transform.localScale=new Vector3(1f,1f,scaleZ);
        render.material.SetTexture("_Volume", currTexture);
        render.material.SetFloat("_HoundLow", textLoader.getLowestHound());
        render.material.SetFloat("_HoundMax", textLoader.getHighestHound());
        Debug.Log("HU Scale --  Low: " + textLoader.getLowestHound() + " Upper: " + textLoader.getHighestHound());
        Debug.Log("Scale: " + scaleZ + " dep: " + currTexture.depth + " heigth: " + currTexture.height);
        render.material.SetTexture("_Volume", currTexture);
        render.material.SetTexture("_NoiseTex",noiseTexture);
    }

    void Update() {
        if (textLoader.isFileLoaded() && firstLoad==false) {
            reloadTexture();
        }
        render.material.SetVector("_SliceMin", new Vector3(sliceXMin, sliceYMin, sliceZMin));
        render.material.SetVector("_SliceMax", new Vector3(sliceXMax, sliceYMax, sliceZMax));
        render.material.SetVector("_LightDir", sceneLight.transform.eulerAngles);
    }

    public void SetRenderMode(RenderMode mode){
        renderMode = mode;
        switch (mode){
        case RenderMode.DirectVolumeRendering: {
                GetComponent<MeshRenderer>().sharedMaterial.EnableKeyword("MODE_DVR");
                GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("MODE_MIP");
                GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("MODE_SURF");
                break;
            }
        case RenderMode.MaximumIntensityProjectipon:{
                GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("MODE_DVR");
                GetComponent<MeshRenderer>().sharedMaterial.EnableKeyword("MODE_MIP");
                GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("MODE_SURF");
                break;
            }
        case RenderMode.IsosurfaceRendering:{
                GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("MODE_DVR");
                GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("MODE_MIP");
                GetComponent<MeshRenderer>().sharedMaterial.EnableKeyword("MODE_SURF");
                break;
            }
        }
    }

    public void SetLightning(bool light)
    {
        if (light == oldLightSetting) {
            return;
        }
        if (light) {
            GetComponent<MeshRenderer>().sharedMaterial.EnableKeyword("LIGHT_ON");
            GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("LIGHT_OFF");
        } else {
            GetComponent<MeshRenderer>().sharedMaterial.DisableKeyword("LIGHT_ON");
            GetComponent<MeshRenderer>().sharedMaterial.EnableKeyword("LIGHT_OFF");
        }
        oldLightSetting = light;
        Debug.Log("Lighning " + light);
    }

    public RenderMode GetRenderMode() {
        return renderMode;
    }

    public void updateTF(Texture2D tfTexture) {
        render.material.SetTexture("_TFTex", tfTexture);
        byte[] bytes = tfTexture.EncodeToPNG();
        File.WriteAllBytes(tfPath, bytes);
    }

    /* 
     * Copyright (c) 2019 Matias Lavik MIT License
     * https://github.com/mlavik1/UnityVolumeRendering
     */
    public SlicingPlane CreateSlicingPlane() {
        GameObject sliceRenderingPlane = GameObject.Instantiate(Resources.Load<GameObject>("SlicingPlane"));
        sliceRenderingPlane.transform.parent = transform;
        sliceRenderingPlane.transform.localPosition = Vector3.zero;
        sliceRenderingPlane.transform.localRotation = Quaternion.identity;
        MeshRenderer sliceMeshRend = sliceRenderingPlane.GetComponent<MeshRenderer>();
        sliceMeshRend.material = new Material(sliceMeshRend.sharedMaterial);
        Material sliceMat = sliceRenderingPlane.GetComponent<MeshRenderer>().sharedMaterial;
        sliceMat.SetTexture("_DataTex", currTexture);
        sliceMat.SetTexture("_TFTex", transferFunction.GetTexture());
        sliceMat.SetMatrix("_parentInverseMat", transform.worldToLocalMatrix);
        sliceMat.SetMatrix("_planeMat", Matrix4x4.TRS(sliceRenderingPlane.transform.position, sliceRenderingPlane.transform.rotation, Vector3.one)); // TODO: allow changing scale

        return sliceRenderingPlane.GetComponent<SlicingPlane>();
    }
}
