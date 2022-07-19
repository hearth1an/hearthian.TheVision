using UnityEngine;
using System.Collections;

namespace TheVision
{
    public class CameraShake : MonoBehaviour
    {        
        public IEnumerator Shake(float duration, float magnitude)
        {
            Vector3 orignalPosition = transform.position;
            float elapsed = 0f;

            TheVision.Instance.ModHelper.Console.WriteLine("Shake added");

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                //float z = UnityEngine.Random.Range(-0.65f, -0.8f) * magnitude;

                transform.position = new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return 0;

                TheVision.Instance.ModHelper.Console.WriteLine("Shaking");
            }
            transform.position = orignalPosition;
        }


    }
}
