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
    private Mat closeMat;
    private Mat xyMat;
    private Mat labelMat;
    private Mat statMat;
    private Mat medianMat;
    private Mat hsvMat;
    private Mat redMat;

    private Mat lineMat;


    // Frame rate parameter
    private int updateFrameCount = 0;
    private int textureCount = 0;
    private int displayCount = 0;
    private Vec3b pix;

    public static Mat YCrCb;
    public GameObject HandPointer_01;
    public GameObject HandPointer_02;

    public OpenCvSharp.Rect statRect;
    private OpenCvSharp.Rect statRect2;
    private OpenCvSharp.Rect statRect3;

    Mat background;
    CascadeClassifier faceCascade;
    Itemlabel[] itemArrMain;
    private Itemlabel[] itemArrMain2;
    private int fy;
    private int fx;
    private Mat statMat2;
    private Mat labelMat2;
    private Mat xyMat2;

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
            closeMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            filterMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            binMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            labelMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            statMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            background = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            lineMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            hsvMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            redMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            medianMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
            //유니티 엔진에 적용하기 위해 사용하는 Texture2D 프레임워크 초기화
            processedTexture = new Texture2D(imWidth, imHeight, TextureFormat.RGBA32, true, true);
            result_texture = new Texture2D(imWidth, imHeight, TextureFormat.RGBA32, true, true);
            statRect = new OpenCvSharp.Rect(0,0, 0, 0);
            //손 위치 초기화
            hand_position = new Vector2();

            faceCascade = new CascadeClassifier("C:/GitHubProject/AR_ImageProcessing_Project/Assets/Resources/haarcascade_frontface.xml");
        }
        else
        {
            Debug.Log("Can't find camera!");
        }
     
    }
    
    // Update is called once per frame
    void Update () {
      
        if (webcamTexture.isPlaying)
        {

            if (webcamTexture.didUpdateThisFrame)
            {



                //유니티 카메라에서 얻은 Texture 형식을 Mat형식으로 변환
                cam_mat = TextureToMat();
                if (textureCount ==20)
                {

                    background = cam_mat;
                    Debug.Log("background : "+background.Size().Height);
                }



                if (cam_mat == null)
                {
                    Debug.Log("생성실패");
                }
                else {
                    //뿌려주기(비동기 가상 스레드) 
                    if (textureCount >= 20)
                    {

                        //영상처리 스레드 생성 (일회성)
                        Thread trackThread = new Thread(new ThreadStart(ThreadRun));
                        trackThread.Start();

                   
                        StartCoroutine(mat_To_Texture(filterMat));
                        //좌표를 화면에 보여줌. 
                        HandPointer_01.GetComponent<RectTransform>().position = new Vector3(320 - statRect.X,statRect.Y, -5);
                        Debug.Log(" point " + fx + " , " + fy);


                    }

                }


                //텍스처 카운트
                textureCount++;



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
        //프레임 카운트
        updateFrameCount++;


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


        // Cv2.Subtract(cam_mat, background, subMat);
        //lineMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        //Cv2.Canny(cam_mat,lineMat)


        /*
        //얼굴인식
        OpenCvSharp.Rect[] faces = faceCascade.DetectMultiScale(cam_mat, 1.08, 2, HaarDetectionType.FindBiggestObject, new Size(100, 100));

        //검출한 얼굴의 위치에 원으로 묘화
        foreach (OpenCvSharp.Rect face in faces)
        {
            var center = new Point
            {
                X = (int)(face.X + face.Width * 0.5),
                Y = (int)(face.Y + face.Height * 0.5)
            };
            var axes = new Size
            {
                Width = (int)(face.Width * 0.5),
                Height = (int)(face.Height * 0.5)
            };
            Debug.Log(" point " + center.X + " , " + center.Y);

            //얼굴영역제외
            Cv2.Circle(cam_mat, new Point(center.X, center.Y), 10, -1, 100, LineTypes.AntiAlias);
        }

        */

        Cv2.CvtColor(cam_mat, hsvMat, ColorConversionCodes.RGB2HSV_FULL);
        Cv2.InRange(hsvMat, new Scalar(175, 50, 0), new Scalar(180, 255, 255), redMat);
        Cv2.MedianBlur(redMat, medianMat, 7);
        xyMat2 = new Mat(imHeight, imWidth, MatType.CV_64F);
        labelMat2 = new Mat();
        statMat2 = new Mat();

        //관심영역 처리-레이블링
        int nLab2 = Cv2.ConnectedComponentsWithStats(medianMat, labelMat2, statMat2, xyMat2);

        //라벨 인덱스 생성
        var statsIndexer2 = statMat2.GetGenericIndexer<int>();

        Itemlabel[] itemArr2 = new Itemlabel[nLab2];
        int[] sizearr2 = new int[nLab2];

        int index = 1;
        int maxsize = 0;
        //살색후보군 저장. - 라벨
        for (int i = 0; i < nLab2; i++)
        {

            OpenCvSharp.Rect tabRect2 = new OpenCvSharp.Rect()
            {
                X = statsIndexer2[i, 0],
                Y = statsIndexer2[i, 1],
                Width = statsIndexer2[i, 2],
                Height = statsIndexer2[i, 3]
            };

            Itemlabel item2 = new Itemlabel();
            item2.rect = tabRect2;
            item2.labelnum = i;
            item2.labelsize = tabRect2.Width * tabRect2.Height;

            itemArr2[i] = item2;
            sizearr2[i] = tabRect2.Width * tabRect2.Height;

            if (sizearr2[i] > maxsize && sizearr2[i] < 57000) {
                maxsize = sizearr2[i];
                index = i;

            }


        }


        if (nLab2 >=2) {
            fx = itemArr2[index].rect.X + itemArr2[index].rect.Width / 2;
            //fx = itemArr2[0].labelsize;
            fy = itemArr2[index].rect.Y + itemArr2[index].rect.Height / 2;
        }


        ////////////////////////////////////////////////////////////////

        //YCrCb 로 변환
        YCrCb = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        Cv2.CvtColor(cam_mat,YCrCb,ColorConversionCodes.RGB2YCrCb);

        //피부색 추출 -> gray color로 출력 
        Cv2.InRange(YCrCb, new Scalar(0, 73,133), new Scalar(255, 148, 173),skinMat);

        //이진화(gray color -> 흰색과 검은색으로 이진화)
        binMat =  new Mat(imHeight, imWidth, MatType.CV_8UC3);
        Cv2.Threshold(skinMat, binMat, 0, 255, ThresholdTypes.Binary);

        //침식
        lineMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        Cv2.MorphologyEx(binMat, lineMat, MorphTypes.DILATE, new Mat());


        //모폴로지 연산 - 침식
        openMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        Cv2.MorphologyEx(lineMat, openMat, MorphTypes.Open, new Mat());

        //가우시안 필터에 비해 윤곽선을 강조하기에 유리하며 임펄스성분 제거에 유리 
        filterMat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        //Cv2.GaussianBlur(openMat, filterMat,new Size(5,5),7);
        Cv2.MedianBlur(openMat, filterMat, 11);


        //얼굴영역제외  
        Mat tabmat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        tabmat = filterMat;
        Cv2.Circle(tabmat, new Point(fx, fy), 10, -1, 100, LineTypes.AntiAlias);
        
       xyMat = new Mat(imHeight,imWidth,MatType.CV_64F);
       labelMat = new Mat();
       statMat = new Mat();

       //관심영역 처리-레이블링
       int nLab = Cv2.ConnectedComponentsWithStats(tabmat,labelMat,statMat,xyMat);

       //라벨 인덱스 생성
       var statsIndexer = statMat.GetGenericIndexer<int>();

       Itemlabel[] itemArr = new Itemlabel[nLab];
       itemArrMain = new Itemlabel[nLab];
       int[] sizearr = new int[nLab];

       //살색후보군 저장. - 라벨
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

       //살색 사이즈가 큰 순서로 적용 
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

       //가장 큰 살색
       //statRect = itemArrMain[0].rect;

       
        //코너검출 
        Point2f[] cornerPointArr = Cv2.GoodFeaturesToTrack(tabmat, 1, 0.5, 20, tabmat, 15, false, 0.04);

        float sum_x = 0;
        float sum_y = 0;
        float cnt = 0;
        //코너들의 평균 좌표값 계산
        for (int i = 0; i < cornerPointArr.Length; i++)
        {
            //Cv2.Circle(cam_mat, cornerPointArr[i], 3, Scalar.Black,1, LineTypes.AntiAlias);
            //라벨 안에 검출된 코너가 있을 경우 
            if (cornerPointArr[i].X>= itemArrMain[1].rect.X&& cornerPointArr[i].X <= itemArrMain[1].rect.X+ itemArrMain[1].rect.Width ) {
                if (cornerPointArr[i].Y >= itemArrMain[1].rect.Y&& cornerPointArr[i].Y <= itemArrMain[1].rect.Y + itemArrMain[1].rect.Height)
                {
                    //그 코너 위치를 표시
                    Cv2.Circle(tabmat, cornerPointArr[i], 4, Scalar.Red, 2, LineTypes.AntiAlias);
                    sum_x += cornerPointArr[i].X;
                    sum_y += cornerPointArr[i].Y;
                    cnt += 1.0f;
                }
            }

        }
        int avgx = (int)(sum_x / cnt);
        int avgy = (int)(sum_y / cnt);
        //좌표반환        
        statRect = new OpenCvSharp.Rect(avgx, avgy,0,0);
    }

    /*
    //외부에서 카메라상의 손의 위치를 쉽게 불러다가 사용할 수 있도록 함. 
    //320*180 해상도 전용
    public static Vector2 get_Hand_Position()
    {
        return new Vector2(320-statRect.X,statRect.Y);
    }

    */
    class Itemlabel{
        public OpenCvSharp.Rect rect;
        public int labelsize;
        public int labelnum;

    }


}
