using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;

public class AdaptPlayModelMove : MonoBehaviour
{
    [SerializeField] private GameObject Guidance, FeedBack, User;
    private bool fileOpenFlag = false;
    StreamReader sr;
    [SerializeField] string FileName = "default";
    [SerializeField] int FileRowCount = 200;
    Vector3[] modelPositions;
    Vector3[] userPositions;
    //int availableNum = 5;
    //float actionPotential = 0; // これが1になると、Guidanceが1フレーム分進む。
    [SerializeField] LineRenderer myrenderer;
    int correspondTime = 1;  // Userの現在地に対応するModelの時間
    int guidanceTime = 1;   // ガイダンスの現在の時間
    //float score = 0f;       // 現フレームの評価値（ズレの小ささや、前フレームからの省略の小ささ）

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
            correspondTime = 1;                 // 0行目には文字が入っており、データは1行目から始まるため。
        }
        if(correspondTime == FileRowCount)
        {
            correspondTime = 1;
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
                        modelPositions[i-1] = position;
                    }
                    i++;
                }

                if(i-1 != FileRowCount)
                {
                    Debug.Log("FileRowCountが不適切です。\n" + (i - 1) + "に設定してください。" + FileName);
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
        //float minimum = 1f;    // マウスのx座標と配列のx座標（候補は現在の点から10個）の差の最小値
        //int progress = 5;    // ガイダンスをどれだけ先行させるか
        //float amplitude = 3f;
        //int regression = 2;   // FBをどれだけ後にずらすか
        float diff_y = 1;       // マウスとモデルのy座標のズレ
        int nearest = 0;        // 今回の呼び出しでインデックスがどれだけ進むか
        //float score = 0;        // ズレの小ささや省略の無さが評価され、これがたまるとモデルが進む
        
        User.transform.position = screen_mousePos;

        // 現在のマウスのx座標から最も近いモデルのx座標を探索
        /*
        for(int i = 0; i < guidanceTime - correspondTime; i++)
        {
            if(minimum >= Mathf.Abs(screen_mousePos.x - modelPositions[correspondTime + i].x))
            {
                minimum = Mathf.Abs(screen_mousePos.x - modelPositions[correspondTime + i].x);
                nearest =  i;
            }
        }
        */

        // 現時点（correspondTime）とガイダンス時点（guidanceTime）の間で、最もUserに近い点を二分探索。
        if(guidanceTime - correspondTime > 0)
        {
            int head = 0;
            int tail = guidanceTime - correspondTime;
            for(nearest = (guidanceTime + correspondTime) / 2; tail - head > 1; )
            {
                if(modelPositions[correspondTime + nearest].x - screen_mousePos.x > 0)
                {
                    tail = nearest;
                }
                else
                {
                    head = nearest;
                }
                nearest = (head + tail) / 2;
            }

            correspondTime += nearest;
            if(nearest != 0)
            {
                Debug.Log("nearest = " + nearest);
            }
            // y座標のズレに応じて、モデルの進み具合を変化
            diff_y = Mathf.Abs(screen_mousePos.y - modelPositions[correspondTime].y);

            if(nearest != 0)
            {
                guidanceTime += nearest + (int)((10f - (10f * diff_y )) / 5f);  // diff_yの値が1より大きければガイダンスが遅延。
            }
        }
        else
        {
            guidanceTime = correspondTime + 10;
        }
        
        if(guidanceTime < FileRowCount)
        {
            Guidance.transform.position = modelPositions[guidanceTime];
        }
        else 
        {
            correspondTime = 0;
            guidanceTime = 0;
        }

        /*
        // FeedBackを提供
        if(ModelTime > regression)
        {
            FeedBack.transform.position = positions[ModelTime - regression];
            User.transform.position = userPositions[ModelTime - regression];
            myrenderer.SetPosition(0, positions[ModelTime - regression]);
            if(userPositions[ModelTime- regression] != new Vector3(0, 0, 0))
            {
                myrenderer.SetPosition(1, userPositions[ModelTime - regression]);
            }

        }
        */
    }
}
