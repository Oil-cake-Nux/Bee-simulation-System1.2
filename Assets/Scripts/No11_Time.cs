using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：时间
//***************************************** 
public class No11_Time : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 10;
    }

    void Update()
    {
        //print("完成上一帧所用的时间（以秒为单位）" + Time.deltaTime);
        //print("执行物理或者其他固定帧率更新的时间间隔" + Time.fixedDeltaTime);
        //print("自游戏启动以来的总时间（以物理或者其他固定帧率更新的时间间隔累积计算的）" + Time.fixedTime);
        //print("游戏启动以来的总时间" + Time.time);
        //print(Time.timeScale+"时间流逝的标度，可以用来控制游戏时间流失的快慢");
        print(Time.timeSinceLevelLoad + "开始加载当前场景以来到目前的时间");
    } 
}
