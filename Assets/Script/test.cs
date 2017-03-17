using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using OpenCvSharp;




public class test : MonoBehaviour {

   
    public RawImage cam_image;
    public RawImage result_image; 
    public int deviceNumber; 
    WebCamTexture webcamTexture;
    Texture2D result_texture;

    // Video size
    private const int imWidth = 640;
    private const int imHeight = 360;


    // OpenCVSharp parameters
    private Mat videoSource_Mat_Image;
    private Mat testmat;
    private Mat outputmat;
    private Texture2D processedTexture;
    private Vec3b[] videoSourceImageData;
  

    // Frame rate parameter
    private int updateFrameCount = 0;
    private int textureCount = 0;
    private int displayCount = 0;


    // Use this for initialization
    void Start () {
        Debug.Log("test");

        // create a list of webcam devices that is available
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            webcamTexture = new WebCamTexture(devices[deviceNumber].name, imWidth, imHeight);

            cam_image.texture = webcamTexture;
            //cam_image.material.mainTexture = webcamTexture;
            webcamTexture.Play();


            // initialize video / image with given size
            videoSource_Mat_Image = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            videoSourceImageData = new Vec3b[imHeight * imWidth];

           

            // create processed video texture as Texture2D object
            processedTexture = new Texture2D(imWidth, imHeight, TextureFormat.RGBA32, true, true);

            result_texture = new Texture2D(imWidth, imHeight, TextureFormat.RGBA32, true, true);
        }
        else
        {
            Debug.Log("Can't find camera!");
        }
     
    }
    
    // Update is called once per frame
    void Update () {
        updateFrameCount++;
        if (webcamTexture.isPlaying)
        {

            if (webcamTexture.didUpdateThisFrame)
            {


                textureCount++;

                //유니티 카메라에서 얻은 Texture 형식을 Mat형식으로 변환
                testmat = TextureToMat();

                if (testmat == null)
                {
                    Debug.Log("생성실패");
                }
                else {
                    Debug.Log("생성성공, bitmap형식으로 변환 시도중...");
                   
                    StartCoroutine(mat_To_Texture(testmat));
                   
                }

        

            }

        }
        else
        {
            Debug.Log("Can't find camera!");
        }


        // output frame rate information
        if (updateFrameCount % 30 == 0)
        {
            Debug.Log("Frame count: " + updateFrameCount + ", Texture count: " + textureCount + ", Display count: " + displayCount);
        }


    }



    // Convert Unity Texture2D object to OpenCVSharp Mat object
    Mat TextureToMat()
    {
        // Color32 array : r, g, b, a
        Color32[] c = webcamTexture.GetPixels32();

        // Parallel for loop
        // convert Color32 object to Vec3b object
        // Vec3b is the representation of pixel for Mat
        Parallel.For(0, imHeight, i => {
            for (var j = 0; j < imWidth; j++)
            {
                var col = c[j + i * imWidth];
                var vec3 = new Vec3b
                {
                    Item0 = col.b,
                    Item1 = col.g,
                    Item2 = col.r
                };
                // set pixel to an array
                videoSourceImageData[j + i * imWidth] = vec3;
            }
        });
        // assign the Vec3b array to Mat
        Mat result_mat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        result_mat.SetArray(0, 0, videoSourceImageData);
        return result_mat;
    }

    IEnumerator mat_To_Texture(Mat mat) {

        //mat을 openCV의 함수를 이용하여 정보추출 혹은 변환
        //외곽선 추출

        Mat mat01 = new Mat();
        Cv2.Canny(mat, mat01, 50, 200);          

        //
        byte[] imagedata;
        Cv2.ImEncode(".png", mat01, out imagedata);

        Texture2D tex = new Texture2D(640,360);
        tex.LoadImage(imagedata);
        tex.Apply();
        result_image.texture = tex as Texture;
        
        yield return null;

    }

    




}
