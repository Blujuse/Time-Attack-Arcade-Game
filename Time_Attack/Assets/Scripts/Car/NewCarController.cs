using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewCarController : MonoBehaviour
{
    private Rigidbody carRB;
    public WheelColliders wheelColliders;

    public WheelMeshes wheelMeshes;

    public float accelerateInput;
    public float steeringInput;

    public float motorPower;
    private float currentSpeed;

    public AnimationCurve steeringCurve;

    private void Start()
    {
        carRB = gameObject.GetComponent<Rigidbody>();
    }
    private void Update()
    {
        currentSpeed = carRB.velocity.magnitude;
        CheckInput();
        ApplyMotor();
        ApplySteering();
        ApplyWheelPositions();
    }

    void CheckInput()
    {
        accelerateInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");
    }

    void ApplyMotor()
    {
        wheelColliders.rlWheelCol.motorTorque = motorPower * accelerateInput;
        wheelColliders.rrWheelCol.motorTorque = motorPower * accelerateInput;
    }

    void ApplySteering()
    {
        float steeringAngle = steeringInput * steeringCurve.Evaluate(currentSpeed);
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