using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Addons.Physics;
using TMPro;
using UnityEngine;

public class PlayerController : PlayerAvatar, IAttackable
{
    [Space]
    [SerializeField] float _topSpeed;
    [SerializeField] float _acceleration, _decceleration;

    [Space]
    [SerializeField] float _jumpForce;
    [SerializeField] float _groundpoundForce, _dashForce;

    [Space]
    [SerializeField] float _attackReach;
    [SerializeField] float _attackSize;
    [SerializeField] float _knockbackAmount;

    [Space]
    [SerializeField] float _stunTime;

    [Space]
    [SerializeField] Pancake pancakePrefab;

    Rigidbody _rb;
    GroundedCheck _grounded;
    Animator _anim;

    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    [Networked] private TickTimer stunTimer { get; set; }

    private void Awake() 
    {
        _rb = GetComponent<Rigidbody>();
        _grounded = GetComponent<GroundedCheck>();
        _anim = GetComponentInChildren<Animator>();
    }

    public override void Spawned()
    {
        if(!HasInputAuthority) return;

        var cameraRoot = Camera.main.transform.root;
        cameraRoot.GetComponent<CameraController>().SetTarget(transform).enabled = true;
    }

    public override void SetupVisuals(string desiredName, Color desiredColor)
    {
        GetComponentInChildren<SkinnedMeshRenderer>().materials[1].color = desiredColor;

        var textMesh = GetComponentInChildren<TextMeshPro>();

        textMesh.text = desiredName[0].ToString().ToUpper();
        textMesh.color = desiredColor;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if(!HasInputAuthority) return;

        var cameraRoot = Camera.main.transform.root;
        cameraRoot.GetComponent<CameraController>().enabled = false;
    }

    public override void FixedUpdateNetwork() 
    {
        // Update our constantly-updating animations
        if(Runner.IsForward)
        {
            _anim.SetFloat("Speed", _rb.velocity.CollapseAxis(1).magnitude);

            _anim.SetBool("Grounded", _grounded.isGrounded);
            _anim.SetBool("Falling", _rb.velocity.y < -0.1f);
        }

        if (!stunTimer.ExpiredOrNotRunning(Runner)) // If we're stunned, do nothing except updating our animator
        {
            // Update our damage animations
            if(Runner.IsForward)
            {
                _anim.SetInteger("Damaged", lastAttackIndex);
                _anim.SetBool("Stunned", true);
            }

            return;
        }
        if (!GetInput(out NetworkInputData inputData)) return; // If there is no input for us, do nothing

        var pressedButtons = inputData.buttons.GetPressed(ButtonsPrevious); // Get buttons pressed this update
        var releasedButtons = inputData.buttons.GetReleased(ButtonsPrevious); // Get buttons released this update

        // Do actions if certain buttons are pressed
        if(pressedButtons.IsSet(PlayerButtons.Jump)) Jump(inputData);
        if(pressedButtons.IsSet(PlayerButtons.Crouch)) Crouch(inputData);
        if(pressedButtons.IsSet(PlayerButtons.Dash)) Dash(inputData);
        if(pressedButtons.IsSet(PlayerButtons.Attack)) Attack(inputData);

        // Do other actions if certain buttons are released
        if(releasedButtons.IsSet(PlayerButtons.Jump)) UnJump(inputData);

        // Set our ButtonsPrevious variables so that we may be able to check which buttons have been newly pressed next update
        ButtonsPrevious = inputData.buttons;

        // Update our action animations
        if (Runner.IsForward)
        {
            _anim.SetBool("Jumping", inputData.buttons.IsSet(PlayerButtons.Jump));
            _anim.SetBool("Crouching", inputData.buttons.IsSet(PlayerButtons.Crouch));
            _anim.SetBool("Punching", pressedButtons.IsSet(PlayerButtons.Attack));
            _anim.SetBool("Dashing", pressedButtons.IsSet(PlayerButtons.Dash));

            _anim.SetBool("Stunned", false);
        }


        // If we're punching, look towards where we're punching.
        if (inputData.buttons.IsSet(PlayerButtons.Attack)) transform.forward = inputData.cameraForward.CollapseAxis(1);

        // Move towards camera forward
        Vector3 fwd = GetForward(inputData);
        Move(fwd, inputData);

        // We're falling! Let's check if we are stomping on a head
        if(_rb.velocity.y < -0.1f) StompHeads();

        // We've fallen out of bounds! Lets just die.
        if(transform.position.y < -10f) Die();
    }

