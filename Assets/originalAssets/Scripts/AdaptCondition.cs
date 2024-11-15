using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text;

public class AdaptCondition : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] private GameObject guidance, user;
    [SerializeField] string readFileName = "default";
    [SerializeField] string writeFileName = "default";
    [SerializeField] [Range(1, 6)] int experiment4_condition = 7;
    [SerializeField] GameObject StartLine, EndLine;

    [SerializeField] int readFileRowCount = 1000;
    FileOperation adaptFile;
    AdaptPlay adaptGuidance;
    [SerializeField] bool Recording = false;

    [SerializeField] Material[] materialArray = new Material[3];


    
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
        adaptGuidance = new AdaptPlay(guidance, user, readFileRowCount, adaptFile.modelPositions, materialArray, experiment4_condition);
        adaptFile.ReadOpenData();

        adaptFile.FileSettingCheck();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        adaptGuidance.GuidanceUpdate();

        // 0.5秒に1度、効果音を鳴らす
        if(adaptGuidance.GuidanceTime % 25 == 0)
        {
            audioSource.Play();
        }

        if(Recording)
        {
            // if(adaptGuidance.distToFile != 0)
            // {
                adaptFile.RecordingUpdate(adaptGuidance.DistToFile, adaptGuidance.UserLevel, adaptGuidance.TrialOffset, adaptGuidance.LevelOffset);
            // }
            // else
            // {
            //     adaptFile.RecordingUpdate();
            // }
        }
    }
}
