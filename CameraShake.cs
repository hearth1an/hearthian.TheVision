using UnityEngine;
using System.Collections;

namespace TheVision
{
    public class CameraShake : MonoBehaviour
    {        
        public IEnumerator Shake(float duration, float magnitude)
        {
            Vector3 orignalPosition = transform.position;
            orignalPosition.z = -5f;
            

            //Vector3 newPosition = transform.position = new Vector3(x, y, 0f);
            float elapsed = 0f;            

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-2f, 2f) * magnitude;
                float y = UnityEngine.Random.Range(-2f, 2f) * magnitude;
                float z = UnityEngine.Random.Range(-3f, -3.3f) * magnitude;

                Vector3 newPosition = transform.position = new Vector3(x, y, z);
                elapsed += Time.deltaTime;
                yield return 0;
               
            }
            
            transform.position = orignalPosition;
           


        }


    }
}
