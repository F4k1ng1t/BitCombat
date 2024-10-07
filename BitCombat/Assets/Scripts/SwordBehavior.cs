using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordBehavior : MonoBehaviour
{
    public PlayerController player;
    float attackerId;
    private void Start()
    {
        attackerId = player.id;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && player.isSwinging)
        {
            PlayerController enemy = GameManager.instance.GetPlayer(other.gameObject);
            if (enemy.isParrying)
            {
                player.EnterHitstun();
                
            }
            else
            {
                enemy.photonView.RPC("TakeDamage", enemy.photonPlayer, attackerId, 20);
            }
        }
    }

}
