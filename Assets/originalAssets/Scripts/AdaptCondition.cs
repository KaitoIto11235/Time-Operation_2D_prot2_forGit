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
    [SerializeField] int testInterval = 3;

    [SerializeField] int readFileRowCount = 200;
    FileOperation adaptFile;
    GuidancePlay adaptGuidance;
    [SerializeField] bool Recording = false;

    // Start is called before the first frame update
    void Start()
    {
        if(Recording)
        {
            adaptFile = new FileOperation(readFileName, readFileRowCount, writeFileName, user, StartLine, EndLine, testInterval);
            adaptFile.WriteOpenData();
        }
        else
        {
            adaptFile = new FileOperation(readFileName, readFileRowCount, testInterval);
        }
        adaptGuidance = new GuidancePlay(guidance, user, readFileRowCount, adaptFile.modelPositions);
        adaptFile.ReadOpenData();
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(adaptFile.TrialCount % testInterval != 0)
        {
            adaptGuidance.GuidanceUpdate();
        }
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
    private int testInterval;
    private StreamReader sr;
    private StreamWriter sw;
    public Vector3[] modelPositions;
    public Vector3[] userPositions;

    // recording用変数↓
    private float StartLine = -8f;
    private float EndLine = 8f;
	private int trialCount = 1;
    public int TrialCount
    { 
        get{ return trialCount; }
    }
    private int testCount = 0;
    private float preMousePos = -10f; // マウスの前回のx座標
    private bool startFlag = false; // true: 準備OK
    private float time = 0f;

    public FileOperation(string readFileName, int fileRowCount, int testInterval)
    {
        this.readFileName = readFileName;
        this.fileRowCount = fileRowCount;
        this.testInterval = testInterval;
        modelPositions = new Vector3[fileRowCount];
        userPositions = new Vector3[fileRowCount];
    }
    public FileOperation(string readFileName, int fileRowCount, string writeFileName, GameObject recordObj, GameObject startLine, GameObject endLine, int testInterval)
    {
        this.recordObject = recordObj;
        this.startLine = startLine;
        this.endLine = endLine;
        this.readFileName = readFileName;
        this.writeFileName = writeFileName;
        this.fileRowCount = fileRowCount;
        this.testInterval = testInterval;
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
                return null;
            }
        }
        else
        {
            Debug.Log("ファイルは既に開かれています。");
            return null;
        }

    }
    public void RecordingUpdate()
    {
        time += Time.deltaTime;
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition); // マウスの現在のx座標
        Vector3 mousePos = new Vector3(worldMousePos.x, worldMousePos.y, 10f);

        if(Input.GetMouseButton(0) && mousePos.x <= StartLine) // マウスがスタートエリアに入ったとき、記録準備OKにする。
        {
            time = 0f;
            startFlag = true;

            
            recordObject.GetComponent<SpriteRenderer>().color = Color.yellow;
        }
        else if (Input.GetMouseButton(0) && mousePos.x - preMousePos > 0 && startFlag &&
        mousePos.x >= StartLine && mousePos.x <= EndLine) // Playエリアでマウスが右に動いているとき、データを保存する。
        {
            SaveData(mousePos);
            
            startLine.GetComponent<SpriteRenderer>().color = Color.white;
            endLine.GetComponent<SpriteRenderer>().color = Color.yellow;
        }
        else if(Input.GetMouseButton(0) && mousePos.x >= EndLine && startFlag) // マウスがエンドエリアに入ったとき、空行を入れる。「startFlag」条件のため、一度しか呼び出されない。
        {
            EndData();
            startFlag = false; // もう一度スタートエリアに入らない限り、データが保存されないようにするためのもの。

            startLine.GetComponent<SpriteRenderer>().color = Color.yellow;
            endLine.GetComponent<SpriteRenderer>().color = Color.white;
            recordObject.GetComponent<SpriteRenderer>().color = Color.white;
        }
        else if (Input.GetKeyDown(KeyCode.Return)) // エンターキーが押されたら、ファイルを閉じる。
        {
            WriteCloseData();
        }

        preMousePos = mousePos.x;
    }

    public void EndData()
    {
        sw.WriteLine("");
        sw.Flush();
        Debug.Log("End_Trail:" + trialCount);
        if(trialCount % testInterval == 0)
        {
            testCount++;
        }
        trialCount++;
    }

    public void SaveData(Vector3 mousePos)
    {
        //myCircle.transform.position = mousePos;

        string[] s1 =
        {
            Convert.ToString(10 * (testCount+1) + trialCount - testCount), Convert.ToString(time),
            Convert.ToString(mousePos.x), Convert.ToString(mousePos.y), Convert.ToString(mousePos.z),
        };
        string[] s2 =
        {
            "test" + Convert.ToString(trialCount / testInterval), Convert.ToString(time),
            Convert.ToString(mousePos.x), Convert.ToString(mousePos.y), Convert.ToString(mousePos.z),
        };
        
        string s3 = string.Join(",", s1);
        string s4 = string.Join(",", s2);
        if(trialCount % testInterval != 0)
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
    float time = 0f;
    private int availableNum = 5, notAvailableNum = 0;
    private int correspondTime = 0;  // Userの現在地に対応するModelの時間。 値が-1のとき、試行と試行の間であることを意味する
    private int guidanceTime = 0;   // ガイダンスの現在の時間。値が-1のとき、ユーザーが右端まで到達したことを意味する
    private float frame_5_score = 0f;       // 5フレームでのスコア
    private float trialDiff;        // 1試行での誤差
    private float trialScore = 0f; // 1試行でのスコア
    private int updateCount;

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
    /*public float Evaluation()
    {
        float diff_y = 1f;       // マウスとモデルのy座標のズレ
        int nearest = 0;        // 今回の呼び出しで対応点のインデックスがどれだけ進むか

        Vector3 mousePos = Input.mousePosition;
        Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        screen_mousePos = new Vector3(screen_mousePos.x, screen_mousePos.y, 10f);
        user.transform.position = screen_mousePos;

        // 現ユーザーのポジションを評価
        if(correspondTime <= guidanceTime)
        {
            int head = correspondTime;
            int tail = guidanceTime;
            int center = (tail + head) / 2;
            float correntScore = 0f;
            
            // 現時点（correspondTime）とガイダンス時点（guidanceTime）の間で、最もUserに近い点を二分探索。
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
                nearest = -1;               // ガイダンスに追いついてしまったとき、下のif文が常にfalseになりガイダンスが更新されなくなるため。それの対処。
            }

            // y座標のズレをスコア化
            diff_y = Mathf.Abs(screen_mousePos.y - modelPositions[correspondTime].y);
            //correntScore = (2f - diff_y);
            correntScore = -2f * diff_y + 4f;


            if(nearest != 0)  // ユーザーが止まってなければ
            {
                return correntScore;  // スコアを返す。
            }
            else
            {
                return 0f;
            }
        }
        else
        {
            // Debug.Log("correspondTime > guidanceTime");
            return 0f;
        }
    }*/

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
            float correntScore = 0f;
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

            // 現時点（correspondTime）とガイダンス時点（guidanceTime）の間で、最もUserに近い点を探索。
            for(int i = 0; correspondTime + i <= maxIndex; i++)
            {
                diff = Vector3.Distance(screen_mousePos, modelPositions[correspondTime + i]);
                if(diff < minDiff)
                {
                    minDiff = diff;     // 現フレームにおけるユーザー位置のズレの最小値を更新
                    nearest = i;        // 最小値をとるモデル位置と現ユーザー位置のindex差を更新
                }
            }
            correspondTime += nearest;
            if(correspondTime == guidanceTime)
            {
                nearest = -1;           // ガイダンスに追いついてしまったとき、下のif文が常にfalseになりガイダンスが更新されなくなるため。それの対処。
            }
            correntScore = -2f * (minDiff - 1f);

            if(nearest != 0 && nearest != -1)  // ユーザーが止まっておらず、ガイダンスに追いついていなければ、
            {
                return correntScore;  // スコアを返す。
            }
            else if(nearest == -1)
            {
                return 1f;
            }
            else
            {
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
        if(time <= 0.1f) // 試行開始10フレームは、前回の利用可能フレーム分だけ見本が進む
        {
            Debug.Log("availableNum" + availableNum);
            guidanceTime = (int)(availableNum * (time * 10f));
        }
        else
        {
            guidanceTime += (int)(availableNum * updateCount / 5) - notAvailableNum;  // 今回の呼び出しで表示されるガイダンスのインデックス
            notAvailableNum = availableNum * updateCount / 5;
        }

        if(guidanceTime < fileRowCount)
        {
            guidance.transform.position = modelPositions[guidanceTime];  // ガイダンスが右端に到達しても、guidanceTimeの値自体は更新される。ガイダンスは進まない
        }
        else
        {
            Debug.Log("guidanceTime" + guidanceTime);
        }

        if(correspondTime >= fileRowCount - 1 && guidanceTime != -1)
        {
            Debug.Log("correspondTime >= fileRowCount");
            Debug.Log("guidanceTime:" + guidanceTime);
            Debug.Log("correspondTime:" + correspondTime);
            trialScore = guidanceTime - correspondTime;     // どれだけ先行させられたか。ガイダンスが終わった時点で呼び出され、それ以降呼び出されない。
            guidanceTime = -1;                              // それ以降呼び出されないための処理。
        }
    }
    public void GuidanceUpdate()
    {
        // Debug用
        if(Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("guidanceTime:" + guidanceTime);
            Debug.Log("correspondTime:" + correspondTime);
        }
        //

        if (Input.GetMouseButton(0))
        {
            time += Time.deltaTime;
            if(correspondTime == -1 && guidanceTime == -1)  // 第1試行を除いたすべての試行の初期動作。
            {
                availableNum = (int)trialScore;
                trialScore = 0f;
                time = 0f;
                correspondTime = 0;
                guidanceTime = 0;
                notAvailableNum = 0;
                frame_5_score = 0f;
            }
            else if(time > 0.2f) //&& guidanceTime != -1)
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
        /*
        if(correspondTime >= fileRowCount)
        {
            correspondTime = -1;
            guidanceTime = -1;
        }*/
    }
}
