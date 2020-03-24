using System ;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using LockingPolicy = Thalmic.Myo.LockingPolicy;
using Pose = Thalmic.Myo.Pose;
using UnlockType = Thalmic.Myo.UnlockType;
using VibrationType = Thalmic.Myo.VibrationType;



public class CarController : MonoBehaviour {
    public enum DriveMod { FrontDrive, RearDrive, FullDrive };
    public LogitechGSDK.DIJOYSTATE2ENGINES steerState;

    private Rigidbody rb;
    private float _referenceRoll = 0.0f;
    private Pose _lastPose = Pose.Unknown;

    [Header("Tire Colliders")]
    public WheelCollider FL;
    public WheelCollider FR;
    public WheelCollider RL;
    public WheelCollider RR;

    [Space]

    [Header("Tire Meshes")]
    public GameObject FLMesh;
    public GameObject FRMesh;
    public GameObject RLMesh;
    public GameObject RRMesh;

    [Space]
    [Space]

    [Header("Engine And Car Preferences")]
    public float motorTorque;
    public float breakTorque;
    public int steerAngle;
    public DriveMod driveMod;

    private Shift[] GearBox = new Shift[7];
    private Shift Gear;
    private int tempGear;
    private float currentVelocity=0;
    private float motorTorqueV;
    private JointSpring _springJoint;

    [Space]

    [Header("Visualizing")]
    public Text velocityText;
    public Text gearText;

    [Header("Car Sounds")]
    public AudioClip startUpSound;
    public AudioClip idleSound;
    public AudioClip maxRpm;

    private Camera _carFollower ;

    private AudioSource audioSource;
    private float tempPitch;

    private Vector3 FLWheelCCenter ;
    private Vector3 FRWheelCCenter ;
    private Vector3 RLWheelCCenter ;
    private Vector3 RRWheelCCenter ;
    private RaycastHit hit ;

    private void StartUp()
    {
        audioSource.Stop();
        audioSource.clip = startUpSound;
        audioSource.loop = false;
        audioSource.Play();
    }

