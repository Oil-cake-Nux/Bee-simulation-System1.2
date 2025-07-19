using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：
//***************************************** 
public class No9_MesssageTest : MonoBehaviour
{
    void Start()
    {
        //gameObject.SendMessage("GetMsg");
    }

    void Update()
    {

    }
    public void GetMsg()
    {
        print("发送消息给其它的组件");
    }
}
