using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：动画
//***************************************** 
public class No10_Animator : MonoBehaviour
{
    public Animator animator;
    void Start()
    {
        //animator.Play("Idle");
        ////animator.speed = 1.5f;
        //animator.SetFloat("Speed",1);
        //if (animator.GetFloat("Speed") >= 1)
        //{
        //    animator.Play("Run");
        //}
    }

    void Update()
    {
        //if (Input.GetKey(KeyCode.LeftShift))
        //{
        //    //淡入淡出式的转换为目标动画，转换时间为1整个动画的时间
        //    animator.CrossFade("Run",1);
        //}
        ////else
        ////{
        ////    // 播放 Idle 动画
        ////    animator.Play("Idle");
        ////}
        //if(Input.GetKeyDown(KeyCode.W))
        //{
        //    //淡入淡出式的转换为目标动画，转换时间为1s
        //    animator.CrossFadeInFixedTime("Walk", 1);
        //}
        animator.SetFloat("Speed", Input.GetAxis("Horizontal"));
    } 
}
