using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：
//***************************************** 
public class No20_Resources : MonoBehaviour
{
    void Start()
    {
        //Debug.Log(Resources.Load<AudioClip>("sound"));
        //Debug.Log(Resources.Load<MonoScript>("TestScript"));
        //加载完毕会返回一个AudioClip类型的变量
        //Resources.Load<AudioClip>("sound");
        //Resources.Load<MonoScript>("TestScript");
        //AudioClip clip = Resources.Load<AudioClip>("sound");
        //AudioSource.PlayClipAtPoint(clip,gameObject.transform.position);
        //Instantiate(Resources.Load<GameObject>("Prefabs/Gris"),transform.position, Quaternion.identity);
        //Object obj = Resources.Load("sound");
        //AudioClip aud=obj as AudioClip;//或强转AudioClip aud=(AudioClip)obj;
        //AudioSource.PlayClipAtPoint(aud,transform.position);
        //加载子目录Prefabs下的所有AudioClip类型的对象，返回值为AudioClip数组
        //Resources.LoadAll<AudioClip>("Prefabs");
        AudioClip[] audioClips=Resources.LoadAll<AudioClip>("");
        foreach (var item in audioClips)
        {
            Debug.Log(item);
        }
    }

    void Update()
    {

    } 
}
