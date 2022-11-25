using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackHand : MonoBehaviour
{
    public SocketClientManager tcpClientManager;
    // Start is called before the first frame update
    public void Awake()
    {
        tcpClientManager = GetComponent<SocketClientManager>();
    }


    public void Update()
    {
        try
        {
            ResultDataPackage frameData;
            // 1. ���� �ֽ� �����͸� �������� ���.(������ frame ���� �� ������ delay ����.)
            tcpClientManager.GetLatestResultData(out frameData);
            
            // 2. ������ ������� �����͸� �������� ���. (delay ���� �� ������ ������ frame ����.)
            //tcpClientManager.GetOldestResultData(out frameData);
            var frameInfo = frameData.frameInfo;
            var handData = frameData.handDataPackage;

            
        }
        catch (NoDataReceivedExecption e)
        {
            //Debug.Log(e); // �� �̻� �޾ƿ� �����Ͱ� ���� �� �߻��ϴ� ����.
        }
    }

 
}
