using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：
//***************************************** 
public class No14_OnMouseEvenFunction : MonoBehaviour
{
    private void OnMouseDown()
    {
        print("在Gris上按下了鼠标");//仅仅鼠标左键
    }
    private void OnMouseUp()
    {
        print("在Gris上按下的鼠标抬起了");
    }
    private void OnMouseDrag()
    {
        print("在Gris身上用鼠标进行拖拽");
    }
    private void OnMouseEnter()
    {
        print("鼠标移入了Gris上");
    }
    private void OnMouseExit() {
        print("鼠标移出了Gris");
    }
    private void OnMouseOver()
    {
        print("鼠标悬停在了Gris上");
    }
    private void OnMouseUpAsButton()
    {
        print("在Gris上按下的鼠标在Gris上松开了");
    }
}
