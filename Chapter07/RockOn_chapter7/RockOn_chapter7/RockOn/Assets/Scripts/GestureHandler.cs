using UnityEngine;
using UnityEngine.VR.WSA;
using UnityEngine.VR.WSA.Input;
using UnityEngine.VR.WSA.Persistence;
using Random = System.Random;
using System;
using System.Collections.Generic;
using UnityEngine.VR.WSA.Sharing;

public class GestureHandler : MonoBehaviour
{
    private GestureRecognizer _gestureRecognizer;
    public GameObject objectToPlace;
    private WorldAnchorStore _store;

    private int _guitarCount;
    private Dictionary<string, WorldAnchor> _anchors = new Dictionary<string, WorldAnchor>();

    private void Start()
    {
        _gestureRecognizer = new GestureRecognizer();
        _gestureRecognizer.TappedEvent += GestureRecognizerOnTappedEvent;

        _gestureRecognizer.StartCapturingGestures();

        WorldAnchorStore.GetAsync(WorldAnchorStoreLoaded);
    }


    void LoadAllAnchors()
    {

        var allIds = _store.GetAllIds();
        _guitarCount = allIds.Length;

        foreach (var id in allIds)
        {
            var newGuitar = Instantiate(objectToPlace);

            _store.Load(id, newGuitar);

        }
    }
    private void WorldAnchorStoreLoaded(WorldAnchorStore store)
    {
        _store = store;
        LoadAllAnchors();
    }


    void TransferAnchor(string anchorName, WorldAnchor worldAnchor)
    {
        var batch = new WorldAnchorTransferBatch();
        batch.AddWorldAnchor(anchorName, worldAnchor);
        WorldAnchorTransferBatch.ExportAsync(batch, OnDataAvailable, OnDataExported);
    }

    private void OnDataAvailable(byte[] data)
    {
        // Set up the TCP connection, 
        // send the data (byte[]) to the server

    }
    private void OnDataExported(SerializationCompletionReason reason)
    {
        if (reason != SerializationCompletionReason.Succeeded)
        {
            // Something went wrong....
        }
        else
        {
            // It went ok.
            // Inform the client everything worked!
        }
    }

    private void ReceiveNewAnchor(byte[] rawData)
    {
        WorldAnchorTransferBatch.ImportAsync(rawData, OnReceiveCompleted);
    }

    private void OnReceiveCompleted(SerializationCompletionReason reason, WorldAnchorTransferBatch batch)
    {
        if (reason != SerializationCompletionReason.Succeeded)
        {
            // Oops..
            return;
        }

        var allIds = batch.GetAllIds();
        foreach (var id in allIds)
        {
            GameObject newGuitar = Instantiate(objectToPlace);
            newGuitar.GetComponent<Identifier>().AnchorName = id;
            var anchor = batch.LockObject(id, newGuitar);

            _anchors.Add(id, anchor);
        }
    }


    private void GestureRecognizerOnTappedEvent(
        InteractionSourceKind source,
        int tapCount,
        Ray headRay)
    {
        if (objectToPlace == null)
            return;
        var distance = new Random().Next(2, 10);
        var location =
            transform.position +
            transform.forward * distance;

        var newGuitar = Instantiate(
            objectToPlace,
            location,
            Quaternion.LookRotation(transform.up, transform.forward));

        var worldAnchor = newGuitar.AddComponent<WorldAnchor>();
        _guitarCount++;
        var anchorName = string.Format("Guitar{0:000}", _guitarCount);

        _store.Save(anchorName, worldAnchor);

        newGuitar.GetComponent<Identifier>().AnchorName = anchorName;
        _anchors.Add(anchorName, worldAnchor);
        TransferAnchor(anchorName, worldAnchor);

    }
}