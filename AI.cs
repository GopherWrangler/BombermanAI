using UnityEngine;
using System.Collections.Generic;
using System;

public enum Direction
{
    FORWARD,
    BACK,
    LEFT,
    RIGHT
}

public class RaycastInfo
{
    public Vector3 vec;
    public Vector3 localVec;
    public RaycastHit hitInfo;
    public Direction direction;
}

[Serializable]
public class State
{
    public Vector3 targetPos;
    public StateManager m_owner;
    public string m_name;

    public List<RaycastInfo> raycasts = new List<RaycastInfo>();


    public State(StateManager owner)
    {
        m_name = ToString();
        m_owner = owner;
        //m_owner.m_Pos = m_owner.gObject.transform.position;
        //m_owner.gObject = GameObject.FindWithTag("Player");
        raycasts.Add(new RaycastInfo { localVec = new Vector3(0, 0, 1), direction = Direction.FORWARD });
        raycasts.Add(new RaycastInfo { localVec = new Vector3(0, 0, -1), direction = Direction.BACK });
        raycasts.Add(new RaycastInfo { localVec = new Vector3(1, 0, 0), direction = Direction.RIGHT });
        raycasts.Add(new RaycastInfo { localVec = new Vector3(-1, 0, 0), direction = Direction.LEFT });
    }

    virtual public void Update()
    {
        foreach (RaycastInfo raycast in raycasts)
        {
            raycast.vec = m_owner.gObject.transform.TransformDirection(raycast.localVec);
        }
    }
}


[Serializable]
public class State_FindingBlock : State
{
    float targetDist;
    //Vector3 targetPos;

    public State_FindingBlock(StateManager owner) : base(owner)
    {
        targetDist = 10.0f;
    }

    override public void Update()
    {
        base.Update();
        //targetDist = 10.0f;
        // If I found a block, change states

        foreach (RaycastInfo raycast in raycasts)
        {
            if (Physics.Raycast(m_owner.gObject.transform.position, raycast.vec, out raycast.hitInfo, 3.0f))
            {
                if (raycast.hitInfo.collider.gameObject.tag == "DestroyBlock")
                {
                    if (raycast.hitInfo.distance < targetDist)
                    {
                        targetDist = raycast.hitInfo.distance;
                        targetPos = raycast.hitInfo.collider.gameObject.transform.position;
                    }
                }
            }
        }

        //If no block found, move one block and search again
        if (targetDist == 10.0f)
        {
            m_owner.SetState(new State_MoveOneUnit(m_owner));
        }
        else
        {
            m_owner.SetState(new State_MovingToBlock(targetPos, m_owner));  // Move to block at targetPos
        }
        Debug.Log("Finding Block\n");
    }
}

[Serializable]
public class State_MoveOneUnit : State
{
    public State_MoveOneUnit(StateManager owner) : base(owner)
    {

    }
    public override void Update()
    {
        base.Update();
        foreach (RaycastInfo raycast in raycasts)
        {
            if (!Physics.Raycast(m_owner.gObject.transform.position, raycast.vec, out raycast.hitInfo, 1.0f))
            {
                m_owner.SetState(new State_MovingToBlock(raycast.vec, m_owner));
            }
        }
    }
}

[Serializable]
public class State_MovingToBlock : State
{

    Vector3 target;
    float dTime;

    public State_MovingToBlock(Vector3 pos, StateManager owner) : base(owner)
    {
        dTime = Time.deltaTime;

        if (target != pos)
        {
            target = pos;
        }

    }

