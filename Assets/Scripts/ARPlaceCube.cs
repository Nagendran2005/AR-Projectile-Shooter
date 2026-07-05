using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlaceCube : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject objectPrefab;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button placeButton;

    [TextArea]
    public string detectingMessage = "Searching for a floor...";

    [TextArea]
    public string detectedMessage = "Floor Detected!\nPress the button to place the object.";

    [Header("Button Blink")]
    public Color blinkColor = Color.green;
    public float blinkSpeed = 2f;

    private Color normalColor;
    private Image buttonImage;

    private bool planeDetected = false;
    private bool isPlacing = false;

    void Start()
    {
        buttonImage = placeButton.GetComponent<Image>();
        normalColor = buttonImage.color;

        placeButton.gameObject.SetActive(false);
        statusText.text = detectingMessage;

        placeButton.onClick.AddListener(OnPlaceButtonPressed);
    }

    void Update()
    {
        if (raycastManager == null)
            return;

        CheckPlaneDetection();

        if (planeDetected)
        {
            BlinkButton();
        }
    }

    void CheckPlaneDetection()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        bool foundPlane = raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);

        if (foundPlane && !planeDetected)
        {
            planeDetected = true;

            statusText.text = detectedMessage;

            placeButton.gameObject.SetActive(true);
        }
        else if (!foundPlane && planeDetected)
        {
            planeDetected = false;

            statusText.text = detectingMessage;

            placeButton.gameObject.SetActive(false);

            buttonImage.color = normalColor;
        }
    }

    void BlinkButton()
    {
        float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);

        buttonImage.color = Color.Lerp(normalColor, blinkColor, t);
    }

    void OnPlaceButtonPressed()
    {
        if (isPlacing)
            return;

        isPlacing = true;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        PlaceObject(screenCenter);

        StartCoroutine(ResetPlacement());
    }

    void PlaceObject(Vector2 position)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        if (raycastManager.Raycast(position, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;

            Instantiate(objectPrefab, pose.position, pose.rotation);

            placeButton.gameObject.SetActive(false);

            statusText.text = "Object Placed Successfully!";
        }
    }

    IEnumerator ResetPlacement()
    {
        yield return new WaitForSeconds(0.25f);
        isPlacing = false;
    }
}