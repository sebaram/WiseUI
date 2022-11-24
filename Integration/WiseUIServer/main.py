import json
import time

from SocketServer.type_definitions import HoloLens2PVImageData, HoloLens2SensorData
from SocketServer.StreamServer import StreamServer
#from handtracker.module_SARTE import HandTracker
# import track_object
import cv2


#track_hand = HandTracker()

def processing_loop(client_obj):
    while True:
        if client_obj.quit_event.is_set():
            break
        try:
            pv_frame : HoloLens2PVImageData = client_obj.get_latest_pv_frame() # 프레임 손실, delay 적음
            # pv_frame:HoloLens2SensorData = client_obj.get_oldest_pv_frame()  # 프레임 무손실, delay 더 큼
            # print(pv_frame.frameID)

            # intrinsic = pv_frame.intrinsic
            # extrinsic = pv_frame.extrinsic

            cv2.imshow("pv", pv_frame.data)
            cv2.waitKey(1)

            result_object = None #track_object.Process(pv_frame.data)
            result_hand = None #track_hand.Process(rgb_image)

            """ Packing data for sending to hololens """
            resultData = dict()
            resultData['frameInfo'] = pv_frame.encode_frame_info()
            resultData['objectDataPackage'] = encode_object_data(result_object)
            resultData['handDataPackage'] = encode_hand_data(result_hand)

            """ Send data """
            resultBytes = json.dumps(resultData).encode('utf-8')
            # print("bytes of total : {}".format(len(resultBytes)))
            client_obj.socket.send(resultBytes)
            #time.sleep(0.1) # 10Hz, 테스트용 처리시간.

        except IndexError as e:
            # empty deque
            pass


def encode_hand_data(hand_result):
    """ Encode hand data to json format """

    """ Example """
    handDataPackage = dict()
    num_joints = 21
    joints = list()
    for id in range(num_joints):
        joint = dict()
        joint['id'] = id
        joint['x'] = 1#float(hand_result[id, 0])
        joint['y'] = 1#float(hand_result[id, 1])
        joint['z'] = 1#float(hand_result[id, 2])
        joints.append(joint)
    handDataPackage['joints'] = joints

    return handDataPackage

def encode_object_data(object_result):
    """ Example """
    num_obj = 3
    objectDataPackage = dict()

    objects = list()
    for obj_id in range(num_obj):
        objectInfo = dict()
        keyPoints = list()
        for kpt_id in range(8):
            keyPoint = dict()
            keyPoint['id'] = kpt_id
            keyPoint['x'] = 0.123
            keyPoint['y'] = 0.456
            keyPoint['z'] = 0.789
            keyPoints.append(keyPoint)

        objectInfo['keypoints'] = keyPoints
        objectInfo['id'] = obj_id
        objects.append(objectInfo)

    objectDataPackage['objects'] = objects

    return objectDataPackage

if __name__ == '__main__':

    server = StreamServer()

    server.listening('', 9091, processing_loop)

    #processing thread 시작





