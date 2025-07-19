using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//*****************************************
//创建人： Trigger 
//功能说明：场景管理
//***************************************** 
public class No21_SceneManager : MonoBehaviour
{
    AsyncOperation ao;
    void Start()
    {
        //SceneManager.LoadScene(1);
        //SceneManager.LoadScene("TriggerTest");
        //异步场景加载
        //SceneManager.LoadSceneAsync(1);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(LoadNextAsyncScene());
        }
        if (Input.anyKeyDown && ao.progress >= 0.9f)
        {
            ao.allowSceneActivation = true;
        }
    } 
    //定义协程
    IEnumerator LoadNextAsyncScene()
    {
        //SceneManager.LoadSceneAsync(1);的返回值为AsyncOperation类型
        ao =SceneManager.LoadSceneAsync(1);//在执行该句时场景1就开始加载
        ao.allowSceneActivation =false;//场景加载完毕后不允许自动激活
        //可以通过ao.progress产看当前场景加载进度
        while (ao.progress < 0.9f)
        {
            //当场景加载进度小于90%时，挂起场景，让其一直加载，直到加载基本完成
            yield return null;
        }
    }
}
