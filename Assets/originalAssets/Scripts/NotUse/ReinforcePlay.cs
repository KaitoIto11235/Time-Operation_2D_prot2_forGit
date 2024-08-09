using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;
public class ReinforcePlay : MonoBehaviour
{
    [SerializeField] private GameObject Guidance, FeedBack, User, NowModekLine;
    private bool fileOpenFlag = false;
    StreamReader sr;
    [SerializeField] string FileName = "default";
    [SerializeField] int FileRowCount = 200;
    Vector3[] modelPositions;
    Vector3[] userPositions;
    int ModelTime = 1;


    Vector3 pre_screen_mousePos;
    Vector3 pre_modelPositions;
    // Use this for initialization
    void Start()
    {
        modelPositions = new Vector3[FileRowCount];
        userPositions = new Vector3[FileRowCount];
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
                    if (values.Length >= 4 && i >= 1)
                    {
                        float x = float.Parse(values[1]);
                        float y = float.Parse(values[2]);
                        float z = float.Parse(values[3]);
                        Vector3 position = new Vector3(x, y, z);
                        // ここでVector3を使用するか、配列に保存する
                        modelPositions[i] = position;
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
        //Debug.Log("Moving()");
        Vector3 mousePos = Input.mousePosition;
        Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        screen_mousePos = new Vector3(screen_mousePos.x, screen_mousePos.y, 10f);
        float maxcos = 0; // 学習者ベクトルとnフレーム後見本ベクトルのcos
        int progress = 10;    // ガイダンスをどれだけ先行させるか
        int nearest = 0;      // 今回の呼び出しでインデックスがどれだけ進むか

        
        for(int i = 0; i < 5; i++) // 学習者ベクトルと0-4フレーム後見本ベクトルを比較して、最も近い（角度が小さい）フレームを選ぶ
        {
            Vector3 userVec = screen_mousePos - pre_screen_mousePos;
            Vector3 modelVec = modelPositions[ModelTime + i] - pre_modelPositions;
            if(maxcos < Vector3.Dot(modelVec, userVec) / modelVec.magnitude * userVec.magnitude &&
             0.02 < Mathf.Abs(Vector3.Dot(modelVec, userVec) / modelVec.magnitude * userVec.magnitude))
            {
                maxcos = Vector3.Dot(modelVec, userVec) / modelVec.magnitude * userVec.magnitude;
                nearest =  i;
            }
        }
        ModelTime += nearest;
        userPositions[ModelTime] = screen_mousePos;

        pre_screen_mousePos = screen_mousePos;
        pre_modelPositions = modelPositions[ModelTime];

        // Guidanceと現在どこを評価しているかを表す縦のラインを示す
        if(ModelTime + progress <= FileRowCount)
        {
            Guidance.transform.position = modelPositions[ModelTime + progress];
            NowModekLine.transform.position = modelPositions[ModelTime];
        }
        else 
        {
            ModelTime = 0;
        }

    }
}
