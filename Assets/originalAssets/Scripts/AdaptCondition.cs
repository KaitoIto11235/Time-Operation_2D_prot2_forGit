using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;

public class AdaptCondition : MonoBehaviour
{
    [SerializeField] private GameObject guidance, user;
    [SerializeField] string readFileName = "default";
    [SerializeField] string writeFileName = "default";
    [SerializeField] GameObject StartLine, EndLine;

    [SerializeField] int readFileRowCount = 1000;
    FileOperation adaptFile;
    GuidancePlay adaptGuidance;
    [SerializeField] bool Recording = false;

    
    void Start()
    {
        if(Recording)
        {
            adaptFile = new FileOperation(readFileName, readFileRowCount, writeFileName, user, StartLine, EndLine);
            adaptFile.WriteOpenData();
        }
        else
        {
            adaptFile = new FileOperation(readFileName, readFileRowCount);
        }
        adaptGuidance = new GuidancePlay(guidance, user, readFileRowCount, adaptFile.modelPositions);
        adaptFile.ReadOpenData();

        adaptFile.FileSettingCheck();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        adaptGuidance.GuidanceUpdate();

        if(Recording)
        {
            adaptFile.RecordingUpdate();
        }
    }
}

public class FileOperation  // ファイルの読み書きを行う。
{
    private GameObject recordObject, startLine, endLine;
    private bool readFileOpenFlag = false;
    private bool writeFileOpenFlag = false;
    private string readFileName;
    private int fileRowCount;
    private string writeFileName;
    private StreamReader sr;
    private StreamWriter sw;
    public Vector3[] modelPositions;
    public Vector3[] userPositions;

    // recording用変数↓
    private float StartLinePosX = -8f;
    private float EndLinePosX = 8f;
	private int trialCount = 1;
    public int TrialCount
    { 
        get{ return trialCount; }
    }
    private bool startFlag = false; // true: 準備OK
    private float time = 0f;        // true: どこかでファイル設定が間違っていて開けないため、再生停止

    private bool fileSettingWrong = false;
    private bool testFlag = false;

    // 書き込みなしコンストラクタ
    public FileOperation(string readFileName, int fileRowCount)
    {
        this.readFileName = readFileName;
        this.fileRowCount = fileRowCount;
        modelPositions = new Vector3[fileRowCount];
        userPositions = new Vector3[fileRowCount];
    }

    // 読み込みなしコンストラクタ
    public FileOperation(string writeFileName, GameObject recordObj, GameObject startLine, GameObject endLine)
    {
        this.writeFileName = writeFileName;
        this.recordObject = recordObj;
        this.startLine = startLine;
        this.endLine = endLine;
        testFlag = true;
    }

