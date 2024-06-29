using System.Collections;
using System.Collections.Generic;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] float _topSpeed, _acceleration, _decceleration;

    [SerializeField] float _jumpForce, _groundpoundForce, _dashForce;

    Rigidbody _rb;
    GroundedCheck _grounded;

    [Networked] public NetworkButtons ButtonsPrevious { get; set; }

    private void Awake() 
    {
        _rb = GetComponent<Rigidbody>();
        _grounded = GetComponent<GroundedCheck>();
    }

    private void Start() 
    {
        if(HasInputAuthority)
        {
            Camera.main.transform.root.GetComponent<CameraController>().SetTarget(transform).enabled = true;
        }
    }

    public override void FixedUpdateNetwork() 
    {
        if(!GetInput(out NetworkInputData inputData)) return;

        var pressedButtons = inputData.buttons.GetPressed(ButtonsPrevious);
        var releasedButtons = inputData.buttons.GetReleased(ButtonsPrevious);

        if(pressedButtons.IsSet(PlayerButtons.Jump)) Jump(inputData);
        if(pressedButtons.IsSet(PlayerButtons.Crouch)) Crouch(inputData);
        if(pressedButtons.IsSet(PlayerButtons.Dash)) Dash(inputData);

        if (releasedButtons.IsSet(PlayerButtons.Jump)) UnJump(inputData);

        ButtonsPrevious = inputData.buttons;

        Move(UpdateForward(inputData));
    }

    Vector3 UpdateForward(NetworkInputData input)
    {
        if(input.direction.magnitude == 0) return default;

        Vector3 inputForward = input.cameraForward.CollapseAxis(1) * input.direction.y + input.cameraRight.CollapseAxis(1) * input.direction.x;
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

    void Jump(NetworkInputData input)
    {
        if(!_grounded.isGrounded) return;

        _rb.velocity = new Vector3(_rb.velocity.x, _jumpForce, _rb.velocity.z);
    }

    void UnJump(NetworkInputData input)
    {   
        if (_rb.velocity.y < 0f) return;

        _rb.velocity = new Vector3(_rb.velocity.x, _rb.velocity.y * 0.5f, _rb.velocity.z);
    }

    void Crouch(NetworkInputData input)
    {
        if(!_grounded.isGrounded)
        {
            _rb.velocity = -transform.up * _groundpoundForce;
        }
    }

    void Dash(NetworkInputData input)
    {
        Vector3 newVel = UpdateForward(input) * _dashForce;
        newVel.y = _rb.velocity.y;
        _rb.velocity = newVel;
    }
}
