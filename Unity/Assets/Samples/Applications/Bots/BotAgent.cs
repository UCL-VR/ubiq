using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Ubiq.Samples.Bots
{
    /// <summary>
    /// The main controller for a Bot; this Component controls the behaviour of the bot, moving it around the scene
    /// and emulating interaction.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class BotAgent : MonoBehaviour
    {
        private NavMeshAgent navMeshAgent;
        private Vector3 destination;

        public float Radius = 10f;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            // Have the agent wander around the scene
            if(Vector3.Distance(destination, transform.position) < 1f)
            {
                Wander();
            }
        }

        public void Wander()
        {
            NavMeshHit hit;
            do
            {
                destination = transform.position + Random.insideUnitSphere * Radius;
            } while (!NavMesh.SamplePosition(destination, out hit, float.MaxValue, NavMesh.AllAreas));
            destination = hit.position;
            navMeshAgent.destination = destination;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(destination, 0.5f);
        }
    }
}