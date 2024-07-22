using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;

public class Adapt2Play : MonoBehaviour
{
    [SerializeField] private GameObject Guidance, User;
    private bool fileOpenFlag = false;
    StreamReader sr;
    [SerializeField] string fileName = "default";
    [SerializeField] int fileRowCount = 200;
    Vector3[] modelPositions;
    Vector3[] userPositions;
    int availableNum = 5, notAvailableNum = 0;
    int correspondTime = 0;  // Userの現在地に対応するModelの時間
    int guidanceTime = 0;   // ガイダンスの現在の時間
    float score = 0f;
    int updateCount = 0;
    // Use this for initialization
    void Start()
    {
        modelPositions = new Vector3[fileRowCount];
        userPositions = new Vector3[fileRowCount];
        OpenData();
    }

    private void FixedUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            if(correspondTime == 0 && guidanceTime == 0)
            {
                correspondTime = 1;
                guidanceTime = 1;
            }

            updateCount++;
            score += Evaluation();
            if(availableNum > 0)
            {
                Moving();
            }
            if(updateCount == 5)
            {
                availableNum = (int)score;
                notAvailableNum = 0;
                score = 0f;
                updateCount = 0;
                Debug.Log("availableNum:" + availableNum);
            }
            Debug.Log("correspondTime:" + correspondTime);
            Debug.Log("guidanceTime:" + guidanceTime);

        }
        else
        {
            correspondTime = 0;                 // 0行目には文字が入っており、データは1行目から始まる。
            guidanceTime = 0;
        }
        if(correspondTime == fileRowCount)
        {
            correspondTime = 0;
            guidanceTime = 0;
        }
        
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
                        modelPositions[i] = position;
                    }
                    i++;
                }

                if(i+1 != fileRowCount)
                {
                    Debug.Log("FileRowCountが不適切です。\n" + (i + 1) + "に設定してください。" + fileName);
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

    float Evaluation()
    {
        float diff_y = 1f;       // マウスとモデルのy座標のズレ
        int nearest = 0;        // 今回の呼び出しで対応点のインデックスがどれだけ進むか

        Vector3 mousePos = Input.mousePosition;
        Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        screen_mousePos = new Vector3(screen_mousePos.x, screen_mousePos.y, 10f);
        User.transform.position = screen_mousePos;

        // 現時点（correspondTime）とガイダンス時点（guidanceTime）の間で、最もUserに近い点を二分探索。
        if(correspondTime <= guidanceTime)
        {
            int head = correspondTime;
            int tail = guidanceTime;
            int center = (tail + head) / 2;
            float correntScore = 0f;

            
            for(int count = 0; tail - head > 0 && count < 10; count++)
            {
                if(modelPositions[center].x - screen_mousePos.x > 0)
                {
                    tail = center;
                }
                else
                {
                    head = center + 1;
                }
                center = head + (tail - head) / 2;
                if(count == 9)
                {
                    Debug.Log("count =" + count);
                }
            }
            nearest = center - correspondTime;
            correspondTime = center;
            if(correspondTime == guidanceTime)
            {
                nearest = -1;
            }
            // y座標のズレに応じて、モデルの進み具合を変化
            diff_y = Mathf.Abs(screen_mousePos.y - modelPositions[correspondTime].y);
            
            //correntScore = (2f - diff_y);
            correntScore = -2f * diff_y + 4f;
            //if(correntScore > 0f && nearest != 0)
            if(nearest != 0)
            {
                return correntScore;
            }
            else
            {
                return 0f;
            }
        }
        else
        {
            Debug.Log("correspondTime > guidanceTime");
            
            
            return 0f;
        }
        
    }
    void Moving()
    {
        guidanceTime += (int)(availableNum * updateCount / 5) - notAvailableNum;  // 今回の呼び出しで表示されるガイダンスのインデックス
        notAvailableNum = availableNum * updateCount / 5;
        if(guidanceTime < fileRowCount)
        {
            Guidance.transform.position = modelPositions[guidanceTime];
        }
        else
        {
            Debug.Log("Moving():guidanceTime >= FileRowCount");
            guidanceTime = 0;
        }

    }
}
