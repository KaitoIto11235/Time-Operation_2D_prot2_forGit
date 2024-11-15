using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;

public class AutoPlay : BaseGuidance // ガイダンスに関する計算・処理を行う。
{
    //private int correspondTime = 0;  // Userの現在地に対応するModelの時間。 値が-1のとき、試行と試行の間であることを意味する
    private int guidanceTime = 0;   // ガイダンスの現在の時間。値が-1のとき、ユーザーが右端まで到達したことを意味する
    public int GuidanceTime
    {
        get {return guidanceTime;}
    }

    private float playSpeed = 1.0f;
    private float forSpeedChange = 0f;

    public AutoPlay(GameObject guidance, GameObject user, int fileRowCount, Vector3[] positions, int commaPlaySpeed, Material[] materialArray)
        : base(guidance, user, fileRowCount, positions, materialArray)
    {
        this.playSpeed = (float)commaPlaySpeed/10f;
    }

    public override float Evaluation()
    {
        // 呼び出されない
        return -1f;
    }

    public override void Moving(int updateCount)
    {
        // 呼び出されない
    }
    public override void GuidanceUpdate()
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