    private void Idle()
    {
        audioSource.Stop();
        audioSource.clip = idleSound;
        audioSource.pitch = 0.5f;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void MaxRPM()
    {
        audioSource.Stop();
        audioSource.clip = maxRpm;
        audioSource.pitch = 2.5f;
        audioSource.loop = true;
        audioSource.Play();
    }




    public void VisualizeWheel(WheelCollider FrontLeftC , WheelCollider FrontRightC , WheelCollider RearLeftC , WheelCollider RearRightC ,
                               GameObject FrontLeftM , GameObject FrontRightM , GameObject RearLeftM , GameObject RearRightM)
    {
        
        FLWheelCCenter = FrontLeftC.transform.TransformPoint(FrontLeftC.center);
        FRWheelCCenter = FrontRightC.transform.TransformPoint(FrontRightC.center);
        RLWheelCCenter = RearLeftC.transform.TransformPoint(RearLeftC.center);
        RRWheelCCenter = RearRightC.transform.TransformPoint(RearRightC.center);
 
        if ( Physics.Raycast(FLWheelCCenter, -FrontLeftC.transform.forward, out hit, FrontLeftC.suspensionDistance + FrontLeftC.radius) ) {
            FrontLeftM.transform.position = hit.point + (FrontLeftC.transform.forward * FrontLeftC.radius);
        } else {
            FrontLeftM.transform.position = FLWheelCCenter - (FrontLeftC.transform.forward * FrontLeftC.suspensionDistance);
        }
        
        if ( Physics.Raycast(FRWheelCCenter, -FrontRightC.transform.forward, out hit, FrontRightC.suspensionDistance + FrontRightC.radius) ) {
            FrontRightM.transform.position = hit.point + (FrontRightC.transform.forward * FrontRightC.radius);
        } else {
            FrontRightM.transform.position = FRWheelCCenter - (FrontRightC.transform.forward * FrontRightC.suspensionDistance);
        }
        
        if ( Physics.Raycast(RLWheelCCenter, -RearLeftC.transform.forward, out hit, RearLeftC.suspensionDistance + RearLeftC.radius) ) {
            RearLeftM.transform.position = hit.point + (RearLeftC.transform.forward * RearLeftC.radius);
        } else {
            RearLeftM.transform.position = RLWheelCCenter - (RearLeftC.transform.forward * RearLeftC.suspensionDistance);
        }
        
        if ( Physics.Raycast(RRWheelCCenter, -RearRightC.transform.forward, out hit, RearRightC.suspensionDistance + RearRightC.radius) ) {
            RearRightM.transform.position = hit.point + (RearRightC.transform.forward * RearRightC.radius);
        } else {
            RearRightM.transform.position = RRWheelCCenter - (RearRightC.transform.forward * RearRightC.suspensionDistance);
        }
        
        FrontLeftM.transform.rotation = Quaternion.Euler(Vector3.zero+FrontLeftC.steerAngle*Vector3.up);
        FrontRightM.transform.rotation = Quaternion.Euler(Vector3.zero+FrontRightC.steerAngle*Vector3.up);
        RearLeftM.transform.rotation = Quaternion.Euler(Vector3.zero+RearLeftC.steerAngle*Vector3.up);
        RearRightC.transform.rotation = Quaternion.Euler(Vector3.zero+RearRightC.steerAngle*Vector3.up);
        
    }

    public void breaking(LogitechGSDK.DIJOYSTATE2ENGINES brakeControl)
    {
        if (brakeControl.lRz <= 32756)
        {
            FL.brakeTorque = breakTorque * Mathf.Abs(brakeControl.lRz - 32767) / 32767;
            FR.brakeTorque = breakTorque * Mathf.Abs(brakeControl.lRz - 32767) / 32767;
            RL.brakeTorque = breakTorque * Mathf.Abs(brakeControl.lRz - 32767) / 32767;
            RR.brakeTorque = breakTorque * Mathf.Abs(brakeControl.lRz - 32767) / 32767;
        }
        else
        {
            FL.brakeTorque = 0;
            FR.brakeTorque = 0;
            RL.brakeTorque = 0;
            RR.brakeTorque = 0;
        }
    }


    public void Shifting(ref int tempGearParam) 
    {
        if (Gear.id < GearBox.Length - 1)
        {
            if (LogitechGSDK.LogiButtonTriggered(0,4)) //  Gear shift up triggered
            {
                tempGearParam = Gear.id;
                Gear = GearBox[1];
            }
            if (LogitechGSDK.LogiButtonReleased(0,4)) // Gear shift up released
            {
                Gear = GearBox[tempGearParam + 1];
                tempGearParam = Gear.id;
                gearText.text = Gear.shift.ToString();
            }
        }
        if (Gear.id > 0)
        {
            if (LogitechGSDK.LogiButtonTriggered(0,5)) //  Gear shift down triggered
            {
                tempGearParam = Gear.id;
                Gear = GearBox[1];
            }
            if (LogitechGSDK.LogiButtonReleased(0,5)) //   Gear shift down released
            {
                Gear = GearBox[tempGearParam - 1];
                tempGearParam = Gear.id;
                gearText.text = Gear.shift.ToString();
            }
        }
    }

    
    void Start ()
    {
        RenderSettings.fogDensity = 0.001f ;
        VisualizeWheel(FL, FR, RL, RR, FLMesh, FRMesh, RLMesh, RRMesh);
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        _carFollower = GameObject.Find("CarFollower").GetComponent<Camera>() ;
        _springJoint = new JointSpring() ;
        _springJoint.spring = 8000 ;
        _springJoint.damper = 5000 ;
        _springJoint.targetPosition = 0.85f ;
     
        // steering first initialize
       if(!LogitechGSDK.LogiSteeringInitialize(false))
        {
            Debug.Log("LogiInitialize return false");
        }

        velocityText.text = "0 km/h";
        gearText.text = "N";
        for (int i = 0; i < GearBox.Length; i++)
        {
            GearBox[i] = (Shift)ScriptableObject.CreateInstance(typeof(Shift));
            if (i > 1)
            {
                GearBox[i].minSpeed = (i-2) * 80 ;
                GearBox[i].maxSpeed = (i - 2) * 80 + 80 ;
                GearBox[i].shift = (char)(i - 1) ;
                GearBox[i].id = i ;
                continue;
            }

            if (i == 0)
            {
                GearBox[0].minSpeed = 0;
                GearBox[0].maxSpeed = 50;
                GearBox[0].shift = 'R';
                GearBox[0].id = 0;
            }
            else
            {
                GearBox[1].minSpeed = 0;
                GearBox[1].maxSpeed = 0;
                GearBox[1].shift = 'N';
                GearBox[1].id = 1;
            }   
        }

        Gear =(Shift)ScriptableObject.CreateInstance(typeof(Shift));
        Gear = GearBox[1];
        tempGear = 1;
        steerState = new LogitechGSDK.DIJOYSTATE2ENGINES();
        StartUp();
        Invoke(nameof(Idle),0.65f);
    }

    void Update()
    {
        _carFollower.transform.position = Vector3.right*transform.position.x + Vector3.up*_carFollower.transform.position.y
                                                                             + Vector3.forward*transform.position.z;
        velocityText.text = ((int)currentVelocity)+" km/h";
        VisualizeWheel(FL, FR, RL, RR, FLMesh, FRMesh, RLMesh, RRMesh);
    }
    
    
	void FixedUpdate () {

        if (LogitechGSDK.LogiUpdate()&&LogitechGSDK.LogiIsConnected(0))
        {

            steerState = LogitechGSDK.LogiGetStateUnity(0);
            Shifting(ref tempGear);
            currentVelocity = rb.velocity.magnitude * 3600 / 1000;
            


            if (currentVelocity > Gear.maxSpeed)
            { 
                motorTorqueV = 0;
                if (!Gear.shift.Equals('N'))
                {
                    if (!audioSource.clip.Equals(maxRpm))
                    {
                        MaxRPM();
                    }
                }
                else
                {
                    tempPitch = (float)steerState.lY / (-32767) + 1.5f;
                    if (tempPitch >= 2.45)
                    {
                        if (!audioSource.clip.Equals(maxRpm))
                        {
                            MaxRPM();
                        }
                        
                    }
                    else
                    {
                        if (!audioSource.clip.Equals(idleSound))
                        {
                            Idle();
                        }
                    }
                    audioSource.pitch = tempPitch;
                }
               
            }
            else
            {
                if (!audioSource.clip.Equals(idleSound))
                {
                    Idle();
                    audioSource.pitch = tempPitch;
                }
            }
            if (currentVelocity <= Gear.maxSpeed && currentVelocity >= Gear.minSpeed)
            {
                float velocityAverage = Gear.maxSpeed - Gear.minSpeed / 2;
                if ((currentVelocity < velocityAverage + velocityAverage / 10) && (currentVelocity >= velocityAverage - velocityAverage / 10))
                {
                    motorTorqueV = Mathf.Abs(steerState.lY - 32767) * motorTorque / 32767;
                }
                else
                {
                    float velocityDifference = Mathf.Abs(currentVelocity - velocityAverage);
                    if (velocityDifference <= 10)
                    {
                        motorTorqueV = Mathf.Abs(steerState.lY - 32767) * motorTorque / 32767;
                        motorTorqueV = motorTorqueV / 1.1f;
                    }
                    else
                    {
                        motorTorqueV = Mathf.Abs(steerState.lY - 32767) * motorTorque / 32767 / (velocityDifference / 10);
                    }

                }

                if (!GearBox[tempGear].shift.Equals('N'))
                {
                    if (!audioSource.clip.Equals(idleSound))
                    {
                        audioSource.clip = idleSound;
                    }
                    if ((currentVelocity + 20.0f) / ((!Gear.shift.Equals('R') ? ((float)Gear.id - 1) : (1.0f)) * 40) <= 0.5f)
                    {
                        tempPitch = 0.5f;
                        audioSource.pitch = tempPitch;
                    }
                    else
                    {
                        tempPitch = (currentVelocity + 20.0f) / ((!Gear.shift.Equals('R') ? ((float)Gear.id - 1) : (1.0f)) * 40);
                        audioSource.pitch = tempPitch;
                    }
                }
                else
                {
                    tempPitch = (float)steerState.lY / (-32767) + 1.5f;
                    if (tempPitch >= 2.45)
                    {
                        if (!audioSource.clip.Equals(maxRpm))
                        {
                            MaxRPM();
                        }
                            
                    }
                    else
                    {
                        if (!audioSource.clip.Equals(idleSound))
                        {
                            Idle();
                        }
                       
                    }
                    audioSource.pitch = tempPitch;
                }
            }

            if (currentVelocity < Gear.minSpeed)
            {
                float velocityDifference = Gear.minSpeed - currentVelocity;
                if (velocityDifference <= 10)
                {
                    motorTorqueV = Mathf.Abs(steerState.lY - 32767) * motorTorque / 32767 / 10;
                    motorTorqueV = motorTorqueV / 1.1f;
                }
                else
                {
                    motorTorqueV = (Mathf.Abs(steerState.lY - 32767) * motorTorque / 32767 / 10) / (velocityDifference / 10);
                }
               
                if (!GearBox[tempGear].shift.Equals('N'))
                {
                    if ((currentVelocity + 20.0f) / ((!Gear.shift.Equals('R') ? ((float)Gear.id - 1) : (1.0f)) * 40) <= 0.5f)
                    {
                        tempPitch = 0.5f;
                        audioSource.pitch = tempPitch;
                    }
                    else
                    {
                        tempPitch = (currentVelocity + 20.0f) / ((!Gear.shift.Equals('R') ? ((float)Gear.id - 1) : (1.0f)) * 40);      
                        audioSource.pitch = tempPitch;
                    }
                }
                else
                {
                    tempPitch = (float)steerState.lY / (-32767) + 1.5f;
                    if (tempPitch >= 2.45)
                    {
                        if (!audioSource.clip.Equals(maxRpm))
                        {
                            MaxRPM();
                        }
                       
                    }
                    else
                    {
                        if (!audioSource.clip.Equals(idleSound))
                        {
                            Idle();
                        }
                        audioSource.pitch = tempPitch;
                    }
                    audioSource.pitch = tempPitch;
                }


            }
            

            float steering = (float)steerState.lX / 32767 * steerAngle;



            if (driveMod == DriveMod.FrontDrive)
            {
                if (Gear.shift != 'R')
                {
                    FL.motorTorque = FR.motorTorque = motorTorqueV;
                }
                else
                {
                    FL.motorTorque = FR.motorTorque = -motorTorqueV;
                }
            }
            else if (driveMod == DriveMod.RearDrive)
            {
                if (Gear.shift != 'R')
                {
                    RR.motorTorque = RL.motorTorque = motorTorqueV;
                }
                else
                {
                    RR.motorTorque = RL.motorTorque = -motorTorqueV;
                }
            }
            else if (driveMod == DriveMod.FullDrive)
            {
                if (Gear.shift == 'R')
                {
                    FL.motorTorque = FR.motorTorque = motorTorqueV;
                    RR.motorTorque = RL.motorTorque = motorTorqueV;
                }
                else
                {
                    FL.motorTorque = FR.motorTorque = -motorTorqueV;
                    RR.motorTorque = RL.motorTorque = -motorTorqueV;
                }
            }

            FL.steerAngle = FR.steerAngle = steering;
            breaking(steerState);
        }
	    
	    if (currentVelocity < 200)
	    {
	        _springJoint.damper = 5000 + 75 * currentVelocity ;
	        _springJoint.targetPosition = 0.85f + 0.00075f * currentVelocity ;
	        FR.suspensionSpring = _springJoint ;
	        FL.suspensionSpring = _springJoint ;
	        RR.suspensionSpring = _springJoint ;
	        RL.suspensionSpring = _springJoint ;
	    }
	}
}
