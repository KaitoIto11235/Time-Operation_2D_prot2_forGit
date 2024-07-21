using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;

public class AutePlayModelMove : MonoBehaviour
{
    [SerializeField] private GameObject myCircle, User;
    private bool fileOpenFlag = false;
    StreamReader sr;
    [SerializeField] string FileName = "default";
    [SerializeField] int FileRowCount = 200;
    Vector3[] positions;

    int ModelTime = 1;
    // Use this for initialization
    void Start()
    {
        positions = new Vector3[FileRowCount];
        OpenData();
    }

    private void FixedUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            Moving();
        }
        else
        {
            ModelTime = 1;
        }
        if(ModelTime == FileRowCount)
        {
            //myCircle.SetActive(false);
            //myCircle.GetComponent<Renderer>().enabled = false;
            ModelTime = 1;
        }
        
    }


    public void OpenData()
    {

        if (!fileOpenFlag)
        {
            string file;
            {
                //file = Application.persistentDataPath + FileName + ".csv";

                file = Application.dataPath + @"/originalAssets/File/" + FileName + ".csv";
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
                        positions[i] = position;
                    }
                    i++;
                }

                if(i+1 != FileRowCount)
                {
                    Debug.Log("FileRowCountが不適切です。\n" + (i + 1) + "に設定してください。" + FileName);
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

    void Moving()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        screen_mousePos = new Vector3(screen_mousePos.x, screen_mousePos.y, 10f);
        User.transform.position = screen_mousePos;

        this.transform.position = positions[ModelTime];
        ModelTime++;
    }
}
