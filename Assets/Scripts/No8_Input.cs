using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：访问输入系统的接口类
//***************************************** 
public class No8_Input : MonoBehaviour
{
    void Start()
    {

    }

    void Update()
    {
        ////连续检测（移动）
        ////快速的逐渐变化到1或-1
        //Debug.Log("玩家输入的水平轴为：" + Input.GetAxis("Horizontal"));
        //Debug.Log("玩家输入的垂直轴为：" + Input.GetAxis("Vertical"));
        ////瞬间变化为1或-1
        //Debug.Log("玩家输入的边界水平轴为：" + Input.GetAxisRaw("Horizontal"));
        //Debug.Log("玩家输入的边界垂直轴为：" + Input.GetAxisRaw("Vertical"));
        //Debug.Log("玩家鼠标在X轴移动的变化量为：" + Input.GetAxis("Mouse X"));
        //Debug.Log("玩家鼠标移动的Y轴的变化量为：" + Input.GetAxis("Mouse Y"));
        //连续检测（事件）
        if (Input.GetButton("Fire1"))
        {
            Debug.Log("玩家正在攻击");
        }
        //间隔检测（事件）
        if (Input.GetButtonDown("Jump"))
        {
            Debug.Log("玩家按下跳跃键");
        }
        if (Input.GetButtonUp("Jump"))
        {
            Debug.Log("玩家松开蹲下键");
        }
        if(Input.GetKeyDown(KeyCode.Q))
        {
            print("当前玩家按下了Q键");
        }
        if (Input.anyKeyDown)
        {
            print("当前玩家按下了任意一个按键，游戏开始");
        }
        if (Input.GetMouseButton(0))
        {
            print("玩家按住了鼠标左键");
        }
        if(Input.GetMouseButtonDown(1))
        {
            print("玩家按下了鼠标右键");
        }
        if (Input.GetMouseButtonUp(2))
        {
            print("玩家松开了鼠标中键");
        }
    } 
}
