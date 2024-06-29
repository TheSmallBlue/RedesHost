using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform _target;

    [SerializeField] Vector2 _yRotationLimits;
    [SerializeField] float _sensitivity = 300, _distance, _yDampening, _xzDampening;

    [SerializeField] bool _flipXAxis, _flipYAxis;

    Vector2 _movement;

    Vector2 _input;

    private void Start() 
    {
        Cursor.lockState = CursorLockMode.Locked;

        transform.position = _target.position;
        Camera.main.transform.position = _target.position - _target.forward * _distance;

        _movement = transform.localRotation.eulerAngles;
    }

    void Update()
    {
        _input = new Vector2(Input.GetAxisRaw("Mouse X") * (_flipXAxis ? -1 : 1), Input.GetAxisRaw("Mouse Y") * (_flipYAxis ? -1 : 1));

        SetRotation();
    }

    private void LateUpdate() 
    {
        if(_target == null) return;
        
        SetPosition();
    }

    void SetRotation()
    {
        if(_input.magnitude < 0.01f) return;

        _movement.x += _input.x * _sensitivity * Time.deltaTime;

        _movement.y += _input.y * _sensitivity * Time.deltaTime;
        _movement.y = Mathf.Clamp(_movement.y, _yRotationLimits.x, _yRotationLimits.y);

        transform.rotation = Quaternion.Euler(-_movement.y, _movement.x, 0);
    }

    void SetPosition()
    {
        Vector3 vel = _target.position - transform.position;

        Vector3 smoothedMovement = transform.position;

        smoothedMovement.x = Mathf.SmoothDamp(transform.position.x, _target.position.x, ref vel.x, _xzDampening * Time.deltaTime);
        smoothedMovement.z = Mathf.SmoothDamp(transform.position.z, _target.position.z, ref vel.z, _xzDampening * Time.deltaTime);

        smoothedMovement.y = Mathf.SmoothDamp(transform.position.y, _target.position.y, ref vel.y, _yDampening * Time.deltaTime);

        transform.position = smoothedMovement;
    }

    public CameraController SetTarget(Transform newTarget)
    {
        _target = newTarget;

        return this;
    }
}
