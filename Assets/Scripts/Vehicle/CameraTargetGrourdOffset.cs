using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTargetGrourdOffset : MonoBehaviour
{

    [SerializeField] private float minimumGroundDistance;

    private Vector3 _startPos;
    
    void Start()
    {
        _startPos = transform.localPosition;
    }

    
    void Update()
    {
        var ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, out var rayHit, 2f))
        {
            var distance = Vector3.Distance(ray.origin, rayHit.point);
            if (distance < minimumGroundDistance)
            {
                var newHeight = rayHit.point + Vector3.up * minimumGroundDistance;

                var localPos = transform.localPosition;

                transform.localPosition = new Vector3(localPos.x,
                    transform.worldToLocalMatrix.MultiplyPoint(newHeight).y, localPos.z);
            }
        }
    }
}
