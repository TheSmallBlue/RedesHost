using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform _target;

    [Space]
    [SerializeField] Vector2 _yRotationLimits;
    [SerializeField] float _sensitivity = 300, _distance, _yDampening, _xzDampening;

    [Space]
    [SerializeField] bool _flipXAxis, _flipYAxis;

    [Space]
    [SerializeField] LayerMask wallMask;

    Vector2 _movement;
    Vector2 _input;
    bool collision;

    private void OnEnable() 
    {
        Cursor.lockState = CursorLockMode.Locked;
        transform.position = _target.position;
        _movement = transform.localRotation.eulerAngles;

        GetComponent<Animator>().enabled = false;
    }

    private void OnDisable() 
    {
        Cursor.lockState = CursorLockMode.None;

        GetComponent<Animator>().enabled = true;
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

        if(collision && Physics.Raycast(_target.position, -transform.forward, out RaycastHit hitInfo, 10f, wallMask))
        {
            transform.GetChild(0).localPosition = new Vector3(0f, 0f, -Vector3.Distance(transform.position, hitInfo.point));
        } else 
        {
            transform.GetChild(0).localPosition = new Vector3(0f, 0f, -10f);
        }

        Vector3 smoothedMovement = transform.position;

        smoothedMovement.x = Mathf.SmoothDamp(transform.position.x, _target.position.x, ref vel.x, _xzDampening * Time.deltaTime);
        smoothedMovement.z = Mathf.SmoothDamp(transform.position.z, _target.position.z, ref vel.z, _xzDampening * Time.deltaTime);

        smoothedMovement.y = Mathf.SmoothDamp(transform.position.y, _target.position.y, ref vel.y, _yDampening * Time.deltaTime);

        transform.position = smoothedMovement;
    }

    public CameraController SetTarget(Transform newTarget, bool collision = true)
    {
        _target = newTarget;
        this.collision = collision;

        transform.position = _target.position;

        return this;
    }
}
