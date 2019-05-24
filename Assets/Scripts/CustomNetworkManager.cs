using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CustomNetworkManager : NetworkManager {

    const int MaxClients = 10;
    public Text connectionText;
    public Text floorText;
    public static string imageTartgetDetected = "";
    protected static short messageID = 777;
    [SerializeField]public GameObject ARCamera, map, camManager;
    [SerializeField] public GameObject Building, FirstFloor, SecondFloor, ThirdFloor, FourthFloor;
    public GameObject playerObject;
    [SerializeField] GameObject connectedPlayerPrefab;
    string hostName;
    string localIP;
    static public GameObject[] ClientRawGameObjects = new GameObject[MaxClients];
    BuildingFloors defaultBuildingLocation = new BuildingFloors();

    //Custom message class used by client to update the server
    public class customMessage : MessageBase
    {
        public string deviceType, purpose, ipAddress;
        public Vector3 devicePosition;
        public Quaternion deviceRotation;

    }

    //enum FloorLevel{
    //    first, second, third, fourth, unknown
    //}

    // Custom message class used by server 
    public class ClientCoordinates : MessageBase
    {
        public Vector3[] positions;
        public Quaternion[] rotations;
        public int numberOfClients;

    }

    private void Start()
    {

        defaultBuildingLocation.firstFloorLocation = FirstFloor.transform.position;
        defaultBuildingLocation.secondFloorLocation = SecondFloor.transform.position;
        defaultBuildingLocation.thirdFloorLocation = ThirdFloor.transform.position;
        defaultBuildingLocation.fourthFloorLocation = FourthFloor.transform.position;
       
    }

    private void Update()
    {

        //Update the location if the connected with server
        if (this.IsClientConnected()) {
            if (playerObject == null){
                //Instantiate the player anywhere.
                playerObject = GameObject.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            }else{
                CreateMessageAndSend();
            }
        }
    }

    void CreateMessageAndSend()
    {
        var msg = new customMessage();

        msg.deviceType = "iPAD";
        msg.ipAddress = localIP;
        msg.purpose = "Simulation";
        msg.devicePosition = playerObject.transform.position;
        msg.deviceRotation = playerObject.transform.rotation;
        SendToServer(msg);
    }

    [SerializeField] float updateFrequency = 0.2f;
    float nextUpdate = 0.0f;
    private void SendToServer(customMessage msg)
    {
        //TODO:- Update frequency is set. Modify this function properly
        if(Time.time >= nextUpdate){
            nextUpdate = Time.time + updateFrequency;
            client.Send(messageID, msg);
        }

    }

    //TODO: See if this function can go out of network manager
    private void OnGUI()
    {
        //Buttons
        if (GUI.Button(new Rect(5, 30, 200, 75), "Connect to Server"))
            StartClient();
        
        if (GUI.Button(new Rect(10, 150, 200, 75), "Disconnect from Server"))
            StopClient();

    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        //UI Task
        Debug.Log("connected to server. Connection address is "+conn.address.ToString());
        base.OnClientConnect(conn);
        print("local ip of this machine is " + localIP);
        connectionText.text = "Connected";
        connectionText.color = Color.green;
        Debug.Log("Connected to server " + conn.address + " with ID " + conn.connectionId);

        MessageToServer("iPad");

        this.hostName = System.Net.Dns.GetHostName();
        this.localIP = "::ffff:"+System.Net.Dns.GetHostEntry(hostName).AddressList[0].ToString();

        //TODO: Register event handler to server to client communication

        //TODO: Remove 778 handler once 800 is tested well.
        //this.client.RegisterHandler(778, OnReceivedMessage);
        this.client.RegisterHandler(800, OnReceivedCoordinates);
    }

    //Function call back when this client disconnects the server
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        connectionText.text = "Disconnected";
        connectionText.color = Color.red;
        Debug.Log("Disconnected from server " + conn.address);
    }

    //TODO: Not so important function. Can remove this implementation altogether
    void MessageToServer(string deviceConnecting)
    {
        var sendMSG = new customMessage();
        sendMSG.deviceType = deviceConnecting; // adding values to message class
        sendMSG.devicePosition = new Vector3(16.5f, 0.5f, 59);
        client.Send(messageID, sendMSG); // send message
    }


    protected void OnReceivedCoordinates(NetworkMessage netMsg)
    {
        print("Received location updates from server");
        var msg = netMsg.ReadMessage<ClientCoordinates>();

        //Scan through the client coordinate response one by one and check if there are any updates. If so update the list of clients on the client side.
        for (int i = 0; i < MaxClients ; i ++){
            Vector3 playerPosition = msg.positions[i];
            Quaternion playerRotation = msg.rotations[i];

            if (playerPosition != Vector3.zero && playerRotation != Quaternion.identity)
            {
                GameObject localPlayer;
                if (ClientRawGameObjects[i] == null)
                {
                    localPlayer = (GameObject)Instantiate(connectedPlayerPrefab, new GameObject().transform, true);
                    ClientRawGameObjects[i] = localPlayer;
                }
                else
                {
                    localPlayer = ClientRawGameObjects[i];
                }

                ClientRawGameObjects[i] = localPlayer;


                //Determine the floor
                FloorLevel floor = DetermineFloor(playerPosition);

                //Select floor model as a parent
                GameObject parent = Building;
                parent = SelectParent(floor);

                //Get position based on floor's location in the world
                Vector3 floorOffset = GetFloorOffsetForFloor(floor);

                Vector3 localizedPosition = playerPosition - floorOffset;
                localPlayer.transform.SetParent(parent.transform, true);
                //Set the local players parent as the floor model
                // Make transformation based on where the floor currently is
                // assign the localposition as required
                localPlayer.transform.localPosition = localizedPosition;
                localPlayer.transform.localRotation = playerRotation;


            }
            else
            { //TODO: Have to handle else condition
                ClientRawGameObjects[i] = null;
            }


        }
    }

    private Vector3 GetFloorOffsetForFloor(FloorLevel floorLevel)
    {
        Vector3 offset = Vector3.zero;
        switch (floorLevel){
            case FloorLevel.first:
                offset = defaultBuildingLocation.firstFloorLocation;
                break;
            case FloorLevel.second:
                offset = defaultBuildingLocation.secondFloorLocation;
                break;
            case FloorLevel.third:
                offset = defaultBuildingLocation.thirdFloorLocation;
                break;
            case FloorLevel.fourth:
                offset = defaultBuildingLocation.fourthFloorLocation;
                break;
            case FloorLevel.unknown:
                break;
        }

        return offset;
    }

    //TODO: THe function to get floor value is getting redundant in many classes. Try to optimize it.
    private GameObject SelectParent(FloorLevel floor)
    {
        GameObject parent;
        switch (floor)
        {
            case FloorLevel.first:
                {
                    parent = FirstFloor;
                    break;
                }

            case FloorLevel.second:
                {
                    parent = SecondFloor;
                    break;
                }

            case FloorLevel.third:
                {
                    parent = ThirdFloor;
                    break;
                }

            case FloorLevel.fourth:
                {
                    parent = FourthFloor;
                    break;
                }

            case FloorLevel.unknown:
                {
                    parent = Building;
                    break;
                }

            default:
                {
                    parent = Building;
                    print("Going in default case...");
                    break;
                }
        }

        return parent;
    }

    private FloorLevel DetermineFloor(Vector3 playerPosition)
    {
        //TODO: Determine the floor based on y axis.
        FloorLevel floor = FloorLevel.unknown;
        // +2 ... -1 -> Fourth Floor
        // -1... -4 -> Third Floor
        // -4 ... -8 -> Second floor
        // -8 ... -15 > First Floor
        var yPosition = playerPosition.y;
        if (yPosition > -1 && yPosition <= 4) { floor = FloorLevel.fourth; }
        else if (yPosition > -4 && yPosition <= -1) { floor = FloorLevel.third; }
        else if (yPosition > -8 && yPosition <= -4) { floor = FloorLevel.second; }
        else if (yPosition > -15 && yPosition <= -8) { floor = FloorLevel.first; }
        else { floor = FloorLevel.unknown; }
        floorText.text = floor.ToString();
        return floor;
    }

    private void HideLocalPlayer()
    {
        if (playerObject.GetComponent<Renderer>().enabled == true){
            playerObject.GetComponent<Renderer>().enabled = false;
        }
    }
}
