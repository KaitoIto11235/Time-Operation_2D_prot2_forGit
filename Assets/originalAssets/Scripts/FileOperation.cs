using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;

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
                "frameDiff", "userLevel", "preTrialOffset", "preLevelOffset",
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
    public void RecordingUpdate(float distToFile, int userLevel, int trialOffset, int levelOffset)
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
            SaveData(mousePos, distToFile, userLevel, trialOffset, levelOffset);
            
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
    public void SaveData(Vector3 mousePos, float distToFile, int userLevel, int trialOffset, int levelOffset)
    {
        string[] s1 =
        {
            Convert.ToString(trialCount), Convert.ToString(time),
            Convert.ToString(mousePos.x), Convert.ToString(mousePos.y), Convert.ToString(mousePos.z),
            Convert.ToString(distToFile), Convert.ToString(userLevel), Convert.ToString(trialOffset), Convert.ToString(levelOffset),
        };
        string[] s2 =
        {
            "test" + Convert.ToString(trialCount), Convert.ToString(time),
            Convert.ToString(mousePos.x), Convert.ToString(mousePos.y), Convert.ToString(mousePos.z),
            Convert.ToString(distToFile), Convert.ToString(userLevel), Convert.ToString(trialOffset), Convert.ToString(levelOffset),
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
