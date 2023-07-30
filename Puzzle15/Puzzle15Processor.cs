using System.Runtime.CompilerServices;
using Android.Util;
using OpenCV.Core;
using OpenCV.ImgProc;
using Size = OpenCV.Core.Size;

namespace Puzzle15
{
    //
    // This class is a controller for puzzle game.
    // It converts the image from Camera into the shuffled image
    //
    public class Puzzle15Processor
    {
        private const int GridSize = 4;
        private const int GridArea = GridSize * GridSize;
        private const int GridEmptyIndex = GridArea - 1;
        private const string Tag = "Puzzle15Processor";
        private Scalar GridEmptyColor = new Scalar(0x33, 0x33, 0x33, 0xFF);

        private int[] mIndexes;
        private int[] mTextWidths;
        private int[] mTextHeights;

        private Mat mRgba15;
        private Mat[] mCells15;
        private bool mShowTileNumbers = true;

        public Puzzle15Processor()
        {
            mTextWidths = new int[GridArea];
            mTextHeights = new int[GridArea];

            mIndexes = new int [GridArea];

            for (int i = 0; i < GridArea; i++)
                mIndexes[i] = i;
        }

        // this method is intended to make processor prepared for a new game
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PrepareNewGame()
        {
            do {
                Shuffle(mIndexes);
            } while (!IsPuzzleSolvable());
        }

        //
        // This method is to make the processor know the size of the frames that
        // will be delivered via puzzleFrame.
        // If the frames will be different size - then the result is unpredictable
        //
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PrepareGameSize(int width, int height)
        {
            mRgba15 = new Mat(height, width, CvType.Cv8uc4);
            mCells15 = new Mat[GridArea];

            for (int i = 0; i < GridSize; i++)
            {
                for (int j = 0; j < GridSize; j++)
                {
                    int k = i * GridSize + j;
                    mCells15[k] = mRgba15.Submat(i * height / GridSize, (i + 1) * height / GridSize, j * width / GridSize, (j + 1) * width / GridSize);
                }
            }

            for (int i = 0; i < GridArea; i++)
            {
                Size s = Imgproc.GetTextSize("" + i + 1, 3/* CV_FONT_HERSHEY_COMPLEX */, 1, 2, null);
                mTextHeights[i] = (int) s.Height;
                mTextWidths[i] = (int) s.Width;
            }
        }

        //
        // this method to be called from the outside. it processes the frame and shuffles
        // the tiles as specified by mIndexes array
        //
        [MethodImpl(MethodImplOptions.Synchronized)]
        public Mat PuzzleFrame(Mat inputPicture)
        {
            Mat[] cells = new Mat[GridArea];
            int rows = inputPicture.Rows();
            int cols = inputPicture.Cols();

            rows = rows - rows % 4;
            cols = cols - cols % 4;

            for (int i = 0; i < GridSize; i++)
            {
                for (int j = 0; j < GridSize; j++)
                {
                    int k = i * GridSize + j;
                    cells[k] = inputPicture.Submat(i * inputPicture.Rows() / GridSize, (i + 1) * inputPicture.Rows() / GridSize, j * inputPicture.Cols() / GridSize, (j + 1) * inputPicture.Cols() / GridSize);
                }
            }

            rows = rows - rows % 4;
            cols = cols - cols % 4;

            // copy shuffled tiles
            for (int i = 0; i < GridArea; i++)
            {
                int idx = mIndexes[i];
                if (idx == GridEmptyIndex)
                    mCells15[i].SetTo(GridEmptyColor);
                else
                {
                    cells[idx].CopyTo(mCells15[i]);
                    if (mShowTileNumbers)
                    {
                        Imgproc.PutText(mCells15[i], "" + 1 + idx, new Point((cols / GridSize - mTextWidths[idx]) / 2,
                                (rows / GridSize + mTextHeights[idx]) / 2), 3/* CV_FONT_HERSHEY_COMPLEX */, 1, new Scalar(255, 0, 0, 255), 2);
                    }
                }
            }

            for (int i = 0; i < GridArea; i++)
                cells[i].Release();

            DrawGrid(cols, rows, mRgba15);

            return mRgba15;
        }

        public void ToggleTileNumbers()
        {
            mShowTileNumbers = !mShowTileNumbers;
        }

        public void DeliverTouchEvent(int x, int y)
        {
            int rows = mRgba15.Rows();
            int cols = mRgba15.Cols();

            int row = (int) Math.Floor((float) y * GridSize / rows);
            int col = (int) Math.Floor((float) x * GridSize / cols);

            if (row < 0 || row >= GridSize || col < 0 || col >= GridSize)
            {
                Log.Error(Tag, "It is not expected to get touch event outside of picture");
                return ;
            }

            int idx = row * GridSize + col;
            int idxtoswap = -1;

            // left
            if (idxtoswap < 0 && col > 0)
                if (mIndexes[idx - 1] == GridEmptyIndex)
                    idxtoswap = idx - 1;
            // right
            if (idxtoswap < 0 && col < GridSize - 1)
                if (mIndexes[idx + 1] == GridEmptyIndex)
                    idxtoswap = idx + 1;
            // top
            if (idxtoswap < 0 && row > 0)
                if (mIndexes[idx - GridSize] == GridEmptyIndex)
                    idxtoswap = idx - GridSize;
            // bottom
            if (idxtoswap < 0 && row < GridSize - 1)
                if (mIndexes[idx + GridSize] == GridEmptyIndex)
                    idxtoswap = idx + GridSize;

            // swap
            if (idxtoswap >= 0)
            {
                lock(this)
                {
                    int touched = mIndexes[idx];
                    mIndexes[idx] = mIndexes[idxtoswap];
                    mIndexes[idxtoswap] = touched;
                }
            }
        }

        private void DrawGrid(int cols, int rows, Mat drawMat)
        {
            for (int i = 1; i < GridSize; i++)
            {
                Imgproc.Line(drawMat, new Point(0, i * rows / GridSize), new Point(cols, i * rows / GridSize), new Scalar(0, 255, 0, 255), 3);
                Imgproc.Line(drawMat, new Point(i * cols / GridSize, 0), new Point(i * cols / GridSize, rows), new Scalar(0, 255, 0, 255), 3);
            }
        }

        private static void Shuffle(int[] array)
        {
            Random random = new Random();
            for (int i = array.Length; i > 1; i--)
            {
                int temp = array[i - 1];
                int randIx = (int) (random.NextDouble() * i);
                array[i - 1] = array[randIx];
                array[randIx] = temp;
            }
        }

        private bool IsPuzzleSolvable()
        {

            int sum = 0;
            for (int i = 0; i < GridArea; i++)
            {
                if (mIndexes[i] == GridEmptyIndex)
                    sum += (i / GridSize) + 1;
                else
                {
                    int smaller = 0;
                    for (int j = i + 1; j < GridArea; j++)
                    {
                        if (mIndexes[j] < mIndexes[i])
                            smaller++;
                    }
                    sum += smaller;
                }
            }
            return sum % 2 == 0;
        }
    }
}
