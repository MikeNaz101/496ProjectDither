using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class NoteSpawner : MonoBehaviour
{

    [System.Serializable]
    public class NoteData
    {
        public Vector3 position;
        public float timeToSpawn;
        public GameObject notePrefab;
    }

    public List<NoteData> notesToSpawn;

    public Transform noteParent;

    public void SpawnNotes()
    {
        foreach (NoteData data in notesToSpawn)
        {
            GameObject note = Instantiate(data.notePrefab, data.position, Quaternion.identity, noteParent);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
