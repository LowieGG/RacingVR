using UnityEngine;

public class GazeDetector : MonoBehaviour
{
    public float gazeDistance = 2f;
    public bool testMetMuis = true; // Zet uit als je VR bril gebruikt
    private GameObject huidigObject;

    void Update()
    {
        Ray ray;

        Debug.DrawRay(transform.position, transform.forward * gazeDistance, Color.blue);

        if (testMetMuis)
        {
            // Test met muis vanuit de camera
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        }
        else
        {
            // Echte gaze vanuit VR camera
            ray = new Ray(transform.position, transform.forward);
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, gazeDistance))
        {
            GameObject geraakt = hit.collider.gameObject;
            if (geraakt != huidigObject)
            {
                if (huidigObject != null)
                    VerbergLabel(huidigObject);
                huidigObject = geraakt;
                ToonLabel(geraakt);
                Debug.Log("Kijkt naar: " + geraakt.name);
            }
        }
        else
        {
            if (huidigObject != null)
            {
                VerbergLabel(huidigObject);
                huidigObject = null;
            }
        }
    }

    void ToonLabel(GameObject obj)
    {
        Canvas canvas = obj.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
            canvas.gameObject.SetActive(true);
    }

    void VerbergLabel(GameObject obj)
    {
        Canvas canvas = obj.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }
}
