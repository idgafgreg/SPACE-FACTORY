using UnityEngine;

/// <summary>
/// Reusable waypoint-follower for non-enemy objects (e.g. animated carts,
/// tutorial indicators). For enemies, use the movement built into EnemyBase.
/// </summary>
public class PathFollower : MonoBehaviour
{
    [Header("Config")]
    public LanePath path;
    public float    speed        = 2f;
    public bool     loop         = false;
    public bool     playOnStart  = true;

    int  _index;
    bool _active;

    void Start() { if (playOnStart) Play(); }

    public void Play()
    {
        _index  = 0;
        _active = true;
    }

    public void Stop() => _active = false;

    void Update()
    {
        if (!_active || path == null || path.PointCount == 0) return;

        Vector3 target = path.GetPoint(_index);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.02f)
        {
            _index++;
            if (_index >= path.PointCount)
            {
                if (loop) _index = 0;
                else       Stop();
            }
        }
    }
}
