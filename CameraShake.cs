using UnityEngine;
using System.Collections;
using NewHorizons.Utility;

namespace TheVision
{
    public class CameraShake : MonoBehaviour
    {
        public IEnumerator Shake(float duration, float magnitude)
        {
            Vector3 originalPosition = transform.position;
           
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                //float z = UnityEngine.Random.Range(-3f, -3.3f) * magnitude;

                transform.position = new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return 0;
            }

            transform.position = originalPosition;

            
            
        }
    }
}
