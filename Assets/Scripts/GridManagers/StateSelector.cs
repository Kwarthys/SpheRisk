using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class StateSelector : NetworkBehaviour
{
    public LayerMask planetMask;
    public Camera cam;
    public Planet planetManager;
    public PlayerNetworkManager playerManager;

    public override void OnStartLocalPlayer()
    {
        planetManager = GameObject.FindGameObjectWithTag("PlanetManager").GetComponent<Planet>();
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    /*
    public override void OnStartServer()
    {
        planetManager = GameObject.FindGameObjectWithTag("PlanetManager").GetComponent<Planet>();
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }
    */

    void Update()
    {
        if (!isLocalPlayer) return;

        bool fire = false;
        bool command = false;

        if(Input.GetMouseButtonDown(0))
        {
            fire = true;
        }
        else if(Input.GetMouseButtonDown(1))
        {
            fire = true;
            command = true;
        }

        if(fire)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 150, planetMask))
            {
                Vector3 clic = hit.point.normalized;
                if (!command)
                {
                    planetManager.registerClic(clic);
                }
                else
                {
                    bool bothSelected = planetManager.tryGetBothStates(clic, out State selected, out State clicked);
                    //Debug.Log("Selected:" + selected + " clicked:" + clicked);
                    if (bothSelected)
                    {
                        playerManager.submitOrder(selected.stateIndex, clicked.stateIndex);
                    }
                    else if(selected == null && clicked != null)
                    {
                        playerManager.submitOrder(-1, clicked.stateIndex);
                    }
                }
            }
        }
    }

}
