using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：
//***************************************** 
public class No19_Collider : MonoBehaviour
{
    //public GameObject test;
    void Start()
    {
        //test = GameObject.Find("19_Collider");
        //test.AddComponent<No18_Audio>();
        
    }

    void Update()
    {

    }
    //碰撞检测，要有受力碰撞，穿模情况下不会实现
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //输出与自己相撞的物体的名字
        Debug.Log(collision.gameObject.name);
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        Debug.Log("在碰撞器里");
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        Debug.Log("离开碰撞检测");
    }
    //触发检测，只有穿模情况下也可实现，受力碰撞不可以
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.gameObject.name+"穿模");
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        Debug.Log("在触发器里");
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log("从触发器里移出");
    }
}
