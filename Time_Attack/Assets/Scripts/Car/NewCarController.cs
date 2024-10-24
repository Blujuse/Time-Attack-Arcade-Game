using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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

    private float currentSpeed;
    public AnimationCurve steeringCurve;

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
        currentSpeed = carRB.velocity.magnitude;
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
        wheelColliders.rlWheelCol.motorTorque = motorPower * accelerateInput;
        wheelColliders.rrWheelCol.motorTorque = motorPower * accelerateInput;
    }

    void ApplySteering()
    {
        float steeringAngle = steeringInput * steeringCurve.Evaluate(currentSpeed);
        steeringAngle += Vector3.SignedAngle(transform.forward, carRB.velocity + transform.forward, Vector3.up);
        steeringAngle = Mathf.Clamp(steeringAngle, -90f, 90f);
        wheelColliders.flWheelCol.steerAngle = steeringAngle;
        wheelColliders.frWheelCol.steerAngle = steeringAngle;
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
        WheelHit[] wheelsHits = new WheelHit[4];

        wheelColliders.flWheelCol.GetGroundHit(out wheelsHits[0]);
        wheelColliders.frWheelCol.GetGroundHit(out wheelsHits[1]);

        wheelColliders.rlWheelCol.GetGroundHit(out wheelsHits[2]);
        wheelColliders.rrWheelCol.GetGroundHit(out wheelsHits[3]);

        float slipAllowance = 0.3f;

        if ((Mathf.Abs(wheelsHits[0].sidewaysSlip) + Mathf.Abs(wheelsHits[0].forwardSlip) > slipAllowance))
        {
            wheelParticles.flWheelParticles.Play();
        }
        else
        {
            wheelParticles.flWheelParticles.Stop();
        }

        if ((Mathf.Abs(wheelsHits[1].sidewaysSlip) + Mathf.Abs(wheelsHits[1].forwardSlip) > slipAllowance))
        {
            wheelParticles.frWheelParticles.Play();
        }
        else
        {
            wheelParticles.frWheelParticles.Stop();
        }

        if ((Mathf.Abs(wheelsHits[2].sidewaysSlip) + Mathf.Abs(wheelsHits[2].forwardSlip) > slipAllowance))
        {
            wheelParticles.rlWheelParticles.Play();
        }
        else
        {
            wheelParticles.rlWheelParticles.Stop();
        }

        if ((Mathf.Abs(wheelsHits[3].sidewaysSlip) + Mathf.Abs(wheelsHits[3].forwardSlip) > slipAllowance))
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