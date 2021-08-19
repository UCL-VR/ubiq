using UnityEngine;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace PuzzleMaker
{

    public class PuzzlePieceMaker
    {

        private bool ColorPieces = true;

        private Texture2D[] _JointMask = null;
        public SPieceMetaData[,] _CreatedImagePiecesData = null;


#region "Properties"

        private int _NoOfPiecesInRow = 0;
        public int NoOfPiecesInRow
        {
            get { return _NoOfPiecesInRow; }
        }


        private int _NoOfPiecesInCol = 0;
        public int NoOfPiecesInCol
        {
            get { return _NoOfPiecesInCol; }
        }


        private int _PieceWidthWithoutJoint = 0;
        public int PieceWidthWithoutJoint
        {
            get { return _PieceWidthWithoutJoint; }
        }


        private int _PieceHeightWithoutJoint = 0;
        public int PieceHeightWithoutJoint
        {
            get { return _PieceHeightWithoutJoint; }
        }


        private Texture2D _Image = null;
        public Texture2D Image
        {
            //Return a copy of actual image
            get { return Object.Instantiate(_Image) as Texture2D; }
        }

        public Texture2D[] JointMasks
        {
            get
            {
                Texture2D[] Temp = new Texture2D[_JointMask.Length];
                
                //Make copy of array to return so that noone can make changes inside
                for (int i = 0; i < Temp.Length; i++)
                    Temp[i] = Object.Instantiate(_JointMask[i]) as Texture2D;

                return Temp;
            }
        }


        private Texture2D _CreatedBackgroundImage = null;
        public Texture2D CreatedBackgroundImage
        {
            get
            {
                return Object.Instantiate(_CreatedBackgroundImage) as Texture2D;
            }
        }

#endregion


#region "Constructors"

        public PuzzlePieceMaker(string FilePath)
        {
            LoadData(FilePath);
        }

        public PuzzlePieceMaker(Stream PMFileStream)
        {
            LoadStreamData(PMFileStream);
        }

        public PuzzlePieceMaker(Texture2D Image, Texture2D[] JointMaskImage, int NoOfPiecesInRow, int NoOfPiecesInCol)
        {

#region "Arguments error checking"

            if (Image == null)
            {
                Debug.LogError("Error creating puzzle piece maker , Image cannot be null");
                return;
            }
            else if (NoOfPiecesInRow < 2 || NoOfPiecesInCol < 2)
            {
                Debug.LogError("Error creating puzzle piece maker , NoOfPiecesInRow or NoOfPiecesInCol cannot be less then 2");
                return;
            }

#endregion

            _Image = Image;

            _JointMask = JointMaskImage;

            _NoOfPiecesInRow = NoOfPiecesInRow;
            _NoOfPiecesInCol = NoOfPiecesInCol;


            Texture2D _CreatedImageMask;

            _CreatedImagePiecesData = GenerateJigsawPieces(Image, JointMaskImage, out _CreatedImageMask,  NoOfPiecesInRow, NoOfPiecesInCol);

            _CreatedBackgroundImage = PuzzleImgToBackgroundImage(Image, _CreatedImageMask, _NoOfPiecesInRow, NoOfPiecesInCol);
        }

        /// <summary>
        /// Returns whether file loading is supported on this platform.
        /// </summary>
        /// <returns></returns>
        public static bool IsPMFileSupportedPlatform()
        {
            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return true;
            }

            return false;
        }

#endregion


        
        /// <summary>
        /// Generate puzzle pieces from image.
        /// </summary>
        /// <param name="NoOfPiecesInRow"></param>
        /// <param name="NoOfPiecesInCol"></param>
        /// <param name="Image"></param>
        /// <param name="JointMaskImage">Mask image for joints. Null value means default circular joints.</param>
        /// <returns></returns>
        private SPieceMetaData[,] GenerateJigsawPieces(Texture2D Image, Texture2D[] JointMaskImage, out Texture2D CreatedImageMask, int NoOfPiecesInRow, int NoOfPiecesInCol)
        {

#region "Argument Error Checking"

            if (NoOfPiecesInRow < 2)
            {
                throw new System.ArgumentOutOfRangeException("NoOfPiecesInRow", "Argument should be greater then 1");
            }
            else if (NoOfPiecesInCol < 2)
            {
                throw new System.ArgumentOutOfRangeException("NoOfPiecesInCol", "Argument should be greater then 1");
            }
            else if (Image == null)
            {
                throw new System.ArgumentNullException("No texture2d assigned to this class");
            }

#endregion

            
            Texture2D[,] PuzzlePieces = null;

            Color[][] _PuzzleImageMask = Texture2DToColorArr(Image);

            int PieceWidthWithoutJoint = 0;
            int PieceHeightWithoutJoint = 0;
            SPieceInfo[,] PiecesInformation = null;

            if (JointMaskImage == null || JointMaskImage.Length == 0)
            {
                Texture2D[] _maskImage = new Texture2D[1];
                _maskImage[0] = CreateCircularJointMaskImage();

                PiecesInformation = DrawCustomPieceJointsMask(ref _PuzzleImageMask, _maskImage, out PieceWidthWithoutJoint,
                    out PieceHeightWithoutJoint, Image.width, Image.height, NoOfPiecesInCol, NoOfPiecesInRow);

            }
            else
            {
                PiecesInformation = DrawCustomPieceJointsMask(ref _PuzzleImageMask, JointMaskImage,  out PieceWidthWithoutJoint,
                    out PieceHeightWithoutJoint,Image.width, Image.height, NoOfPiecesInCol, NoOfPiecesInRow);
            }

            
            _PieceWidthWithoutJoint = PieceWidthWithoutJoint;
            _PieceHeightWithoutJoint = PieceHeightWithoutJoint;


            CreatedImageMask = ColorArrToTexture2D(_PuzzleImageMask);

            Color[][] _PuzzleImage = Texture2DToColorArr(Image);
            PuzzlePieces = MaskToPieces(ref _PuzzleImage, _PuzzleImageMask, PieceWidthWithoutJoint, PieceHeightWithoutJoint,
                    Image.width, Image.height, NoOfPiecesInCol, NoOfPiecesInRow);

            _NoOfPiecesInRow = NoOfPiecesInRow;
            _NoOfPiecesInCol = NoOfPiecesInCol;


            //Return data for puzzle pieces
            SPieceMetaData[,] ResultData = new SPieceMetaData[NoOfPiecesInCol, NoOfPiecesInRow];
            for (int i = 0; i < NoOfPiecesInCol; i++)
            {
                for (int j = 0; j < NoOfPiecesInRow; j++)
                {
                    ResultData[i, j] = new SPieceMetaData(PiecesInformation[i, j], PuzzlePieces[i, j]);

                }
            }

            return ResultData;

        }


        private Texture2D[,] MaskToPieces(ref Color[][] Image, Color[][] Mask, int PieceWidthWithoutJoint,
                        int PieceHeightWithoutJoint, int PuzzleImgWidth, int PuzzleImgHeight , int MaskRows = 5, int MaskCols = 5)
        {


#region "Arguments Error Checking"
            /*
            if (Image == null || Mask == null)
            {
                throw new System.ArgumentNullException();
            }
            else
            {
                if (Image.width != Mask.width || Image.height != Mask.height)
                    throw new System.ArgumentOutOfRangeException("Image and Mask should be of same size");
            }
            */
#endregion

            //Texture2D[,] ResultedMaskPieces = new Texture2D[MaskRows, MaskCols];
            Texture2D[,] ResultedImagePieces = new Texture2D[MaskRows, MaskCols];

            Color TransparentColor = new Color(255, 255, 255, 0);


            //Initialize blank image for piece extraction process
            Texture2D BlankImage = new Texture2D((int)(PieceWidthWithoutJoint * 2f), (int)(PieceHeightWithoutJoint * 2f) );
            for (int i = 0; i < BlankImage.width; i++)
                for (int j = 0; j < BlankImage.height; j++)
                    BlankImage.SetPixel(i, j, TransparentColor);
            BlankImage.Apply();


            //Use to start next piece extraction from image
            int LastXPosition = 0;
            int LastYPosition = 0;

            int PieceWidth = 0;
            int PieceHeight = 0;

            //Create pieces from mask
            for (int RowTrav = 0; RowTrav < MaskRows; RowTrav++)
            {

                LastXPosition = 0;

                for (int ColTrav = 0; ColTrav < MaskCols; ColTrav++)
                {
                    int PieceX = ColTrav * PieceWidthWithoutJoint;
                    int PieceY = RowTrav * PieceHeightWithoutJoint;

                    Color PieceColor = Mask[PieceX + 20][PieceY + 20];


#region " Extract colored pieces from mask and make pieces from image"

                    Stack<Vector2> pixelStack = new Stack<Vector2>();

                    Texture2D resultMaskPiece = Object.Instantiate(BlankImage) as Texture2D;
                    Texture2D resultImgPiece = Object.Instantiate(BlankImage) as Texture2D;

                    Vector2 maskPixelPostion = new Vector2(PieceWidthWithoutJoint/2, PieceHeightWithoutJoint/2);


                    pixelStack.Push(maskPixelPostion);

                    
                    //Maximum and minimum x pixel positions used in resultpiece image
                    int maxX = 0;
                    int maxY = 0;

                    //Offsets use to leave some white while drawing pixels in result piece image
                    int resultPieceXOffset = (int)(PieceWidthWithoutJoint * 0.4f);
                    int resultPieceYOffset = (int)(PieceHeightWithoutJoint * 0.4f);

                    int minX = PieceX + resultPieceXOffset;
                    int minY = PieceY + resultPieceYOffset;

                    int MaskWidth = PuzzleImgWidth;
                    int MaskHeight = PuzzleImgHeight;


                    while (pixelStack.Count > 0)
                    {
                        maskPixelPostion = pixelStack.Pop();

                        int pixelPositionX = (int)maskPixelPostion.x;
                        int pixelPositionY = (int)maskPixelPostion.y;


                        resultMaskPiece.SetPixel(pixelPositionX + resultPieceXOffset,
                                            pixelPositionY + resultPieceYOffset, PieceColor);
                        resultImgPiece.SetPixel(pixelPositionX + resultPieceXOffset,
                                            pixelPositionY + resultPieceYOffset, Image[PieceX + pixelPositionX]
                                                                                                [PieceY + pixelPositionY]);
                        

                        if (maxX < pixelPositionX + resultPieceXOffset) maxX = pixelPositionX + resultPieceXOffset;
                        if (maxY < pixelPositionY + resultPieceYOffset) maxY = pixelPositionY + resultPieceYOffset;

                        if (minX > pixelPositionX + resultPieceXOffset) minX = pixelPositionX + resultPieceXOffset;
                        if (minY > pixelPositionY + resultPieceYOffset) minY = pixelPositionY + resultPieceYOffset;

                        
                        //From center pixel spread outwards to get this piece
                        if (pixelPositionX + 1 < resultMaskPiece.width && PieceX + pixelPositionX +1 < MaskWidth)
                            if (Mask[PieceX + pixelPositionX + 1][PieceY + pixelPositionY] == PieceColor &&
                                    resultMaskPiece.GetPixel(resultPieceXOffset + pixelPositionX + 1, resultPieceYOffset + pixelPositionY) != PieceColor)
                                pixelStack.Push(new Vector2(maskPixelPostion.x + 1, maskPixelPostion.y));


                        if (pixelPositionY + 1 < resultMaskPiece.height && PieceY + pixelPositionY + 1 < MaskHeight)
                            if (Mask[PieceX + pixelPositionX][PieceY + pixelPositionY + 1] == PieceColor &&
                                    resultMaskPiece.GetPixel(resultPieceXOffset + pixelPositionX, resultPieceYOffset + pixelPositionY + 1) != PieceColor)
                                pixelStack.Push(new Vector2(maskPixelPostion.x, maskPixelPostion.y + 1));
                        

                        if ( PieceX + pixelPositionX - 1 > 0 )
                            if (Mask[PieceX + pixelPositionX - 1][PieceY + pixelPositionY] == PieceColor &&
                                        resultMaskPiece.GetPixel(resultPieceXOffset + pixelPositionX - 1, resultPieceYOffset + pixelPositionY) != PieceColor)
                                pixelStack.Push(new Vector2(maskPixelPostion.x - 1, maskPixelPostion.y));

                        if ( PieceY + pixelPositionY - 1 > 0 )
                            if (Mask[PieceX + pixelPositionX][PieceY + pixelPositionY - 1] == PieceColor &&
                                        resultMaskPiece.GetPixel(resultPieceXOffset + pixelPositionX, resultPieceYOffset + pixelPositionY - 1) != PieceColor)
                                pixelStack.Push(new Vector2(maskPixelPostion.x, maskPixelPostion.y - 1));
                       

                    }


                    resultMaskPiece.Apply();
                    

                    
                    PieceWidth = maxX - minX + 1;
                    PieceHeight = maxY - minY + 1;


                    //Extract this piece in real size image i.e.
                    //Texture2D finalMaskResultPiece = Object.Instantiate(BlankImage) as Texture2D;
                    Texture2D finalImgResultPiece = Object.Instantiate(BlankImage) as Texture2D;

                    //finalMaskResultPiece.Resize(PieceWidth, PieceHeight);
                    finalImgResultPiece.Resize(PieceWidth, PieceHeight);

                    for (int i = minX; i <= maxX; i++)
                    {
                        for (int j = minY; j <= maxY; j++)
                        {
                            //finalMaskResultPiece.SetPixel(i - minX, j - minY, resultMaskPiece.GetPixel(i, j));
                            finalImgResultPiece.SetPixel(i - minX, j - minY, resultImgPiece.GetPixel(i, j));
                        }
                    }


                    //finalMaskResultPiece.Apply();
                    finalImgResultPiece.Apply();

                    //ResultedMaskPieces[RowTrav, ColTrav] = finalMaskResultPiece;
                    ResultedImagePieces[RowTrav, ColTrav] = finalImgResultPiece;
                    
                    ResultedImagePieces[RowTrav, ColTrav].wrapMode = TextureWrapMode.Clamp;
                    //ResultedMaskPieces[RowTrav, ColTrav].wrapMode = TextureWrapMode.Clamp;
                    
#endregion


                    LastXPosition += PieceWidth;
                }

                LastYPosition += PieceHeight;

            }
            
            

            return ResultedImagePieces;

        }


        private SPieceInfo[,] DrawCustomPieceJointsMask(ref Color[][] Image, Texture2D[] JointMaskImage,
                out int PieceWidthWithoutJoint, out int PieceHeightWithoutJoint, int PuzzleImgWidth, int PuzzleImgHeight, int Rows = 5, int Cols = 5)
        {

            //Create direction wise mask images
            Texture2D[] LeftJointMask = new Texture2D[JointMaskImage.Length];
            int[] LeftJointMaskWidth = new int[JointMaskImage.Length];
            int[] LeftJointMaskHeight = new int[JointMaskImage.Length];

            Texture2D[] RightJointMask = new Texture2D[JointMaskImage.Length];
            int[] RightJointMaskWidth = new int[JointMaskImage.Length];
            int[] RightJointMaskHeight = new int[JointMaskImage.Length];

            Texture2D[] TopJointMask = new Texture2D[JointMaskImage.Length];
            int[] TopJointMaskWidth = new int[JointMaskImage.Length];
            int[] TopJointMaskHeight = new int[JointMaskImage.Length];

            Texture2D[] BottomJointMask = new Texture2D[JointMaskImage.Length];
            int[] BottomJointMaskWidth = new int[JointMaskImage.Length];
            int[] BottomJointMaskHeight = new int[JointMaskImage.Length];


            SPieceInfo[,] ResultPiecesData = new SPieceInfo[Rows, Cols];

            //Initialize pieces data
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    ResultPiecesData[i, j] = new SPieceInfo((i * Cols) + j);
            
            int PieceHeight = PuzzleImgHeight / Rows;
            int PieceWidth = PuzzleImgWidth / Cols;

            PieceWidthWithoutJoint = PieceWidth;
            PieceHeightWithoutJoint = PieceHeight;



            for (int ArrayTav = 0; ArrayTav < JointMaskImage.Length; ArrayTav++)
            {
                LeftJointMask[ArrayTav] = JointMaskImage[ArrayTav];
                RightJointMask[ArrayTav] = rotateImage(JointMaskImage[ArrayTav], 180);
                TopJointMask[ArrayTav] = rotateImage(JointMaskImage[ArrayTav], 270);
                BottomJointMask[ArrayTav] = rotateImage(JointMaskImage[ArrayTav], 90);

#region "Resize Joint mask images for drawing inside mask image And calculate joints width and height"

                //Resize Joint mask images for drawing inside mask image
                //  Image will be resized according to piece width
                int MaskImageWidth = (int)(PieceWidth * 0.3f);
                int MaskImageHeight = (int)((float)MaskImageWidth / ((float)JointMaskImage[ArrayTav].width / (float)JointMaskImage[ArrayTav].height));

                LeftJointMask[ArrayTav] = resizeImage(LeftJointMask[ArrayTav], MaskImageWidth, MaskImageHeight);
                RightJointMask[ArrayTav] = resizeImage(RightJointMask[ArrayTav], MaskImageWidth, MaskImageHeight);
                TopJointMask[ArrayTav] = resizeImage(TopJointMask[ArrayTav], MaskImageWidth, MaskImageHeight);
                BottomJointMask[ArrayTav] = resizeImage(BottomJointMask[ArrayTav], MaskImageWidth, MaskImageHeight);

                
                //Calculate joints width and heights
                CalculateCustomJointDimensions(LeftJointMask[ArrayTav], out LeftJointMaskWidth[ArrayTav], out LeftJointMaskHeight[ArrayTav]);
                
                RightJointMaskWidth[ArrayTav] = LeftJointMaskWidth[ArrayTav];
                RightJointMaskHeight[ArrayTav] = LeftJointMaskHeight[ArrayTav];

                TopJointMaskWidth[ArrayTav] = LeftJointMaskHeight[ArrayTav];
                TopJointMaskHeight[ArrayTav] = LeftJointMaskWidth[ArrayTav];

                BottomJointMaskWidth[ArrayTav] = LeftJointMaskHeight[ArrayTav];
                BottomJointMaskHeight[ArrayTav] = LeftJointMaskWidth[ArrayTav];
                
                
                /*
                //Save these image
                saveTexture2D(LeftJointMask[ArrayTav], "c:\\Images\\LeftJoint.png");
                saveTexture2D(RightJointMask[ArrayTav], "c:\\Images\\RightJointMask.png");
                saveTexture2D(TopJointMask[ArrayTav], "c:\\Images\\TopJointMask.png");
                saveTexture2D(BottomJointMask[ArrayTav], "c:\\Images\\BottomJointMask.png");
                */

#endregion

            }




#region "Argument Error Checking"

            //Joint mask image width and height should be same
            //Joint mask image should have only black and white pixels inside it

            if (JointMaskImage[0].width != JointMaskImage[0].height)
            {
                Debug.LogError("JointMaskImage width and height should be same");
                return null;
            }
            else
            {
                bool ErrorFound = false;  //If Non-Black or Non-White pixel found

                //Check for pixel colors in joint mask image
                for (int rowtrav = 0; rowtrav < JointMaskImage[0].height && !ErrorFound; rowtrav++)
                {
                    for (int coltrav = 0; coltrav < JointMaskImage[0].width && !ErrorFound; coltrav++)
                    {
                        Color PixelColor = JointMaskImage[0].GetPixel(coltrav, rowtrav);

                        if (PixelColor != Color.white || PixelColor != Color.black)
                        {
                            ErrorFound = true;

                            //Debug.LogError("Only white and black pixels are allowed in JointMaskImage");

                            //return null;
                        }
                    }
                }


            }

#endregion

            Color[][] CreatedMaskImage = new Color[PuzzleImgWidth][];
            
            //Clear Instantiated mask image
            for (int i = 0; i < PuzzleImgWidth; i++)
            {
                CreatedMaskImage[i] = new Color[PuzzleImgHeight];
                for (int j = 0; j < PuzzleImgHeight; j++)
                {
                    CreatedMaskImage[i][j] = Color.white;
                }
            }


#region "Color Pieces Alternatively And Generate random joint info And Draw joints"

            bool AlternatePiece = true;

            //if (ColorPieces)
            {
                Random.seed = System.DateTime.Now.Second;

                for (int RowTrav = 0; RowTrav < Rows; RowTrav++)
                {

                    for (int ColTrav = 0; ColTrav < Cols; ColTrav++)
                    {
                        int PieceX = ColTrav * PieceWidth;
                        int PieceY = RowTrav * PieceHeight;
                        Color PieceColor = AlternatePiece ? Color.green : Color.red;

                        for (int InternalRowTrav = PieceY; InternalRowTrav < PieceY + PieceHeight; InternalRowTrav++)
                            for (int InternalColTrav = PieceX; InternalColTrav < PieceX + PieceWidth; InternalColTrav++)
                                if (CreatedMaskImage[InternalColTrav][InternalRowTrav] == Color.white)
                                    CreatedMaskImage[InternalColTrav][InternalRowTrav] = PieceColor;


#region "Generate Random joint info and Draw Joints From Mask Image"


#region "Draw right joints according to piece joint information"

                        if (ColTrav < Cols - 1)
                        {
                            int SelectedRandomJoint = Random.Range(1, JointMaskImage.Length) - 1;

                            //Create random joint information
                            int RndVal = (int)(Random.Range(1f, 18f) >= 10 ? 1 : 0);
                            ResultPiecesData[RowTrav, ColTrav].AddJoint(new SJointInfo((EJointType)RndVal, EJointPosition.Right, 
                                            RightJointMaskWidth[SelectedRandomJoint], RightJointMaskHeight[SelectedRandomJoint]));


                            int JointX = PieceX + PieceWidth - 5;
                            int JointY = PieceY + (PieceHeight / 2) - (RightJointMask[SelectedRandomJoint].height / 2);

                            bool Result = false;
                            SJointInfo RightJointInfo = ResultPiecesData[RowTrav, ColTrav].GetJoint(EJointPosition.Right, out Result);

                            if (!Result)
                            {
                                Debug.LogError("Logical error in draw joints from mask image Right Joints");
                            }
                            else
                            {
                                if ( RightJointInfo.JointType == EJointType.Male )
                                    drawJoint(ref CreatedMaskImage, RightJointMask[SelectedRandomJoint], PieceColor, JointX, JointY);
                            }

                        }
#endregion

#region"Draw left joints according to piece joint information"

                        if (ColTrav > 0)
                        {
                            int SelectedRandomJoint = Random.Range(1, JointMaskImage.Length) - 1;

                            //Create random joint information
                            bool Result = false;

                            SJointInfo PreviousRightJoint = ResultPiecesData[RowTrav, ColTrav - 1].GetJoint(EJointPosition.Right, out Result);

                            if (Result == false)
                            {
                                Debug.LogError("Logical error in joints information left joint");
                            }
                            else
                            {
                                SJointInfo CalcLeftJoint = new SJointInfo(PreviousRightJoint.JointType == EJointType.Female ?
                                            EJointType.Male : EJointType.Female, EJointPosition.Left, 
                                            LeftJointMaskWidth[SelectedRandomJoint], LeftJointMaskHeight[SelectedRandomJoint]);
                                ResultPiecesData[RowTrav , ColTrav].AddJoint(CalcLeftJoint);
                            }


                            int JointX = PieceX - LeftJointMask[SelectedRandomJoint].width + 5;
                            int JointY = PieceY + (PieceHeight / 2) - (LeftJointMask[SelectedRandomJoint].height / 2);

                            Result = false;
                            SJointInfo LeftJointInfo = ResultPiecesData[RowTrav, ColTrav].GetJoint(EJointPosition.Left, out Result);

                            if (!Result)
                            {
                                Debug.LogError("Logical error in draw joints from mask image Left Joints");
                            }
                            else
                            {
                                if (LeftJointInfo.JointType == EJointType.Male)
                                    drawJoint(ref CreatedMaskImage, LeftJointMask[SelectedRandomJoint], PieceColor, JointX, JointY);
                            }
                        }

#endregion

#region"Draw Top joints according to piece joint information"
                        
                        if (RowTrav < Rows - 1)
                        {
                            int SelectedRandomJoint = Random.Range(1, JointMaskImage.Length) - 1;

                            //Create random joint information
                            int RndVal = (int)(Random.Range(1f, 17f) >= 10 ? 1 : 0);
                            ResultPiecesData[RowTrav, ColTrav].AddJoint(new SJointInfo((EJointType)RndVal, EJointPosition.Top,
                                            TopJointMaskWidth[SelectedRandomJoint], TopJointMaskHeight[SelectedRandomJoint] ));

                            int JointX = PieceX + (PieceWidth / 2) - (TopJointMask[SelectedRandomJoint].width / 2);
                            int JointY = PieceY + PieceHeight - 5;

                            bool Result = false;
                            SJointInfo TopJointInfo = ResultPiecesData[RowTrav, ColTrav].GetJoint(EJointPosition.Top, out Result);

                            if (!Result)
                            {
                                Debug.LogError("Logical error in draw joints from mask image Top Joints");
                            }
                            else
                            {
                                if (TopJointInfo.JointType == EJointType.Male)
                                    drawJoint(ref CreatedMaskImage, TopJointMask[SelectedRandomJoint], PieceColor, JointX, JointY);
                            }

                        }
                        
#endregion

#region"Draw Bottom joints according to piece joint information"

                        if (RowTrav > 0)
                        {
                            int SelectedRandomJoint = Random.Range(1, JointMaskImage.Length) - 1;

                            //Create random joint information
                            bool Result = false;

                            SJointInfo PreviousPieceTopJoint = ResultPiecesData[RowTrav - 1, ColTrav].GetJoint(EJointPosition.Top, out Result);

                            if (Result == false)
                            {
                                Debug.LogError("Logical error in joints information Bottom joint");
                            }
                            else
                            {
                                SJointInfo CalcBottomJoint = new SJointInfo(PreviousPieceTopJoint.JointType == EJointType.Female ?
                                            EJointType.Male : EJointType.Female, EJointPosition.Bottom,
                                            BottomJointMaskWidth[SelectedRandomJoint], BottomJointMaskHeight[SelectedRandomJoint] );

                                ResultPiecesData[RowTrav, ColTrav].AddJoint(CalcBottomJoint);
                            }


                            int JointX = PieceX + (PieceWidth / 2) - (BottomJointMask[SelectedRandomJoint].width / 2);
                            int JointY = PieceY - BottomJointMask[SelectedRandomJoint].height;

                            Result = false;
                            SJointInfo BottomJointInfo = ResultPiecesData[RowTrav, ColTrav].GetJoint(EJointPosition.Bottom, out Result);

                            if (!Result)
                            {
                                Debug.LogError("Logical error in draw joints from mask image Top Joints");
                            }
                            else
                            {
                                if (BottomJointInfo.JointType == EJointType.Male)
                                    drawJoint(ref CreatedMaskImage, BottomJointMask[SelectedRandomJoint], PieceColor, JointX, JointY);
                            }

                        }

#endregion


#endregion


                        AlternatePiece = !AlternatePiece;
                    }

                    if (Cols % 2 == 0)
                        AlternatePiece = !AlternatePiece;
                }


            }

#endregion

            //Store mask image for testing purposes
            //System.IO.File.WriteAllBytes("c:\\Images\\MaskImage.png", CreatedMaskImage.EncodeToPNG());

            Image = CreatedMaskImage;

            
            return ResultPiecesData;
        }


        private Texture2D PuzzleImgToBackgroundImage(Texture2D Image, Texture2D ImageMask, int NoOfPiecesInRow, int NoOfPiecesInCol)
        {
            Texture2D ImageCopy = Object.Instantiate(Image) as Texture2D;


            //Give image an effect
            Color[] texColors = ImageCopy.GetPixels();
            for (int i = 0; i < texColors.Length; i++)
            {
                float grayValue = texColors[i].grayscale;
                texColors[i] = new Color(grayValue, grayValue, grayValue, texColors[i].a);
            }
            ImageCopy.SetPixels(texColors);
            

            
            //Draw pieces borders
            Color BorderColor = new Color(30,30,30,100);

            for (int X = 0; X < ImageMask.width - 1; X++)
            {
                for (int Y = 0; Y < ImageMask.height - 1; Y++)
                {
                    if (ImageMask.GetPixel(X, Y) == Color.green &&
                            (ImageMask.GetPixel(X + 1, Y) == Color.red || ImageMask.GetPixel(X, Y + 1) == Color.red))
                    {
                        ImageCopy.SetPixel(X, Y, BorderColor);
                        ImageCopy.SetPixel(X+1, Y, BorderColor);
                        ImageCopy.SetPixel(X + 1, Y + 1, BorderColor);
                    }
                    else if (ImageMask.GetPixel(X, Y) == Color.red &&
                            (ImageMask.GetPixel(X + 1, Y) == Color.green || ImageMask.GetPixel(X, Y + 1) == Color.green))
                    {
                        ImageCopy.SetPixel(X, Y, BorderColor);
                        ImageCopy.SetPixel(X + 1, Y, BorderColor);
                        ImageCopy.SetPixel(X + 1, Y + 1, BorderColor);
                    }

                }
            }

            ImageCopy.Apply();

            return ImageCopy;
        }


        private Texture2D CreateCircularJointMaskImage()
        {
            int PieceWidth = 188;
            int PieceHeight = 188;

            Texture2D ResultPiece = new Texture2D(PieceWidth, PieceHeight);


            //Initialize with white color
            Color[] TempRPC = new Color[PieceWidth * PieceHeight];
            for (int i = 0; i < TempRPC.Length; i++)
            {
                TempRPC[i] = new Color(255,255,255,0);
            }

            ResultPiece.SetPixels(TempRPC);


#region "Draw Joint Mask"

            int r = 60;
            int cx = PieceWidth - r/4;
            int cy = PieceHeight/2;

            int x, y, px, nx, py, ny, d;

            for (x = 0; x <= r; x++)
            {
                d = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
                for (y = 0; y <= d; y++)
                {
                        
                    px = cx + x;
                    nx = cx - x;
                    py = cy + y;
                    ny = cy - y;
                        
                        
                    if ( px < PieceWidth)
                        ResultPiece.SetPixel(px, py, Color.black);
                        
                    ResultPiece.SetPixel(nx, py, Color.black);
                        
                    if (px < PieceWidth)
                            ResultPiece.SetPixel(px, ny, Color.black);
                        
                    ResultPiece.SetPixel(nx, ny, Color.black);
                   
                }

            }
            

#endregion


            ResultPiece.Apply();

            return ResultPiece;
        }



        /// <summary>
        /// Saves all created pieces and other information required by this class.
        /// </summary>
        /// <param name="FilePath"></param>
        /// <returns></returns>
        public bool SaveData(string FilePath)
        {
            System.IO.BinaryWriter bWriter = null;

            using (bWriter = new System.IO.BinaryWriter(new System.IO.FileStream(FilePath, System.IO.FileMode.Create)))
            {
                //Write a random number to make sure when opening file the correct file is provided
                int secretNumber = 6640330;
                bWriter.Write(secretNumber);

                //Save basic variables data
                bWriter.Write(_NoOfPiecesInRow);
                bWriter.Write(_NoOfPiecesInCol);

                bWriter.Write(_PieceWidthWithoutJoint);
                bWriter.Write(_PieceHeightWithoutJoint);

                bWriter.Write(ColorPieces);


                //Check for error in created class variables
                if (_Image == null || _JointMask == null || _CreatedImagePiecesData == null ||
                    _CreatedBackgroundImage == null)
                {

                    Debug.LogError("Error saving data class may into be created properly");

                    bWriter.Close();

                    if (System.IO.File.Exists(FilePath))
                        System.IO.File.Delete(FilePath);

                    return false;
                }


#region "Save Basic Images"

                byte[] imageEncoded = _Image.EncodeToPNG();
                bWriter.Write(imageEncoded.Length);
                bWriter.Write(imageEncoded);

                byte[] createdBackgroundImageEncoded = _CreatedBackgroundImage.EncodeToPNG();
                bWriter.Write(createdBackgroundImageEncoded.Length);
                bWriter.Write(createdBackgroundImageEncoded);

#endregion

                //Save joint mask array
                bWriter.Write(_JointMask.Length);
                for (int i = 0; i < _JointMask.Length; i++)
                {
                    byte[] Temp = _JointMask[i].EncodeToPNG();
                    bWriter.Write(Temp.Length);
                    bWriter.Write(Temp);
                }


#region "Save pieces metadata"

                for (int RowTrav = 0; RowTrav < _NoOfPiecesInCol; RowTrav++)
                {
                    for (int ColTrav = 0; ColTrav < _NoOfPiecesInRow; ColTrav++)
                    {
                        //Save metadata for this piece
                        bWriter.Write(_CreatedImagePiecesData[RowTrav, ColTrav].PieceMetaData.ID);

                        SJointInfo[] TempJI = _CreatedImagePiecesData[RowTrav, ColTrav].PieceMetaData.GetJoints();
                        bWriter.Write(TempJI.Length);
                        for (int i = 0; i < TempJI.Length; i++)
                        {
                            bWriter.Write((int)TempJI[i].JointType);
                            bWriter.Write(TempJI[i].JointWidth);
                            bWriter.Write(TempJI[i].JointHeight);
                            bWriter.Write((int)TempJI[i].JointPosition);
                        }


                        //Save piece image data
                        byte[] Temp = _CreatedImagePiecesData[RowTrav, ColTrav].PieceImage.EncodeToPNG();
                        bWriter.Write(Temp.Length);
                        bWriter.Write(Temp);

                    }
                }

#endregion

                bWriter.Close();

            }


            return true;
        }

        /// <summary>
        /// Loads all created pieces and other information required by this class.
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns></returns>
        private bool LoadData(string FilePath)
        {
            
            System.IO.Stream stream = null;


            string Filename = System.IO.Path.GetFileName(FilePath);

            string StreammingFilePath = "";


            if (Application.platform == RuntimePlatform.Android )
            {
                StreammingFilePath = Application.streamingAssetsPath;
               
                StreammingFilePath = System.IO.Path.Combine(StreammingFilePath, Filename);

                WWW www = new WWW(StreammingFilePath);

                while (!www.isDone) ;

                if (string.IsNullOrEmpty(www.error))
                {

                    byte[] wwwLoadedData = www.bytes;
                    stream = new System.IO.MemoryStream(wwwLoadedData);

                }
                else
                {
                    Debug.LogError("www errror: " + www.error);
                    return false;
                }

            }
                            
            else if (Application.platform == RuntimePlatform.WindowsEditor ||
                        Application.platform == RuntimePlatform.WindowsPlayer ||
                        Application.platform == RuntimePlatform.OSXEditor ||
                        Application.platform == RuntimePlatform.OSXPlayer )
            {
                StreammingFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, Filename);

                stream = System.IO.File.OpenRead(StreammingFilePath);
            }

            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                StreammingFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, Filename);

                stream = System.IO.File.OpenRead(StreammingFilePath);
            }

            else
            {
                Debug.LogError(Application.platform + " not supported");
            }

            Debug.Log("Loading file from " + StreammingFilePath);


            return LoadStreamData(stream);
        }

        

        /// <summary>
        /// Load data from stream of file for puzzle maker.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private bool LoadStreamData(System.IO.Stream stream)
        {
            System.IO.BinaryReader bReader = null;

            try
            {
                using (bReader = new System.IO.BinaryReader(stream))
                {
                    //Get secret number to check file intigrity
                    int secretNumber = bReader.ReadInt32();
                    if (secretNumber != 6640330)
                    {
                        bReader.Close();
                        Debug.LogError("Error reading file. Make sure this file is created with puzzle maker`s current version");
                        return false;
                    }

                    //Get basic variables data
                    _NoOfPiecesInRow = bReader.ReadInt32();
                    _NoOfPiecesInCol = bReader.ReadInt32();

                    _PieceWidthWithoutJoint = bReader.ReadInt32();
                    _PieceHeightWithoutJoint = bReader.ReadInt32();

                    ColorPieces = bReader.ReadBoolean();


#region "Retrieve basic images"


                    int lengthImageEncoded = bReader.ReadInt32();
                    byte[] imageEncoded = bReader.ReadBytes(lengthImageEncoded);
                    _Image = new Texture2D(100, 100);
                    _Image.LoadImage(imageEncoded);


                    int lengthBackgroundImageEncoded = bReader.ReadInt32();
                    byte[] createdBackgroundImageEncoded = bReader.ReadBytes(lengthBackgroundImageEncoded);
                    _CreatedBackgroundImage = new Texture2D(100, 100);
                    _CreatedBackgroundImage.LoadImage(createdBackgroundImageEncoded);

#endregion


#region "Retrieve joint mask array"

                    int lengthJointMaskArr = bReader.ReadInt32();
                    _JointMask = new Texture2D[lengthJointMaskArr];
                    for (int i = 0; i < lengthJointMaskArr; i++)
                    {
                        int TempLength = bReader.ReadInt32();

                        _JointMask[i] = new Texture2D(100, 100);
                        byte[] TempMaskArr = bReader.ReadBytes(TempLength);
                        _JointMask[i].LoadImage(TempMaskArr);
                    }

#endregion


#region "Retreive Pieces Metadata"

                    _CreatedImagePiecesData = new SPieceMetaData[_NoOfPiecesInCol, _NoOfPiecesInRow];

                    for (int RowTrav = 0; RowTrav < _NoOfPiecesInCol; RowTrav++)
                    {
                        for (int ColTrav = 0; ColTrav < _NoOfPiecesInRow; ColTrav++)
                        {
                            int pieceID = bReader.ReadInt32();

                            //Get joints info
                            int JointInfoLength = bReader.ReadInt32();
                            SPieceInfo TempSPieceInfo = new SPieceInfo(pieceID);

                            for (int i = 0; i < JointInfoLength; i++)
                            {
                                int jointType = bReader.ReadInt32();
                                int jointWidth = bReader.ReadInt32();
                                int jointHeight = bReader.ReadInt32();
                                int jointPosition = bReader.ReadInt32();


                                TempSPieceInfo.AddJoint(new SJointInfo((EJointType)jointType, (EJointPosition)jointPosition,
                                                        jointWidth, jointHeight));
                            }

                            //Get this piece image
                            int pieceImgArrLength = bReader.ReadInt32();
                            byte[] pieceTempArr = bReader.ReadBytes(pieceImgArrLength);
                            Texture2D pieceTempImage = new Texture2D(100, 100);
                            pieceTempImage.LoadImage(pieceTempArr);
                            pieceTempImage.wrapMode = TextureWrapMode.Clamp;

                            //Insert this piece data in list
                            _CreatedImagePiecesData[RowTrav, ColTrav] = new SPieceMetaData(TempSPieceInfo, pieceTempImage);

                        }
                    }

#endregion

                    bReader.Close();

                }
            }
            catch (System.Exception ex)
            {
                throw new System.Exception("Exception in load data 2: " + ex.Message);
                //Debug.LogError("Puzzle Maker LoadData Method: " + ex.Message);
            }

            return true;
        }


