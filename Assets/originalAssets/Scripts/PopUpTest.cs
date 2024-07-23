using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;
public class PopUpTest : MonoBehaviour
{
    private bool fileOpenFlag = false;
    StreamReader sr;
    [SerializeField] string fileName = "default";
    [SerializeField] int fileRowCount = 200;
    Vector3[] positions;

    // Start is called before the first frame update
    void Start()
    {
        positions = new Vector3[fileRowCount];
        OpenData();
        
        LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);

        Invoke(nameof(DeleteLine), 1f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void DeleteLine()
    {
        LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
    }

    public void OpenData()
    {

        if (!fileOpenFlag)
        {
            string file;
            {
                //file = Application.persistentDataPath + FileName + ".csv";

                file = Application.dataPath + @"/originalAssets/File/" + fileName + ".csv";
            }

            if(File.Exists(file))
            {
                sr = new StreamReader(new FileStream(file, FileMode.Open), Encoding.UTF8);
                int i = 0;
                while(sr.EndOfStream == false)
                {
                    string line = sr.ReadLine();
                    string[] values = line.Split(',');
                    if (values.Length >= 5 && i >= 1)
                    {
                        float x = float.Parse(values[2]);
                        float y = float.Parse(values[3]);
                        float z = float.Parse(values[4]);
                        Vector3 position = new Vector3(x, y, z);
                        // ここでVector3を使用するか、配列に保存する
                        positions[i-1] = position;
                    }
                    i++;
                }

                if(i-1 != fileRowCount)
                {
                    Debug.Log("FileRowCountが不適切です。\n" + (i - 1) + "に設定してください。" + fileName);
                }

                fileOpenFlag = true;
                Debug.Log(file);

                CloseData();
            }

            if (!File.Exists(file))
            {
                Debug.Log("ファイルが見つかりません。");
            }
        }
        else
        {
            Debug.Log("File has already opened.");
        }

    }
    
    void CloseData()
    {
        sr.Dispose();
        Debug.Log("Close_csv");
        fileOpenFlag = false;
    }
}
