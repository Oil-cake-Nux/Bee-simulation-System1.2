using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
//*****************************************
//创建人： Trigger 
//功能说明：2D刚体（通过物理模拟控制物体的位置）
//***************************************** 
public class No17_Rigidbody : MonoBehaviour
{
    private float angle = 90;
    private Rigidbody2D rd;
    //private float movespeed=1;
    void Start()
    {
        //获取子物体的刚体组件
        rd = GetComponentInChildren<Rigidbody2D>();
        print(rd.position);
        print(rd.gravityScale + ",该物体所受重力影响的程度");
        //对物体施加一个大小为10方向为水平向右的力（保持不变的力）
        //rd.AddForce(Vector2.right * 10);
        //对物体施加一个瞬时力
        //rd.AddForce(Vector2.right * 5,ForceMode2D.Impulse);
        //直接作用到刚体的速度上(只给一个初速度)
        //rd.velocity = Vector2.right * movespeed;
        //旋转九十度(逆时针)
        //rd.MoveRotation(90);
    }
    //物理系统应该放在`FixedUpdate`里
    void FixedUpdate()
    {
        //控制Gris物体随时间水平向右上移动，s+△s
        //rd.MovePosition(rd.position + Vector2.right * movespeed * Time.fixedDeltaTime+ Vector2.up * movespeed * Time.fixedDeltaTime);
        //不停地旋转，∠+△∠
        //rd.MoveRotation(rd.rotation+angle*Time.fixedDeltaTime);
    } 
}
