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
    private const int imWidth = 320;
    private const int imHeight = 180;


    // OpenCVSharp parameters
    private Mat videoSource_Mat_Image;
    private Mat cam_mat;
    private Mat outputmat;
    private Texture2D processedTexture;
    private Vec3b[] videoSourceImageData;

    private Mat skinMat;
    private Mat openMat;
    private Mat binMat;
    private Mat filterMat;
    private Mat dilMat;
    private Mat xyMat;
    private Mat labelMat;
    private Mat statMat;


    // Frame rate parameter
    private int updateFrameCount = 0;
    private int textureCount = 0;
    private int displayCount = 0;
    private Vec3b pix;

    public static Mat YCrCb;
   


    public GameObject HandPointer_01;
    public GameObject HandPointer_02;

    private OpenCvSharp.Rect statRect;
    private OpenCvSharp.Rect statRect2;
    private OpenCvSharp.Rect statRect3;

    Itemlabel[] itemArrMain;
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
            YCrCb = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            openMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            dilMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            filterMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            binMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            labelMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            statMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);


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
                  

                    //영상처리 스레드 생성 (일회성)
                    Thread trackThread = new Thread(new ThreadStart(ThreadRun));
                    trackThread.Start();
                    
                    //뿌려주기(비동기 가상 스레드) 
                    StartCoroutine(mat_To_Texture(filterMat));

                    //xy좌표 
                    Debug.Log("X = "+statRect2.X+"   Y = "+statRect2.Y);
                     

                    HandPointer_01.GetComponent<RectTransform>().position = new Vector3(320-(statRect2.X+(statRect2.Width/2)), (statRect2.Y + (statRect2.Height / 2)), 0);
                    
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
        
        //뿌려주기
        byte[] imagedata;
        Cv2.ImEncode(".png", mat, out imagedata);
        Texture2D tex = new Texture2D(imWidth,imHeight);
        tex.LoadImage(imagedata);
        tex.Apply();
        result_image.texture = tex as Texture;
       
        yield return null;

    }

    //영상처리파트
    void ThreadRun() {
      

       


        //YCrCb 로 변환
        YCrCb = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        Cv2.CvtColor(cam_mat,YCrCb,ColorConversionCodes.RGB2YCrCb);

        //피부색 추출 -> gray color로 출력 
        Cv2.InRange(YCrCb, new Scalar(10, 77,133), new Scalar(240, 127, 153),skinMat);

        //이진화(gray color -> 흰색과 검은색으로 이진화)
        binMat =  new Mat(imHeight, imWidth, MatType.CV_8UC3);
        Cv2.Threshold(skinMat, binMat, 0, 255, ThresholdTypes.Binary);

        //팽창 
        dilMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        Cv2.Dilate(binMat, dilMat, new Mat());

        //메디안 필터링(입력,출력,중간기준을 설정하는 필터값) : 중간값 필터링, 마스크에 있어서 중간값을 취함. 
        //가우시안 필터에 비해 윤곽선을 강조하기에 유리하며 임펄스성분 제거에 유리 
        filterMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        Cv2.MedianBlur(dilMat, filterMat, 11);

        xyMat = new Mat(imHeight,imWidth,MatType.CV_64F);
        labelMat = new Mat();
        statMat = new Mat();




        //라벨링
        int nLab = Cv2.ConnectedComponentsWithStats(dilMat,labelMat,statMat,xyMat);

        //라벨들의 인덱스
        var statsIndexer = statMat.GetGenericIndexer<int>();

        Itemlabel[] itemArr = new Itemlabel[nLab];
        itemArrMain = new Itemlabel[nLab];
        int[] sizearr = new int[nLab];
       
        //라벨정보 저장.
        for (int i=0;i<nLab;i++) {

            OpenCvSharp.Rect tabRect = new OpenCvSharp.Rect()
            {
                X = statsIndexer[i, 0],
                Y = statsIndexer[i, 1],
                Width = statsIndexer[i, 2],
                Height = statsIndexer[i, 3]
            };

            Itemlabel item = new Itemlabel();
            item.rect = tabRect;
            item.labelnum = i;
            item.labelsize = tabRect.Width * tabRect.Height;

            itemArr[i] = new Itemlabel();
            itemArr[i] = item; 
            sizearr[i] = tabRect.Width * tabRect.Height;

        }

        //사이즈가 큰 순서로 적용 
        System.Array.Sort(sizearr);
        System.Array.Reverse(sizearr);
        
        //라벨 리스트 정렬
        for (int i = 0; i < nLab; i++) {

            for (int j = 0; j < nLab; j++) {


                if (sizearr[i] == itemArr[j].labelsize ) {
                    itemArrMain[i] = new Itemlabel();
                    itemArrMain[i] = itemArr[j];

                }

            }


        }

        //머리
        statRect = itemArrMain[0].rect;
       
        //손1
        statRect2 = itemArrMain[1].rect;

        //손1
        statRect3 = itemArrMain[2].rect;





        //모폴로지 연산 - 침식후 팽창
        //openMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        //Cv2.MorphologyEx(filterMat, openMat, MorphTypes.Open, new Mat());


    }


    //외부에서 카메라상의 손의 위치를 쉽게 불러다가 사용할 수 있도록 함. 
    public static Vector2 get_Hand_Position()
    {
        return hand_position;
    }


    class Itemlabel{
        public OpenCvSharp.Rect rect;
        public int labelsize;
        public int labelnum;

    }


}
