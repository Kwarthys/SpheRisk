using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraController : NetworkBehaviour
{
    public Transform horizontalRotateHelper;
    public Transform verticalRotateHelper;

    public Transform cameraTransform;
    public Transform playerCueTransform;

    public float horizontalSpeed = 1f;

    public Vector2 verticalMinMax = new Vector2(-80, 80);
    public float verticalSpeed = 1f;
    private float verticalCounter = 0.5f;

    public Vector2 cameraForwardMinMax = new Vector2(-12,-30);
    public float forwardSpeed = 1f;
    private float camForwardCounter = 0;

    public float camHeightToSpeedCoef = 1 / 12;

    public Transform atmHolder;
    public Transform atmosphere;
    public Vector2 atmoScaleFromTo = new Vector2(1, 10);

    public override void OnStartLocalPlayer()
    {
        if (isLocalPlayer)
        {
            atmosphere = GameObject.FindGameObjectWithTag("Atmosphere").transform;
            cameraTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;

            atmosphere.parent = atmHolder;
            atmosphere.localPosition = Vector3.zero;
            atmosphere.localRotation = Quaternion.identity;
            cameraTransform.parent = playerCueTransform;
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float z = Input.mouseScrollDelta.y;

        moveCamera(h, v, z);
    }

    public void moveCamera(float h, float v, float scroll)
    {
        if (h != 0)
        {
            horizontalRotateHelper.Rotate(new Vector3(0, -horizontalSpeed * Time.deltaTime * h * camHeightToSpeedCoef * -playerCueTransform.localPosition.z, 0));
        }

        if (v != 0)
        {
            verticalCounter += verticalSpeed * Time.deltaTime * v * camHeightToSpeedCoef * -playerCueTransform.localPosition.z;
            verticalCounter = Mathf.Clamp01(verticalCounter);
            verticalRotateHelper.localRotation = Quaternion.Euler(new Vector3(Mathf.Lerp(verticalMinMax.x, verticalMinMax.y, verticalCounter), 0, 0));
        }

        if (scroll != 0)
        {
            camForwardCounter -= scroll * Time.deltaTime * forwardSpeed;
            camForwardCounter = Mathf.Clamp01(camForwardCounter);
            playerCueTransform.localPosition = new Vector3(0, 0, Mathf.Lerp(cameraForwardMinMax.x, cameraForwardMinMax.y, camForwardCounter));

            float s = Mathf.Lerp(atmoScaleFromTo.y, atmoScaleFromTo.x, 1 - ((1 - camForwardCounter) * (1 - camForwardCounter)));
            atmHolder.localScale = new Vector3(s, s, s);
        }
    }
}
