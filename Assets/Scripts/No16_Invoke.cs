using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：延迟调用
//***************************************** 
public class No16_Invote : MonoBehaviour
{
    public GameObject Grisgo;
    void Start()
    {
        //延迟3秒调用CreateGris函数
        //Invoke("CreateGris", 3);
        //功能：重复调用CreateGris函数，第一个1是第一次调用的延迟时间，第二个一是后续重复的延迟时间
        InvokeRepeating("CreateGris", 1, 1);
        //停止
        //CancelInvoke("CreateGris");//停止一个
        //CancelInvoke();//停止所有
    }

    void Update()
    {
        //输出代码中是否有延迟调用CreateGrisgo函数，输出为布尔值
        print(IsInvoking("CreateGris"));
    } 
    private void CreateGris()
    {
        Instantiate(Grisgo);
    }
}
