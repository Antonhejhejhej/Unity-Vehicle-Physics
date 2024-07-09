using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarAudio : MonoBehaviour
{

    [Header("AudioSettings")]
    [SerializeField] private AnimationCurve rpmMinToMax;

    [Header("AudioClips")]
    [SerializeField] private AudioClip engineLoop;
    
    

    private AudioSource _audioSource;
    private VehiclePhysicsDefault _carPhysics;
    
    
    private AnimationCurve _torqueDistribution;
    private float _normalizedSpeed;
    private float currentRpm;
    private float refRpm;


    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _carPhysics = GetComponent<VehiclePhysicsDefault>();
        _torqueDistribution = _carPhysics.torqueDistribution;

        _audioSource.clip = engineLoop;
        _audioSource.loop = true;
        _audioSource.Play();
    }

    void Start()
    {
        
    }

    
    void Update()
    {
        _normalizedSpeed = _carPhysics.normalizedSpeed;

        if (Input.GetAxisRaw("Vertical") != 0f && !_carPhysics.isBreaking && _carPhysics.ignition)
        {
            currentRpm = Mathf.SmoothDamp(currentRpm, (_normalizedSpeed + _torqueDistribution.Evaluate(_normalizedSpeed)) * .5f,
                ref refRpm, .1f);
        }
        else
        {
            currentRpm = Mathf.SmoothDamp(currentRpm, 0,
                ref refRpm, .3f);
        }

        
        
        _audioSource.pitch = rpmMinToMax.Evaluate(currentRpm);

    }
}
