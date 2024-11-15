using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;
public interface IGuidance
{
    float Evaluation();
    void Moving(int updateCount);
    void GuidanceUpdate();
}
public abstract class BaseGuidance : IGuidance
{
    protected GameObject user, guidance;
    protected int fileRowCount;
    protected Vector3[] modelPositions;
    protected Material[] materialArray;

    public BaseGuidance(GameObject guidance, GameObject user, int fileRowCount, Vector3[] positions, Material[] materialArray)
    {
        this.guidance = guidance;
        this.user = user;
        this.fileRowCount = fileRowCount;
        this.modelPositions = new Vector3[fileRowCount];
        this.modelPositions = positions;
        this.materialArray = materialArray;
    }

    public abstract float Evaluation();
    public abstract void Moving(int updateCount);
    public abstract void GuidanceUpdate();
}

public class AdaptPlay : BaseGuidance  // ガイダンスに関する計算・処理を行う。
{
    float trialTime = 0f;        // 1試行の時間
    private int availableNum = 5, notAvailableNum = 0;
    private int correspondTime = 0;  // Userの現在地に対応するModelの時間。 値が-1のとき、試行と試行の間であることを意味する
    private int offsetCorrespondTime = 0;  // Userの現在地に、trialScoreを加えたもの。
    private int guidanceTime = 0;   // ガイダンスの現在の時間。値が-1のとき、ユーザーが右端まで到達したことを意味する
    public int GuidanceTime
    {
        get {return guidanceTime;}
    }
    private float frame_5_score = 0f;       // 5フレームでのスコア
    private int trialOffset = 5; // 1試行終了時にガイダンスが何フレーム離れているか
    public int TrialOffset
    {
        get{return trialOffset;}
    }
    private int levelOffset = 0; // 現レベルにおいて、ガイダンスが何フレーム離れているか。trialOffsetの合計。
    public int LevelOffset
    {
        get{return levelOffset;}
    }
    private int  userLevel = 1;  // ユーザーのレベル 
    public int UserLevel
    {
        get {return userLevel;}
    }
    private int updateCount;
    private int stopCount = 0;   // 同じ対応点で停止している時間
    private bool initialOperation = true;

    //private int fileRowCount;
    //private GameObject user, guidance;
    //private Vector3[] modelPositions;

    private float distToFile = 0f;  // ファイルに1フレームの誤差を記録するための変数（correspondTimeが更新されたときに限る）
    public float DistToFile
    {
        get {return distToFile;}
    }

    private int condition;  // 実験条件

    public AdaptPlay(GameObject guidance, GameObject user, int fileRowCount, Vector3[] positions, Material[] materialArray, int experiment4_condition)
        : base(guidance, user, fileRowCount, positions, materialArray)
    {
        condition = experiment4_condition;
    }
    
    float JumpPenalty(int jump, int stopCount) // 前フレームの対応点から2つ以上離れた点に対して、距離に対してペナルティを与える関数
    {
        if(jump == 0 || jump == 1 || stopCount > 25)
        {
            return 0f;
        }
        else
        {
            return (25 - stopCount) * 0.01f * jump;
        }
    }

    float FrameScore(float minDist, int stopCount, int level, int condition) // 今回の呼び出しにおいて、最も近い点との距離minDiffをスコア化する関数
    {
        // if(stopCount > 25)
        // {
        //     return 0.2f;
        // }
        if(condition == 1 || condition == 4)
        {
            return 1.2f;
        }
        else if(condition == 2 || condition == 5)
        {
            return 1.35f;
        }
        else if(condition == 3 || condition == 6)
        {
            return 1.5f;
        }
        else // error
        {
            Debug.Log("Condition_Error");
            return 0f;
        }
    }

