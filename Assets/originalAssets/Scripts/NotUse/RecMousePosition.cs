using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text; //文字コードを指定するために必要

public class RecMousePosition : MonoBehaviour
{
    private float time = 0f;
    private bool fileOpenFlag = false;
    StreamWriter sw = null;
    [SerializeField] string FileName = "default";
    [SerializeField] private GameObject myCircle, startLine, endLine;
    private float StartLine = -8f;
    private float EndLine = 8f;
	private int trialCount = 1;
    private float preMousePos = -10f; // マウスの前回のx座標
    private bool startFlag = false; // true: 準備OK


    private void Start()
	{
		OpenData();

        startLine.GetComponent<SpriteRenderer>().color = Color.yellow;
	}

    private void FixedUpdate()
    {
        time += Time.deltaTime;
        float MousePosX = Camera.main.ScreenToWorldPoint(Input.mousePosition).x; // マウスの現在のx座標

        if(Input.GetMouseButton(0) && MousePosX <= StartLine) // マウスがスタートエリアに入ったとき、記録準備OKにする。
        {
            time = 0f;
            startFlag = true;

            myCircle.transform.position = new Vector3(-10f, 0f, 10f);
            myCircle.GetComponent<SpriteRenderer>().color = Color.yellow;
        }
        else if (Input.GetMouseButton(0) && MousePosX - preMousePos > 0 && startFlag &&
        MousePosX >= StartLine && MousePosX <= EndLine) // Playエリアでマウスが右に動いているとき、データを保存する。
        {
            SaveData();

            startLine.GetComponent<SpriteRenderer>().color = Color.white;
            endLine.GetComponent<SpriteRenderer>().color = Color.yellow;
        }
        else if(Input.GetMouseButton(0) && MousePosX >= EndLine && startFlag) // マウスがエンドエリアに入ったとき、空行を入れる。「startFlag」条件のため、一度しか呼び出されない。
        {
            EndData();
            startFlag = false; // もう一度スタートエリアに入らない限り、データが保存されないようにするためのもの。

            startLine.GetComponent<SpriteRenderer>().color = Color.yellow;
            endLine.GetComponent<SpriteRenderer>().color = Color.white;
            myCircle.GetComponent<SpriteRenderer>().color = Color.white;
        }
        else if (Input.GetKeyDown(KeyCode.Return)) // エンターキーが押されたら、ファイルを閉じる。
        {
            CloseData();
        }

        preMousePos = MousePosX;
    }

    public void OpenData()
        {

            if (!fileOpenFlag)
            {
                string file = Application.dataPath + @"/originalAssets/File/" + FileName + ".csv";

                if (!File.Exists(file))
                {
                    sw = File.CreateText(file);
                    sw.Flush();
                    sw.Dispose();

                    //UTF-8で生成...2番目の引数はtrueで末尾に追記、falseでファイルごとに上書き
                    sw = new StreamWriter(new FileStream(file, FileMode.Open), Encoding.UTF8);

                    string[] s1 =
                    {
                    "Trial", "time",
                    "PositionX", "PositionY", "PositionZ",
                    };
                    string s2 = string.Join(",", s1);
                    sw.WriteLine(s2);
                    sw.Flush();

                    fileOpenFlag = true;
                    Debug.Log("Create_csv");
                    Debug.Log(file);
                }
                else
                {
                    Debug.Log("そのファイルは既に存在しています。ファイル名をInspectorから変更してください。"); 
                }
            }
            else
            {
                Debug.Log("ファイルは既に開かれています。");
            }

            time = 0f;
        }


    public void SaveData()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        screen_mousePos = new Vector3(screen_mousePos.x, screen_mousePos.y, 10f);
        myCircle.transform.position = screen_mousePos;

        string[] s1 =
        {
                Convert.ToString(trialCount), Convert.ToString(time),
                Convert.ToString(screen_mousePos.x), Convert.ToString(screen_mousePos.y), Convert.ToString(screen_mousePos.z),
        };
        string s2 = string.Join(",", s1);
        sw.WriteLine(s2);
        sw.Flush();
    }

    //1試行が終わった合図として、一行空白を入れる。
    public void EndData()
    {
        sw.WriteLine("");
        sw.Flush();
        Debug.Log("End_Trail:" + trialCount);
        trialCount++;
    }
    void CloseData()
    {
        if (sw != null) // swがnullでないことを確認
        {
        sw.Dispose();
        Debug.Log("Close_csv");
        fileOpenFlag = false;  
        }
        else
        {
        Debug.Log("StreamWriterが初期化されていません。");
        }
    }

}
