using UnityEngine;

public class KartLaser : MonoBehaviour
{
    public AudioClip laserSound;
    private AudioSource audioSource;
    private float lastFireTime;
    private ESP32Manager esp32;
    private bool vorigeLaserStatus = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        esp32 = FindObjectOfType<ESP32Manager>();
    }

    void Update()
    {
        bool huidigeStatus = esp32 != null && esp32.laserIngedrukt;
        bool risingEdge = huidigeStatus && !vorigeLaserStatus;

        if ((risingEdge || Input.GetKeyDown(KeyCode.L)) && Time.time > lastFireTime + 0.5f)
        {
            if (laserSound != null)
                audioSource.PlayOneShot(laserSound);
            FireLaser();
        }

        vorigeLaserStatus = huidigeStatus;
    }

    void FireLaser()
    {
        lastFireTime = Time.time;
        GameObject laser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        laser.transform.position = transform.position + transform.up * 0.2f;
        laser.transform.rotation = transform.rotation;
        laser.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        Destroy(laser.GetComponent<Collider>());
        Material mat = laser.GetComponent<Renderer>().material;
        mat.color = Color.red;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.red * 3f);
        Destroy(laser, 2f);
        StartCoroutine(MoveLaser(laser));
    }

    System.Collections.IEnumerator MoveLaser(GameObject laser)
    {
        while (laser != null)
        {
            laser.transform.position += laser.transform.up * 40f * Time.deltaTime;
            Collider[] hits = Physics.OverlapSphere(laser.transform.position, 0.2f);
            foreach (Collider hit in hits)
            {
                Debug.Log("Hit: " + hit.gameObject.name + " Tag: " + hit.gameObject.tag);
                if (hit.gameObject.CompareTag("Obstacle"))
                {
                    Destroy(hit.gameObject);
                    Destroy(laser);
                    yield break;
                }
            }
            yield return null;
        }
    }
}