    override public void Update()
    {
        base.Update();

        // ========== MOVE TO MAIN UPDATE ==========//  
        /*
        foreach (RaycastInfo raycast in raycasts)
        {
            Physics.Raycast(m_owner.gObject.transform.position, raycast.vec, out raycast.hitInfo, 3.0f);
            if (raycast.hitInfo.collider.tag == "Player")
            {
                //  Search for nearby players, if found: m_owner.SetState(new State_ChasingPlayer())
                m_owner.SetState(new State_ChasingPlayer(m_owner, raycast.hitInfo.collider.transform.position));
            }
        }
        */
        // Move to position 
        //if (m_owner.gObject.transform.position != targetPos) // Set to collision, not position
        if (m_owner.gObject.transform.position != target)
        {
            m_owner.gObject.GetComponent<AI>().targetPos = target;                                  // Set target for movement in Main Update
            Debug.Log("Moving to Block\n");

            if (GameVariables.AIDestroyBlockCollide)
            {
                m_owner.SetState(new State_PlacingBomb(m_owner));
                GameVariables.PlaceBomb = true;
            }
        }
        else
        {
            m_owner.SetState(new State_FindingBlock(m_owner));
        }
    }
}

[Serializable]
public class State_PlacingBomb : State
{
    bool HaveBomb;
    GameObject Bomb;
    GameObject currentBomb;
    int bombCount;
    float timer;

    public State_PlacingBomb(StateManager owner) : base(owner)
    {
        Bomb = Resources.Load("Bomb") as GameObject;
        bombCount = 0;
        timer = 5.0f;
    }


    override public void Update()
    {
        GameObject.Instantiate(Bomb, m_owner.gObject.transform.position + m_owner.gObject.transform.forward, m_owner.gObject.transform.rotation);
        Debug.Log("Placed Bomb");

        /*
        HaveBomb = true;

        if (!HaveBomb)
        {
            GameObject.Instantiate(Bomb, m_owner.gObject.transform.position + (m_owner.gObject.transform.forward * 2), m_owner.gObject.transform.rotation);
            bombCount++;
            HaveBomb = true;
        }
        else
        {
            timer -= Time.deltaTime;
            if (timer <= 0.0f)
            {
                HaveBomb = false;
                bombCount = 0;
                timer = 5.0f;
            }
        }
        */
        // Create a bomb, run away
        m_owner.SetState(new State_RunningAwayFromBomb(m_owner));
    }
}

[Serializable]
public class State_RunningAwayFromBomb : State
{
    public bool targetSet;
    public float bombDist;
    public float rot;
    public Vector3 newPos;
    public Vector3 vel;
    GameObject bomb;

    public State_RunningAwayFromBomb(StateManager owner) : base(owner)
    {
        targetSet = false;
        //rot = m_owner.gObject.GetComponent<Rigidbody>().rotation.x;
    }

    override public void Update()
    {
        base.Update();
        Debug.Log("Running Away From Bomb");

        bombDist = 99999;
        foreach (RaycastInfo raycast in raycasts)
        {
            if (Physics.Raycast(m_owner.gObject.transform.position, raycast.vec, out raycast.hitInfo, 4.0f))
            {
                if (raycast.hitInfo.collider.gameObject.tag == "Bomb" && raycast.hitInfo.distance < bombDist)
                {
                    bombDist = raycast.hitInfo.distance;
                    bomb = raycast.hitInfo.collider.gameObject;
                    newPos = m_owner.gObject.transform.position - raycast.vec * 3;
                }
            }
        }

        if (bombDist <= 2.0f)
        {
            // If Path to the RIGHT or LEFT is Clear:
            m_owner.gObject.GetComponent<AI>().targetPos = newPos;
        }
        else
        {
            //make new state to wait for bomb to explode
            m_owner.SetState(new State_WaitingForBomb(m_owner, bomb));
        }
    }
}

[Serializable]
public class State_WaitingForBomb: State
{
    GameObject bomb;

    public State_WaitingForBomb(StateManager owner, GameObject obj) : base(owner)
    {
        bomb = obj;
    }

    public override void Update()
    {
        base.Update();
        
        if (bomb)
        {
            m_owner.gObject.GetComponent<AI>().targetPos = m_owner.gObject.transform.position;
        }
        else
        {
            m_owner.SetState(new State_FindingBlock(m_owner));
        }
        
    }

}

[Serializable]
public class State_ChasingPlayer : State
{
    public State_ChasingPlayer(StateManager owner, Vector3 pos) : base(owner)
    {

    }
    //    
}

