using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：用于外部测试或外部赋值时的顺序
//***************************************** 
public class NewBehaviourScript : MonoBehaviour
{
    
    public No2_EventFunction api;//引用No2_EventFunction，可以对其变量进行赋值
    void Start()
    {

        //api.attackValue = 100;
    }

    void Update()
    {
        
    } 
}
