﻿using ARRCObjectron;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Barracuda;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static ARRCObjectronHelper;
using static Unity.Burst.Intrinsics.X86;

public class ARRCObjectronTest
{
    [DllImport("ARRCObjectronCore", EntryPoint = "_LoadModel", CharSet = CharSet.Ansi)]
    static extern int _LoadModel(IntPtr modelPath);

    [DllImport("ARRCObjectronCore")]
    static extern int _DestroyModel();

    [DllImport("ARRCObjectronCore")]
    unsafe static extern _TENSOR_ARRAY _Eval(_TENSOR_ARRAY inputTensors);

    [DllImport("ARRCObjectronCore")]
    static extern int _ReleaseTensorArray(_TENSOR_ARRAY tensors);

    [DllImport("ARRCObjectronCore")]
    static extern void _WriteHeatMapTensor(_TENSOR tensor, int resizeX, int resizeY, IntPtr writepath, int waitMilliSec);

    [DllImport("ARRCObjectronCore")]
    static extern void _VisTest(_TENSOR tensor, IntPtr writepath, int waitMilliSec);

    [Test]
    public void AOInferenceTest_VirnectAll()
    {
        string modelFileName = "Virnect_All_000114";
        string imageFileName = "Virnect_All_sample";

        var nnModel = LoadNNModelAsset(modelFileName);
        var model = ModelLoader.Load(nnModel);
        var engine = WorkerFactory.CreateWorker(model, WorkerFactory.Device.GPU);

        var inputImage = LoadTexture2DAsset(imageFileName);
        //var c_inputImage = ConvertForamt(inputImage, TextureFormat.BGRA32);
        //SaveTexture(c_inputImage, "Assets/input.png");
        //var input_raw = new Tensor(c_inputImage, 3);

        // #가정 : Tensor생성자에서 float data를 생성하는 규칙. 2022-10-31
        // Tensor 생성자가 지원하는 Input Texture2D image의 포맷은 오직 RGBAHalf
        // Half가 아닌 경우, 기대했던 값으로 Tensor에 할당되지 않는다.
        // Texture2D의 format이 BGRAHalf인 경우 RGBAHalf로 자동 변경된다.
        // 따라서, RGB의 순서를 바꾸기 위해서는 Shader를 이용해서 처리해야 한다.

        var input = new Tensor(ARRCObjectronHelper.PrepareTextureForInput(inputImage, Shader.Find("ML/NormalizeAndSwapRB_MobilePose")), 3);
        //var input = new Tensor(1, 480, 640, 3);
        float[] input_data = input.data.Download(input.shape);
        //SaveTexture(inputImage, "./"+imageFileName + "_input.png");
        //AssignInputRandom(input_data);
        //input.data.Upload(input_data, input.shape);
        CheckRange(input_data, -1, 1);

        double start_time = EditorApplication.timeSinceStartup;
        var output_barrcuda = engine.Execute(input);
        var heatmap_barrcuda = output_barrcuda.PeekOutput("output");
        var offsetmap_barracuda = output_barrcuda.PeekOutput("550");

        float[] heatmap_data_barracua = heatmap_barrcuda.data.Download(heatmap_barrcuda.shape);
        float[] offsetmap_data_barracuda = offsetmap_barracuda.data.Download(offsetmap_barracuda.shape);
        Debug.LogFormat("barracuda runtime : {0:F6}", EditorApplication.timeSinceStartup - start_time);

        CheckRange(heatmap_data_barracua, 0, 1);

        var ModeilFileFullPath = GetFirstFoundFilePath(Application.dataPath, modelFileName + ".onnx");
        IntPtr pModelPath = Marshal.StringToHGlobalAnsi(ModeilFileFullPath);
        _LoadModel(pModelPath);
        Marshal.FreeHGlobal(pModelPath);

        float[] heatmap_data_onnxruntime;
        float[] offsetmap_data_onnxruntime;

        //onnx method.
        unsafe
        {
            var input_tensors = ConvertTensor2TENSOR_ARRAY(input);

            //// Input Image Test
            //{
            //    float[] input_data_raw = input.data.Download(input.shape);
            //    _TENSOR input_tensor = new _TENSOR(1, 480, 640, 3, input_data_raw);
            //    Assert.AreEqual(input_tensor.elementCount, 640 * 480 * 3);
            //    string writepath = "rgb";
            //    IntPtr pWritePath = Marshal.StringToHGlobalAnsi(writepath);
            //    _VisTest(input_tensor, pWritePath, 0);
            //    Marshal.FreeHGlobal(pWritePath);
            //}
            start_time = EditorApplication.timeSinceStartup;
            var output_tensors = _Eval(input_tensors); //must be deleted manually.
            Debug.LogFormat("onnx runtime : {0:F6}", EditorApplication.timeSinceStartup - start_time);
            
            //// Output Image Test
            //{ 
            //    _TENSOR output_tensor_heatmap_barracuda = new _TENSOR(1, 30, 40, 1, heatmap_data_barracua);
            //    string writepath1 = "heatmap_by_barracuda";
            //    IntPtr pWritePath1 = Marshal.StringToHGlobalAnsi(writepath1);
            //    _WriteHeatMapTensor(output_tensor_heatmap_barracuda, pWritePath1, 0);
            //    Marshal.FreeHGlobal(pWritePath1);

            //    string writepath2 = "heatmap_by_onnxruntime";
            //    IntPtr pWritePath2 = Marshal.StringToHGlobalAnsi(writepath2);
            //    _WriteHeatMapTensor(output_tensors.tensor[0], pWritePath2, 0);
            //    Marshal.FreeHGlobal(pWritePath2);
            //}

            heatmap_data_onnxruntime = output_tensors.tensor[0].DownloadData();
            offsetmap_data_onnxruntime = output_tensors.tensor[1].DownloadData();
            _ReleaseTensorArray(output_tensors); //must be deleted manually.
            _DestroyModel();
        }


        //Check outputs.
        Assert.True(heatmap_data_barracua.Length > 0 && heatmap_data_barracua.Length == heatmap_data_onnxruntime.Length);
        Assert.True(offsetmap_data_barracuda.Length > 0 && offsetmap_data_barracuda.Length == offsetmap_data_onnxruntime.Length);

        CompareDistribution(heatmap_data_barracua, heatmap_data_onnxruntime);
        //CompareDistribution(offsetmap_data_barracuda, offsetmap_data_onnxruntime);

        CheckRange(heatmap_data_barracua, 0, 1);
        CheckRange(heatmap_data_onnxruntime, 0, 1);
        Check_Average_of_Residuals(heatmap_data_barracua, heatmap_data_onnxruntime, 0.00001f);
        Check_Average_of_Residuals(offsetmap_data_barracuda, offsetmap_data_barracuda, 0.000f);

        input.Dispose();
        engine.Dispose();
        Resources.UnloadUnusedAssets();
    }

