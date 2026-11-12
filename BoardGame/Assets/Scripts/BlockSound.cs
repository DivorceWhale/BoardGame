using UnityEngine;

// Added to each real block: knocks when it hits something hard enough.
public class BlockSound : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        GameSounds.Hit(collision.relativeVelocity.magnitude);
    }
}
