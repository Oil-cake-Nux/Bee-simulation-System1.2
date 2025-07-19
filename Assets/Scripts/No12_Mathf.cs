using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：数学库函数
//***************************************** 
public class No12_Mathf : MonoBehaviour
{
    private float endTime = 10f;
    void Start()
    {
        //print(Mathf.Deg2Rad + "度到弧度的换算常量");
        //print(Mathf.Infinity + "为正无穷大的表示形式");
        //print(Mathf.NegativeInfinity + "为负无穷大的表示形式");
        //print(Mathf.PI + "为π的表示形式");
        //print(Mathf.Abs(-9) + "-9的绝对值");
        //print(Mathf.FloorToInt(2.3f) + "，向下取模,返回值为INT型");
        //print(Mathf.Floor(2.3f) + "，向下取模,返回值为浮点型");
        //print(Mathf.Lerp(1, 3, 0.2f) + ",a和b间按参数t进行线性插值");
    }

    void Update()
    {
        print("游戏倒计时："+endTime);
        endTime = Mathf.MoveTowards(endTime, 0, 0.01f);
    } 
}
