using UnityEngine;

public class RefreshParticles : MonoBehaviour
{
    public Material particlePrefab;
    ParticleSystemRenderer particles;

    void OnEnable()
    {
        particles = GetComponent<ParticleSystemRenderer>();   
        particles.material = particlePrefab;
    }
}
