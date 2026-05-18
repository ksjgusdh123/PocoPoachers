using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] private ParticleSystem _forwardFlash;
    [SerializeField] private ParticleSystem _coneFlashes;

    public void Play()
    {
        _forwardFlash.Play();
        _coneFlashes.Play();
    }
}
