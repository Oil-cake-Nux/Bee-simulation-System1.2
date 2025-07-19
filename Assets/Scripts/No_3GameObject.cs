using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：游戏物体
//***************************************** 
public class No_3GameObject : MonoBehaviour
{
    //public No4_MonoBehaviour test;
    public GameObject grisGo; 
    void Start()
    {
        No4_MonoBehaviour No4_MonoBehaviour= gameObject.GetComponent<No4_MonoBehaviour>();

        //test = gameObject.GetComponent<No4_MonoBehaviour>();
        //创建一个名为Mygameobject的游戏物体
        //GameObject myGo = new GameObject("Mygameobject");
        //GameObject.Instantiate(grisGo);
        //grisGo.SetActive(false);
        //Debug.Log("当前脚本挂载的对象是"+gameObject.name);
        //通过名称查找
        //GameObject Camera = GameObject.Find("Main Camera");
        //Debug.Log(Camera.activeSelf);
        //通过标签查找
        //GameObject Camera = GameObject.FindGameObjectWithTag("MainCamera");  
        //Debug.Log(Camera.activeSelf);
        //通过类型查找
        //No2_EventFunction No2_EventFunction = GameObject.FindObjectOfType<No2_EventFunction>();
        //Debug.Log(No2_EventFunction.name);
        //通过标签查找多个游戏物体
        //GameObject[] Enemy = GameObject.FindGameObjectsWithTag("Enemy");
        //for (int i = 0; i < Enemy.Length; i++)
        //{
        //    Debug.Log("敌人的名字为" + Enemy[i].name);
        //}
        //通过类型查找多个游戏物体
        //BoxCollider[] Enemys = GameObject.FindObjectsOfType<BoxCollider>();//查找所以带有BoxCollider组件的物体
        //for (int i = 0; i < Enemys.Length; i++)
        //{
        //    Debug.Log("敌人的名字为" + Enemys[i].name);
        //}
    }

    void Update()
    {
        //Debug.Log("当前组件是否激活并使用Behaviour" + test.isActiveAndEnabled);
    } 
}
