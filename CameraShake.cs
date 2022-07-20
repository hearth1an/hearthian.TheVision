using UnityEngine;
using System.Collections;

namespace TheVision
{
    public class CameraShake : MonoBehaviour
    {        
        public IEnumerator Shake(float duration, float magnitude)
        {
            Vector3 orignalPosition1 = transform.position;
            Vector3 orignalPosition2 = transform.position;

            //Vector3 newPosition = transform.position = new Vector3(x, y, 0f);
            float elapsed = 0f;            

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-2f, 2f) * magnitude;
                float y = UnityEngine.Random.Range(-2f, 2f) * magnitude;
                //float z = UnityEngine.Random.Range(-0.65f, -0.8f) * magnitude;

                Vector3 newPosition = transform.position = new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return 0;
               
            }
            orignalPosition1.z = -1f;
            transform.position = orignalPosition2;


        }


    }
}