#region "Helper Methods"

        private void CalculateCustomJointDimensions(Texture2D JointMaskImage, out int Width, out int Height)
        {

            //Used to track which pixels have been added to stack while getting width and height
            Texture2D TrackPixels = Object.Instantiate(JointMaskImage) as Texture2D;

            //Make Trackpixels image white
            for (int X = 0; X < TrackPixels.width; X++)
                for (int Y = 0; Y < TrackPixels.height; Y++)
                    TrackPixels.SetPixel(X, Y, Color.white);
            TrackPixels.Apply();


            int minX = JointMaskImage.width;
            int minY = JointMaskImage.height;

            int maxX = 0;
            int maxY = 0;

            Stack<Vector2> pixelStack = new Stack<Vector2>();
            Vector2 PixelPostion = new Vector2(0,0);

            //Find pixel position with black pixel
            bool IsPixelFound = false;
            for (int RowTrav = 0; RowTrav < JointMaskImage.height && !IsPixelFound; RowTrav++)
            {
                for (int ColTrav = 0; ColTrav < JointMaskImage.width && !IsPixelFound; ColTrav++)
                {
                    if (JointMaskImage.GetPixel(ColTrav, RowTrav) == Color.black)
                    {
                        PixelPostion = new Vector2(ColTrav, RowTrav);
                        IsPixelFound = true;
                        break;
                    }
                }
            }


            pixelStack.Push(PixelPostion);



            while (pixelStack.Count > 0)
            {
                PixelPostion = pixelStack.Pop();

                int pixelPositionX = (int)PixelPostion.x;
                int pixelPositionY = (int)PixelPostion.y;

                TrackPixels.SetPixel(pixelPositionX, pixelPositionY, Color.black);


                if (maxX < pixelPositionX) maxX = pixelPositionX;
                if (maxY < pixelPositionY) maxY = pixelPositionY;

                if (minX > pixelPositionX) minX = pixelPositionX;
                if (minY > pixelPositionY) minY = pixelPositionY;


                //From center pixel spread outwards to get this piece
                if (pixelPositionX + 1 < JointMaskImage.width)
                    if (JointMaskImage.GetPixel(pixelPositionX + 1, pixelPositionY) == Color.black &&
                          TrackPixels.GetPixel(pixelPositionX + 1, pixelPositionY) != Color.black)
                        pixelStack.Push(new Vector2(PixelPostion.x + 1, PixelPostion.y));


                if (pixelPositionY + 1 < JointMaskImage.height)
                    if ( JointMaskImage.GetPixel(pixelPositionX, pixelPositionY + 1) == Color.black &&
                            TrackPixels.GetPixel(pixelPositionX, pixelPositionY + 1) != Color.black)
                        pixelStack.Push(new Vector2(PixelPostion.x, PixelPostion.y + 1));


                if (pixelPositionX - 1 > 0)
                    if (JointMaskImage.GetPixel(pixelPositionX - 1, pixelPositionY) == Color.black &&
                            TrackPixels.GetPixel(pixelPositionX - 1, pixelPositionY) != Color.black)
                        pixelStack.Push(new Vector2(PixelPostion.x - 1, PixelPostion.y));

                if (pixelPositionY - 1 > 0)
                    if (JointMaskImage.GetPixel(pixelPositionX, pixelPositionY - 1) == Color.black &&
                            TrackPixels.GetPixel(pixelPositionX, pixelPositionY - 1) != Color.black)
                        pixelStack.Push(new Vector2(PixelPostion.x, PixelPostion.y - 1));
            }


            Width = maxX - minX;
            Height = maxY - minY;

        }
        

        private void Circle(Texture2D tex, int cx, int cy, int r, Color col)
        {

            var y = r;
            var d = 1 / 4 - r;
            var end = Mathf.Ceil(r / Mathf.Sqrt(2));

            for (int x = 0; x <= end; x++)
            {
                tex.SetPixel(cx + x, cy + y, col);
                tex.SetPixel(cx + x, cy - y, col);
                tex.SetPixel(cx - x, cy + y, col);
                tex.SetPixel(cx - x, cy - y, col);
                tex.SetPixel(cx + y, cy + x, col);
                tex.SetPixel(cx - y, cy + x, col);
                tex.SetPixel(cx + y, cy - x, col);
                tex.SetPixel(cx - y, cy - x, col);

                d += 2 * x + 1;

                if (d > 0)
                    d += 2 - 2 * y--;

            }

            tex.Apply();

        }


        private void SolidCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            int x, y, px, nx, py, ny, d;

            for (x = 0; x <= r; x++)
            {
                d = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
                for (y = 0; y <= d; y++)
                {
                    px = cx + x;
                    nx = cx - x;
                    py = cy + y;
                    ny = cy - y;

                    tex.SetPixel(px, py, col);
                    tex.SetPixel(nx, py, col);

                    tex.SetPixel(px, ny, col);
                    tex.SetPixel(nx, ny, col);

                }
            }
        }


        /// <summary>
        /// Converts a texture2d image to two dimensional jagged color array
        /// </summary>
        /// <param name="Image">Texture2d image to be converted</param>
        /// <returns></returns>
        public static Color[][] Texture2DToColorArr(Texture2D Image)
        {
            Color[][] Temp = new Color[Image.width][];

            if (Image != null)
            {
                for (int XTrav = 0; XTrav < Image.width; XTrav++)
                {
                    Temp[XTrav] = new Color[Image.height];

                    for (int YTrav = 0; YTrav < Image.height; YTrav++)
                    {
                        Temp[XTrav][YTrav] = Image.GetPixel(XTrav, YTrav);
                    }
                }

                return Temp;
            }

            return null;
        }


        /// <summary>
        /// Converts two dimensional jagged color array to texture2d image
        /// </summary>
        /// <param name="ImgClrArr">Jagged color array to be converted</param>
        /// <returns></returns>
        public static Texture2D ColorArrToTexture2D(Color[][] ImgClrArr)
        {
            int ImageWidth = ImgClrArr.GetLength(0);
            int ImageHeight = ImgClrArr[0].Length;

            Texture2D ResultedTexture = new Texture2D(ImageWidth, ImageHeight);
            for (int XTrav = 0; XTrav < ImageWidth; XTrav++)
            {
                for (int YTav = 0; YTav < ImageHeight; YTav++)
                {
                    ResultedTexture.SetPixel(XTrav, YTav, ImgClrArr[XTrav][YTav]);
                }
            }
            ResultedTexture.Apply();

            return ResultedTexture;
        }


        /// <summary>
        /// Rotates an image to provided angle value
        /// </summary>
        /// <param name="Image">Image to rotate</param>
        /// <param name="angle">Angle value between 0 and 360</param>
        /// <returns></returns>
        public static Texture2D rotateImage(Texture2D Image, int angle)
        {
            int x;
            int y;
            int i;
            int j;

            float phi = Mathf.Deg2Rad * angle;

            float sn = Mathf.Sin(phi);
            float cs = Mathf.Cos(phi);

            Color32[] arr = Image.GetPixels32();

            Texture2D texture = Object.Instantiate(Image) as Texture2D;
            Color32[] arr2 = texture.GetPixels32();

            int W = texture.width;
            int H = texture.height;
            int xc = W / 2;
            int yc = H / 2;

            for (j = 0; j < H; j++)
            {
                for (i = 0; i < W; i++)
                {
                    arr2[j * W + i] = new Color32(255, 255, 255, 0);

                    x = (int)cs * (i - xc) + (int)sn * (j - yc) + xc;
                    y = -(int)sn * (i - xc) + (int)cs * (j - yc) + yc;

                    if ((x > -1) && (x < W) && (y > -1) && (y < H))
                    {
                        arr2[j * W + i] = arr[y * W + x];
                    }
                }
            }

            texture.SetPixels32(arr2);
            texture.Apply();

            return texture;

        }


        /// <summary>
        /// Scales provided image
        /// </summary>
        /// <param name="Image">  </param>
        /// <param name="Scale"> Scale amount 0 and above </param>
        /// <returns>Return new scaled image</returns>
        public static Texture2D scaleImage(Texture2D Image, float Scale)
        {
            Texture2D Temp = Object.Instantiate(Image) as Texture2D;

            float NewWidth = Image.width * Scale;
            float NewHeight = Image.height * Scale;

            TextureScale.Bilinear(Temp, (int)NewWidth, (int)NewHeight);
            
            return Temp;
        }


        public static Texture2D resizeImage(Texture2D Image, int Width, int Height)
        {
            Texture2D Temp = Object.Instantiate(Image) as Texture2D;

            TextureScale.Bilinear(Temp, (int)Width, (int)Height);

            return Temp;
        }


        /// <summary>
        /// Draws using Joint Mask Image Onto Destination Image At Provided X And Y Coordinates.
        /// </summary>
        /// <param name="DestinationImage"> Image to be drawn on </param>
        /// <param name="JointMaskImage"> Image to draw </param>
        /// <param name="X">X position value should be greater then 0 and less then width - SourceImage Width of destination image</param>
        /// <param name="Y">Y position value should be greater then 0 and less then height - SourceImage Height of destination image</param>
        /// <returns></returns>
        private static void drawJoint(ref Color[][] DestinationImage, Texture2D JointMaskImage, Color JointColor, int X, int Y)
        {

            Texture2D TempS = JointMaskImage;

            for (int i = X; i < X + JointMaskImage.width; i++)
            {
                for (int j = Y; j < Y + JointMaskImage.height; j++)
                {
                    if (TempS.GetPixel(i - X, j - Y) == Color.black)
                        DestinationImage[i][j] = JointColor;
                    
                }
            }

        }


        public static void saveTexture2D(Texture2D Texture, string Path)
        {
           // System.IO.File.WriteAllBytes(Path, Texture.EncodeToPNG());
        }


