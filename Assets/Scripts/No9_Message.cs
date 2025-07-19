using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：消息的发送
//***************************************** 
public class No9_Message: MonoBehaviour
{
    void Start()
    {
        //发送消息给游戏物体自己（以及其身上其他MonoBehaviour对象）
        gameObject.SendMessage("GetMsg");
        //广播消息，向下发，所有的子对象，包括自己
        BroadcastMessage("GetMsg");

        SendMessageUpwards("GetMsg");
    }

    void Update()
    {

    }
    public void GetMsg()
    {
        print("发送消息给自己");
    }
}