    // 読み書きありコンストラクタ
    public FileOperation(string readFileName, int fileRowCount, string writeFileName, GameObject recordObj, GameObject startLine, GameObject endLine)
    {
        this.recordObject = recordObj;
        this.startLine = startLine;
        this.endLine = endLine;
        this.readFileName = readFileName;
        this.writeFileName = writeFileName;
        this.fileRowCount = fileRowCount;
        modelPositions = new Vector3[fileRowCount];
        userPositions = new Vector3[fileRowCount];
    }
    public void ReadOpenData()
    {
        if (!readFileOpenFlag)
        {
            string file;
            {
                //file = Application.persistentDataPath + FileName + ".csv";

                file = Application.dataPath + @"/originalAssets/File/" + readFileName + ".csv";
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

                if(i-1 != fileRowCount)
                {
                    Debug.Log("FileRowCountが不適切です。\n" + (i - 1) + "に設定してください。" + readFileName);
                    fileSettingWrong = true;
                }

                readFileOpenFlag = true;
                Debug.Log(file);

                ReadCloseData();
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

    public StreamWriter WriteOpenData()
    {
        if (!writeFileOpenFlag)
        {
            string file = Application.dataPath + @"/originalAssets/File/" + writeFileName + ".csv";

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

                writeFileOpenFlag = true;
                Debug.Log("Create_csv");
                Debug.Log(file);

                startLine.GetComponent<SpriteRenderer>().color = Color.yellow;  // 記録されていることを示すために、StartLineを黄色にする。

                return sw;
            }
            else
            {
                Debug.Log("そのファイルは既に存在しています。ファイル名をInspectorから変更してください。"); 
                fileSettingWrong = true;
                return null;
            }
        }
        else
        {
            Debug.Log("ファイルは既に開かれています。");
            return null;
        }
    }

    public void FileSettingCheck()
    {
        if(fileSettingWrong)
        {
            UnityEditor.EditorApplication.isPlaying = false;            // エディタの再生を強制終了
        }
    }
    public void RecordingUpdate()
    {
        time += Time.deltaTime;
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); // マウスの現在のx座標
        Vector3 mousePos = new Vector3(worldMousePos.x, worldMousePos.y, 10f);

        if(Input.GetMouseButton(0) && mousePos.x <= StartLinePosX) // マウスがスタートエリアに入ったとき、記録準備OKにする。
        {
            time = 0f;
            startFlag = true;

            
            recordObject.GetComponent<SpriteRenderer>().color = Color.yellow;
        }
        else if (Input.GetMouseButton(0)  && startFlag &&
        mousePos.x >= StartLinePosX && mousePos.x <= EndLinePosX) // Playエリアでマウスが右に動いているとき、データを保存する。
        {
            SaveData(mousePos);
            
            startLine.GetComponent<SpriteRenderer>().color = Color.white;
            endLine.GetComponent<SpriteRenderer>().color = Color.yellow;
        }
        else if(Input.GetMouseButton(0) && mousePos.x >= EndLinePosX && startFlag) // マウスがエンドエリアに入ったとき、空行を入れる。「startFlag」条件のため、一度しか呼び出されない。
        {
            EndData();
            startFlag = false; // もう一度スタートエリアに入らない限り、データが保存されないようにするためのもの。

            startLine.GetComponent<SpriteRenderer>().color = Color.yellow;
            endLine.GetComponent<SpriteRenderer>().color = Color.white;
            recordObject.GetComponent<SpriteRenderer>().color = Color.white;
        }
        if (Input.GetKeyDown(KeyCode.Return)) // エンターキーが押されたら、ファイルを閉じる。
        {
            WriteCloseData();
        }

    }

    public void EndData()
    {
        sw.WriteLine("");
        sw.Flush();
        Debug.Log("End_Trail:" + trialCount);
        trialCount++;
    }

    public void SaveData(Vector3 mousePos)
    {
        string[] s1 =
        {
            Convert.ToString(trialCount), Convert.ToString(time),
            Convert.ToString(mousePos.x), Convert.ToString(mousePos.y), Convert.ToString(mousePos.z),
        };
        string[] s2 =
        {
            "test" + Convert.ToString(trialCount), Convert.ToString(time),
            Convert.ToString(mousePos.x), Convert.ToString(mousePos.y), Convert.ToString(mousePos.z),
        };
        
        string s3 = string.Join(",", s1);
        string s4 = string.Join(",", s2);
        if(!testFlag)
        {
            sw.WriteLine(s3);
        }
        else
        {
            sw.WriteLine(s4);
        }
        
        sw.Flush();
    }
    private void ReadCloseData()
    {
        if (sr != null) // swがnullでないことを確認
        {
            sr.Dispose();
            Debug.Log("CloseRead_csv");
            readFileOpenFlag = false;
        }
        else
        {
            Debug.Log("StreamReaderが初期化されていません。");
        }
    }
    private void WriteCloseData()
    {
        if (sw != null) // swがnullでないことを確認
        {
            sw.Dispose();
            Debug.Log("CloseWrite_csv");
            writeFileOpenFlag = false;  
        }
        else
        {
            Debug.Log("StreamWriterが初期化されていません。");
        }
    }
}
public class GuidancePlay  // ガイダンスに関する計算・処理を行う。
{
    float trialTime = 0f;        // 1試行の時間
    private int availableNum = 5, notAvailableNum = 0;
    private int correspondTime = 0;  // Userの現在地に対応するModelの時間。 値が-1のとき、試行と試行の間であることを意味する
    private int guidanceTime = 0;   // ガイダンスの現在の時間。値が-1のとき、ユーザーが右端まで到達したことを意味する
    private float frame_5_score = 0f;       // 5フレームでのスコア
    private float trialError = 0f;   // 1試行での誤差
    private float trialScore = 0f; // 1試行でのスコア
    private int updateCount;
    private int stopCount = 0;   // 同じ対応点で停止している時間

    private int fileRowCount;
    private GameObject user, guidance;
    private Vector3[] modelPositions;

    public GuidancePlay(GameObject guidance, GameObject user, int fileRowCount, Vector3[] positions)
    {
        this.guidance = guidance;
        this.user = user;
        this.fileRowCount = fileRowCount;
        this.modelPositions = new Vector3[fileRowCount];
        this.modelPositions = positions;
    }
    

