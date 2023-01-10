using System.Collections.Generic;
using System.Numerics;

public enum DataFormat
{
    INVALID = -1,
    RGBA = 1,
    BGRA = 2, //proper to numpy format.
    ARGB = 3,
    RGB = 4,
    U16 = 5,
    U8 = 6,
    Float32 = 7
}

public enum DataType
{
    PV = 1,
    Depth = 2,
    PointCloud = 3,
    IMU = 4,
}
public enum ImageCompression
{
    None = 0,
    JPEG = 1,
    PNG = 2
}

[System.Serializable]
public class RGBImageHeader
{
    public int frameID = -1;
    public double timestamp = -1;
    public DataType dataType;
    public ImageCompression dataCompressionType = ImageCompression.None;
    public DataFormat dataFormat = DataFormat.INVALID;
    public int imageQulaity = 100;
    public long data_length;
    public int width;
    public int height;
}

[System.Serializable]
public class Joint
{
    public int id;
    public float u, v, d;
    //public float q1, q2, q3, q4; 
}


[System.Serializable]
public class HandDataPackage
{
    //�ʿ��� �� ����
    public List<Joint> joints = new List<Joint>();

}

[System.Serializable]
public class Keypoint
{
    public int id;
    public float x, y, z;
}
[System.Serializable]
public class ObjectInfo
{
    public List<Keypoint> keypoints = new List<Keypoint>();
    public int id;
}


[System.Serializable]
public class ObjectDataPackage
{
    public List<ObjectInfo> objects = new List<ObjectInfo>();
}
[System.Serializable]
public class FrameInfo
{   
    public int frameID;
    /// <summary>
    /// //Ȧ�η���� �̹����� ������ ���� ����
    /// </summary>
    public double timestamp_sentFromClient;

    /// <summary>
    ///  //�������� ó�� ������� Ȧ�η���� ���� ����
    /// </summary>
    public double timestamp_sentFromServer;
}
[System.Serializable]
public class ResultDataPackage
{
    public FrameInfo frameInfo = new FrameInfo();
    public ObjectDataPackage objectDataPackage = new ObjectDataPackage();
    public HandDataPackage handDataPackage = new HandDataPackage();
}

