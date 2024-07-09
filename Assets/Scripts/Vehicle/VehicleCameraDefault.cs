using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleCameraDefault : MonoBehaviour
{
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float heightOffset;
    [SerializeField] private Transform lookAt;
    [SerializeField] private float panningMultiplier;
    [SerializeField] private float positionSmoothing;
    [SerializeField] private float rotationSmoothing;
    
    
    //Position & Smoothing
    private Transform targetParent;
    private Vector3 refVelPos;

    //RotationSmoothing
    private float angularVelocity;

    private void Awake()
    {
        Screen.SetResolution(960,540, FullScreenMode.FullScreenWindow);
    }


    void Start()
    {
        targetParent = cameraTarget.parent;
        transform.parent = null;
    }

    
    void Update()
    {
        
        CameraPositionFollow(transform);
        
        CameraRotationFollow();
        
    }

    private void FixedUpdate()
    {
        
    }

    private void CameraPositionFollow(Transform trans)
    {
        var currentPos = trans.position;

        var targetPos = cameraTarget.position;

        var heightAdjustedTargetPos = new Vector3(targetPos.x, targetParent.position.y + heightOffset, targetPos.z);

        var newPos = Vector3.SmoothDamp(currentPos, heightAdjustedTargetPos, ref refVelPos, positionSmoothing);

        transform.position = newPos;
    }

    private void CameraRotationFollow()
    {
        /*Vector3 lookAtOffset = lookAt.right * Input.GetAxis("Horizontal") * panningMultiplier * Input.GetAxis("Vertical");

        var lookAtWithPan = lookAt.position + lookAtOffset + lookAt.forward * 10;*/
        
        var targetRot = Quaternion.LookRotation(lookAt.position - transform.position);
        var delta = Quaternion.Angle(transform.rotation, targetRot);
        if (delta > 0.0f)
        {
            var t = Mathf.SmoothDampAngle(delta, 0.0f, ref angularVelocity, rotationSmoothing);
            t = 1.0f - t/delta;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
        }
    }
    
}
