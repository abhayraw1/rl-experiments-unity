﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

[System.Serializable]
public struct ActionRange{
    public float min;
    public float max;
    public float defaultValue;
    public float getDefaultInRange()
    {
        return 2.0f * (defaultValue - min)/(max - min) - 1.0f;
    }
}

public class MinionG2G : Agent
{
    public GameObject area;
    public GameObject goalPrefab;
    public ActionRange[] actionRange;
    [ReadOnly]
    public float[] defaultAction;

    private bool ready = false;
    private GameObject goal;
    private Transform agent;
    private Rigidbody agentRb;
    private Vector3 areaCenter;
    private Material agentMaterial;
    private Color agentColor;
    private Vector3 relativePosition;
    private float currRelativeDistance;

    void Start()
    {
        ReadyUp();
    }

    public void ReadyUp()
    {
        if (!ready)
        {
            defaultAction = new float[actionRange.Length];
            for(var i = 0; i < actionRange.Length; i++)
                defaultAction[i] = actionRange[i].getDefaultInRange();
            goal = Instantiate(goalPrefab);
            agent = GetComponent<Transform>();
            areaCenter = area.GetComponent<Transform>().position;
            agentRb = agent.GetComponent<Rigidbody>();
            SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.75f, 1f));
            ready = true;
        }
    }

    public override void Initialize()
    {
        ReadyUp();
    }

    public void SetColor(Color color)
    {
        agentColor = color;
        agentMaterial = agent.GetChild(0).GetComponent<Renderer>().material;agentMaterial.color = agentColor;
        goal.GetComponent<Renderer>().material = agentMaterial;
    }

    public void RandomSpawn()
    {
        float x, z;
        do
        {
            x = Random.Range(areaCenter.x - 50f, areaCenter.x + 50f);
            z = Random.Range(areaCenter.y - 50f, areaCenter.y + 50f);

        } while(
            Physics.CheckBox(
                new Vector3(x, 0.65f, z),
                new Vector3(0.55f, 0.55f, 0.55f)
            )
        );
        agent.position = new Vector3(x, 0.65f, z);
    }

    public void SetRandomGoal()
    {
        // Set some random goal inside the bounds of the arena!
        float x, z;
        do
        {
            x = Random.Range(areaCenter.x - 50f, areaCenter.x + 50f);
            z = Random.Range(areaCenter.y - 50f, areaCenter.y + 50f);

        } while(
            Physics.CheckBox(
                new Vector3(x, 0.5f, z),
                new Vector3(0.7f, 0.1f, 0.7f)
            )
        );
        goal.transform.position = new Vector3(x, 0.025f, z);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // VectorSensor.size = 8
        if (goal != null)
        {
            relativePosition = goal.transform.position - agent.position;   
        }
        // Not necessary to give color codes because the relative pos vec of the goal is provided!
        // sensor.AddObservation(agentColor.r);
        // sensor.AddObservation(agentColor.g);
        // sensor.AddObservation(agentColor.b);
        sensor.AddObservation(relativePosition.x);
        sensor.AddObservation(relativePosition.y);
        sensor.AddObservation(agent.InverseTransformDirection(agentRb.velocity));
        sensor.AddObservation(agent.InverseTransformDirection(agentRb.angularVelocity));
    }

    public void MoveAgent(float[] action)
    {
        var locVel = transform.InverseTransformDirection(agentRb.velocity);
        locVel.z = action[0];
        agentRb.velocity = agent.TransformDirection(locVel);
        agent.Rotate(agent.up, Time.deltaTime * action[1]);
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        var currDistance = relativePosition.magnitude;
        if (currRelativeDistance != currDistance)
        {
            AddReward(currRelativeDistance - currDistance);
            currRelativeDistance = currDistance;
        }
        for (var i = 0; i < vectorAction.Length; i++)
        {
            vectorAction[i] = ScaleAction(vectorAction[i], actionRange[i].min, actionRange[i].max);
        }
        MoveAgent(vectorAction);
    }

    public override void Heuristic(float[] actionsOut)
    {
        Array.Copy(defaultAction, actionsOut, defaultAction.Length);
        if (Input.GetKey(KeyCode.D))
        {
            actionsOut[1] = +15f;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            actionsOut[1] = -15f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            actionsOut[0] = +1f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            actionsOut[0] = -1f;
        }
    }

    public override void OnEpisodeBegin()
    {
        agentRb.velocity = Vector3.zero;
        RandomSpawn();
        SetRandomGoal();
        relativePosition = goal.transform.position - agent.position;
        currRelativeDistance = relativePosition.magnitude;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == goal.GetComponent<BoxCollider>())
        {
            AddReward(2f);
            EndEpisode();
        }
    }

    void OnCollisionEnter(Collision collisionInfo)
    {
        if (collisionInfo.gameObject.name != "Floor")
        {
            AddReward(-10f);
            EndEpisode();
        }
    }
}
