using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;
public class NearestVectorDis : MonoBehaviour
{
    [SerializeField] private GameObject Guidance, User, NowModelLine;
    private bool fileOpenFlag = false;
    StreamReader sr;
    [SerializeField] string FileName = "default";
    [SerializeField] int FileRowCount = 200;
    int ModelTime = 0; // userとの近さを測るときのモデル位置
    int progress = 20;    // ガイダンスのインデックス
    int threshold = 1;

    Vector3[] modelPositions;
    Vector3[] userPositions;

    float sumAccuracy = 0; //フレームごとの(3-dis)を足していき、この値が閾値thresholdを超えると、モデルが1フレーム進む。

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
            //ModelTime = 0;
        }
        if(ModelTime == FileRowCount)
        {
            ModelTime = 0;
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
        Vector3 mousePos = Input.mousePosition;
        Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        screen_mousePos = new Vector3(screen_mousePos.x, screen_mousePos.y, 10f);
        float minDis = 0.5f;
        int nearest = 0;      // 今回の呼び出しでインデックスがどれだけ進むか

        if(ModelTime + 3 < FileRowCount)
        {
            for(int i = 1; i < 3; i++) 
            {
                float dis = Distance(modelPositions[ModelTime + i], modelPositions[ModelTime + i + 1], screen_mousePos);
                if(minDis > dis)
                {
                    minDis = dis;
                    nearest = i;
                }
                
            }
        }

        ModelTime += nearest; // 一度選ばれたモデル点は、二度と選ばれない（ユーザーが停止したら更新されないように）。
        Debug.Log("ModelTime=" + ModelTime);

        /*
        if(1 - 3 * minDis > 0)
        {
            sumAccuracy += 1 - 3 * minDis;
        }
        Debug.Log("sumAccuracy=" + sumAccuracy);
        if(sumAccuracy > threshold)
        {
            progress += 1;
            sumAccuracy = 0;
        }
        */

        if(1 - minDis > 0)
        {
            progress += 1;
        }
        else 
        {
            progress += -1;
        }
        // ↓これ何の意味がある？
        userPositions[ModelTime] = screen_mousePos;

        Debug.Log("progress=" + progress);


        // Guidanceと現在どこを評価しているかを表す縦のラインを示す
        if(progress <= FileRowCount && ModelTime + progress >= 0)
        {
            Guidance.transform.position = modelPositions[ModelTime + progress];
            NowModelLine.transform.position = modelPositions[ModelTime];
        }
        else 
        {
            //ModelTime = 0;
        }

    }

    /*
    // Modelの2点m1, m2からなる線分とUserの点uの垂線の長さを返す（垂線の足が線分の外なら10を返す）
    float Distance(Vector3 m1, Vector3 m2, Vector3 u) 
    {
        Vector3 lineVec = m2 - m1;
        Vector3 pointVec = u - m1;

        Vector3 normLineVec = Vector3.Normalize(lineVec);
        float distance = (Vector3.Cross(normLineVec, pointVec)).magnitude;

        if((Vector3.Dot(pointVec, normLineVec) * normLineVec).magnitude < lineVec.magnitude)
        {
            return distance;
        }
        else
        {
            return 10f;
        }
    }
    */
    float Distance(Vector3 m1, Vector3 m2, Vector3 u) 
    {
        Vector3 lineVec = m2 - m1;
        Vector3 pointVec = u - m1;

        Vector3 normLineVec = Vector3.Normalize(lineVec);
        float distance = (Vector3.Cross(normLineVec, pointVec)).magnitude;

        return distance;
    }

}