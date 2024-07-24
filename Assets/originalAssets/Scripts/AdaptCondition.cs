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

    [SerializeField] int readFileRowCount = 200;
    FileOperation adaptFile;
    GuidancePlay adaptGuidance;

    // Start is called before the first frame update
    void Start()
    {
        adaptFile = new FileOperation(readFileName, writeFileName, readFileRowCount);
        adaptGuidance = new GuidancePlay(guidance, user, readFileRowCount, adaptFile.modelPositions);
        adaptFile.ReadOpenData();
        adaptFile.WriteOpenData();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        adaptGuidance.GuidanceUpdate();
        adaptFile.RecordingUpdate();
    }
}

public class FileOperation
{
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
    private float StartLine = -8f;
    private float EndLine = 8f;
	private int trialCount = 1;
    private float preMousePos = -10f; // マウスの前回のx座標
    private bool startFlag = false; // true: 準備OK
    private float time = 0f;

    public FileOperation(string readFileName, int fileRowCount)
    {
        this.readFileName = readFileName;
        this.fileRowCount = fileRowCount;
        modelPositions = new Vector3[fileRowCount];
        userPositions = new Vector3[fileRowCount];
    }
    public FileOperation(string readFileName, string writeFileName, int fileRowCount)
    {
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

            /*
            myCircle.transform.position = new Vector3(-10f, 0f, 10f);
            myCircle.GetComponent<SpriteRenderer>().color = Color.yellow;
            */
        }
        else if (Input.GetMouseButton(0) && mousePos.x - preMousePos > 0 && startFlag &&
        mousePos.x >= StartLine && mousePos.x <= EndLine) // Playエリアでマウスが右に動いているとき、データを保存する。
        {
            SaveData(mousePos);
            /*
            startLine.GetComponent<SpriteRenderer>().color = Color.white;
            endLine.GetComponent<SpriteRenderer>().color = Color.yellow;
            */
        }
        else if(Input.GetMouseButton(0) && mousePos.x >= EndLine && startFlag) // マウスがエンドエリアに入ったとき、空行を入れる。「startFlag」条件のため、一度しか呼び出されない。
        {
            EndData();
            startFlag = false; // もう一度スタートエリアに入らない限り、データが保存されないようにするためのもの。

            /*
            startLine.GetComponent<SpriteRenderer>().color = Color.yellow;
            endLine.GetComponent<SpriteRenderer>().color = Color.white;
            myCircle.GetComponent<SpriteRenderer>().color = Color.white;
            */
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
        trialCount++;
    }

    public void SaveData(Vector3 mousePos)
    {
        //myCircle.transform.position = mousePos;

        string[] s1 =
        {
                Convert.ToString(trialCount), Convert.ToString(time),
                Convert.ToString(mousePos.x), Convert.ToString(mousePos.y), Convert.ToString(mousePos.z),
        };
        string s2 = string.Join(",", s1);
        sw.WriteLine(s2);
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
public class GuidancePlay
{
    public int availableNum = 5, notAvailableNum = 0;
    public int correspondTime = 0;  // Userの現在地に対応するModelの時間
    public int guidanceTime = 0;   // ガイダンスの現在の時間
    public float score = 0f;
    private int updateCount;

    public int fileRowCount;
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
    public float Evaluation()
    {
        float diff_y = 1f;       // マウスとモデルのy座標のズレ
        int nearest = 0;        // 今回の呼び出しで対応点のインデックスがどれだけ進むか

        Vector3 mousePos = Input.mousePosition;
        Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        screen_mousePos = new Vector3(screen_mousePos.x, screen_mousePos.y, 10f);
        user.transform.position = screen_mousePos;

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
    
    public void Moving(int updateCount)
    {
        guidanceTime += (int)(availableNum * updateCount / 5) - notAvailableNum;  // 今回の呼び出しで表示されるガイダンスのインデックス
        notAvailableNum = availableNum * updateCount / 5;
        if(guidanceTime < fileRowCount)
        {
            guidance.transform.position = modelPositions[guidanceTime];
        }
        else
        {
            Debug.Log("Moving():guidanceTime >= FileRowCount");
            guidanceTime = 0;
        }

    }
    public void GuidanceUpdate()
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
                Moving(updateCount);
            }
            if(updateCount == 5)
            {
                availableNum = (int)score;
                notAvailableNum = 0;
                score = 0f;
                updateCount = 0;
            }

        }
        else
        {
            correspondTime = 0;                 
            guidanceTime = 0;
        }
        if(correspondTime == fileRowCount)
        {
            correspondTime = 0;
            guidanceTime = 0;
        }
    }
}
