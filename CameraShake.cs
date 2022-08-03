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
                float x = UnityEngine.Random.Range(-0.5f, 0.5f) * magnitude;
                float y = UnityEngine.Random.Range(-30f, -30f) * magnitude;
                //float z = UnityEngine.Random.Range(-3f, -3.3f) * magnitude;

                transform.position = new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return 0;
            }

            // transform.position = originalPosition;

            /*var playerFixPosition = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/NoPlayerGroundCollider").GetComponent<SphereCollider>();
            Vector3 vector3 = new Vector3(15, 0, 0);
            playerFixPosition.center = vector3;
            playerFixPosition.enabled = true;
            */
            
        }
    }
}
