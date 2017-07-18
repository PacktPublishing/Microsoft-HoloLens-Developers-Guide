using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA;
using UnityEngine.VR.WSA.Sharing;

public class ImportExportManager  {

    public void ReceiveAnchors(byte[] rawData)
    {
        WorldAnchorTransferBatch.ImportAsync(rawData, OnImportCompleted);
    }

    private void OnImportCompleted(SerializationCompletionReason reason, WorldAnchorTransferBatch batch)
    {
        if (reason != SerializationCompletionReason.Succeeded)
        {
            // Something went wrong. Retry or abort...
            return;
        }

        var ids = batch.GetAllIds();
        foreach (var id in ids)
        {

            //batch.LockObject(id)
        }
    }
}
