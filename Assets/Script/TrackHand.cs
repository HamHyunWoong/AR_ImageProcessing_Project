using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using OpenCvSharp;
using System.Threading;

public class TrackHand : MonoBehaviour {

    
    public RawImage cam_image;
    public RawImage result_image; 
    public int deviceNumber; 
    WebCamTexture webcamTexture;
    Texture2D result_texture;

    private static Vector2 hand_position;

    // Video size
    private const int imWidth = 160;
    private const int imHeight = 90;


    // OpenCVSharp parameters
    private Mat videoSource_Mat_Image;
    private Mat cam_mat;
    private Mat outputmat;
    private Texture2D processedTexture;
    private Vec3b[] videoSourceImageData;
    private Mat skinMat;
  

    // Frame rate parameter
    private int updateFrameCount = 0;
    private int textureCount = 0;
    private int displayCount = 0;
    private Vec3b pix;

    public static Mat YCrCb;



    // Use this for initialization
    void Start () {
   
        // 화상 카메라에서 영상가져오기 
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length > 0)
        {
            //원본영상 뿌려주기
            webcamTexture = new WebCamTexture(devices[deviceNumber].name, imWidth, imHeight);
            cam_image.texture = webcamTexture;       
            webcamTexture.Play();


            //Mat 및 이미지데이터 형식정의
            videoSource_Mat_Image = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            videoSourceImageData = new Vec3b[imHeight * imWidth];
            skinMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
           // YCrCb = new Mat(imHeight, imWidth, MatType.CV_8UC3);

            //유니티 엔진에 적용하기 위해 사용하는 Texture2D 프레임워크 초기화
            processedTexture = new Texture2D(imWidth, imHeight, TextureFormat.RGBA32, true, true);
            result_texture = new Texture2D(imWidth, imHeight, TextureFormat.RGBA32, true, true);

            //손 위치 초기화
            hand_position = new Vector2();
        }
        else
        {
            Debug.Log("Can't find camera!");
        }
     
    }
    
    // Update is called once per frame
    void Update () {
        //프레임 카운트
        updateFrameCount++;

        if (webcamTexture.isPlaying)
        {

            if (webcamTexture.didUpdateThisFrame)
            {

                //텍스처 카운트
                textureCount++;

                //유니티 카메라에서 얻은 Texture 형식을 Mat형식으로 변환
                cam_mat = TextureToMat();

                if (cam_mat == null)
                {
                    Debug.Log("생성실패");
                }
                else {
                    Debug.Log("생성성공, 영상에서 정보 추출 변환 시도중...");

                    //영상처리 스레드 생성 (일회성)
                    Thread trackThread = new Thread(new ThreadStart(ThreadRun));
                    trackThread.Start();
                    
                    //뿌려주기(비동기 가상 스레드) 
                    StartCoroutine(mat_To_Texture(skinMat));

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



    // 유니티 Texture 형식을 Mat형식으로 변환 
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

        Debug.Log("뿌려주기 시도");
        //뿌려주기
        byte[] imagedata;
        Cv2.ImEncode(".png", mat, out imagedata);
        Texture2D tex = new Texture2D(imWidth,imHeight);
        tex.LoadImage(imagedata);
        tex.Apply();
        result_image.texture = tex as Texture;
        Debug.Log("뿌려주기 완료");
        yield return null;

    }

    //영상처리파트
    void ThreadRun() {

        Debug.Log("StartThread");

        //cam_mat을 YCrCb 로 변환
        YCrCb = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        Cv2.CvtColor(cam_mat,YCrCb,ColorConversionCodes.RGB2YCrCb);

        //피부색 추출
        Cv2.InRange(YCrCb, new Scalar(0, 77,133), new Scalar(125, 127, 173),skinMat);
        
   }




    

    


    //외부에서 카메라상의 손의 위치를 쉽게 불러다가 사용할 수 있도록 함. 
    public static Vector2 get_Hand_Position()
    {
        return hand_position;
    }





}
