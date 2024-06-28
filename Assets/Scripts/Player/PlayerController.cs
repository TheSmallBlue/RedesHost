using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] float _topSpeed, _acceleration, _decceleration;

    [SerializeField] float _jumpForce;

    Vector2 _input;

    Rigidbody _rb;

    private void Awake() 
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update() 
    {
        _input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if(/* isGrounded && */ Input.GetButtonDown("Jump"))
        {
            _rb.velocity = new Vector3(_rb.velocity.x, _jumpForce, _rb.velocity.z);
        }

        if(Input.GetButtonUp("Jump") && _rb.velocity.y > 0f)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, _rb.velocity.y * 0.5f, _rb.velocity.z);
        }
    }

    public override void FixedUpdateNetwork() 
    {
        Move(UpdateForward());
    }

    Vector3 UpdateForward()
    {
        if(_input.magnitude == 0) return default;

        Vector3 inputForward = Camera.main.transform.forward.CollapseAxis(1) * _input.y + Camera.main.transform.right.CollapseAxis(1) * _input.x;
        inputForward.Normalize();

        transform.forward = inputForward;
        return inputForward;
    }

    void Move(Vector3 direction)
    {
        Vector3 velocityDif = (direction * _topSpeed) - _rb.velocity.CollapseAxis(1);
        float acc = direction.magnitude > 0.01f ? _acceleration : _decceleration;

        _rb.AddForce(velocityDif * acc);
    }
}