    // float FrameScore1(float minDist, int stopCount, int level) // 今回の呼び出しにおいて、最も近い点との距離minDiffをスコア化する関数
    // {
    //     if(stopCount > 25)
    //     {
    //         return 0.2f;
    //     }
    //     else
    //     {
    //         return 1.2f;
    //     }
    // }
    // float FrameScore2(float minDist, int stopCount, int level) // 今回の呼び出しにおいて、最も近い点との距離minDiffをスコア化する関数
    // {
    //     if(stopCount > 25)
    //     {
    //         return 0.2f;
    //     }
    //     else
    //     {
    //         return -minDist + 1.7f;
    //     }
    // }
    // float FrameScore3(float minDist, int stopCount, int level) // 今回の呼び出しにおいて、最も近い点との距離minDiffをスコア化する関数
    // {
    //     if(stopCount > 25)
    //     {
    //         return 0.2f;
    //     }
    //     else
    //     {
    //         return -2f * minDist + 2.2f;
    //     }
    // }
    // float FrameScore4(float minDist, int stopCount, int level) // 今回の呼び出しにおいて、最も近い点との距離minDiffをスコア化する関数
    // {
    //     if(stopCount > 25)
    //     {
    //         return 0.2f;
    //     }
    //     else
    //     {
    //         return (float)(1 - level) * minDist + (0.5f * level) + 0.7f;
    //     }
    // }
    // float FrameScore5(float minDist, int stopCount, int level) // 今回の呼び出しにおいて、最も近い点との距離minDiffをスコア化する関数
    // {
    //     if(stopCount > 25)
    //     {
    //         return 0.2f;
    //     }
    //     else
    //     {
    //         return (1.5f - 1.5f * level) * minDist + (0.75f * level) + 0.45f;
    //     }
    // }
    // float FrameScore6(float minDist, int stopCount, int level) // 今回の呼び出しにおいて、最も近い点との距離minDiffをスコア化する関数
    // {
    //     if(stopCount > 25)
    //     {
    //         return 0.2f;
    //     }
    //     else
    //     {
    //         return (2f - 2f * level) * minDist + (float)level + 0.2f;
    //     }
    // }
    
