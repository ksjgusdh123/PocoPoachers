using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShellCasing : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private Coroutine _returnCoroutine;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Eject(Vector3 force, Vector3 torque, float lifetime)
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.AddForce(force, ForceMode.VelocityChange);
        _rigidbody.AddTorque(torque, ForceMode.VelocityChange);

        if (_returnCoroutine != null) StopCoroutine(_returnCoroutine);
        _returnCoroutine = StartCoroutine(ReturnAfterDelay(lifetime));
    }

    private IEnumerator ReturnAfterDelay(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        ShellCasingPool.Instance.Release(this);
    }
}