    [Test]
    public void AOInferenceTest_MobileNetV2()
    {
        string modelFileName = "mobilenet_v2";
        string imageFileName = "Bee";

        var nnModel = LoadNNModelAsset(modelFileName);
        var model = ModelLoader.Load(nnModel);
        var engine = WorkerFactory.CreateWorker(model, WorkerFactory.Device.GPU);
        //var engine = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);

        var inputImage = LoadTexture2DAsset(imageFileName);
        var input = new Tensor(PrepareTextureForInput(inputImage, Shader.Find("ML/NormalizeInput")), 3);

        double start_time = EditorApplication.timeSinceStartup;
        engine.Execute(input);
        var output_barracuda = engine.PeekOutput();
        var res = output_barracuda.ArgMax()[0];
        Debug.LogFormat("barracuda runtime : {0:F6}", EditorApplication.timeSinceStartup - start_time);

        TextAsset labelsAsset = LoadTextAsset("class_desc");
        var labels = labelsAsset.text.Split('\n');
        var label = labels[res];
        var accuracy = output_barracuda[res];

        Assert.AreEqual(imageFileName, label.Trim("\r".ToCharArray()));

        float[] data_barracuda = output_barracuda.data.Download(output_barracuda.shape);
        float[] data_onnxruntime;

        var ModeilFileFullPath = GetFirstFoundFilePath(Application.dataPath, modelFileName + ".onnx");
        IntPtr pModelPath = Marshal.StringToHGlobalAnsi(ModeilFileFullPath);
        _LoadModel(pModelPath);
        Marshal.FreeHGlobal(pModelPath);

        //onnx method.
        unsafe
        {
            var input_tensors = ConvertTensor2TENSOR_ARRAY(input);

            var output_tensors = _Eval(input_tensors); //must be deleted manually.

            //Debug.LogFormat("output count {0}", output_tensors.count);
            //for (int i = 0; i < output_tensors.count; i++)
            //    Debug.LogFormat("output {0} : tensor data size {1}", i, output_tensors.tensor[i].elementCount);

            data_onnxruntime = output_tensors.tensor[0].DownloadData();

            _ReleaseTensorArray(output_tensors); //must be deleted manually.
        }
        _DestroyModel();

        //Check outputs.
        CheckRange(data_onnxruntime, 0, 1);
        CompareDistribution(data_barracuda, data_onnxruntime);
        Check_Average_of_Residuals(data_barracuda, data_onnxruntime, 0.00001f);

        //clean memory
        input.Dispose();
        engine.Dispose();
        Resources.UnloadUnusedAssets();

    }