    // 現フレームのユーザーの精度を評価
    public override float Evaluation()
    {
        int nearest = 0;        // 今回の呼び出しで対応点のインデックスがどれだけ進むか

        Vector3 mousePos = Input.mousePosition;
        Vector3 screen_mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        screen_mousePos = new Vector3(screen_mousePos.x, screen_mousePos.y, 10f);
        user.transform.position = screen_mousePos;

        // 現ユーザーのポジションを評価
        if(offsetCorrespondTime <= guidanceTime)
        {
            float dist = 0f;
            float minDist = 500f;
            int searchRange = 0;

            if(stopCount < 100)
            {
                searchRange = guidanceTime - offsetCorrespondTime;
            }
            else  // 学習者が軌道を大きく外れ、ガイダンス位置についてしまい、更新されなくなったとき用。
            {
                searchRange = guidanceTime - correspondTime;
            }
            if(correspondTime + searchRange > fileRowCount - 1)
            {
                searchRange = fileRowCount - 1 - correspondTime;
            }

            //offsetCorrespondTimeからguidanceTimeまでの幅maxIndex-offsetCorrespondTimeだけ、現地点（correspondTime）から探索。
            for(int jump = 0; jump <= searchRange; jump++)
            {
                dist = Vector3.Distance(screen_mousePos, modelPositions[correspondTime + jump]) + JumpPenalty(jump, stopCount);
                if(dist < minDist)
                {
                    minDist = dist;     // 現フレームにおけるユーザー位置のズレの最小値を更新
                    nearest = jump;     // 最小値をとるモデル位置と現ユーザー位置のindex差を更新
                }
            }
            correspondTime += nearest;
            offsetCorrespondTime += nearest;
            
            if(stopCount >= 100)
            {
                initialOperation = true;
                
                guidanceTime = correspondTime;
                levelOffset = Math.Max(5, levelOffset / 2);
                trialOffset = Math.Max(5, levelOffset / 2);
                availableNum = levelOffset + trialOffset;
                offsetCorrespondTime = correspondTime + levelOffset;

                if(updateCount == 5)
                {
                    updateCount = 4;  // このフレームは無かったことに、、。こうしないと、updateCount==5のとき、すぐにavailableNumが書き換えられてしまい、initialOperationによる再生が数フレームで終わってしまう。
                }
                stopCount = 0;
                return 0f;
            }
            else if(offsetCorrespondTime == guidanceTime) // このとき、次の5フレームでガイダンスが5フレーム以上進んでくれないと、探索範囲が2点のみになり、抜け出せなくなってしまう。
            {
                float score = FrameScore(minDist, stopCount, userLevel, condition);       // スコアを返す。
                stopCount = 0;
                distToFile = minDist;    // ファイルに今フレームの誤差を記録するためのもの
                if(score > 1f)
                {
                    return score;
                }
                else
                {
                    return 1f;
                }
            }
            else if(nearest == 0)  // つまり、進んでいないとき。
            {
                stopCount++;
                distToFile = 0f;
                return 0f; 
            }
            else  // ユーザーが止まっておらず、ガイダンスに追いついていなければ、
            {
                float score = FrameScore(minDist, stopCount, userLevel, condition);       // スコアを返す。
                stopCount = 0;
                distToFile = minDist;    // ファイルに今フレームの誤差を記録するためのもの
                return score;            // スコアを返す。
            }
        }
        else
        {
            return 0f;
        }
    }
    
    
    public override void Moving(int updateCount)
    {
        //if(trialTime <= 0.1f) // 試行開始10フレームは、前回の利用可能フレーム分だけ見本が進む
        if(initialOperation == true)
        {
            //guidanceTime = (int)(availableNum * (trialTime * 10f));
            guidanceTime++;
            availableNum--;
            //Debug.Log("availableNum: " + availableNum);
            if(availableNum <= 0)
            {
                initialOperation = false;
            }
        }
        else
        {
            guidanceTime += (int)(availableNum * updateCount / 5) - notAvailableNum;  // 今回の呼び出しで表示されるガイダンスのインデックス
            notAvailableNum = availableNum * updateCount / 5;
        }

        if(guidanceTime < fileRowCount - 1)
        {
            guidance.transform.position = modelPositions[guidanceTime];  
        }
        else
        {
            guidance.transform.position = modelPositions[fileRowCount - 2];  // ガイダンスが右端に到達しても、guidanceTimeの値自体は更新される。ガイダンスは進まない
        }

        if(correspondTime >= fileRowCount - 1 && guidanceTime != -1)  // どれだけ先行させられたか。ガイダンスが終わった時点で呼び出され、それ以降呼び出されない。
        {
            int still_addition = Math.Max(0, (int)frame_5_score) + (availableNum - notAvailableNum); // correspondTime=fileRowCount-1のときに、gauidanceTimeに追加されなかった分
            guidance.transform.position = modelPositions[0];
            trialOffset = guidanceTime +  still_addition - offsetCorrespondTime-6;  // -6(動作開始直後のguidanceTime＋５の分)
            guidanceTime = -1;                              // それ以降呼び出されないための処理。
        }
    }
    public override void GuidanceUpdate()
    {
        if(Input.GetMouseButton(1)) // デバッグ用
        {
            Debug.Log("correspondTime: " + correspondTime);
            Debug.Log("offsetCorrespondTime: " + offsetCorrespondTime);
            Debug.Log("guidanceTime: " + guidanceTime);
        }
        if (Input.GetMouseButton(0))
        {
            trialTime += Time.deltaTime;

            if(correspondTime == -1 && guidanceTime == -1)  // 第1試行を除いたすべての試行の初期動作。
            {
                levelOffset += trialOffset;
                Debug.Log("trialOffset" + trialOffset);
                Debug.Log("levelOffset" + levelOffset);
                if(condition <= 3)
                {
                    if(levelOffset > 50)
                    {
                        levelOffset = 5;
                        userLevel++;
                        Debug.Log("level up!");
                    }
                }
                else if(condition <= 6)
                {
                    if(levelOffset > userLevel * 10 + 40)
                    {
                        levelOffset = 5;
                        userLevel++;
                        Debug.Log("level up!");
                    }
                }
                
                availableNum = levelOffset;            // 初期動作でガイダンスがどれだけ進むか
                correspondTime = 0;
                offsetCorrespondTime = levelOffset;    // offsetCorrespondTimeはcorrespondTimeから常にこれ以上は先行する。 
                guidanceTime = 0;
                trialTime = 0f;
                notAvailableNum = 0;
                frame_5_score = 0f;
                initialOperation = true;
            }
            else if(initialOperation == false)
            {
                // userを白色,guidanceを黒色に変更→ユーザーに対して進行可能を表現。
                user.GetComponent<Renderer>().material = materialArray[1];
                guidance.GetComponent<Renderer>().material = materialArray[2];

                updateCount++;
                frame_5_score += Evaluation();  // ユーザーが止まっていない かつ correspondTime < guidanceTime ⇒ 現フレームのスコアが返される。
            }
            else if(initialOperation == true)
            {
                // userのマテリアルを赤に変更→ユーザーに対して進行停止を表現。
                user.GetComponent<Renderer>().material = materialArray[0];
                guidance.GetComponent<Renderer>().material = materialArray[0];

            }

            //if(availableNum > 0 && guidanceTime != -1) // guidanceTime == -1 ⇒ ユーザーが最後まで到達したことを意味する
            if(guidanceTime != -1)
            {
                Moving(updateCount);
            }

            //if(updateCount == 5 || (time > 0.08f && time <= 0.2f))  // 初期動作時または5フレームごとに呼び出し、利用可能インデックス数の更新や使用済み利用可能インデックス数を0に。
            if(updateCount == 5)
            {
                availableNum = Math.Max(0, (int)frame_5_score);
                notAvailableNum = 0;
                frame_5_score = 0f;
                updateCount = 0;
            }
        }
        else if(guidanceTime == -1)
        {
            correspondTime = -1;  // 1試行が正常終了したことを意味する
        }
        else    // 正常終了前に、左クリックが外れてしまった場合。
        {
            guidance.transform.position = modelPositions[0];
            trialOffset = 0;

            guidanceTime = -1;
            correspondTime = -1;
        }
    }
}

