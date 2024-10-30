using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public enum GearState
{
    Neutral,
    Running,
    CheckingChange,
    Changing
}

public class NewCarController : MonoBehaviour
{
    private Rigidbody carRB;
    public WheelColliders wheelColliders;

    public WheelMeshes wheelMeshes;

    public WheelParticles wheelParticles;
    public GameObject smokePrefab;

    public float accelerateInput;
    public float brakeInput;
    public float steeringInput;

    public float motorPower;
    public float brakePower;
    private float slipAngle;

    public int isEngineRunning = 0;
    private float currentSpeed;
    private float clampedSpeed;
    public float maxSpeed;
    public AnimationCurve steeringCurve;

    public float currentRPM;
    public float engineRedline;
    public float idleRPM;
    public TMP_Text rpmText;
    public TMP_Text gearText;
    public Transform rpmClusterNeedle;
    public float minNeedleRotation;
    public float maxNeedleRotation;
    public int currentGear;

    public float[] gearRatios;
    public float differentialRatio;
    private float currentTorque;
    private float clutch;
    private float wheelRPM;
    public AnimationCurve horsepowerToRPMCurve;
    private GearState gearState;
    public float increaseGearRPM;
    public float decreaseGearRPM;
    public float changeGearTime;

    private void Start()
    {
        carRB = gameObject.GetComponent<Rigidbody>();
        InstantiateSmoke();
    }

