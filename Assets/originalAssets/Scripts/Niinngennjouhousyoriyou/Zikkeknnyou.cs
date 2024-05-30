using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zikkeknnyou : MonoBehaviour
{
    public float initialSpeed = 1.0f; // 初期速度
    private float speedIncreaseRate = 0.1f; // 速度の増加率
    private float speed; // 現在の速度
    private float baseAmplitude = 0.1f; // 基本振幅（mm単位）
    private float amplitude; // 現在の振幅
    private float startPositionY; // 開始位置
    private float direction = 1.0f; // 移動方向
    private List<GameObject> objects;
    private int currentIndex = -1;
    private Color defaultColor = Color.white; // 元の色
    private bool isMoving = false; // 動き出すかどうかのフラグ
    private float delayTime = 0; // 遅延時間

    void Start()
    {
        startPositionY = transform.position.y; // 初期位置を保存
        speed = initialSpeed; // 初期速度を設定
        amplitude = baseAmplitude; // 初期振幅を設定
        objects = new List<GameObject> {
            GameObject.Find("-20"),
            GameObject.Find("-10"),
            GameObject.Find("-3"),
            GameObject.Find("0"),
            GameObject.Find("3"),
            GameObject.Find("10"),
            GameObject.Find("20")
        };

        // オブジェクトのリストをシャッフル
        Shuffle(objects);
    }

    void Update()
    {
        if (isMoving)
        {
            // 振幅を速度に応じて調整
            amplitude = baseAmplitude / speed * initialSpeed;

            // 現在の位置を更新
            transform.position += new Vector3(0, direction * speed, 0);

            // 範囲を超えたら方向転換
            if (transform.position.y <= -amplitude / 1000.0f)
            {
                direction = 1;
            }
            else if(transform.position.y >= 0f)
            {
                direction = -1;
                speed += speedIncreaseRate; 
            }
        }

        if (delayTime > 0)
        {
            delayTime -= Time.deltaTime;
            if (delayTime <= 0)
            {
                isMoving = true; // 遅延後に動き出す
            }
        }

        // マウスクリックで速度を表示
        if (Input.GetMouseButtonDown(0))
        {
            if (currentIndex != -1)
            {
                // 選択されているオブジェクトの情報を出力
                Debug.Log("選択されたオブジェクト: " + objects[currentIndex].name + ", 速度: " + speed + " m/s");
                // 前のオブジェクトの色を元に戻す
                objects[currentIndex].GetComponent<Renderer>().material.color = defaultColor;
            }

            currentIndex++;
            if (currentIndex >= objects.Count)
            {
                currentIndex = 0; // リストの最初に戻る
                Shuffle(objects); // 全オブジェクトが選択されたらシャッフル
            }
            transform.position = new Vector3(0, 0, 0);
            // 新しいオブジェクトを赤くする
            objects[currentIndex].GetComponent<Renderer>().material.color = Color.red;
            isMoving = false; // 動きを停止
            delayTime = Random.Range(1.0f, 10.0f); // 1秒から10秒のランダムな遅延を設定
        }
    }

    void Shuffle(List<GameObject> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            GameObject temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}