    Vector3 GetForward(NetworkInputData input)
    {
        if(input.direction.magnitude == 0) return default;

        Vector3 inputForward = input.cameraForward.CollapseAxis(1) * input.direction.y + input.cameraRight.CollapseAxis(1) * input.direction.x;
        inputForward.Normalize();

        transform.forward = inputForward;

        return inputForward;
    }

    void Move(Vector3 direction, NetworkInputData input)
    {
        Vector3 velocityDif = (direction * (input.buttons.IsSet(PlayerButtons.Crouch) ? _topSpeed * 0.5f : _topSpeed)) - _rb.velocity.CollapseAxis(1);
        float acc = direction.magnitude > 0.01f ? _acceleration : _decceleration;

        _rb.AddForce(velocityDif * acc);
    }

    void Jump(NetworkInputData input)
    {
        if(!_grounded.isGrounded) return;

        _rb.velocity = new Vector3(_rb.velocity.x, input.buttons.IsSet(PlayerButtons.Crouch) ? _jumpForce * 1.5f : _jumpForce, _rb.velocity.z);
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
        Vector3 newVel = GetForward(input) * _dashForce;
        newVel.y = _rb.velocity.y;
        _rb.velocity = newVel;
    }

    void Attack(NetworkInputData input)
    {
        // Only run on server
        if (!HasStateAuthority) return;

        var playersHit = Physics.OverlapSphere(transform.position + input.cameraForward * _attackReach, _attackSize, 1 << 6);

        foreach (var player in playersHit)
        {
            IAttackable attackable = player.transform.GetComponent<IAttackable>();

            if (attackable is PlayerController && attackable as PlayerController == this) continue;

            IAttackable.AttackType attackType = IAttackable.AttackType.Forward;
            if(input.buttons.IsSet(PlayerButtons.Jump)) attackType = IAttackable.AttackType.Up;
            else if(input.buttons.IsSet(PlayerButtons.Crouch)) attackType = IAttackable.AttackType.Down;

            attackable.OnAttack(attackType, transform);
        }
    }

    public void OnAttack(IAttackable.AttackType type, Transform source)
    {
        Vector3 newVel = Vector3.zero;

        switch (type)
        {
            case IAttackable.AttackType.Forward:
                newVel = -(source.position - transform.position).normalized * _knockbackAmount;
                newVel.y = _rb.velocity.y;
                RPC_SetLastAttackIndex(1);
                break;
            case IAttackable.AttackType.Up:
                newVel = Vector3.up * _jumpForce;
                RPC_SetLastAttackIndex(2);
                break;
            case IAttackable.AttackType.Down:
                newVel = -Vector3.up * _groundpoundForce * 1.5f;
                RPC_SetLastAttackIndex(3);
                break;
        }

        _rb.velocity = Vector3.zero;
        _rb.velocity = newVel;

        stunTimer = TickTimer.CreateFromSeconds(Runner, _stunTime);
    }

    void StompHeads()
    {
        if (!HasStateAuthority) return;

        var possibleHeads = Physics.OverlapSphere(transform.position - transform.up, .5f, 1 << 6);

        foreach (var head in possibleHeads)
        {
            var headAsPlayer = head.GetComponent<PlayerController>();
            if (head.GetComponent<PlayerController>() == this) continue;

            _rb.velocity = new Vector3(_rb.velocity.x, _jumpForce, _rb.velocity.z);

            headAsPlayer.Die();
        }
    }

    public void Die()
    {
        RPC_SpawnPancake();
        RoundManager.Instance.RPC_OnPlayerDead(this);
    }

    [Rpc]
    public void RPC_SpawnPancake()
    {
        Instantiate(pancakePrefab, transform.position, Quaternion.identity).Initialize(Head.Color);
    }

    int lastAttackIndex = 0;
    [Rpc]
    public void RPC_SetLastAttackIndex(int index)
    {
        lastAttackIndex = index;
    }
}
