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
using System.Threading;
public class TextureMat : MonoBehaviour {


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
    Mat result_mat;
    Mat yCrCb_mat;


    //vector클래스 
    private Vec3b[] SourceImageData;


    // Use this for initialization
    void Start()
    {
        //처음 이미지 화면에 뿌려주기
        show_image.texture = start_image;

        //Texture2D형식으로 이미지 불러오기, 사진 픽셀 크기와 texture2d크기가 맞지 않을 경우 안됨.
        byte[] fileData = File.ReadAllBytes("C:/GitHubProject/AR_ImageProcessing_Project/Assets/image/aaa.jpg");
        Texture2D start2D_image = new Texture2D(500, 500);
        start2D_image.LoadImage(fileData);

        //이미지크기 배열 
        SourceImageData = new Vec3b[imHeight * imWidth];

        //결과 화면 정의
        result_texture = new Texture2D(imWidth, imHeight, TextureFormat.RGBA32, true, true);

        //Texture2D 이미지를 mat형식으로 변환 
        trans_mat = TextureToMat(start2D_image);
        yCrCb_mat = new Mat(imHeight, imWidth, MatType.CV_8UC3);


        //이곳에서 변환처리 
        
        //yCrCb생성 : 변환예시
        Cv2.CvtColor(trans_mat, yCrCb_mat, ColorConversionCodes.RGB2YCrCb);

        //뿌려주기 
        StartCoroutine(mat_To_Texture(yCrCb_mat));
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

    //mat 형식을 Texture로 바꿔서 화면에 뿌려줌
    IEnumerator mat_To_Texture(Mat mat)
    {
  
        //mat을 jpg포멧의 byte[]형식으로 변환 
        byte[] imagedata;
        Cv2.ImEncode(".jpg", mat, out imagedata);


        //이미지 화면에 띄우기
        Texture2D tex = new Texture2D(imWidth, imHeight);
        tex.LoadImage(imagedata);
        tex.Apply();
        result_image.texture = tex as Texture;

        yield return null;

    }


}
