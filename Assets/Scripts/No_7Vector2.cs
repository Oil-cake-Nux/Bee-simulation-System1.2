using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Trigger 
//功能说明：2D向量
//***************************************** 
public class No_7Vector2 : MonoBehaviour
{
    public Transform Gristf;
    public Transform Tragettf;
    public float percent;
    public float lerpspeed;
    Vector2 currentvelocity = new Vector2(1, 0);
    public struct Mystruct
    {
        public string name;
        public int age;
        public override string ToString()
        {

            return $"({name}, {age})";
        }
    }
    void Start()
    {
        //静态变量
        //Debug.Log(Vector2.up);//y轴正方xiang
        //Debug.Log(transform.up);//三维向量y轴正方向
        //Debug.Log(Vector2.down);
        //Debug.Log(Vector2.left);
        //Debug.Log(Vector2.right);//x轴正方向
        //Debug.Log(Vector2.one);//单位向量
        //Debug.Log(Vector2.zero);//零向量
        ////构造函数
        //Vector2 v2 = new Vector2(2, 2);
        //print(v2);
        //print("v2向量的模长为："+v2.magnitude);
        //print("v2向量的模长的平方为："+v2.sqrMagnitude);
        //print("v2向量单位化："+v2.normalized);
        ////v2=v2.normalized;
        //print("v2向量的XY值分别是："+v2.x+","+v2.y);
        //print("v2向量的XY值(使用索引器访问)分别是："+v2[0]+","+v2[1]);
        //bool v2e = v2.Equals(new Vector2(1, 1));
        //print(v2e);
        //v2.Normalize();//将v2变成单位向量
        //print("v2向量为：" + v2);
        //v2.Set(23, 4);//设置v2向量的值
        //transform.position = v2;      
        //Mystruct mystruct = new Mystruct();
        //mystruct.name = "Tigger";
        //mystruct.age = 21;
        //Mystruct youstruct=mystruct;
        //youstruct.age = 18;
        //youstruct.name = "伞兵";
        //print(mystruct);
        ////修改位置
        //v2.Set(23, 4);//设置v2向量的值
        //transform.position = v2;
        ////静态函数
        //Vector2 va=new Vector2(1,0);
        //Vector2 vb = new Vector2(0,1);
        //print("va、vb两向量间的无符号夹角为：" +Vector2.Angle(va,vb));
        //print("va、vb两点间的距离为：" +Vector2.Distance(va,vb));
        //print("va、vb中更大的x值和更大的y值来组成一个新的向量为：" + Vector2.Max(va, vb));
        //print("va、vb中更小的x值和更小的y值来组成一个新的向量为：" + Vector2.Min(va, vb));
        //print("取va、vb向量差值的0.5倍与va相加得来的新向量(线性差值)" + Vector2.Lerp(va, vb, 0.5f));//等于va+(vb-va)*t，t属于0-1
        //print("取va、vb向量差值的-1倍与va相加得来的新向量(线性差值)" + Vector2.LerpUnclamped(va, vb, -1));//等于va+(vb-va)*t，t属于整个实数域
        //print("将va以不超过0.5f的移动步频移向vb" + Vector2.MoveTowards(va, vb, 0.5f));
        //print("va到vb的有符号角度（以逆时针为正）为：" + Vector2.SignedAngle(va, vb));
        //print("va和vb在x、y方向上的分量相乘得到的新向量为：" + Vector2.Scale(va, vb));
       

    }

    void Update()
    {
        //percent += lerpspeed*Time.deltaTime;//以每秒为单位进行插值
        //Gristf.position = Vector2.Lerp(Gristf.position, Tragettf.position, percent);

        //Gristf.position=Vector2.MoveTowards(Gristf.position,Tragettf.position,0.01f);

        
        Gristf.position = Vector2.SmoothDamp(Gristf.position, Tragettf.position, ref currentvelocity, 1f);//Gristf平滑过度到目标位置

    } 
}
