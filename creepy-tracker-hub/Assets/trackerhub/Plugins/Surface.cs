using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Surface
{
    public string name;
    public string sensorid;
    public Vector3 BottomLeft;
    public Vector3 BottomRight;
    public Vector3 TopLeft;
    public Vector3 TopRight;
    public Vector3 cBottomLeft;
    public Vector3 cBottomRight;
    public Vector3 cTopLeft;
    public Vector3 cTopRight;
    public GameObject surfaceGO;

    public string filename;

    public Surface()
    {
        filename = "";
        name = "";
        sensorid = "";
        BottomLeft = Vector3.zero;
        BottomRight = Vector3.zero;
        TopLeft = Vector3.zero;
        TopRight = Vector3.zero;
        cBottomLeft = Vector3.zero;
        cBottomRight = Vector3.zero;
        cTopLeft = Vector3.zero;
        cTopRight = Vector3.zero;
        surfaceGO = null;
    }

    public static Surface[] loadSurfaces(string folder)
    {
        List<Surface> surfaces = new List<Surface>();

        folder = Application.dataPath + "/" + folder + "/";
        if (Directory.Exists(folder))
        {
            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                if (file.EndsWith(".txt"))
                {
                    string[] lines = File.ReadAllLines(file);
                    Surface s = new Surface();
                    s.filename = file;
                    foreach (string line in lines)
                    {
                        if (line.Length != 0 || line[0] != '%')
                        {
                            string[] st = line.Split('=');

                            if (st[0] == "surface.name") s.name = st[1];
                            else if (st[0] == "surface.kinect") s.sensorid = st[1];
                            else if (st[0] == "surface.bottom.left") s.BottomLeft = _parseVector3(st[1].Replace(',', '.'));
                            else if (st[0] == "surface.bottom.right") s.BottomRight = _parseVector3(st[1].Replace(',', '.'));
                            else if (st[0] == "surface.top.left") s.TopLeft = _parseVector3(st[1].Replace(',', '.'));
                            else if (st[0] == "surface.top.right") s.TopRight = _parseVector3(st[1].Replace(',', '.'));
                        }
                    }
                    if (s.name.Length != 0) surfaces.Add(s);
                }
            }
        }
        return surfaces.ToArray();
    }

    private static Vector3 _parseVector3(string v)
    {
        Vector3 r = Vector3.zero;
        string[] values = v.Split(':');
        if (values.Length == 3)
        {
            r.x = float.Parse(values[0]);
            r.y = float.Parse(values[1]);
            r.z = float.Parse(values[2]);
        }
        return r;
    }

    internal void saveSurface(GameObject bl, GameObject br, GameObject tl, GameObject tr)
    {
        cBottomLeft = bl.transform.position;
        cBottomRight = br.transform.position;
        cTopLeft = tl.transform.position;
        cTopRight = tr.transform.position;

        if (File.Exists(filename))
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename, false))
            {
                file.WriteLine("% " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                file.WriteLine("surface.name=" + name);
                file.WriteLine("surface.kinect=" + sensorid);
                file.WriteLine("surface.bottom.left=" + _pointToString(BottomLeft));
                file.WriteLine("surface.bottom.right=" + _pointToString(BottomRight));
                file.WriteLine("surface.top.left=" + _pointToString(TopLeft));
                file.WriteLine("surface.top.right=" + _pointToString(TopRight));
                file.WriteLine("tracker.surface.bottom.left=" + _pointToString(bl.transform.position));
                file.WriteLine("tracker.surface.bottom.right=" + _pointToString(br.transform.position));
                file.WriteLine("tracker.surface.top.left=" + _pointToString(tl.transform.position));
                file.WriteLine("tracker.surface.top.right=" + _pointToString(tr.transform.position));
            }
        }
    }

    private string _pointToString(Vector3 p)
    {
        return ("" + p.x + ":" + p.y + ":" + p.z).Replace(',', '.');
    }
}