#endregion


    }


    /// <summary>
    /// Code taken from http://wiki.unity3d.com/index.php/TextureScale
    /// </summary>
    public class TextureScale
    {

        public class ThreadData
        {
            public int start;
            public int end;
            public ThreadData(int s, int e)
            {
                start = s;
                end = e;
            }
        }


        private static Color[] texColors;
        private static Color[] newColors;
        private static int w;
        private static float ratioX;
        private static float ratioY;
        private static int w2;
        private static int finishCount;
        private static Mutex mutex;
        //private static Object thisLock = new Object();


        public static void Point(Texture2D tex, int newWidth, int newHeight)
        {
            ThreadedScale(tex, newWidth, newHeight, false);
        }

        public static void Bilinear(Texture2D tex, int newWidth, int newHeight)
        {
            ThreadedScale(tex, newWidth, newHeight, true);
        }

        private static void ThreadedScale(Texture2D tex, int newWidth, int newHeight, bool useBilinear)
        {
            texColors = tex.GetPixels();
            newColors = new Color[newWidth * newHeight];
            if (useBilinear)
            {
                ratioX = 1.0f / ((float)newWidth / (tex.width - 1));
                ratioY = 1.0f / ((float)newHeight / (tex.height - 1));
            }
            else
            {
                ratioX = ((float)tex.width) / newWidth;
                ratioY = ((float)tex.height) / newHeight;
            }
            w = tex.width;
            w2 = newWidth;
            var cores = Mathf.Min(SystemInfo.processorCount, newHeight);
            var slice = newHeight / cores;

            finishCount = 0;
            /*if (mutex == null)
            {
                mutex = new Mutex(false);
            }*/
            if (cores > 1)
            {
                int i = 0;
                ThreadData threadData;
                for (i = 0; i < cores - 1; i++)
                {
                    threadData = new ThreadData(slice * i, slice * (i + 1));
                    ParameterizedThreadStart ts = useBilinear ? new ParameterizedThreadStart(BilinearScale) : new ParameterizedThreadStart(PointScale);
                    Thread thread = new Thread(ts);
                    thread.Start(threadData);
                }
                threadData = new ThreadData(slice * i, newHeight);
                if (useBilinear)
                {
                    BilinearScale(threadData);
                }
                else
                {
                    PointScale(threadData);
                }
                while (finishCount < cores)
                {
                    Thread.Sleep(1);
                }
            }
            else
            {
                ThreadData threadData = new ThreadData(0, newHeight);
                if (useBilinear)
                {
                    BilinearScale(threadData);
                }
                else
                {
                    PointScale(threadData);
                }
            }

            tex.Resize(newWidth, newHeight);
            tex.SetPixels(newColors);
            tex.Apply();
        }

        public static void BilinearScale(System.Object obj)
        {
            ThreadData threadData = (ThreadData)obj;
            for (var y = threadData.start; y < threadData.end; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                var y1 = yFloor * w;
                var y2 = (yFloor + 1) * w;
                var yw = y * w2;

                for (var x = 0; x < w2; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    var xLerp = x * ratioX - xFloor;
                    newColors[yw + x] = ColorLerpUnclamped(ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp),
                                                           ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp),
                                                           y * ratioY - yFloor);
                }
            }

            //mutex.WaitOne();
            //lock (thisLock)
            //{
                finishCount++;
            //}
            //mutex.ReleaseMutex();
        }

        public static void PointScale(System.Object obj)
        {
            ThreadData threadData = (ThreadData)obj;
            for (var y = threadData.start; y < threadData.end; y++)
            {
                var thisY = (int)(ratioY * y) * w;
                var yw = y * w2;
                for (var x = 0; x < w2; x++)
                {
                    newColors[yw + x] = texColors[(int)(thisY + ratioX * x)];
                }
            }

            //mutex.WaitOne();
            //lock (thisLock)
            //{
                finishCount++;
            //}
            //mutex.ReleaseMutex();
        }

        private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
        {
            return new Color(c1.r + (c2.r - c1.r) * value,
                              c1.g + (c2.g - c1.g) * value,
                              c1.b + (c2.b - c1.b) * value,
                              c1.a + (c2.a - c1.a) * value);
        }
    
    }


}