    void InstantiateSmoke()
    {
        wheelParticles.flWheelParticles = Instantiate(smokePrefab, wheelColliders.flWheelCol.transform.position - Vector3.up * wheelColliders.flWheelCol.radius, Quaternion.identity, wheelColliders.flWheelCol.transform)
                    .GetComponent<ParticleSystem>();
        wheelParticles.frWheelParticles = Instantiate(smokePrefab, wheelColliders.frWheelCol.transform.position - Vector3.up * wheelColliders.frWheelCol.radius, Quaternion.identity, wheelColliders.frWheelCol.transform)
                    .GetComponent<ParticleSystem>();
        wheelParticles.rlWheelParticles = Instantiate(smokePrefab, wheelColliders.rlWheelCol.transform.position - Vector3.up * wheelColliders.rlWheelCol.radius, Quaternion.identity, wheelColliders.rlWheelCol.transform)
                    .GetComponent<ParticleSystem>();
        wheelParticles.rrWheelParticles = Instantiate(smokePrefab, wheelColliders.rrWheelCol.transform.position - Vector3.up * wheelColliders.rrWheelCol.radius, Quaternion.identity, wheelColliders.rrWheelCol.transform)
                    .GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        rpmClusterNeedle.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(minNeedleRotation, maxNeedleRotation, currentRPM / (engineRedline * 1.1f)));
        rpmText.text = currentRPM.ToString("0,000") + "RPM";
        gearText.text = (gearState == GearState.Neutral) ? "N": (currentGear + 1).ToString();
        currentSpeed = wheelColliders.rrWheelCol.rpm * wheelColliders.rrWheelCol.radius * 2f * Mathf.PI / 10f;
        clampedSpeed = Mathf.Lerp(clampedSpeed, currentSpeed, Time.deltaTime);
        CheckInput();
        ApplyMotor();
        ApplyBrake();
        ApplySteering();
        ApplyWheelPositions();
        CheckParticles();
    }

    void CheckInput()
    {
        accelerateInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");
        slipAngle = Vector3.Angle(transform.forward, carRB.velocity - transform.forward);

        float movingDirection = Vector3.Dot(transform.forward, carRB.velocity);

        if (gearState != GearState.Changing)
        {
            if (gearState == GearState.Neutral)
            {
                clutch = 0;
                if (Mathf.Abs(accelerateInput) > 0)
                {
                    gearState = GearState.Running;
                }
            }
            else
            {
                clutch = Input.GetKey(KeyCode.LeftShift) ? 0 : Mathf.Lerp(clutch, 1, Time.deltaTime);
            }
        }
        else
        {
            clutch = 0;
        }

        if (movingDirection < -0.5f && accelerateInput > 0)
        {
            brakeInput = Mathf.Abs(accelerateInput);
        }
        else if (movingDirection > 0.5f && accelerateInput < 0)
        {
            brakeInput = Mathf.Abs(accelerateInput);
        }
        else
        {
            brakeInput = 0;
        }

        if (Mathf.Abs(accelerateInput) > 0 && isEngineRunning == 0)
        {
            StartCoroutine(GetComponent<EngineAudio>().StartEngine());
            gearState = GearState.Running;
            isEngineRunning = 1;
        }
    }

    void ApplyBrake()
    {
        wheelColliders.flWheelCol.brakeTorque = brakeInput * brakePower * 0.7f;
        wheelColliders.frWheelCol.brakeTorque = brakeInput * brakePower * 0.7f;

        wheelColliders.rlWheelCol.brakeTorque = brakeInput * brakePower * 0.7f;
        wheelColliders.rrWheelCol.brakeTorque = brakeInput * brakePower * 0.7f;
    }

    void ApplyMotor()
    {
        currentTorque = CalculateTorque();
        wheelColliders.rlWheelCol.motorTorque = currentTorque * accelerateInput;
        wheelColliders.rrWheelCol.motorTorque = currentTorque * accelerateInput;
    }

    float CalculateTorque()
    {
        float torque =0;

        if (currentRPM < idleRPM + 200 && accelerateInput == 0 && currentGear == 0)
        {
            gearState = GearState.Neutral;
        }
        if (gearState == GearState.Running && clutch > 0)
        {
            if (currentRPM > increaseGearRPM)
            {
                StartCoroutine(ChangeGear(1));
            }
            else if (currentRPM < decreaseGearRPM)
            {
                StartCoroutine(ChangeGear(-1));
            }
        }
        
        if (isEngineRunning > 0)
        {
            if (clutch < 0.1f)
            {
                currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(idleRPM, engineRedline * accelerateInput) + Random.Range( -50, 50), Time.deltaTime );
            }
            else
            {
                wheelRPM = Mathf.Abs((wheelColliders.rlWheelCol.rpm + wheelColliders.rrWheelCol.rpm) / 2f) * gearRatios[currentGear] * differentialRatio;
                currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(idleRPM - 100, wheelRPM), Time.deltaTime * 3f);
                torque = (horsepowerToRPMCurve.Evaluate(currentRPM / engineRedline) * motorPower / currentRPM) * gearRatios[currentGear] * differentialRatio * 5252f * clutch;
            }
        }

        return torque;
    }

    void ApplySteering()
    {
        // Calculate the basic steering angle using the steering curve and input
        float steeringAngle = steeringInput * steeringCurve.Evaluate(currentSpeed);

        // Check for slip and apply countersteering when the slip angle is within limits
        if (slipAngle < 120f)
        {
            // Apply countersteering based on the signed angle between the car's forward direction and its velocity
            steeringAngle += Vector3.SignedAngle(transform.forward, carRB.velocity + transform.forward, Vector3.up);
        }

        // Reduce steering sensitivity when reversing
        if (currentSpeed < 0)
        {
            steeringAngle *= 0.5f; // Adjust sensitivity when reversing
        }

        // Apply jitter mitigation: reduce the steering sensitivity when there's a high angular velocity
        if (Mathf.Abs(carRB.angularVelocity.y) > 0.1f) // Threshold for angular velocity jitter correction
        {
            float angularVelocityFactor = Mathf.Clamp01(Mathf.Abs(carRB.angularVelocity.y) / 5f);
            steeringAngle *= (1f - angularVelocityFactor); // Reduce steering angle to mitigate jitter
        }

        // Clamp the steering angle to prevent unrealistic turning (adjust based on your car setup)
        steeringAngle = Mathf.Clamp(steeringAngle, -90f, 90f);

        // Apply the final steering angle to the front wheels
        wheelColliders.frWheelCol.steerAngle = steeringAngle;
        wheelColliders.flWheelCol.steerAngle = steeringAngle;
    }

    void ApplyWheelPositions()
    {
        UpdateWheel(wheelColliders.flWheelCol, wheelMeshes.flWheelMesh);
        UpdateWheel(wheelColliders.frWheelCol, wheelMeshes.frWheelMesh);
        UpdateWheel(wheelColliders.rlWheelCol, wheelMeshes.rlWheelMesh);
        UpdateWheel(wheelColliders.rrWheelCol, wheelMeshes.rrWheelMesh);
    }

    void CheckParticles()
    {
        WheelHit[] wheelHits = new WheelHit[4];
        wheelColliders.flWheelCol.GetGroundHit(out wheelHits[0]);
        wheelColliders.frWheelCol.GetGroundHit(out wheelHits[1]);

        wheelColliders.rlWheelCol.GetGroundHit(out wheelHits[2]);
        wheelColliders.rrWheelCol.GetGroundHit(out wheelHits[3]);

        float slipAllowance = 0.1f;

        if ((Mathf.Abs(wheelHits[0].sidewaysSlip) + Mathf.Abs(wheelHits[0].forwardSlip) > slipAllowance))
        {
            wheelParticles.flWheelParticles.Play();
        }
        else
        {
            wheelParticles.flWheelParticles.Stop();
        }
        if ((Mathf.Abs(wheelHits[1].sidewaysSlip) + Mathf.Abs(wheelHits[1].forwardSlip) > slipAllowance))
        {
            wheelParticles.frWheelParticles.Play();
        }
        else
        {
            wheelParticles.frWheelParticles.Stop();
        }
        if ((Mathf.Abs(wheelHits[2].sidewaysSlip) + Mathf.Abs(wheelHits[2].forwardSlip) > slipAllowance))
        {
            wheelParticles.rlWheelParticles.Play();
        }
        else
        {
            wheelParticles.rlWheelParticles.Stop();
        }
        if ((Mathf.Abs(wheelHits[3].sidewaysSlip) + Mathf.Abs(wheelHits[3].forwardSlip) > slipAllowance))
        {
            wheelParticles.rrWheelParticles.Play();
        }
        else
        {
            wheelParticles.rrWheelParticles.Stop();
        }


    }

    void UpdateWheel(WheelCollider col, MeshRenderer wheelMesh) 
    {
        Quaternion quat;
        Vector3 pos;
        col.GetWorldPose(out pos, out quat);
        wheelMesh.transform.position = pos;
        wheelMesh.transform.rotation = quat;
    } 

    public float GetSpeedRatio()
    {
        var gas = Mathf.Clamp(Mathf.Abs(accelerateInput), 0.5f, 1f);
        return currentRPM * gas / engineRedline;
    }

    IEnumerator ChangeGear(int gearChange)
    {
        gearState = GearState.CheckingChange;
        if (currentGear + gearChange >= 0)
        {
            if (gearChange > 0)
            {
                // increase gear
                yield return new WaitForSeconds(0.7f);

                if (currentRPM < increaseGearRPM || currentGear >= gearRatios.Length - 1)
                {
                    gearState = GearState.Running;
                    yield break;
                }
            }

            if (gearChange < 0)
            {
                // decrease gear
                yield return new WaitForSeconds(0.1f);

                if (currentRPM > decreaseGearRPM || currentGear <= 0)
                {
                    gearState = GearState.Running;
                    yield break;
                }
            }
            gearState = GearState.Changing;
            yield return new WaitForSeconds(changeGearTime);
            currentGear += gearChange;
        }
        gearState = GearState.Running;
    }
}

[System.Serializable]
public class WheelColliders
{
    public WheelCollider flWheelCol;
    public WheelCollider frWheelCol;
    public WheelCollider rlWheelCol;
    public WheelCollider rrWheelCol;

}

[System.Serializable]
public class WheelMeshes
{
    public MeshRenderer flWheelMesh;
    public MeshRenderer frWheelMesh;
    public MeshRenderer rlWheelMesh;
    public MeshRenderer rrWheelMesh;
}

[System.Serializable]
public class WheelParticles
{
    public ParticleSystem flWheelParticles;
    public ParticleSystem frWheelParticles;
    public ParticleSystem rlWheelParticles;
    public ParticleSystem rrWheelParticles;
}