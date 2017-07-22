﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class RecorededTime
{
    public RecorededTime()
    {
        checkPoints = new List<float>();
        ghostPosition = new List<float[]>();
        ghostRotation = new List<float[]>();
    }

    public string startID;
    public string endID;

    public float lapTime;
    public List<float> checkPoints;
    public List<float[]> ghostPosition;
    public List<float[]> ghostRotation;
}

public delegate bool RaceEndEventHandler();

public class CheckPoint : MonoBehaviour {
    private float startTime = 0;

    private RecorededTime currentLap;
    private RecorededTime bestLap;

    private float ghostTimer = 0;
    private Transform player;
    private Transform ghost;

    public GameObject End;

    public event RaceEndEventHandler Crossed;

    private int index = 0;

    private void OnEnable()
    {
        if (File.Exists(Application.persistentDataPath + "/" + SceneManager.GetActiveScene().name + "-" + name + "-" + End.name))
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(Application.persistentDataPath + "/" + SceneManager.GetActiveScene().name + "-" + name + "-" + End.name, FileMode.Open, FileAccess.Read, FileShare.Read);
            bestLap = (RecorededTime)formatter.Deserialize(stream);
            stream.Close();
        }
        else
        {
            bestLap = new RecorededTime();
        }
        
        End.GetComponent<CheckPoint>().Crossed += RegisterTime;
    }

    private void Update()
    {
        if (startTime != 0 && Time.realtimeSinceStartup - startTime > 0.5)
			GameMode.Instance.LapTimer.text = "Lap Time: " + (Time.realtimeSinceStartup - startTime).ToString("F2");

        if (startTime != 0 && Time.realtimeSinceStartup - ghostTimer > 1f/60f)
        {
            float[] pos = new float[3];
            pos[0] = player.position.x;
            pos[1] = player.position.y;
            pos[2] = player.position.z;

            currentLap.ghostPosition.Add(pos);

            float[] rot = new float[4];
            rot[0] = player.rotation.x;
            rot[1] = player.rotation.y;
            rot[2] = player.rotation.z;
            rot[3] = player.rotation.w;
            currentLap.ghostRotation.Add(rot);

            ghostTimer = Time.realtimeSinceStartup;

            if (bestLap.lapTime != 0 && index < bestLap.ghostPosition.Count-1)
            {
                pos = bestLap.ghostPosition[index++];
                ghost.position = new Vector3(pos[0], pos[1], pos[2]);
                rot = bestLap.ghostRotation[index];
                ghost.rotation = new Quaternion(rot[0], rot[1], rot[2], rot[3]);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.parent &&   other.transform.parent.tag == "Player")
        {

            bool endedRace = false;
            try
            {
                endedRace = Crossed();
            } catch { }

            if (!endedRace && tag == "EntryPoint" && startTime == 0)
            {
                startTime = Time.realtimeSinceStartup;
                player = other.transform.parent;

                currentLap = new RecorededTime();

                currentLap.startID = name;

                if (bestLap.lapTime != 0)
                {
                    ghost = Instantiate(player.gameObject, player.position, player.rotation).transform;
                    ghost.tag = "Ghost";
                    ghost.GetComponent<Rigidbody>().isKinematic = true;

                    foreach (Collider c in ghost.GetComponentsInChildren<Collider>())
                    {
                        c.isTrigger = true;
                        c.gameObject.layer = LayerMask.NameToLayer("Ghost");
                    }
                }
            }
        }
    }

    private bool RegisterTime()
    {
        if (currentLap == null || Time.realtimeSinceStartup - startTime < 5 || startTime == 0)
        {
            ResetTime();
            return false;
        }

        currentLap.lapTime = Time.realtimeSinceStartup - startTime;

        currentLap.endID = End.name;

        if (bestLap.lapTime == 0 || (bestLap.lapTime == 0 || bestLap.lapTime > currentLap.lapTime))
        {
            Debug.Log("New record!!! " + currentLap.lapTime);
			GameMode.Instance.LapTimer.text = "New record!!! " + currentLap.lapTime.ToString("F2");

            bestLap = currentLap;

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(Application.persistentDataPath + "/" + SceneManager.GetActiveScene().name + "-" + name + "-" + End.name, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, currentLap);
            stream.Close();
            
            
        }
        else
        {
            Debug.Log("Lap time: " + currentLap.lapTime);
			GameMode.Instance.LapTimer.text = "Lap Time: " + currentLap.lapTime.ToString("F2");
        }

        ResetTime();
        return true;
    }

    public void ResetTime()
    {
        if (ghost != null)
            Destroy(ghost.gameObject);
        index = 0;
        startTime = 0;
    }
}
