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
    Vector3[] positions;
    Vector3[] userPositions;
    [SerializeField] LineRenderer renderer;
    int ModelTime = 1;
    // Use this for initialization
    void Start()
    {
        positions = new Vector3[FileRowCount];
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
        float minimam = 100f; // マウスのx座標と配列のx座標（候補は現在の点から10個）の差の最小値
        int progress = 10;    // ガイダンスをどれだけ先行させるか
        int regression = 2;   // FBをどれだけ後にずらすか
        int nearest = 0;      // 今回の呼び出しでインデックスがどれだけ進むか
        for(int i = 0; i < 10; i++)
        {
            if(minimam >= Mathf.Abs(screen_mousePos.x - positions[ModelTime + i].x))
            {
                minimam = Mathf.Abs(screen_mousePos.x - positions[ModelTime + i].x);
                nearest =  i;
            }
        }
        ModelTime += nearest;
        userPositions[ModelTime] = screen_mousePos;

        // Guidanceを提供
        if(ModelTime + progress <= FileRowCount)
        {
            Guidance.transform.position = positions[ModelTime + progress];
        }
        else 
        {
            ModelTime = 0;
        }


        // FeedBackを提供
        if(ModelTime > regression)
        {
            FeedBack.transform.position = positions[ModelTime - regression];
            User.transform.position = userPositions[ModelTime - regression];
            renderer.SetPosition(0, positions[ModelTime - regression]);
            if(userPositions[ModelTime- regression] != new Vector3(0, 0, 0))
            {
                renderer.SetPosition(1, userPositions[ModelTime - regression]);
            }

        }
    }
}
