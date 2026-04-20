using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PuppetMaster : MonoBehaviour
{
    public Transform player;
    public List<Transform> allPuppets;
    public float attackRange = 4f;
    public float rotationSpeed = 5f;

    private Transform currentPuppet;
    private bool isAttacking = false;
    private float jumpTimer = 0f;
    private float jumpCooldown = 2.0f;

    void Start()
    {
        if (allPuppets.Count > 0)
            JumpToPuppet(allPuppets[Random.Range(0, allPuppets.Count)]);
    }

    void Update()
    {
        if (currentPuppet == null || isAttacking) return;

        float dist = Vector3.Distance(currentPuppet.position, player.position);

        if (dist <= attackRange)
        {
            StartCoroutine(ExecuteAttack());
            return;
        }

        LookAtPlayer();

        jumpTimer += Time.deltaTime;
        if (jumpTimer >= jumpCooldown && !IsPlayerLookingAtCurrent() && dist > attackRange + 1f)
        {
            TryJumpCloser();
            jumpTimer = 0f;
        }
    }

    void LookAtPlayer()
    {
        Vector3 dir = (player.position - currentPuppet.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            currentPuppet.rotation = Quaternion.Slerp(currentPuppet.rotation, lookRot, Time.deltaTime * rotationSpeed);
        }
    }

    bool IsPlayerLookingAtCurrent()
    {
        Vector3 dir = (currentPuppet.position - player.position).normalized;
        return Vector3.Angle(player.forward, dir) < 60f;
    }

    void TryJumpCloser()
    {
        Transform best = currentPuppet;
        float currentDist = Vector3.Distance(currentPuppet.position, player.position);

        foreach (Transform p in allPuppets)
        {
            float d = Vector3.Distance(p.position, player.position);
            if (d < currentDist && d > 2.5f)
            {
                best = p;
                currentDist = d;
            }
        }
        if (best != currentPuppet) JumpToPuppet(best);
    }

    void JumpToPuppet(Transform newP)
    {
        if (currentPuppet != null)
            SetState(currentPuppet, Color.white, "Reset");

        currentPuppet = newP;
        SetState(currentPuppet, Color.red, "PoseChange");
    }

    void SetState(Transform p, Color c, string t)
    {
        Renderer rend = p.GetComponentInChildren<Renderer>();
        if (rend != null) rend.material.color = c;

        Animator anim = p.GetComponent<Animator>();
        if (anim != null) anim.SetTrigger(t);
    }

    IEnumerator ExecuteAttack()
    {
        isAttacking = true;

        Animator anim = currentPuppet.GetComponent<Animator>();
        if (anim != null)
        {
            anim.ResetTrigger("PoseChange");
            anim.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(1.8f);

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.Die(); 
        }

        yield return new WaitForSeconds(1.0f); 

        Transform farPuppet = currentPuppet;
        float maxDist = 0;
        foreach (Transform p in allPuppets)
        {
            float d = Vector3.Distance(p.position, player.position);
            if (d > maxDist)
            {
                maxDist = d;
                farPuppet = p;
            }
        }

        SetState(currentPuppet, Color.white, "Reset");
        JumpToPuppet(farPuppet);

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }
}