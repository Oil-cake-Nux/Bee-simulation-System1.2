using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：
//***************************************** 
public class No6_Transform : MonoBehaviour
{
    Vector3 v3 = new Vector3(0,0,1);
    private float movespeed = 0.03f;
    public GameObject Grisgo;
    void Start()
    {
        //Grisgo = GameObject.Find("Gris");
        //Debug.Log(this.transform);
        //Debug.Log(Grisgo.transform);
        Transform Gris_tf = Grisgo.transform;
        //Debug.Log(Gris_tf.name);
        //Debug.Log(Gris_tf.gameObject);
        //Debug.Log("Gris下子对象的个数是" + Gris_tf.childCount);
        //Debug.Log("Gris在世界坐标下的位置是" + Gris_tf.position);
        //Debug.Log("Gris以四元数表示的旋转是" + Gris_tf.rotation);
        //Debug.Log("Gris以欧拉角形式（度数形式）表示的旋转是" + Gris_tf.eulerAngles);
        //Debug.Log("显示Gris的父级的Transform:" + Gris_tf.parent);
        //Debug.Log("Gris的相对于父亲的坐标为:" + Gris_tf.localPosition);
        //Debug.Log("Gris的相对于父亲的以四元数表示的旋转为:" + Gris_tf.localRotation);
        //Debug.Log("Gris的相对于父亲的欧拉角形式（度数形式）表示的旋转为:" + Gris_tf.localEulerAngles);
        //Debug.Log("相对于父亲的缩放" + Gris_tf.localScale);
        //Debug.Log("Gris的自身坐标的正前方(Z轴方向)：" + Gris_tf.forward);
        //Debug.Log("Gris的自身坐标的正右方(X轴方向)：" + Gris_tf.right);
        //Debug.Log("Gris的自身坐标的正上方(Y轴方向)：" + Gris_tf.up);
        //Debug.Log("当前组件所挂载的对象的名为Gris的子对象的Transform组件为：" + transform.Find("Gris"));
        //Debug.Log("当前组件所挂载的对象的第一个（0号索引）子对象的Transform组件为：" + transform.GetChild(0));
        //Debug.Log("Gris在其父对象的同级子对象中的索引号为：" + Gris_tf.GetSiblingIndex());
    }

    void Update()
    {
        //以自身坐标系方向进行移动
        /*Grisgo.transform.Translate(1, 1, 1);*///依据自身坐标系方向1，1,1的速度移动
        //Grisgo.transform.Translate(Vector2.left);
        //Grisgo.transform.Translate(Vector2.left * movespeed,Space.Self);
        //Grisgo.transform.Translate(Grisgo.transform.right*movespeed,Space.World);
        //以世界坐标系方向进行移动
        //Grisgo.transform.Translate(Vector2.left * movespeed, Space.World);
        //Grisgo.transform.Translate(Grisgo.transform.right*movespeed);
        //以自身坐标方向，v3为（0,0,1),因此是z轴朝向时，绕着Z轴顺时针旋转为其旋转方向
        //Grisgo.transform.Rotate(v3);
        //绕着某个轴进行旋转，每次选择一度
        Grisgo.transform.Rotate(Vector3.forward, 1);

    }
    //void FixedUpdate()
    //{
    //    // 绕局部坐标系的 Z 轴旋转，每秒50度
    //    Grisgo.transform.Rotate(Vector3.forward,1);
    //}
}
