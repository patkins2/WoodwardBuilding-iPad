using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NodeNavigation : MonoBehaviour {
    
    [SerializeField] GameObject PlayerMovementGameObject;
    PlayerMovement playerMovementManager;

    public static NodeNavigation instance;
    public TextAsset textFile;
    public TextAsset Floor1LocationFile, Floor2LocationFile, Floor3LocationFile, Floor4LocationFile;
    [SerializeField] public GameObject Building, FirstFloor, SecondFloor, ThirdFloor, FourthFloor;
    public NavigationViz navViz;
    bool PlayerToPlayerNavigation = false;
    // Start is called before the first frame update
    public static string startNode, destination;
    Dictionary<string, ArrayList> graph = new Dictionary<string, ArrayList>();
    public static ArrayList possiblePaths = new ArrayList();

    private void Awake(){
        if (instance == null){
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else{
            Destroy(gameObject);
        }

    }

    void Start()
    {
        //Check for player movement manager to get floor level.
        playerMovementManager = PlayerMovementGameObject.GetComponent<PlayerMovement>();
        navViz = new NavigationViz();
        GenerateGraph();
    }

    private void Update()
    {
        //Check if Player to Player Navigation is on. If so do!
        PerformPlayerToPlayerNavigation();
    }

    private void PerformPlayerToPlayerNavigation()
    {
        if(PlayerToPlayerNavigation){
            float updateFrequency = 3f;
            float nextUpdate = 0.0f;
            //TODO:- Update frequency is set. Modify this function properly
            if (Time.time >= nextUpdate)
            {
                GameObject player1 = CustomNetworkManager.ClientRawGameObjects[0];
                GameObject player2 = CustomNetworkManager.ClientRawGameObjects[1];

                if (player2 == null){
                    PlayerToPlayerNavigation = false;
                    return;
                }
                //Cancel the operation if the player2 coordinates are not recived
                if (player2.transform.position == Vector3.zero)
                {
                    print("Player 2 doesn't seem to be a moving real person. Or there are no two players to start this functionality");
                    PlayerToPlayerNavigation = false;
                    return;
                }

                ShowPathBetween(player1, player2);
                nextUpdate = Time.time + updateFrequency;
            }
        }
    }

    //GenerateGraph method will create a datastructure in form of Key value pairs reading from provided text file. It is an independent function 
    void GenerateGraph()
    {
        string text = textFile.text;
        string[] nodeCollection = text.Split('\n');

        foreach (string nodeList in nodeCollection)
        {
            string[] nodes = nodeList.Split(' ');
            var key = nodes[0];
            var childrens = new ArrayList();
            for (int i = 1; i < nodes.Length; i++)
            {
                //print("Adding child to node "+ key +" as " + nodes[i]);
                childrens.Add(nodes[i]);
            }
            graph.Add(key, childrens);
        }
        print("Graph is generated");
    }


    // SetStartNode method is responsible to take find out which floor the player currently is, and scan through each node in that floor to determine which one is the nearest node for the player.
    public void SetStartNode(){
        if (playerMovementManager == null)
        {
            print("Please attach Player Movement GameObject to Node Navigation GameObject in IDE");
            return;
        }
       
        startNode = NearestNode(playerMovementManager.virtualPlayer);

    }

    //Function to calculate nearest node of given gameObject in the floor plan. 
    string NearestNode(GameObject playerGameObject)
    {
        FloorLevel floorLevel = PlayerMovement.getFloorLevel(playerGameObject);


        TextAsset floorNodes = null;
        GameObject parentFloor = null;

        GetParentFloorandAsset(floorLevel, ref floorNodes, ref parentFloor);

        string locationString = floorNodes.text;
        string[] possibleSources = locationString.Split('\n');
        string nearestNode = "";
        float shortestDistance = float.MaxValue;

        Vector3 localizedPosition = playerGameObject.transform.position - parentFloor.transform.position;

        foreach (string node in possibleSources)
        {
            GameObject possibleSource = GameObject.Find(node);
            if (possibleSource == null)
            {
                print("Game object named " + node + " was not found..");
            }
            float distance = (localizedPosition - possibleSource.transform.position).magnitude;
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestNode = node;
            }
        }

        return nearestNode;
    }

    private void GetParentFloorandAsset(FloorLevel floor, ref TextAsset floorNodes, ref GameObject parentFloor)
    {
        switch (floor)
        {
            case FloorLevel.first:
                floorNodes = Floor1LocationFile;
                parentFloor = FirstFloor;
                break;
            case FloorLevel.second:
                floorNodes = Floor2LocationFile;
                parentFloor = SecondFloor;
                break;
            case FloorLevel.third:
                floorNodes = Floor3LocationFile;
                parentFloor = ThirdFloor;
                break;
            case FloorLevel.fourth:
                floorNodes = Floor4LocationFile;
                parentFloor = FourthFloor;
                break;
            case FloorLevel.unknown:
                print("Floor not yet determined");
                parentFloor = Building;
                return;
        }
        return;
    }

    //GetAllPaths uses the dictionary created by Generategraph to find all possible paths from starNode to destination. Make sure startnode and destination are available before calling this method.
    public void GetAllPaths(){

        SetStartNode();

        if (startNode == "")
        {
            print("Start failed to set...");
            return;
        }

        if (destination == "")
        {
            print("Destination not available");
            return;
        }

        GeneratePossiblePaths(startNode, destination);
        //var paths = new ArrayList();
        //paths.Add(startNode);

        //Queue queue = new Queue();
        //queue.Enqueue(paths);
        //possiblePaths.Clear();

        //while (queue.Count > 0){
        //    var temporaryPath = queue.Dequeue() as ArrayList;
        //    var last = temporaryPath[temporaryPath.Count - 1] as string;

        //    //Get list of nodes that this last element of the path can reach to 
        //    //print("Trying to access children for node " + last + " in the graph");
        //    var children = graph[last] as ArrayList;

        //    //print("The last element is of current path is " + last);
        //    if (last == destination){
        //        print("A path has been found.");
        //        possiblePaths.Add(temporaryPath);
        //        string pathPrint = "";
        //        for (int l = 0; l < temporaryPath.Count; l++){
        //            //print(temporaryPath[l]);
        //            pathPrint += " -> ";
        //            pathPrint += temporaryPath[l];
        //        }
        //        print("It goes like : "+pathPrint);
        //    }

        //    //for each child create new path and new paths to the queue.
        //    for (int i = 0; i < children.Count; i++){
        //        string child = children[i] as string;
        //        if (!existsInPath(child, temporaryPath)){
        //            ArrayList newPath = new ArrayList();
        //            foreach (string element in temporaryPath)
        //            {
        //                newPath.Add(element);
        //            }
        //            newPath.Add(child);
        //            queue.Enqueue(newPath);
        //        }
        //    }
        //}
        drawPath();
    }

    void GeneratePossiblePaths(string startNode, string destination){
        var paths = new ArrayList();
        paths.Add(startNode);

        Queue queue = new Queue();
        queue.Enqueue(paths);
        possiblePaths.Clear();

        while (queue.Count > 0)
        {
            var temporaryPath = queue.Dequeue() as ArrayList;
            var last = temporaryPath[temporaryPath.Count - 1] as string;

            //Get list of nodes that this last element of the path can reach to 
            //print("Trying to access children for node " + last + " in the graph");
            var children = graph[last] as ArrayList;

            //print("The last element is of current path is " + last);
            if (last == destination)
            {
                print("A path has been found.");
                possiblePaths.Add(temporaryPath);
                string pathPrint = "";
                for (int l = 0; l < temporaryPath.Count; l++)
                {
                    //print(temporaryPath[l]);
                    pathPrint += " -> ";
                    pathPrint += temporaryPath[l];
                }
                print("It goes like : " + pathPrint);
            }

            //for each child create new path and new paths to the queue.
            for (int i = 0; i < children.Count; i++)
            {
                string child = children[i] as string;
                if (!existsInPath(child, temporaryPath))
                {
                    ArrayList newPath = new ArrayList();
                    foreach (string element in temporaryPath)
                    {
                        newPath.Add(element);
                    }
                    newPath.Add(child);
                    queue.Enqueue(newPath);
                }
            }
        }
    }

    public void ShowPathBetweenUser()
    {
        //Set start node to gameobject near to client 1
        PlayerToPlayerNavigation = true;

       

    }

    //IEnumerator helps starting a coroutine thus avoiding infinite while loop.
    private void ShowPathBetween(GameObject player1, GameObject player2)
    {
        startNode = SetNearestNodeOf(player1);
        destination = SetNearestNodeOf(player2);
        GeneratePossiblePaths(startNode, destination);
        reDrawPath();
    }

    public void stopPlayerToPlayerNavigation(){
        PlayerToPlayerNavigation = false;
        clearPaths();
    }





private string SetNearestNodeOf(GameObject player)
    {
        return NearestNode(player);
    }

    public void clearPaths(){
        navViz.clearLineRenderer();
    }

    public void reDrawPath(){
        clearPaths();
        drawPath();
    }

    public void drawPath(){
        navViz.pathCalculationCompleled();
    }

    private bool existsInPath(string child, ArrayList temporaryPath)
    {
        for (int i = 0; i < temporaryPath.Count; i++){
            if (temporaryPath[i] as string == child){
                return true;
            }
        }
        return false;
    }
}



//Extre code to be deleted
class BuildingNode{

    Dictionary<string, int> connections;
    string name;
    //string parent;
    public BuildingNode(string name, string parent){
        this.name = name;
        //this.parent = parent;
    }


    public void AddConnection(string nodeName, int distance){

        if (connections == null){
            connections = new Dictionary<string, int>();
        }

        connections.Add(nodeName, distance);

    }

}




