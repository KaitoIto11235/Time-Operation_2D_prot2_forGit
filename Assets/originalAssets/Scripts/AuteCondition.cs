using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;

public class AuteCondition : MonoBehaviour
{
    [SerializeField] private GameObject guidance, user;
    [SerializeField] string readFileName = "default";
    [SerializeField] string writeFileName = "default";
    [SerializeField] GameObject StartLine, EndLine;
    [SerializeField] int readFileRowCount = 1000;
    FileOperation auteFile;
    GuidanceAutePlay auteGuidance;
    [SerializeField] bool Recording = false;

    int commaPlaySpeed = 10;
    //[SerializeField, Range(1, 20)] int commaPlaySpeed = 10;


    void Start()
    {
        if(Recording)
        {
            auteFile = new FileOperation(readFileName, readFileRowCount, writeFileName, user, StartLine, EndLine);
            auteFile.WriteOpenData();
        }
        else
        {
            auteFile = new FileOperation(readFileName, readFileRowCount);
        }
        auteGuidance = new GuidanceAutePlay(guidance, user, readFileRowCount, auteFile.modelPositions, commaPlaySpeed);
        auteFile.ReadOpenData();

        
        auteFile.FileSettingCheck();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        auteGuidance.GuidanceUpdate();
        
        if(Recording)
        {
            auteFile.RecordingUpdate();
        }
    }
}

/*
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
*/

public class GuidanceAutePlay  // ガイダンスに関する計算・処理を行う。
{
    //private int correspondTime = 0;  // Userの現在地に対応するModelの時間。 値が-1のとき、試行と試行の間であることを意味する
    private int guidanceTime = 0;   // ガイダンスの現在の時間。値が-1のとき、ユーザーが右端まで到達したことを意味する

    private int fileRowCount;
    private GameObject user, guidance;
    private Vector3[] modelPositions;

    private float playSpeed = 1.0f;
    private float forSpeedChange = 0f;

    public GuidanceAutePlay(GameObject guidance, GameObject user, int fileRowCount, Vector3[] positions, int commaPlaySpeed)
    {
        this.guidance = guidance;
        this.user = user;
        this.fileRowCount = fileRowCount;
        this.modelPositions = new Vector3[fileRowCount];
        this.modelPositions = positions;
        this.playSpeed = (float)commaPlaySpeed/10f;
    }
    public void GuidanceUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
            screen_mousePos = new Vector3(screen_mousePos.x, screen_mousePos.y, 10f);
            user.transform.position = screen_mousePos;

            if(guidanceTime < fileRowCount)
            {
                guidance.transform.position = modelPositions[guidanceTime];
                forSpeedChange += playSpeed;
                if(forSpeedChange >= 1.0f)
                { 
                    guidanceTime += (int)forSpeedChange;
                    forSpeedChange -= (int)forSpeedChange;
                }
            }
        }
        else
        {
            guidanceTime = 0;
        }
    }
}