    // 現フレームのユーザーの精度を評価
    public float Evaluation()
    {
        int nearest = 0;        // 今回の呼び出しで対応点のインデックスがどれだけ進むか

        Vector3 mousePos = Input.mousePosition;
        Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        screen_mousePos = new Vector3(screen_mousePos.x, screen_mousePos.y, 10f);
        user.transform.position = screen_mousePos;

        // 現ユーザーのポジションを評価
        if(correspondTime <= guidanceTime)
        {
            float diff = 0f;
            float minDiff = 100f;
            int maxIndex = 0;

            if(guidanceTime < fileRowCount-1)
            {
                maxIndex = guidanceTime;
            }
            else
            {
                maxIndex = fileRowCount-1;
            }

            float JumpPenalty(int progress, int count) // 前フレームの対応点から2つ以上離れた点に対して、距離に対してペナルティを与える関数
            {
                if(progress == 0 || progress == 1 || count > 25)
                {
                    return 0f;
                }
                else
                {
                    return (25 - count) * 0.01f * progress;
                }
            }
            float FrameScore(float minDiff, int count) // 今回の呼び出しにおいて、最も近い点との距離minDiffをスコア化する関数
            {
                if(count > 25)
                {
                    return 0.2f;
                }
                else
                {
                    return -2f * (minDiff - 1f) - count / 12.5f;
                }
            }

            // 現時点（correspondTime）とガイダンス時点（guidanceTime）の間で、最もUserに近い点を探索。
            for(int progress = 0; correspondTime + progress <= maxIndex; progress++)
            {
                diff = Vector3.Distance(screen_mousePos, modelPositions[correspondTime + progress]) + JumpPenalty(progress, stopCount);
                if(diff < minDiff)
                {
                    minDiff = diff;     // 現フレームにおけるユーザー位置のズレの最小値を更新
                    nearest = progress;        // 最小値をとるモデル位置と現ユーザー位置のindex差を更新
                }
            }
            trialError += minDiff;
            correspondTime += nearest;

            if(correspondTime == guidanceTime)  // nearest == -1 => ユーザーがガイダンスに追いついた
            {
                nearest = -1;                   // ガイダンスに追いついてしまったとき、下のif文が常にfalseになりガイダンスが更新されなくなるため。それの対処。
            }

            if(nearest != 0 && nearest != -1)   // ユーザーが止まっておらず、ガイダンスに追いついていなければ、
            {
                float score = FrameScore(minDiff, stopCount);            // スコアを返す。
                stopCount = 0;
                return score;            // スコアを返す。
            }
            else if(nearest == -1)
            {
                stopCount = 0;
                return 1f;
            }
            else
            {
                stopCount++;
                return 0f;
            }
        }
        else
        {
            // Debug.Log("correspondTime > guidanceTime");
            return 0f;
        }
    }
    
    
    public void Moving(int updateCount)
    {
        if(trialTime <= 0.1f) // 試行開始10フレームは、前回の利用可能フレーム分だけ見本が進む
        {
            guidanceTime = (int)(availableNum * (trialTime * 10f));
        }
        else
        {
            guidanceTime += (int)(availableNum * updateCount / 5) - notAvailableNum;  // 今回の呼び出しで表示されるガイダンスのインデックス
            notAvailableNum = availableNum * updateCount / 5;
        }

        if(guidanceTime < fileRowCount - 1)
        {
            guidance.transform.position = modelPositions[guidanceTime];  // ガイダンスが右端に到達しても、guidanceTimeの値自体は更新される。ガイダンスは進まない
        }
        else
        {
            guidance.transform.position = modelPositions[fileRowCount - 2];
        }

        if(correspondTime >= fileRowCount - 2 && guidanceTime != -1)
        {
            guidance.transform.position = modelPositions[0];
            trialScore = (guidanceTime - correspondTime) / 10;     // どれだけ先行させられたか。ガイダンスが終わった時点で呼び出され、それ以降呼び出されない。
            guidanceTime = -1;                              // それ以降呼び出されないための処理。
        }
    }
    public void GuidanceUpdate()
    {
        if(Input.GetMouseButton(1))
        {
            Debug.Log("guidanceTime:" + guidanceTime);
            Debug.Log("correspondTime" + correspondTime);
            Debug.Log("stopCount" + stopCount);
        }
        if (Input.GetMouseButton(0))
        {
            trialTime += Time.deltaTime;
            if(correspondTime == -1 && guidanceTime == -1)  // 第1試行を除いたすべての試行の初期動作。
            {
                availableNum = (int)trialScore;
                trialScore = 0f;
                trialTime = 0f;
                correspondTime = 0;
                guidanceTime = 0;
                notAvailableNum = 0;
                frame_5_score = 0f;
            }
            else if(trialTime > 0.2f) //&& guidanceTime != -1)
            {
                updateCount++;
                frame_5_score += Evaluation();  // ユーザーが止まっていない かつ correspondTime < guidanceTime ⇒ 現フレームのスコアが返される。
            }

            if(availableNum > 0 && guidanceTime != -1) // guidanceTime == -1 ⇒ ユーザーが最後まで到達したことを意味する
            {
                Moving(updateCount);
            }

            //if(updateCount == 5 || (time > 0.08f && time <= 0.2f))  // 初期動作時または5フレームごとに呼び出し、利用可能インデックス数の更新や使用済み利用可能インデックス数を0に。
            if(updateCount == 5)
            {
                availableNum = (int)frame_5_score;
                notAvailableNum = 0;
                frame_5_score = 0f;
                updateCount = 0;
            }
        }
        else if(guidanceTime == -1)
        {
            correspondTime = -1;  // 1試行終了を意味する
        }
    }
}
