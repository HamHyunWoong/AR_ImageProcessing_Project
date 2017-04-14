using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using OpenCvSharp;
using System.IO;

public class Textrue2D_Mat : MonoBehaviour {

     

    //이미지 
    public RawImage show_image;
    public Texture start_image;

    //결과화면
    public RawImage result_image;
    Texture2D result_texture;

    //사이즈 
    private const int imWidth = 500;
    private const int imHeight = 500;


    //OpenCVSharp의 mat
    private Mat trans_mat;

    //vector클래스 
    private Vec3b[] SourceImageData;
   
   
    // Use this for initialization
    void Start () {
        //처음 이미지 화면에 뿌려주기
        show_image.texture = start_image;

        //Texture2D형식으로 이미지 불러오기, 사진 픽셀 크기와 texture2d크기가 맞지 않을 경우 안됨.
        byte[] fileData = File.ReadAllBytes("C:/Users/DELL/Desktop/HW/Texture2D_Mat/Assets/Resources/image/ddd.jpg");
        Texture2D start2D_image = new Texture2D(500,500);
        start2D_image.LoadImage(fileData);

        //이미지크기 배열 
        SourceImageData = new Vec3b[imHeight * imWidth];
          
        //결과 화면 정의
        result_texture = new Texture2D(imWidth, imHeight, TextureFormat.RGBA32, true, true);

        //Texture2D 이미지를 mat형식으로 변환 
        trans_mat = TextureToMat(start2D_image);

        //결과이미지를 저장할 mat 정의
        Mat result_mat = new Mat();   
        Mat HSV_mat = new Mat();
        Mat yCrCb_mat = new Mat() ;


        //이곳에서 변환처리 
        //yCrCb와 HSV생성
        Cv2.CvtColor(trans_mat,yCrCb_mat, ColorConversionCodes.RGB2YCrCb);
        Cv2.CvtColor(trans_mat,HSV_mat, ColorConversionCodes.RGB2HSV);

        //변환함수 입력 
        result_mat = test2(trans_mat);

        //결과이미지를 화면에 뿌려준다. 
        //코루틴으로 처리(비동기 프로세스)
        StartCoroutine(mat_To_Texture(result_mat));


    }

    
    Mat test1(Mat mat) {
        
        Mat result = new Mat();
        Mat result2 = new Mat();
        Mat result3 = new Mat();
        //rgb를 gray로 변환
        Cv2.CvtColor(mat, result, ColorConversionCodes.RGB2GRAY);

        //입력,결과,최소경계값,최대경계값
        Cv2.Canny(result,result2, 125, 350);

        //입력,결과,gradX,gradY
        Cv2.Sobel(result,result3,MatType.CV_16U,0,1);

        //반환
        return result2;
    }

    Mat test2(Mat mat)
    {

        Mat result = new Mat();
        Mat result2 = new Mat();
        Mat result3 = new Mat();
        Mat result4 = new Mat();
        Mat result5 = new Mat();
        Mat result6 = new Mat();
        Mat result7 = new Mat();
        Mat result8 = new Mat();

        //gray로 변환
        Cv2.CvtColor(mat, result, ColorConversionCodes.RGB2GRAY);

        //입력,결과,이진화값,같은이미지상픽셀값,형태 - 바이너리 
        Cv2.Threshold(result, result2, 127, 255, ThresholdTypes.Binary);

        //모노폴리 Dilation
        Cv2.Dilate(result2,result3,new Mat());
        //모노폴리 Erosion
        Cv2.Erode(result2, result4, new Mat());
        //모노폴리 Opening
        Cv2.MorphologyEx(result2, result5,MorphTypes.Open, new Mat());
        //모노폴리 Closeing
        Cv2.MorphologyEx(result2, result6, MorphTypes.Close, new Mat());

        //반환
        return result6;
    }

    Mat test3(Mat mat)
    {

        Mat result = new Mat();
        Mat result2 = new Mat();
        Mat result3 = new Mat();
        //rgb를 YCrCb로 변환
        Cv2.CvtColor(mat, result, ColorConversionCodes.RGB2YCrCb);

        //입력,결과,최소경계값,최대경계값
        Cv2.Canny(mat, result2, 125, 350);

        //반환
        return result3;
    }


    // Convert Unity Texture2D object to OpenCVSharp Mat object
    Mat TextureToMat(Texture2D texture2D)
    {
        // Color32 array : r, g, b, a
       

       Color32[] c = texture2D.GetPixels32();

        // 루프하면서 병렬처리
        //  Color32 object를  Vec3b object로 변환 
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
                SourceImageData[j + i * imWidth] = vec3;
            }
        });
        // assign the Vec3b array to Mat
        Mat result_mat = new Mat(imHeight, imWidth, MatType.CV_8UC3);
        result_mat.SetArray(0, 0, SourceImageData);
        return result_mat;
    }

    IEnumerator mat_To_Texture(Mat mat) {

        //mat을 jpg포멧의 byte[]형식으로 변환 
        byte[] imagedata;
        Cv2.ImEncode(".jpg", mat, out imagedata);


        //이미지 화면에 띄우기
        Texture2D tex = new Texture2D(imWidth,imHeight);
        tex.LoadImage(imagedata);
        tex.Apply();
        result_image.texture = tex as Texture;
        
        yield return null;

    }

    




}