[Serializable]
public class StateManager
{
    public State m_currentState;
    public GameObject gObject;
    public Vector3 m_Pos;

    public void SetState(State newState)
    {
        m_currentState = newState;
    }

    public void Update()
    {
        if (m_currentState != null)
        {
            m_currentState.Update();
        }

    }
}
public class AI : MonoBehaviour
{

    // Global Variables
    #region
    float maxSpeed;
    float randNum;
    float forwardDist;
    float backwardDist;
    float rightDist;
    float leftDist;
    float playerDist;
    float currDist;
    float testDist;
    float dTime;
    float velMag;
    public Vector3 targetPos;
    Vector3 pos;
    public Vector3 vel;
    Vector3 testPos;
    Vector3 prevPos;
    Vector3 raycastFwd;
    Vector3 raycastBck;
    Vector3 raycastRight;
    Vector3 raycastLeft;
    Vector3 turn90R;
    Vector3 turn90L;
    GameObject targetPlayer;
    GameObject block;
    GameObject currNode;
    GameObject testNode;
    GameObject targetNode;
    GameObject fwdNode;
    GameObject backNode;
    GameObject rightNode;
    GameObject leftNode;
    GameObject[] gameObjects;
    List<GameObject> players = new List<GameObject>();

    //RaycastHit hit;
    public StateManager stateManager;


    #endregion

    // Use this for initialization
    void Start()
    {
        maxSpeed = 5.0f;
        forwardDist = 0;
        backwardDist = 0;
        rightDist = 0;
        leftDist = 0;

        vel = new Vector3(0, 0, 0);
        randNum = 0.0f;

        pos = gameObject.transform.position;

        stateManager = new StateManager();
        stateManager.gObject = gameObject;
        stateManager.SetState(new State_FindingBlock(stateManager));
        stateManager.m_Pos = gameObject.transform.position;
    }

    // Update is called once per frame------------------------------------------------//
    void Update()
    {
        dTime = Time.deltaTime;
        prevPos = pos;                                                                          // Set prevPos to current position in case needed to return to position before update
        playerDist = 15;
        // Clear players list if not empty
        if (players.Count != 0)
        {
            players.Clear();
        }

        gameObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];                      // Find all game objects in the scene and add to array gameObjects
        foreach (GameObject gObject in gameObjects)
        {
            if (gObject.tag == "Player")                                                          // Find all 'Player' game objects and add to List players
            {
                players.Add(gObject);
            }
        }
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == gameObject)                                                         // Remove self from the list
            {
                players.RemoveAt(i);
            }
            if ((Vector3.Distance(pos, players[i].transform.position)) <= playerDist)             // Search through player list to find closest player
            {
                playerDist = Vector3.Distance(pos, players[i].transform.position);                // Save closest player distance and save closest player
                targetPlayer = players[i];
            }
        }

        stateManager.Update();

        Vector3 newVel;
        newVel = targetPos - gameObject.transform.position;
        newVel = Vector3.Normalize(newVel);
        newVel = newVel * maxSpeed;
        gameObject.GetComponent<Rigidbody>().velocity = newVel;

        // If encounter bomb or if bomb placed nearby:  stateManager.SetState(new State_RunningAwayFromBomb())

        // Oh shit, player near me
        /*if (PlayerNearMe() && !(stateManager.m_currentState is State_ChasingPlayer))
        {
            stateManager.SetState(new State_ChasingPlayer());
        }
        stateManager.Update();
        */
    }

    void OnCollisionEnter(Collision collide)
    {
        if (collide.gameObject.tag == "DestroyBlock")
        {
            GameVariables.AIDestroyBlockCollide = true;
            print("Collided with Block");
        }
    }

    void OnCollisionExit(Collision collide)
    {
        if (collide.gameObject.tag == "DestroyBlock")
        {
            GameVariables.AIDestroyBlockCollide = false;
            print("Left Block");
        }
    }

}