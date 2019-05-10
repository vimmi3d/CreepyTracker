
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
    Decompressor _colorDecoder;
    byte[] _depthBytes;
    byte[] _colorBytes;
    int _texScale;
    int _width;
    int _height;

    public float sigmaS = 3;
    public float sigmaT = 3;
    public int medianFilterSize = 2;
    public bool calculateNormals = true;

    void Start()
    {
        _width = 512;
        _height = 424;
        _texScale = 1;
        _objs = null;


        _mat = Resources.Load("Materials/cloudmatDepth") as Material;

        _decoder = new RVLDecoder();
        _colorDecoder = new Decompressor();

        initStructs();
    }


    void initStructs()
    {
        _colorTex = new Texture2D(_width, _height, TextureFormat.BGRA32, false);
        _depthTex = new Texture2D(_width, _height, TextureFormat.BGRA32, false);
        _colorTex.filterMode = FilterMode.Point;
        _depthTex.filterMode = FilterMode.Point;
        _depthBytes = new byte[_width * _height * 4];
        if (_objs != null)
        {
            foreach (GameObject g in _objs)
            {
                GameObject.Destroy(g);
            }
        }
        _objs = new List<GameObject>();

        List<Vector3> points = new List<Vector3>();
        List<int> ind = new List<int>();
        int n = 0;
        int i = 0;

        for (float w = 0; w < _width; w++)
        {
            for (float h = 0; h < _height; h++)
            {
                Vector3 p = new Vector3(w / _width, h / _height, 0);
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
        mfinal.mesh.bounds = new Bounds(new Vector3(0, 0, 4.5f), new Vector3(5, 5, 5));
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
        foreach (GameObject a in _objs)
            a.SetActive(false);
    }

    public void show()
    {
        foreach (GameObject a in _objs)
            a.SetActive(true);
    }

    public void setPoints(byte[] colorBytes, byte[] depthBytes, bool compressed, int sizec, int scale)
    {
        if (scale != _texScale)
        {
            _texScale = scale;
            _width = Mathf.CeilToInt(512.0f / scale);
            _height = Mathf.CeilToInt(424.0f / scale);
            initStructs();
        }

        if (compressed)
        {
            bool ok = _decoder.DecompressRVL(depthBytes, _depthBytes, _width * _height);
            _colorDecoder.Decompress(colorBytes, colorBytes, sizec);
            if (ok)
            {
                _depthTex.LoadRawTextureData(_depthBytes);
                _colorTex.LoadRawTextureData(colorBytes);
            }
        }
        else
        {
            _depthTex.LoadRawTextureData(depthBytes);
            _colorTex.LoadRawTextureData(colorBytes);
        }
        _colorTex.Apply();
        _depthTex.Apply();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer mr = renderers[i];
            mr.material.SetInt("_TexScale", _texScale);
            mr.material.SetTexture("_ColorTex", _colorTex);
            mr.material.SetTexture("_DepthTex", _depthTex);
            mr.material.SetFloat("_sigmaS", sigmaS);
            mr.material.SetFloat("_sigmaS", sigmaS);
            mr.material.SetInt("_SizeFilter", medianFilterSize);
            mr.material.SetInt("_calculateNormals", calculateNormals? 1:0);

        }

    }

    public void setPointsUncompressed(byte[] colorBytes, byte[] depthBytes)
    {
       
        _depthTex.LoadRawTextureData(depthBytes);
        _colorTex.LoadRawTextureData(colorBytes);

        _colorTex.Apply();
        _depthTex.Apply();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer mr = renderers[i];
            mr.material.SetInt("_TexScale", _texScale);
            mr.material.SetTexture("_ColorTex", _colorTex);
            mr.material.SetTexture("_DepthTex", _depthTex);
            mr.material.SetFloat("_sigmaS", sigmaS);
            mr.material.SetFloat("_sigmaS", sigmaS);
            mr.material.SetInt("_SizeFilter", medianFilterSize);
            mr.material.SetInt("_calculateNormals", calculateNormals ? 1 : 0);
        }

    }

}
