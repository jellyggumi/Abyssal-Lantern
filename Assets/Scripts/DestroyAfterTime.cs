using UnityEngine;

namespace CastleBusters
{
    public class DestroyAfterTime : MonoBehaviour
    {
        public float lifetime = 2f;

        private void Start() => Destroy(gameObject, lifetime);

    }
}
