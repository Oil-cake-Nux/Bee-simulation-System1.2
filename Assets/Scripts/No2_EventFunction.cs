using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：时间函数（生命周期函数）
//***************************************** 
public class No2_EventFunction : MonoBehaviour
{
    public float attackValue;
    public float currentHP;
    private void Reset()
    {
        //Debug.Log("调用reset");
        //Attackalue = 10;
    }
    private void Awake()
    {
        //Debug.Log("调用Awake");
        attackValue = 1;
    }
    private void OnEnable()
    {
        Debug.Log("调用OnEnable");
        attackValue = 2;
    }
    void Start()    
    {
        attackValue = 3; 
    }

    void Update()
    {
        //Debug.Log("调用Update");
    }
    private void LateUpdate()
    {
        //Debug.Log("调用LateUpdate");
    }
    private void OnDisable()
    {
        
    }
    private void OnApplicationQuit()
    {
        
    }
    private void OnDestroy()
    {
        
    }
}
