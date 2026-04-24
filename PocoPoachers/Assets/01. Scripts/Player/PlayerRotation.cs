using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotation : MonoBehaviour
{
    private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

    private void Update()
    {
        RotateTowardMouse();
    }

    private void RotateTowardMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!GroundPlane.Raycast(ray, out float distance)) return;

        Vector3 hitPoint = ray.GetPoint(distance);
        Vector3 direction = hitPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }
}
