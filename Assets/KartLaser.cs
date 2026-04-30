using UnityEngine;

public class KartLaser : MonoBehaviour
{
    public AudioClip laserSound;

    private AudioSource audioSource;
    private float lastFireTime;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound, always audible
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L) && Time.time > lastFireTime + 0.5f)
        {
            if (laserSound != null)
                audioSource.PlayOneShot(laserSound);

            FireLaser();
        }
    }

    void FireLaser()
    {
        lastFireTime = Time.time;

        GameObject laser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        laser.transform.position = transform.position;
        laser.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

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
        laser.transform.position += transform.up * 40f * Time.deltaTime;

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