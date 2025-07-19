using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：打印函数
//***************************************** 
public class No1_printfdebug : MonoBehaviour
{
    void Start()
    {
        print("第一节课");
        Debug.Log("UnityAPI");
    }

    void Update()
    {

    } 
    private void Calculate()
    {
        //Debug.Log("计算");
        Add();
    }
    private void Add()
    {
        Debug.Log("加法");
    }
    private void Subtract()
    {
        Debug.Log("减法");
    }
}
