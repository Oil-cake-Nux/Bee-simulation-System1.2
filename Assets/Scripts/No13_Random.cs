using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：生成随机数
//***************************************** 
public class No13_Random : MonoBehaviour
{
    void Start()
    {
        //gameObject.transform.rotation = Random.rotation;
        //print(gameObject.transform.rotation);
        ////静态变量
        //print("随机出的旋转数是(以四元数形式表示)" + Random.rotation);//Unity上的旋转角度为欧拉角（三个数表示）
        //print("随机出的旋转数是(以欧拉角形式表示)" + Random.rotation.eulerAngles);
        //print(gameObject.transform.rotation.eulerAngles + "随机生成的四元数转为欧拉角");
        //print(Quaternion.Euler(gameObject.transform.rotation.eulerAngles) + "欧拉角转四元数");
        //print(Random.value + "随机出[0,1]之间的浮点数");
        print(Random.insideUnitCircle + "在（-1，-1）~（1,1）范围内随机生成的Vector2向量");
        //静态函数
        print(Random.Range(0, 4) + "在区间[0,4)");
        print(Random.Range(0, 4f) + "在区间[0,4]");
        Random.InitState(4);
        print(Random.Range(0, 4f) + "在区间[0,4]");
    }

    void Update()
    {

    } 
}