    [Test]
    public void AOInferenceTest_yolov3_tiny()
    {
        string modelFileName = "yolov3-tiny-original";
        string imageFileName = "balcony";

        var nnModel = LoadNNModelAsset(modelFileName);
        var model = ModelLoader.Load(nnModel);
        var engine = WorkerFactory.CreateWorker(model, WorkerFactory.Device.GPU);
        //var engine = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);

        var inputImage = LoadTexture2DAsset(imageFileName);
        var input = new Tensor(inputImage, 3);
        engine.Execute(input);
        var output20 = engine.PeekOutput("016_convolutional"); //016_convolutional = original output tensor name for 20x20 boundingBoxes
        var output40 = engine.PeekOutput("023_convolutional"); //023_convolutional = original output tensor name for 40x40 boundingBoxes
        //output20 = ChangeDimensionOrder(output20);
        //output40 = ChangeDimensionOrder(output40);

        float[] data20_barracuda = output20.data.Download(output20.shape);
        float[] data40_barracuda = output40.data.Download(output40.shape);
        float[] data20_onnxruntime;
        float[] data40_onnxruntime;

        var ModeilFileFullPath = GetFirstFoundFilePath(Application.dataPath, modelFileName + ".onnx");
        IntPtr pModelPath = Marshal.StringToHGlobalAnsi(ModeilFileFullPath);
        _LoadModel(pModelPath);
        Marshal.FreeHGlobal(pModelPath);

        //onnx method.
        unsafe
        {
            var input_tensors = ConvertTensor2TENSOR_ARRAY(input);

            var output_tensors = _Eval(input_tensors); //must be deleted manually.

            //for (int i = 0; i < output_tensors.count; i++)
            //    Debug.LogFormat("output {0} : tensor data size {1}", i, output_tensors.tensor[i].elementCount);

            data20_onnxruntime = output_tensors.tensor[0].DownloadData();
            data40_onnxruntime = output_tensors.tensor[1].DownloadData();

            _ReleaseTensorArray(output_tensors); //must be deleted manually.
        }
        //_DestroyModel();

        //Check outputs.
        //for (int i = 100; i < 100; i++)
        //    Debug.LogFormat("{0}, {1}", data20_barracuda[i], data20_onnxruntime[i]);
        //Check_Average_of_Residuals(data20_barracuda, data20_onnxruntime, 5);
        CompareDistribution(data20_barracuda, data20_onnxruntime);
        CompareDistribution(data40_barracuda, data40_onnxruntime);

        //clean memory
        input.Dispose();
        engine.Dispose();
        Resources.UnloadUnusedAssets();
    }

