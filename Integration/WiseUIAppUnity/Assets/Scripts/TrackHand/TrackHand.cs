using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackHand : MonoBehaviour
{
    public SocketClientManager tcpClientManager;
    // Start is called before the first frame update
    void Awake()
    {
        tcpClientManager = GetComponent<SocketClientManager>();
    }


    void Update()
    {
        try
        {
            ResultDataPackage frameData;
            // 1. ���� �ֽ� �����͸� �������� ���.(������ frame ���� �� ������ delay ����.)
            tcpClientManager.GetLatestFrameData(out frameData);
            
            // 2. ������ ������� �����͸� �������� ���. (delay ���� �� ������ ������ frame ����.)
            //tcpClientManager.GetNextFrameData(out frameData);
            var frameInfo = frameData.frameInfo;
            var handData = frameData.handDataPackage;

            var now = DateTime.Now.ToLocalTime();
            var span = now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            double total_delay = span.TotalSeconds - frameInfo.timestamp_sentFromClient;
            Debug.LogFormat("frameID : {0}, total_delay {1}, ", frameInfo.frameID, total_delay);
        }
        catch (NoDataReceivedExecption e)
        {
            //Debug.Log(e); // �� �̻� �޾ƿ� �����Ͱ� ���� �� �߻��ϴ� ����.
        }
    }
}
