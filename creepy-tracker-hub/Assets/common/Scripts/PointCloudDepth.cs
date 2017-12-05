
using UnityEngine;
using System.Collections.Generic;

public class PointCloudDepth : MonoBehaviour
{
    uint _id;
    Texture2D _colorTex;
    Texture2D _depthTex;
    List<GameObject> _objs;
    Material _mat;
    RVLDecoder _decoder;
    byte[] _depthBytes;

    void Start()
    {
        _colorTex = new Texture2D(512, 424, TextureFormat.BGRA32, false);
        _depthTex = new Texture2D(512, 424, TextureFormat.BGRA32, false);
        _colorTex.filterMode = FilterMode.Point;
        _depthTex.filterMode = FilterMode.Point;

        _mat = Resources.Load("Materials/cloudmatDepth") as Material;
        _depthBytes = new byte[868352];
        _decoder = new RVLDecoder();
        _objs = new List<GameObject>();
        List<Vector3> points = new List<Vector3>();
        List<int> ind = new List<int>();
        int n = 0;
        int i = 0;
       
        for (int w = 0; w < 512; w++)
        {
            for (int h = 0; h < 424; h++)
            {
                Vector3 p = new Vector3(w / 512.0f, h / 424.0f, 0);
                points.Add(p);
                ind.Add(n);
                n++;
              
                if (n == 65000)
                {
                    GameObject a = new GameObject("cloud" + i);
                    MeshFilter mf = a.AddComponent<MeshFilter>();
                    MeshRenderer mr = a.AddComponent<MeshRenderer>();
                    mr.material = _mat;
                    mf.mesh = new Mesh();
                    mf.mesh.vertices = points.ToArray();
                    mf.mesh.SetIndices(ind.ToArray(), MeshTopology.Points, 0);
                    mf.mesh.bounds = new Bounds(new Vector3(0, 0, 4.5f), new Vector3(5, 5, 5));
                    a.transform.parent = this.gameObject.transform;
                    a.transform.localPosition = Vector3.zero;
                    a.transform.localRotation = Quaternion.identity;
                    a.transform.localScale = new Vector3(1, 1, 1);
                    n = 0;
                    i++;
                    _objs.Add(a);
                    points = new List<Vector3>();
                    ind = new List<int>();
                }
            }
        }
        GameObject afinal = new GameObject("cloud" + i);
        MeshFilter mfinal = afinal.AddComponent<MeshFilter>();
        MeshRenderer mrfinal = afinal.AddComponent<MeshRenderer>();
        mrfinal.material = _mat;
        mfinal.mesh = new Mesh();
        mfinal.mesh.vertices = points.ToArray();
        mfinal.mesh.SetIndices(ind.ToArray(), MeshTopology.Points, 0);
        afinal.transform.parent = this.gameObject.transform;
        afinal.transform.localPosition = Vector3.zero;
        afinal.transform.localRotation = Quaternion.identity;
        afinal.transform.localScale = new Vector3(1, 1, 1);
        n = 0;
        i++;
        _objs.Add(afinal);
        points = new List<Vector3>();
        ind = new List<int>();
    }

    public void hide()
    {
        foreach(GameObject a in _objs)
            a.SetActive(false);
    }

    public void show()
    {
        foreach (GameObject a in _objs)
            a.SetActive(true);
    }

    public void setPoints(byte[] colorBytes, byte[] depthBytes,bool compressed)
    {
        if (compressed) { 
            _decoder.DecompressRVL(depthBytes, _depthBytes, 512 * 424);
            _depthTex.LoadRawTextureData(_depthBytes);
        }else
        {
            _depthTex.LoadRawTextureData(depthBytes);
        }

        _colorTex.LoadRawTextureData(colorBytes);
        _colorTex.Apply();
        _depthTex.Apply();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();       
        for (int i = 0;i < 4; i++)
        {
            MeshRenderer mr = renderers[i];
            mr.material.SetTexture("_ColorTex", _colorTex);
            mr.material.SetTexture("_DepthTex", _depthTex); 

        }
        
       

    }

}
