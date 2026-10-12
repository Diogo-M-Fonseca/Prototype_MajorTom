using UnityEngine;

public class TargetTracker : MonoBehaviour
{

    private Transform _current;
    private Vector3 _lastPosition;
    private Vector3 _velocity;

    public TargetInfo Sample(Transform target, float dt)
    {
        if (target == null)
        {
            _current = null;
            return default;
        }

        Vector3 position = target.position;

        if (target != _current)
        {
            _current = target;
            _velocity = Vector3.zero;
        }
        else if (dt > 0)
        {
            _velocity = (position - _lastPosition) / dt;
        }

        _lastPosition = position;
        return new TargetInfo(true, position, _velocity);
    }



}
