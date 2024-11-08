using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;

public class LeadDistanceCondition : MonoBehaviour
{
    [SerializeField] private GameObject guidance, user;
    [SerializeField] string readFileName = "default";
    [SerializeField] string writeFileName = "default";
    [SerializeField] GameObject StartLine, EndLine;

    [SerializeField] int readFileRowCount = 1000;
    FileOperation leadFile;
    LeadPlay leadGuidance;
    [SerializeField] bool Recording = false;
    [SerializeField] int availableNum = 5;
    
    void Start()
    {
        if(Recording)
        {
            leadFile = new FileOperation(readFileName, readFileRowCount, writeFileName, user, StartLine, EndLine);
            leadFile.WriteOpenData();
        }
        else
        {
            leadFile = new FileOperation(readFileName, readFileRowCount);
        }
        leadGuidance = new LeadPlay(guidance, user, readFileRowCount, leadFile.modelPositions, availableNum);
        leadFile.ReadOpenData();

        leadFile.FileSettingCheck();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        leadGuidance.GuidanceUpdate();

        if(Recording)
        {
            if(leadGuidance.diffToFile != 0)
            {
                leadFile.RecordingUpdate(leadGuidance.diffToFile);
            }
            else
            {
                leadFile.RecordingUpdate();
            }
        }
    }
}


// ユーザーとモデルの間隔を一定にする。（ユーザー精度によらない）
public class LeadPlay: BaseGuidance  // ガイダンスに関する計算・処理を行う。
{
    float trialTime = 0f;        // 1試行の時間
    private int availableNum;
    private int trialFirstAvailableNum;
    private int notAvailableNum = 0;
    private int correspondTime = 0;  // Userの現在地に対応するModelのindex。 値が-1のとき、試行と試行の間であることを意味する
    private int guidanceTime = 0;   // ガイダンスの現在表示しているのindex。値が-1のとき、ユーザーが右端まで到達したことを意味する
    private float frame_5_score = 0f;       // 5フレームでのスコア
    private float trialError = 0f;   // 1試行での誤差
    private float trialScore = 0f; // 1試行でのスコア
    private int updateCount;
    private int stopCount = 0;   // 同じ対応点で停止している時間

    // private int fileRowCount;
    // private GameObject user, guidance;
    // private Vector3[] modelPositions;

    public float diffToFile = 0f;  // ファイルに1フレームの誤差を記録するための変数（correspondTimeが更新されたときに限る）
    public LeadPlay(GameObject guidance, GameObject user, int fileRowCount, Vector3[] positions, int availableNum)
        : base(guidance, user, fileRowCount, positions)
    {
        this.trialFirstAvailableNum = availableNum;
        this.availableNum = availableNum;
    }
    

    // 現フレームのユーザーの評価値が返されえる
    public override float Evaluation()
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
                maxIndex = fileRowCount - 1;  // guidanceTimeがmodelインデックスをオーバーした時の例外処理
            }

            // 前フレームの対応点から2つ以上離れた点において、点を飛ばしたことにに対するペナルティを与える関数
            float JumpPenalty(int progress, int count) 
            {
                if(progress == 0 || progress == 1 || count > 50)
                {
                    return 0f;
                }
                else
                {
                    return 0.01f * (50 - count) / 50 * progress;
                    //return 10f * (float)Math.Cos((Math.PI / 2f) * (count / 50f)) * progress;
                }
            }

            // 今回の呼び出しにおいて、最も近い点との距離minDiffをスコア化する関数
            float FrameScore(float minDiff, int count) 
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
                diff = Vector3.Distance(screen_mousePos, modelPositions[correspondTime + progress]);
                diff += JumpPenalty(progress, stopCount);  // 点を飛ばしたことにに対するペナルティを与える
                if(diff < minDiff)
                {
                    minDiff = diff;     // 現フレームにおけるユーザー位置のズレの最小値を更新
                    nearest = progress;        // 1フレーム前ユーザー位置対応モデルindexと現ユーザー位置対応モデルindexの差を更新
                }
            }
            trialError += minDiff;
            correspondTime += nearest;

            // if(correspondTime == guidanceTime)  // => ユーザーがガイダンスに追いついた
            // {
            //     stopCount = 0;
            //     diffToFile = minDiff;
            //     return 1f;          // ガイダンスに途中（5回の呼び出しのうち3回目など）追いついてしまったとき、4,5回目がnearest == 0となってしまうため、特別処理 
            // }
            // else if(nearest == 0)  // つまり、進んでいないとき。
            if(nearest == 0)
            {
                stopCount++;
                diffToFile = 0f;
                return 0f;
            }
            else  // ユーザーが止まっておらず、ガイダンスに追いついていなければ、
            {
                float score = FrameScore(minDiff, stopCount);       // スコアを返す。
                stopCount = 0;
                diffToFile = minDiff;    // ファイルに今フレームの誤差を記録するためのもの
                return nearest;            // スコアを返す。
            }
        }
        else
        {
            // Debug.Log("correspondTime > guidanceTime");
            return 0f;
        }
    }
    
    // availableNumに応じて進み具合を調整
    public override void Moving(int updateCount)
    {
        if(trialTime < 0.42f) // 試行開始20フレームは、前回の利用可能フレーム分だけ見本が進む
        {
            guidanceTime = (int)(availableNum * (trialTime * 2.5f));
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
    public override void GuidanceUpdate()
    {
        if(Input.GetMouseButton(1))
        {
            Debug.Log("guidanceTime:" + guidanceTime);
            Debug.Log("correspondTime" + correspondTime);
            Debug.Log("availableNum" + availableNum);
        }
        if (Input.GetMouseButton(0))
        {
            trialTime += Time.deltaTime;
            if(correspondTime == -1 && guidanceTime == -1)  // 第1動作を除いたすべての試行の初期動作。
            {
                updateCount = 0;
                availableNum = trialFirstAvailableNum;
                trialScore = 0f;
                correspondTime = 0;
                guidanceTime = 0;
                notAvailableNum = 0;
                frame_5_score = 0f;

                trialTime = 0.02f;
            }

            updateCount++;
            frame_5_score += Evaluation();  // ユーザーが止まっていない かつ correspondTime < guidanceTime ⇒ 現フレームのスコアが返される。

            if(availableNum > 0 && guidanceTime != -1) // guidanceTime == -1 ⇒ ユーザーが最後まで到達したことを意味する
            {
                Moving(updateCount);
            }

            //if(updateCount == 5 || (time > 0.08f && time <= 0.2f))  
            // 初期動作時または5フレームごとに呼び出し、利用可能インデックス数の更新や使用済み利用可能インデックス数を0に。
            if(updateCount == 5)
            {
                if(trialTime >= 0.4f)
                {
                availableNum = (int)frame_5_score;
                }
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
