using System.Collections;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：协程（协同程序）：程序顺序执行，面对非常耗时的进程在这个协程中执行异步操作，比如下载文件。
//***************************************** 
public class No15_Coroutine : MonoBehaviour
{
    //public GameObject Gris;
    public Animator animator;
    public int grisnum=0;
    void Start()
    {
        //协程的开启与结束
        IEnumerator ie = ChangeState();
        StartCoroutine(ie);
        StopCoroutine(ie);
        StopAllCoroutines();//暴力停止所有协程
    }

    void Update()
    {

    }
    //使用协程克隆出五个Gris物体
    IEnumerator CreateGris()
    {
        while (true)
        {
            if (grisnum >= 5)
            {
                yield break;
            }
            Instantiate(animator.gameObject);
            grisnum++;
        }
    } 
    IEnumerator ChangeState()
    {
        yield return new WaitForSeconds(2);//暂停几秒（挂起协程）
        animator.Play("Walk");
        yield return new WaitForSeconds(3);
        animator.Play("Run");
        //以下均为等待一帧
        yield return null;
        yield return 1;
        yield return 20281;
    }
}
