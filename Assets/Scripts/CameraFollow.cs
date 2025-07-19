using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("目标设置")]
    public Transform target; // 蜜蜂的Transform
    public Vector3 worldOffset = new Vector3(0f, 5f, -10f); // 相机在世界坐标系下相对于目标的偏移量

    [Header("跟随参数")]
    public float followSpeed = 5f; // 跟随速度
    public float lookAtSpeed = 10f; // 看向目标的速度
    public bool lockRotation = true; // 是否锁定相机旋转

    [Header("平滑参数")]
    public bool smoothFollow = true; // 是否启用平滑跟随
    public bool lookAtTarget = true; // 是否始终看向目标

    [Header("边界限制")]
    public bool useBounds = false; // 是否使用边界限制
    public Vector3 minBounds = new Vector3(-100f, 0f, -100f);
    public Vector3 maxBounds = new Vector3(100f, 50f, 100f);

    private Vector3 desiredPosition; // 期望的相机位置
    private Quaternion initialRotation; // 相机的初始旋转

    void Start()
    {
        // 记录相机的初始旋转
        initialRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 计算相机在世界坐标系下的目标位置
        desiredPosition = target.position + worldOffset;

        // 应用边界限制（如果启用）
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minBounds.z, maxBounds.z);
        }

        // 平滑移动相机到目标位置
        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = desiredPosition;
        }

        // 锁定相机旋转或看向目标
        if (lockRotation)
        {
            transform.rotation = initialRotation;
        }
        else if (lookAtTarget)
        {
            Vector3 lookPos = target.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookAtSpeed * Time.deltaTime);
        }
    }

    // 提供公共方法允许其他脚本改变相机偏移
    public void SetCameraOffset(Vector3 newOffset)
    {
        worldOffset = newOffset;
    }
}