using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Addons.Physics;
using TMPro;
using UnityEngine;

public class PlayerController : NetworkBehaviour, IAttackable
{
    [SerializeField] float _topSpeed, _acceleration, _decceleration;

    [SerializeField] float _jumpForce, _groundpoundForce, _dashForce;

    [SerializeField] float _attackReach, _attackSize;
    [SerializeField] float _knockbackAmount;

    [SerializeField] float _stunTime;

    Rigidbody _rb;
    GroundedCheck _grounded;
    Animator _anim;

    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    [Networked] private TickTimer stunTimer { get; set; }

    [Networked] public PlayerData.PlayerVisuals visuals { get; set; }
    bool visualsSetUp = false;

    private void Awake() 
    {
        _rb = GetComponent<Rigidbody>();
        _grounded = GetComponent<GroundedCheck>();
        _anim = GetComponentInChildren<Animator>();
    }

    private void Start() 
    {
        if(HasInputAuthority)
        {
            Camera.main.transform.root.GetComponent<CameraController>().SetTarget(transform).enabled = true;
        }
    }

    public override void Spawned()
    {
        if(!HasStateAuthority) return;

        var futureVisuals = RunnerManager.instance.playerObjects[Object.InputAuthority].visuals;
        visuals = futureVisuals;
    }

    public override void Render()
    {
        if(!visualsSetUp) SetupVisuals(visuals);
    }

    private void Update() 
    {
        if(!HasInputAuthority) return;
        
        _anim.SetFloat("Speed", _rb.velocity.CollapseAxis(1).magnitude);

        _anim.SetBool("Grounded", _grounded.isGrounded);
        _anim.SetBool("Falling", _rb.velocity.y < -0.1f);
    }

    void SetupVisuals(PlayerData.PlayerVisuals newVisuals)
    {
        GetComponentInChildren<SkinnedMeshRenderer>().materials[1].color = newVisuals.color;

        var textMesh = GetComponentInChildren<TextMeshPro>();

        string name = newVisuals.name.ToString();
        textMesh.text = name[0].ToString().ToUpper();
        textMesh.color = newVisuals.color;

        visualsSetUp = true;
    }

    public override void FixedUpdateNetwork() 
    {
        if (RunnerManager.instance.roundState != RunnerManager.RoundState.Started) return;
        if (!stunTimer.ExpiredOrNotRunning(Runner)) return;
        if (!GetInput(out NetworkInputData inputData)) return;

        var pressedButtons = inputData.buttons.GetPressed(ButtonsPrevious);
        var releasedButtons = inputData.buttons.GetReleased(ButtonsPrevious);

        if(pressedButtons.IsSet(PlayerButtons.Jump)) Jump(inputData);
        if(pressedButtons.IsSet(PlayerButtons.Crouch)) Crouch(inputData);
        if(pressedButtons.IsSet(PlayerButtons.Dash)) Dash(inputData);
        if(pressedButtons.IsSet(PlayerButtons.Attack)) Attack(inputData);

        if(releasedButtons.IsSet(PlayerButtons.Jump)) UnJump(inputData);

        ButtonsPrevious = inputData.buttons;

        RPC_SetAnim(PlayerButtons.Crouch, inputData.buttons.IsSet(PlayerButtons.Crouch));
        RPC_SetAnim(PlayerButtons.Jump, inputData.buttons.IsSet(PlayerButtons.Jump));

        Vector3 fwd = GetForward(inputData);

        if (inputData.buttons.IsSet(PlayerButtons.Attack)) transform.forward = inputData.cameraForward.CollapseAxis(1);

        Move(fwd, inputData);

        if(_rb.velocity.y < -0.1f) StompHeads();

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
                break;
            case IAttackable.AttackType.Up:
                newVel = Vector3.up * _jumpForce;
                break;
            case IAttackable.AttackType.Down:
                newVel = -Vector3.up * _groundpoundForce * 1.5f;
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

            //BasicSpawner.instance.AddPoints(Object.InputAuthority, 1);

            _rb.velocity = new Vector3(_rb.velocity.x, _jumpForce, _rb.velocity.z);

            headAsPlayer.Die();
        }
    }

    public void Die()
    {
        //BasicSpawner.instance.RespawnIn(Object.InputAuthority, 5f);

        //gameObject.SetActive(false);
        Runner.Despawn(Object);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, HostMode = RpcHostMode.SourceIsServer)]
    public void RPC_ShowWinUI(PlayerRef winningPlayer)
    {
        GameUI.instance.ShowRoundEnd(winningPlayer == Object.InputAuthority);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    public void RPC_SetAnim(PlayerButtons action, bool value)
    {
        switch (action)
        {
            case PlayerButtons.Jump:
                _anim.SetBool("Jumping", value);
                break;
            case PlayerButtons.Crouch:
                _anim.SetBool("Crouching", value);
                break;
        }
    }
}