    [Test]
    public void AOInferenceTest_MobilePoseShape()
    {
        string modelFileName = "object_detection_3d_chair_1stage";
        string imageFileName = "chair1";

        var nnModel = LoadNNModelAsset(modelFileName);
        var model = ModelLoader.Load(nnModel);
        var engine = WorkerFactory.CreateWorker(model, WorkerFactory.Device.GPU);

        var inputImage = LoadTexture2DAsset(imageFileName);
        var input = new Tensor(PrepareTextureForInput(inputImage, Shader.Find("ML/NormalizeAndSwapRB_MobilePose")), 3);

        //var preMat = LoadMaterialAsset("UnlitTexture");
        //var postMat = LoadMaterialAsset("MobilePoseNormalize");
        //var input = new Tensor(PrepareTextureForInput(inputImage, preMat, postMat), 3);

        float[] input_data = input.data.Download(input.shape);
        CheckRange(input_data, -1, 1);

        var output_barrcuda = engine.Execute(input);
        var heatmap_barrcuda = output_barrcuda.PeekOutput("Identity");
        //var offsetmap_barracuda = output_barrcuda.PeekOutput("Identity_1");

        float[] heatmap_data_barracua = heatmap_barrcuda.data.Download(heatmap_barrcuda.shape);
        //float[] offsetmap_data_barracuda = offsetmap_barracuda.data.Download(offsetmap_barracuda.shape);
        CheckRange(heatmap_data_barracua, 0, 1);
        {
            _TENSOR output_tensor_heatmap_barracuda = new _TENSOR(1, 40, 30, 1, heatmap_data_barracua);
            string writepath1 = "heatmap_by_barracuda";
            IntPtr pWritePath1 = Marshal.StringToHGlobalAnsi(writepath1);
            _WriteHeatMapTensor(output_tensor_heatmap_barracuda, 480, 640, pWritePath1, 0);
            Marshal.FreeHGlobal(pWritePath1);
        }
        
        //var ModeilFileFullPath = GetFirstFoundFilePath(Application.dataPath, modelFileName + ".onnx");
        //IntPtr pModelPath = Marshal.StringToHGlobalAnsi(ModeilFileFullPath);
        //_LoadModel(pModelPath);
        //Marshal.FreeHGlobal(pModelPath);

        //float[] heatmap_data_onnxruntime;
        //float[] offsetmap_data_onnxruntime;

        ////onnx method.
        //unsafe
        //{
        //    var input_tensors = ConvertTensor2TENSOR_ARRAY(input);
        //    var output_tensors = _Eval(input_tensors); //must be deleted manually.

        //    _TENSOR output_tensor_heatmap_barracuda = new _TENSOR(1, 40, 30, 1, heatmap_data_barracua);
        //    string writepath1 = "heatmap_by_barracuda";
        //    IntPtr pWritePath1 = Marshal.StringToHGlobalAnsi(writepath1);
        //    _WriteHeatMapTensor(output_tensor_heatmap_barracuda, pWritePath1, 0);
        //    Marshal.FreeHGlobal(pWritePath1);

        //    string writepath2 = "heatmap_by_onnxruntime";
        //    IntPtr pWritePath2 = Marshal.StringToHGlobalAnsi(writepath2);
        //    _WriteHeatMapTensor(output_tensors.tensor[0], pWritePath2, 0);
        //    Marshal.FreeHGlobal(pWritePath2);

        //    heatmap_data_onnxruntime = output_tensors.tensor[0].DownloadData();
        //    offsetmap_data_onnxruntime = output_tensors.tensor[1].DownloadData();
        //    _ReleaseTensorArray(output_tensors); //must be deleted manually.
           
        //}
        //_DestroyModel();

        ////Check outputs.
        //Assert.True(heatmap_data_barracua.Length > 0 && heatmap_data_barracua.Length == heatmap_data_onnxruntime.Length);
        //Assert.True(offsetmap_data_barracuda.Length > 0 && offsetmap_data_barracuda.Length == offsetmap_data_onnxruntime.Length);

        //CheckRange(heatmap_data_barracua, 0, 1);
        //CheckRange(heatmap_data_onnxruntime, 0, 1);
        //Check_Average_of_Residuals(heatmap_data_barracua, heatmap_data_onnxruntime, 0.03f); //평균 3% 차이 이내 (Mahalanobis를 이용해 측정하는 것이 더 좋음.)
        //Check_Average_of_Residuals(offsetmap_data_barracuda, offsetmap_data_barracuda, 0.0f);

        input.Dispose();
        engine.Dispose();
        Resources.UnloadUnusedAssets();
    }


