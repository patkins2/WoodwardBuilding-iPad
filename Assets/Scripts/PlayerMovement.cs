using UnityEngine;


public enum FloorLevel
{
    first, second, third, fourth, unknown
}

/// <summary>
/// A class dedicated to update the player's location based on AR Camera and update it according to the image target that is found.
/// This location will be sent to the server.
/// </summary>
public class PlayerMovement : MonoBehaviour {



    //ServerImage Target is place which is universal. Each client and server will have server image location targets. 
    //TODO: The 'image' word is misleading. Will update the name in next update. Should be something like ServerTargetLocation
    //TODO: The ServerImageTarget should not be Serialize field. It should be dynamically updated when particular image target is found.
    [SerializeField] public GameObject ServerImageTarget;

    //PlayerImageTarget is the vuforia image that the camera detects. It is static so that Vuforia DefaultTrackable can directly assign the detected image's transform here. Thus we have track of image target's transform thus being able to calculate the offset.
    public static GameObject PlayerImageTarget;

    //PlayerCamera is the ARCamera component which moves as the device moves. Can be Hololens or iPad. Drag and drop ARCamera component to this field. Virtual player is a representatinve of player which will change its location based on playercamera. Once we detect the image target, placyercamera is not moved as it is replicating the ARCamera as it is. whatever location and rotation manupulation we have to do is been performed in virtual player object. The same virtual player's transform is continuously sent to server as this player's updated location.
    public GameObject PlayerCamera, virtualPlayer;

    //These are simple checks to reduce some non required loops. Need to improve that
    public static bool vuforiaTargetDetected = false;
    public static string lastImageTracked = "";

    //TODO: Seems like it compensates the initial rotation that image target introduces. Need more analysis.
    private readonly Quaternion xAxisRot = Quaternion.AngleAxis(90, Vector3.right);

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        //Stop scanning after it detects. And keep some voice command or button to enable scanning again.
        if (lastImageTracked == "")
        {
            scanImageTarget();
        }

        //move virtual player based on real player's movement
        UpdatePlayerPositionAndRotation();
    }

    //TODO: Code improvement requried. Scan image flow can be easy and modular.
    private void scanImageTarget()
    {
        if (vuforiaTargetDetected)
        {
            vuforiaTargetDetected = false;
            lastImageTracked = "actualImageTarget. To be dynamic in next update";
        }
    }


    private void UpdatePlayerPositionAndRotation()
    {
        if (lastImageTracked != "")
        {
            RepositionThePlayer();
            RotateThePlayer();
        }
        else
        {
            //Assign camera coordinates to virtual player
            virtualPlayer.transform.position = PlayerCamera.transform.position;
            virtualPlayer.transform.rotation = PlayerCamera.transform.rotation;
        }
    }

    private void RepositionThePlayer()
    {
        //TODO: GetThe Player Image Target which was last recognized in imageTargetInPlayerWorld
        //Code from Elias
        print("Player Image Target transform : " + PlayerImageTarget);
        var m = Matrix4x4.TRS(PlayerCamera.transform.position - PlayerImageTarget.transform.position,
                              PlayerImageTarget.transform.rotation * xAxisRot, 
                              Vector3.one);
        m = m.inverse;

        //Positioning the player
        var pos = MatrixUtils.ExtractTranslationFromMatrix(ref m);
        pos.y = -pos.y;
        //Setting the virtual player as a child of Image Target (for server)
        virtualPlayer.transform.SetParent(ServerImageTarget.transform, false);
        //Assigning local position to virtual player with rewspect to Image Target
        virtualPlayer.transform.localPosition = pos;
    }

    private void RotateThePlayer()
    {
        //Rotating the player
        //Code from Elias
        var targetFlatForward = PlayerImageTarget.transform.up;
        targetFlatForward.y = 0;
        targetFlatForward.Normalize();
        var targQuat = Quaternion.LookRotation(targetFlatForward, Vector3.up);

        var cameraFlatForward = PlayerCamera.transform.forward;
        cameraFlatForward.y = 0;
        cameraFlatForward.Normalize();
        var camQuat = Quaternion.LookRotation(cameraFlatForward, Vector3.up);

        var flatAngle = Quaternion.Inverse(targQuat) * camQuat;
        virtualPlayer.transform.localRotation = flatAngle;

    }

    //A method determining what floor is player located in. This is not a robust approach I feel because this is hard coded based on values that I found via accelerometer data.
    //And also the data will change for each building and we dont want to touch this horrible code :P on adding new building models right?
    public FloorLevel GetFloor(){
     
        return getFloorLevel(virtualPlayer);
    }

    static public FloorLevel getFloorLevel(GameObject playerRawCoordinates){
        var yPosition = playerRawCoordinates.transform.position.y;
        //TODO: Determine the floor based on y axis.
        FloorLevel floor = FloorLevel.unknown;
        // +2 ... -1 -> Fourth Floor
        // -1... -4 -> Third Floor
        // -4 ... -8 -> Second floor
        // -8 ... -15 > First Floor
        if (yPosition > -1 && yPosition <= 4) { floor = FloorLevel.fourth; }
        else if (yPosition > -4 && yPosition <= -1) { floor = FloorLevel.third; }
        else if (yPosition > -8 && yPosition <= -4) { floor = FloorLevel.second; }
        else if (yPosition > -15 && yPosition <= -8) { floor = FloorLevel.first; }
        else { floor = FloorLevel.unknown; }
        return floor;
    }



}
