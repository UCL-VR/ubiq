using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Samples;
using System.Linq;
public class Skeleton : MonoBehaviour
{

    // GameObject Player;
    public FloatingAvatar[] PlayerAvatars;

    Animator animator;

    public float attackCoolDownPeriod = 5.0f;

    float lastAttackTime;

    void Awake()
    {
        animator = GetComponent<Animator>();
        lastAttackTime = Time.time;
    }

    // Start is called before the first frame update
    void Start()
    {
        // ThreePointTrackedAvatar
        PlayerAvatars = GameObject.FindObjectsOfType<FloatingAvatar>();
        foreach (var item in PlayerAvatars)
        {
            Debug.Log(item.name);
        }

        // AvatarManager avatarManager = GameObject.FindObjectOfType<AvatarManager>();
        // avatarManager.Avatars.GetPrefab(avatarManager.localPrefabUuid);
    }

    // Update is called once per frame
    void Update()
    {
        if(PlayerAvatars.Length == 0)
        {
            PlayerAvatars = GameObject.FindObjectsOfType<FloatingAvatar>();
            foreach (var item in PlayerAvatars)
            {
                Debug.Log(item.name);
            }
        } 
        else
        {
            PlayerAvatars = PlayerAvatars.OrderBy((a) => (a.torso.position - transform.position).sqrMagnitude).ToArray();
            // Debug.Log(PlayerAvatars[0].torso.position);
            Vector3 lookPos = new Vector3(PlayerAvatars[0].torso.position.x , 0, PlayerAvatars[0].torso.position.z);
            transform.LookAt(lookPos);
            if(Vector3.Distance (transform.position, lookPos) > 1.0f) 
            {
                // Vector3 diff = PlayerAvatars[0].torso.position - transform.position ;
                // Debug.Log(diff);
                transform.Translate(new Vector3 (0, 0,  1.0f * Time.deltaTime));
                animator.SetFloat("Speed", 0.2f);
            }
            else
            {
                animator.SetFloat("Speed", 0);

                if(Time.time - lastAttackTime > attackCoolDownPeriod)
                {
                    animator.SetTrigger("Attack");
                    lastAttackTime = Time.time;
                }
            }
        }
    }
}
