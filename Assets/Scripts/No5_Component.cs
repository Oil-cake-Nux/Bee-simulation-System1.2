using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：附加到游戏物体的所有内容的基类
//***************************************** 
public class No5_Component : MonoBehaviour
{
    public GameObject Enemygos;
    public GameObject Camera;
    public BoxCollider[] boxColliders;
    void Start()
    {

        
        Enemygos = GameObject.Find("Enemies");
        Camera = GameObject.Find("Main Camera");
        No2_EventFunction No2_EventFunction = this.GetComponent<No2_EventFunction>();
        Debug.Log(No2_EventFunction.attackValue);
        //Debug.Log(No2_EventFunction);
        //Debug.Log(Enemygos.GetComponentInChildren<BoxCollider>());
        ////Debug.Log(Enemygos.GetComponentsInChildren<BoxCollider>());
        //boxColliders=Enemygos.GetComponentsInChildren<BoxCollider>();
        //Debug.Log(boxColliders);
        //Debug.Log(Enemygos.GetComponentInParent<BoxCollider>());
        //通过组件查找
        //GameObject Grisgo = GameObject.Find("Gris");
        //SpriteRenderer sr=Grisgo.GetComponent<SpriteRenderer>();
        //Debug.Log(sr.GetComponent<Transform>());
    }

    void Update()
    {

    } 
}
