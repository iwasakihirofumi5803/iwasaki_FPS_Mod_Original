using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantRotation : MonoBehaviour
{
    [Tooltip("1秒あたりの回転角度")]
    public float rotatingSpeed = 360f;

    void Update()
    {
        //回転を処理します
        transform.Rotate(Vector3.up, rotatingSpeed * Time.deltaTime, Space.Self);
    }
}
