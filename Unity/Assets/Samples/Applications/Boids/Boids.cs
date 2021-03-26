using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;

namespace Ubiq.Samples.Boids
{
    public class Boids : MonoBehaviour, INetworkObject
    {
        public NetworkId Id { get; set; }

        public GameObject boidPrefab;

        public Bounds bounds;
        public bool globalInfluence=true;
        public int numberLocalBoids = 10;
        public float inertia_factor = 0.005f;
        public float pull_factor = 1.0f;
        public float proximity_distance = 5.0f;
        public float proximity_factor = 2.5f;
        public float bound_velocity = 0.2f;
        public float max_velocity = 15.0f;

        public GameObject[] boids;

        /// <summary>
        /// Indicates that this represented a sub-flock of local boids. This flag is informational only. Child components do not have to use it.
        /// </summary>
        public bool local;

        public void Awake()
        {
            boids = new GameObject[numberLocalBoids];
            for (int i = 0; i < numberLocalBoids; i++)
            {
                boids[i] = Instantiate(boidPrefab) as GameObject;
                boids[i].transform.SetParent(this.transform);
                boids[i].transform.localPosition = new Vector3(Random.Range(bounds.min.x, bounds.max.x), Random.Range(bounds.min.y, bounds.max.y), Random.Range(bounds.min.z, bounds.max.z));
            }
        }

        private void Reset()
        {
            local = false; // better not to transmit by accident than to transmit by accident!
        }

        public void Update()
        {
            /* Flock simulation */
            Vector3 center = new Vector3();
            Vector3 inertia=new Vector3();
            Vector3 tmp = new Vector3();
            int proximate;
            float t;

            t = Time.deltaTime;
            if (t > 0.1) t = 0.1f; // Limit the extrapolation to something reasonable

            /* Get the overall characteristics of all the boids in the simulation */
            Boids[] allFlocks;

            if (globalInfluence) // My flock is influenced by the other flocks
            {
                allFlocks = this.transform.parent.GetComponentsInChildren<Boids>();
            }
            else // My flock ignores all the other flocks
            {
                allFlocks = new Boids[1];
                allFlocks[0] = this;
            }

            int totalBoidsCount = 0;

            foreach (Boids anyboids in allFlocks)
            {
                for (int i = 0; i < anyboids.numberLocalBoids; i++)
                {
                    center = anyboids.boids[i].transform.localPosition + center; /* Flock center */
                    inertia = anyboids.boids[i].GetComponent<BoidState>().velocity + inertia; /* Flock inertia */
                    totalBoidsCount++;
                }
            }

            center = center * (1.0f / (totalBoidsCount)); /* Take average to get flock center */
            inertia = inertia * (inertia_factor * t);

            /* This loop iterates over local  boids only because we simulate change only to the local ones */
            for (int i = 0; i < numberLocalBoids; i++)
            {
                Vector3 lcenter=new Vector3();

                /* Flock Pull */
                tmp = center - boids[i].transform.localPosition;
                tmp = tmp * (pull_factor * t);
                boids[i].GetComponent<BoidState>().velocity = tmp + boids[i].GetComponent<BoidState>().velocity;

                /* Flock Inertia */
                boids[i].GetComponent<BoidState>().velocity = inertia + boids[i].GetComponent<BoidState>().velocity;

                /* Neighbor Avoidance */
                proximate = 0;
                foreach (Boids anyboids in allFlocks)
                {
                    for (int j = 0; j < anyboids.numberLocalBoids; j++)
                    {
                        if (!(anyboids==this && i==j))
                        {
                            tmp = boids[i].transform.localPosition - anyboids.boids[j].transform.localPosition;
                            if (tmp.magnitude < proximity_distance)
                            {
                                proximate++;
                                lcenter = tmp + lcenter;
                            }
                        }
                    }
                }
                if (proximate>0)
                {
                    lcenter = lcenter * (proximity_factor * t);
                    boids[i].GetComponent<BoidState>().velocity = lcenter + boids[i].GetComponent<BoidState>().velocity;
                }

                /* Max velocity */
                float speed = boids[i].GetComponent<BoidState>().velocity.magnitude;
                if (speed > max_velocity)
                {
                    boids[i].GetComponent<BoidState>().velocity *= max_velocity / speed;
                }

                /* Calc new position */
                boids[i].transform.localPosition = boids[i].transform.localPosition + (boids[i].GetComponent<BoidState>().velocity * t);

                /* World limits */
                for (int j = 0; j < 3; j++)
                {
                    if (boids[i].transform.localPosition[j] > bounds.center[j] + bounds.extents[j])
                        boids[i].GetComponent<BoidState>().velocity[j] *=-1f;
                    if (boids[i].transform.localPosition[j] < bounds.center[j] - bounds.extents[j])
                        boids[i].GetComponent<BoidState>().velocity[j] *=-1f;
                }
            }
        }
    }
}