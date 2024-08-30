using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Text; //文字コードを指定するために必要

public class TestRecorder : MonoBehaviour
{
    [SerializeField] GameObject user, StartLine, EndLine;
    [SerializeField] string writeFileName = "defalt";
    FileOperation testFile;

    private void Start()
	{
        testFile = new FileOperation(writeFileName, user, StartLine, EndLine);
        testFile.WriteOpenData();
        testFile.FileSettingCheck();
	}

    private void FixedUpdate()
    {
        testFile.RecordingUpdate();
    }
}