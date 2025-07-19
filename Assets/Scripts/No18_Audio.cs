using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：播放音频的类
//***************************************** 
public class No18_Audio : MonoBehaviour
{
    private bool pausestate = false;
    public AudioClip music;
    public AudioClip sound;
    private AudioSource audioSource;
    void Start()
    {
        audioSource =GetComponent<AudioSource>();
        audioSource.clip=music;
        audioSource.Play();
        //从音乐的第三秒开始播放
        audioSource.time = 3;
        
    }

    void Update()
    {
        //mute仅是将其静音
        if (audioSource.mute == false&&Input.GetKeyDown(KeyCode.B))
        {
            audioSource.mute = true;
        }
        else if (audioSource.mute == true && Input.GetKeyDown(KeyCode.B))
        {
            audioSource.mute = false;
        }
        //暂停
        if (Input.GetKeyDown(KeyCode.P))
        {
            pausestate = !pausestate;
            if (pausestate)
            {
                audioSource.Pause();
            }
            else
            {
                audioSource.UnPause();
            }
        }
        //停止，只有play可以从头开始播放
        if (Input.GetKeyDown(KeyCode.S))
        {
            audioSource.Stop();
        }
        //摁下k键播放一次sound音频，声音规模为1
        if (Input.GetKeyDown(KeyCode.K))
        {
            //AudioSource.PlayOneShot(sound, 1);
            //生成一个音频对象在transform.position坐标处，播放一次后消失（耳朵在MainCamera处）
            AudioSource.PlayClipAtPoint(sound,transform.position);
        }

    } 
}
