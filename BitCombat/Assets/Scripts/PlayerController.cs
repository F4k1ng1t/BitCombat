using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;

public class PlayerController : MonoBehaviourPun
{
    public int id;
    public Player photonPlayer;
    private int curAttackerId;
    
    public int curHp;
    public int maxHp;
    public int kills;
    public bool dead;
    public MeshRenderer mr;
    public Animator animator;
    public GameObject Sword;
    bool canParry = false;
    bool canMove = true;
    bool canSwing = false;

    float parryCooldownTimestamp;
    float swingCooldownTimestamp;

    public bool isParrying = false;
    public bool isSwinging = false;
    bool actionable = true;

    [Header("Stats")]
    public float moveSpeed;
    public float swingCooldown;
    public float parryCooldown; 
    [Header("Components")]
    public Rigidbody rig;
    
    void Update()
    {
        if (!photonView.IsMine || dead)
            return;
        if (actionable)
        {
            if (canMove)
                Move();
            if (Input.GetKeyDown(KeyCode.F))
                tryParry();
            if (Input.GetMouseButton(0))
                trySwing();
        }
        

        //if none are active remain idle
        animator.SetBool("Idle", !animator.GetBool("Parry") && !animator.GetBool("Run") && !animator.GetBool("SwordSwing"));

        if (animPlaying("Parry")) isParrying = true;
        else isParrying = false;

        if (animPlaying("SwordSwing")) isSwinging = true;
        else isSwinging = false;
    }
    private void FixedUpdate()
    {
        if (canParry)
        {
            Parry();
            canParry = false;
        }
        if (canSwing)
        {
            Swing();
            canSwing = false;
        }
        switchAnims("SwordSwing");
        switchAnims("Parry");
        switchAnims("Hitstun");
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && animator.GetCurrentAnimatorStateInfo(0).IsName("Hitstun"))
        {
            ExitHitstun();
        }
    }
    void Move()
    {
        // get the input axis
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        bool Run = x != 0 || z != 0;
        animator.SetBool("Run", Run);
        
        // calculate a direction relative to where we're facing
        Vector3 dir = (transform.forward * z + transform.right * x) * moveSpeed;
        if (animPlaying("SwordSwing") || animPlaying("Parry"))
        {
            dir = dir / 3;
        }
        dir.y = rig.velocity.y;
        // set that as our velocity
        rig.velocity = dir;
    }
    void Parry()
    {
        if (animPlaying("Parry"))
            return;
        //canMove = false;
        animator.SetBool("Parry", true);
    }
    void Swing()
    {
        if (animPlaying("SwordSwing"))
            return;
        //canMove = false;
        
        animator.SetBool("SwordSwing", true);
    }
    void tryParry()
    {
        if (Time.time > parryCooldownTimestamp)
        {
            parryCooldownTimestamp = Time.time + parryCooldown;
            canParry = true;
            Debug.Log("Successful parry");
        }
        else
        {
            canParry = false;
            Debug.Log("Failed parry");
        } 
            
        //Debug.Log($"{Time.time}");
        //Debug.Log("Attempted parry");
    }
    void switchAnims(string Bool)
    {
        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && animator.GetCurrentAnimatorStateInfo(0).IsName(Bool) && !animator.IsInTransition(0))
        {
            animator.SetBool(Bool, false);
            actionable = true;

        }
    }
    void trySwing()
    {
        if (Time.time > swingCooldownTimestamp)
        {
            swingCooldownTimestamp = Time.time + swingCooldown;
            Swing();
        }
        else canSwing = false;
        //Debug.Log($"{Time.time}");
    }
    bool animPlaying(string animState)
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(animState))
        {
            animator.SetBool(animState, false);
            return true;
        }
        return false;
    }
    [PunRPC]
    public void Initialize(Player player)
    {
        id = player.ActorNumber;
        photonPlayer = player;
        GameManager.instance.players[id - 1] = this;

        // is this not our local player?
        if (!photonView.IsMine)
        {
            GetComponentInChildren<Camera>().gameObject.SetActive(false);
            rig.isKinematic = true;
        }
        else
        {
            GameUI.instance.Initialize(this);
        }
    }
    [PunRPC]
    public void TakeDamage(int attackerId, int damage)
    {
        if (dead)
            return;
        curHp -= damage;
        curAttackerId = attackerId;
        // update the health bar UI
        GameUI.instance.UpdateHealthBar();
        // die if no health left
        if (curHp <= 0)
            photonView.RPC("Die", RpcTarget.All);
    }
    public void EnterHitstun()
    {
        actionable = false;
        animator.SetBool("SwordSwing",false);
        animator.SetBool("Parry",false);
        animator.SetBool("Run", false);
        animator.SetBool("Hitstun", true);
        rig.velocity = Vector3.zero;
    }
    public void ExitHitstun()
    {
        actionable = true;
        animator.SetBool("Hitstun", false);
    }
    [PunRPC]
    void Die()
    {
        curHp = 0;
        dead = true;
        GameManager.instance.alivePlayers--;
        // host will check win condition
        if (PhotonNetwork.IsMasterClient)
            GameManager.instance.CheckWinCondition();
        // is this our local player?
        if (photonView.IsMine)
        {
            if (curAttackerId != 0)
                GameManager.instance.GetPlayer(curAttackerId).photonView.RPC("AddKill", RpcTarget.All);
            // set the cam to spectator
            GetComponentInChildren<CameraController>().SetAsSpectator();
            // disable the physics and hide the player
            rig.isKinematic = true;
            transform.position = new Vector3(0, -50, 0);
        }
    }
    [PunRPC]
    public void AddKill()
    {
        kills++;
        GameUI.instance.UpdatePlayerInfoText();
    }
    [PunRPC]
    public void Heal(int amountToHeal)
    {
        curHp = Mathf.Clamp(curHp + amountToHeal, 0, maxHp);
        // update the health bar UI
        GameUI.instance.UpdateHealthBar();
    }
}