    void CheckRange(float[] input_data, float minValueInclude, float maxValueInclude)
    {
        var validCount = input_data.Count(i => (i >= minValueInclude && i <= maxValueInclude));
        Assert.AreEqual(input_data.Length, validCount);
    }
    void Check_Average_of_Residuals(float[] arr1, float[] arr2, float threshold)
    {
        var residuals = arr1.Zip(arr2, (d1, d2) => Math.Abs(d1 - d2));
        float avg = residuals.Average();
        //int countWrong = residuals.Count(i => i > threshold);
        Assert.LessOrEqual(avg, threshold);
    }
    void CompareDistribution(float[] arr1, float[] arr2)
    {
        float avg1 = arr1.Average();
        float avg2 = arr2.Average();

        var std1 = arr1.Select(x => x - avg1).Select(x => x * x).Average();
        var std2 = arr2.Select(x => x - avg2).Select(x => x * x).Average();

        Debug.LogFormat("avg : {0}, {1}", avg1, avg2);
        Debug.LogFormat("std : {0}, {1}", std1, std2);

    }
    Tensor ChangeDimensionOrder(Tensor inputTensor)
    {
        //c -> h
        //h -> w
        //w -> c

        // h-> c
        // w -> h
        // c -> w
        //float[] source_data = inputTensor.data.Download(inputTensor.shape);
        float[] output_data = new float[inputTensor.length];
        TensorShape shape = new TensorShape(inputTensor.batch, inputTensor.channels, inputTensor.height, inputTensor.width);
        Tensor outputTensor = new Tensor(shape, output_data);
        
        for (int n = 0; n < shape.batch; n++)
        {
            for (int c = 0; c < shape.channels; c++)
            {
                for (int h = 0; h < shape.height; h++)
                {
                    for (int w = 0; w < shape.width; w++)
                        outputTensor[n, h, w, c] = inputTensor[n, w, c, h];
                }
            }
        }
        return outputTensor;
    }
    Texture2D ConvertForamt(Texture2D src, TextureFormat targetFormat)
    {
        var result = new Texture2D(src.width, src.height, targetFormat, false);
        result.SetPixels(src.GetPixels());
        result.Apply();

        return result;
    }
 
    void SaveTexture(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }
    
    _TENSOR_ARRAY ConvertTensor2TENSOR_ARRAY(Tensor input)
    {
        float[] input_data = input.data.Download(input.shape);
        _TENSOR tensor = new _TENSOR(input.shape.batch, input.shape.height, input.shape.width, input.shape.channels, input_data);
        var input_tensors_data = new System.Collections.Generic.List<_TENSOR>();
        input_tensors_data.Add(tensor);
        var input_tensors = new _TENSOR_ARRAY(input_tensors_data.ToArray());

        return input_tensors;
    }

    void AssignInputRandom(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
            data[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
        //data[i] = 0;

    }


    public NNModel LoadNNModelAsset(string modelFileName)
    {
        string[] allCandidates = AssetDatabase.FindAssets(modelFileName);
        Assert.True(allCandidates.Length > 0);

        var nnModel =
            AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(allCandidates[0]), typeof(NNModel)) as
            NNModel;

        return nnModel;
    }
    public Texture2D LoadTexture2DAsset(string imageFileName)
    {
        string[] allCandidates = AssetDatabase.FindAssets(imageFileName);
        Assert.True(allCandidates.Length > 0);

        var texture =
            AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(allCandidates[0]), typeof(Texture2D)) as
            Texture2D;

        return texture;
    }
    public Material LoadMaterialAsset(string fileName)
    {
        string[] allCandidates = AssetDatabase.FindAssets(fileName);
        Assert.True(allCandidates.Length > 0);

        var texture =
            AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(allCandidates[0]), typeof(Material)) as
            Material;

        return texture;
    }
    public TextAsset LoadTextAsset(string textFileName)
    {
        string[] allCandidates = AssetDatabase.FindAssets(textFileName);
        Assert.True(allCandidates.Length > 0);

        var text =
            AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(allCandidates[0]), typeof(TextAsset)) as
            TextAsset;

        return text;
    }


    static string GetFirstFoundFilePath(string directory, string filename_with_ext)
    {
        string[] files = Directory.GetFiles(directory, filename_with_ext, SearchOption.AllDirectories);

        if (files.Length == 0)
            throw new FileNotFoundException(string.Format("The file {0} is not founds in {1} ", filename_with_ext, directory));

        else if (files.Length > 1)
        {
            Debug.LogWarningFormat("{0} files found.", files.Length);
            files.ToList().ForEach(i => Debug.LogWarning(i));
            Debug.LogWarningFormat("It use the file : {0}", files[0]);
        }

        files[0].Replace("/", "\\");

        return files[0];
    }
}
