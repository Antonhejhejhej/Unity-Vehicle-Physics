using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealTimeReflectionProbe : MonoBehaviour
{

    private ReflectionProbe _reflectionProbe;
    
    
    // Start is called before the first frame update
    void Start()
    {
        _reflectionProbe = GetComponent<ReflectionProbe>();
    }

    // Update is called once per frame
    void Update()
    {
        _reflectionProbe.RenderProbe();
    }
}
