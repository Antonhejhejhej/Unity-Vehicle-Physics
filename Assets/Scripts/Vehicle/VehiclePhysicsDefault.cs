using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehiclePhysicsDefault : MonoBehaviour
{
    [Header("General")] public bool ignition = true;

    [Header("Steering(Controls)")]
    [SerializeField] private bool _4WD;
    [SerializeField] private float engineStrength;

    [SerializeField] private float turboStrength;
    public AnimationCurve torqueDistribution;
    [SerializeField, Tooltip("km/h")] private float topSpeed;
    [SerializeField] private float breakingForce;
    [SerializeField] private float turningRadius;
    [SerializeField] private AnimationCurve steeringStability;
    [SerializeField] private float steeringSmoothing;
    
    

    [Header("Suspension")] [SerializeField]
    private float springStrength;

    [SerializeField] private float springDamping;
    [SerializeField] private float tireRadius;
    [SerializeField] private float rideHeight;

     [Header("Tire Grip")]
    
    [SerializeField] private AnimationCurve gripBySpeed;
    
    [SerializeField] private float tireMass;

    [SerializeField] private float frontGripFactor;
    [SerializeField] private float rearGripFactor;
    [SerializeField] private AnimationCurve frontGripDistribution;
    [SerializeField] private AnimationCurve rearGripDistribution;
    


    [Header("Spring origins")] [SerializeField]
    private Transform frontR;

    [SerializeField] private Transform frontL;
    [SerializeField] private Transform rearR;
    [SerializeField] private Transform rearL;

    [Header("Wheels")] [SerializeField] private Transform frontRwheel;
    [SerializeField] private Transform frontLwheel;
    [SerializeField] private Transform rearRwheel;
    [SerializeField] private Transform rearLwheel;

    //Wheel rigidbodies
    private Rigidbody _frontRbody;
    private Rigidbody _frontLbody;
    private Rigidbody _rearRbody;
    private Rigidbody _rearLbody;

    private Rigidbody[] _wheelBodies;
    

    //Input
    private float _wheelAngle;
    private float _gasForce;
    [HideInInspector] public bool isBreaking;

    //Acceleration
    private Vector3 _accelerationDir;
    private float _carSpeed;
    [HideInInspector] public float normalizedSpeed;
    private float availableTorque;
    private float currentStrength;

    //Physics General
    private Rigidbody _rigidbody;

    //Suspension
    private Ray _ray;
    private RaycastHit _rayHit;
    private Vector3 _tirePos;
    private Vector3 _springDir;
    private Vector3 _tireWorldVel;
    private Vector3 _normalizedTireWorldVel;
    private float _offset;
    private float _velocity;
    private float _force;
    private Transform[] _tireArray;

    //Traction
    private Vector3 steeringRight;
    private float steeringVel;
    private float desiredSteeringVelChange;
    private float desiredSteeringAccel;
    private float totalGrip;

    //Breaking
    private float breakingVel;
    private float desiredBreakVelChange;
    private float desiredBreakAccel;

    //Wheels positioning
    private Transform[] _wheelArray;

    private Vector3 _wheelPos;

    //SmoothDamp wheel position
    private Vector3 _currentWheelPos;
    private Vector3 _refVelocityWheel;
    private Vector3 _wheelTarget;

    //ResetPosition
    private Vector3 startPos;
    private Quaternion startRot;

    //Ackerman steering
    private float _wheelBase;
    private float _trackWidth;
    private float ackermanAngleRight;
    private float ackermanAngleLeft;

    //SteeringSmoothdamp
    private float smoothInput;
    private float refVelocity;


    private void OnValidate()
    {
    }


    private void Awake()
    {
        topSpeed /= 3.6f; //convert to m/s
        _rigidbody = GetComponent<Rigidbody>();
        _tireArray = new Transform[] {frontR, frontL, rearR, rearL};
        _wheelArray = new Transform[] {frontRwheel, frontLwheel, rearRwheel, rearLwheel};
        _wheelBodies = new Rigidbody[4];
        for (var i = 0; i < 4; i++)
        {
            _wheelBodies[i] = _tireArray[i].gameObject.GetComponent<Rigidbody>();
        }
    }


    void Start()
    {
        normalizedSpeed = 0f;
        startPos = _rigidbody.position;
        startRot = _rigidbody.rotation;

        //Ackerman
        _wheelBase = Vector3.Distance(frontR.position, rearR.position);
        _trackWidth = Vector3.Distance(rearL.position, rearR.position);
    }


    private void Update()
    {
        CarInput();

        if (Input.GetKeyDown(KeyCode.R))
        {
            _rigidbody.position = startPos;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.rotation = startRot;
        }
    }


    void FixedUpdate()
    {
        Steering();
        CarPhysics();
        
        Debug.Log(normalizedSpeed);
    }


    private void CarPhysics()
    {
        var i = 0;

        foreach (var tireTransform in _tireArray)
        {
            var currentBody = _wheelBodies[i];
            
            _currentWheelPos = _wheelArray[i].position;

            _wheelArray[i].rotation = tireTransform.rotation;

            _tirePos = tireTransform.position;

            _ray = new Ray(_tirePos, -tireTransform.up);

            if (_wheelBodies[i].SweepTest(-_tireArray[i].up, out var sweepHit, rideHeight))
            {
                var wheelPos = GetWheelPos(tireTransform, _tirePos, sweepHit);
                
                _springDir = tireTransform.up;

                _tireWorldVel = _rigidbody.GetPointVelocity(_tirePos);

                _normalizedTireWorldVel = _tireWorldVel.normalized;

                _offset = rideHeight + tireRadius - Vector3.Distance(_tirePos, wheelPos);

                var gripOffsetFactor = (_offset / (rideHeight + tireRadius));


                _velocity = Vector3.Dot(_springDir, _tireWorldVel);

                _force = (_offset * springStrength) - (_velocity * springDamping);

                var desiredSpringForce = _springDir * _force;

                if (!float.IsNaN(desiredSpringForce.x) && !float.IsNaN(desiredSpringForce.y) &&
                    !float.IsNaN(desiredSpringForce.z))
                {
                    _rigidbody.AddForceAtPosition(_springDir * _force, _tirePos);
                }

                

                //Traction
                steeringRight = tireTransform.right;

                steeringVel = Vector3.Dot(steeringRight, _tireWorldVel);

                totalGrip = 0f;

                var gripBySpeedNormalized = gripBySpeed.Evaluate(normalizedSpeed);

                if (i < 2)
                {
                    //var speedFactor = distributionBySpeed.Evaluate(_normalizedSpeed);


                    if (isBreaking)
                    {
                        totalGrip = frontGripDistribution.Evaluate(Mathf.Abs(Vector3.Dot(_normalizedTireWorldVel,
                                        steeringRight))) *
                                    frontGripFactor;
                    }
                    else
                    {
                        totalGrip = frontGripDistribution.Evaluate(Mathf.Abs(Vector3.Dot(_normalizedTireWorldVel,
                                        steeringRight))) *
                                    frontGripFactor;
                        
                    }

                    totalGrip *= gripBySpeedNormalized; 

                    desiredSteeringVelChange = -steeringVel * totalGrip * gripOffsetFactor;
                }
                else
                {
                    if (isBreaking)
                    {
                        totalGrip = rearGripDistribution.Evaluate(Mathf.Abs(Vector3.Dot(_normalizedTireWorldVel,
                                        steeringRight))) *
                                    rearGripFactor;
                    }
                    else
                    {
                        totalGrip = rearGripDistribution.Evaluate(Mathf.Abs(Vector3.Dot(_normalizedTireWorldVel,
                                        steeringRight))) *
                                    rearGripFactor;

                    }
                    
                    totalGrip *= gripBySpeedNormalized; 

                    desiredSteeringVelChange = -steeringVel * totalGrip * gripOffsetFactor;
                }


                desiredSteeringAccel = desiredSteeringVelChange / Time.fixedDeltaTime;

                var desiredForce = steeringRight * tireMass * desiredSteeringAccel;

                if (!float.IsNaN(desiredForce.x) && !float.IsNaN(desiredForce.y) && !float.IsNaN(desiredForce.z))
                {
                    _rigidbody.AddForceAtPosition(steeringRight * tireMass * desiredSteeringAccel, _tirePos);
                }

                GasAndBreak(tireTransform, i, gripOffsetFactor);

                //WheelPositioning
                
                if (!float.IsNaN(wheelPos.x) && !float.IsNaN(wheelPos.y) && !float.IsNaN(wheelPos.z))
                {
                    _wheelArray[i].position = Vector3.SmoothDamp(_currentWheelPos, wheelPos,
                        ref _refVelocityWheel, .02f);
                }
                else
                {
                    _wheelTarget = _tirePos + (-transform.up * (rideHeight + tireRadius * 0.5f));

                    _wheelArray[i].position =
                        Vector3.SmoothDamp(_currentWheelPos, _wheelTarget, ref _refVelocityWheel, .05f);
                }
                
            }
            else
            {
                _wheelTarget = _tirePos + (-transform.up * (rideHeight + tireRadius * 0.5f));

                _wheelArray[i].position =
                    Vector3.SmoothDamp(_currentWheelPos, _wheelTarget, ref _refVelocityWheel, .05f);
            }

            /*if (Physics.Raycast(_ray, out _rayHit, rideHeight + tireRadius))
            {
                _springDir = tireTransform.up;

                _tireWorldVel = _rigidbody.GetPointVelocity(_tirePos);

                _normalizedTireWorldVel = _tireWorldVel.normalized;

                _offset = rideHeight + tireRadius - _rayHit.distance;

                var gripOffsetFactor = .75f + (_offset / rideHeight);

                _velocity = Vector3.Dot(_springDir, _tireWorldVel);

                _force = (_offset * springStrength) - (_velocity * springDamping);

                _rigidbody.AddForceAtPosition(_springDir * _force, _tirePos);

                //Traction
                steeringRight = tireTransform.right;

                steeringVel = Vector3.Dot(steeringRight, _tireWorldVel);

                totalGrip = 0f;

                if (i < 2)
                {
                    //var speedFactor = distributionBySpeed.Evaluate(_normalizedSpeed);


                    if (isBreaking)
                    {
                        totalGrip = frontGripDistribution.Evaluate(Mathf.Abs(Vector3.Dot(_normalizedTireWorldVel,
                                        steeringRight))) *
                                    frontGripFactor;
                    }
                    else
                    {
                        totalGrip = frontGripDistribution.Evaluate(Mathf.Abs(Vector3.Dot(_normalizedTireWorldVel,
                                        steeringRight))) *
                                    frontGripFactor;
                    }

                    desiredSteeringVelChange = -steeringVel * totalGrip * gripOffsetFactor;
                }
                else
                {
                    if (isBreaking)
                    {
                        totalGrip = rearGripDistribution.Evaluate(Mathf.Abs(Vector3.Dot(_normalizedTireWorldVel,
                                        steeringRight))) *
                                    rearGripFactor;
                    }
                    else
                    {
                        totalGrip = rearGripDistribution.Evaluate(Mathf.Abs(Vector3.Dot(_normalizedTireWorldVel,
                                        steeringRight))) *
                                    rearGripFactor;
                        
                    }

                    desiredSteeringVelChange = -steeringVel * totalGrip * gripOffsetFactor;
                }


                desiredSteeringAccel = desiredSteeringVelChange / Time.fixedDeltaTime;

                _rigidbody.AddForceAtPosition(steeringRight * tireMass * desiredSteeringAccel, _tirePos);

                GasAndBreak(tireTransform, i);

                //WheelPositioning
                //_wheelArray[i].position = _rayHit.point + _springDir * tireRadius * 0.5f;
                _wheelArray[i].position =
                    Vector3.SmoothDamp(_currentWheelPos, _rayHit.point + _springDir * tireRadius * 0.5f,
                        ref _refVelocityWheel, .02f);
            }
            else
            {
                _wheelTarget = _tirePos + (-transform.up * (rideHeight + tireRadius * 0.5f));

                _wheelArray[i].position =
                    Vector3.SmoothDamp(_currentWheelPos, _wheelTarget, ref _refVelocityWheel, .05f);
            }*/

            i++;
        }
    }

    private void Steering()
    {
        //smoothInput = steeringStability.Evaluate(_normalizedSpeed) * smoothInput;
        //var angle = Mathf.Abs(_wheelAngle);

        if (smoothInput > 0f)
        {
            ackermanAngleLeft = Mathf.Rad2Deg * Mathf.Atan(_wheelBase / (turningRadius + (_trackWidth / 2))) *
                                smoothInput;
            ackermanAngleRight = Mathf.Rad2Deg * Mathf.Atan(_wheelBase / (turningRadius - (_trackWidth / 2))) *
                                 smoothInput;
        }
        else if (smoothInput < -0f)
        {
            ackermanAngleLeft = Mathf.Rad2Deg * Mathf.Atan(_wheelBase / (turningRadius - (_trackWidth / 2))) *
                                smoothInput;
            ackermanAngleRight = Mathf.Rad2Deg * Mathf.Atan(_wheelBase / (turningRadius + (_trackWidth / 2))) *
                                 smoothInput;
        }
        else if (smoothInput == 0)
        {
            ackermanAngleLeft = 0f;
            ackermanAngleRight = 0f;
        }


        frontR.localRotation = Quaternion.Euler(0, ackermanAngleRight, 0);
        frontL.localRotation = Quaternion.Euler(0, ackermanAngleLeft, 0);
    }

    private void GasAndBreak(Transform tireTrans, int i, float gripOffsetFactor)
    {
        _accelerationDir = tireTrans.forward;


        if (isBreaking)
        {
            breakingVel = Vector3.Dot(_accelerationDir, _tireWorldVel);

            desiredBreakVelChange = -breakingVel * breakingForce;

            desiredBreakAccel = desiredBreakVelChange / Time.fixedDeltaTime;

            _rigidbody.AddForceAtPosition(_accelerationDir * 0.1f * desiredBreakAccel * gripOffsetFactor, _tirePos);
        }

        /*if (_gasForce is > -0.1f and < 0.1f)
        {
            desiredBreakVelChange = -breakingVel * automaticDeceleration;

            desiredBreakAccel = desiredBreakVelChange / Time.fixedDeltaTime;
                
            _rigidbody.AddForceAtPosition(_accelerationDir * 0.1f * desiredBreakAccel, _tirePos);
        }*/


        _carSpeed = Vector3.Dot(transform.forward, _rigidbody.velocity);

        normalizedSpeed = Mathf.Clamp01(Mathf.Abs(_carSpeed) / topSpeed);

        availableTorque = torqueDistribution.Evaluate(normalizedSpeed) * _gasForce * currentStrength * gripOffsetFactor;


        if (!_4WD)
        {
            if (i is < 2 or > 3) return;
        }
        


        if (_gasForce < 0.0f && !isBreaking || _gasForce > 0.0f && !isBreaking)
        {
            _rigidbody.AddForceAtPosition(_accelerationDir * availableTorque, tireTrans.position);
        }


    }


    private void CarInput()
    {
        if (!ignition) return;

        _wheelAngle = Input.GetAxis("Horizontal") * steeringStability.Evaluate(normalizedSpeed);

        smoothInput = Mathf.SmoothDamp(smoothInput, _wheelAngle, ref refVelocity, steeringSmoothing);

        _gasForce = Input.GetAxis("Vertical");

        isBreaking = Input.GetKey(KeyCode.Space);

        currentStrength = Input.GetKey(KeyCode.LeftShift) ? turboStrength : engineStrength;
    }

    private Vector3 GetWheelPos(Transform tireTransform, Vector3 tirePos, RaycastHit sweep)
    {
        var localTirePos = tireTransform.InverseTransformPoint(tirePos);
        var localHitPoint = tireTransform.InverseTransformPoint(sweep.point); 
        
        var C = Vector3.Distance(localTirePos, localHitPoint);
        var A = Vector3.Distance(new Vector3(localTirePos.x, 0, localTirePos.z),
            new Vector3(localHitPoint.x, 0, localHitPoint.z));
        var B = Mathf.Sqrt(C * C - A * A);

        var b = Mathf.Sqrt(tireRadius * tireRadius - A * A);

        return tirePos - tireTransform.up * (B - b);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (frontL == null || frontR == null || rearL == null || rearR == null) return;

        Gizmos.DrawLine(frontL.position, frontL.position + (-transform.up * (rideHeight + tireRadius)));
        Gizmos.DrawLine(frontR.position, frontR.position + (-transform.up * (rideHeight + tireRadius)));
        Gizmos.DrawLine(rearL.position, rearL.position + (-transform.up * (rideHeight + tireRadius)));
        Gizmos.DrawLine(rearR.position, rearR.position + (-transform.up * (rideHeight + tireRadius)));
    }
}