using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Configuration;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace Lab4
{
    public partial class Form1 : Form
    {
        Image<Gray, byte> inputImage = null;
        int imageWidth = 0, imageHeight = 0;

        Label[] labels = new Label[8];
        PictureBox[] pictureBoxes = new PictureBox[8];

        public Form1()
        {
            InitializeComponent();
        }

        private void btnReview_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = openFileDialog1.ShowDialog();

                if (result == DialogResult.OK)
                {
                    inputImage = new Image<Gray, byte>(openFileDialog1.FileName);
                    tbPath.Text = openFileDialog1.FileName;
                    btnCalculate_Click(this, null);
                }
                else
                    MessageBox.Show("Файл не выбран", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Image<Gray, byte> ConvertToLaplasian(Image<Gray, byte> inputImage, out double[,] arrayImage)
        {
            sbyte[,] mask = new sbyte[,] {{ -1, -1, -1 },
                                          { -1,  8, -1 },
                                          { -1, -1, -1 }};

            Image<Gray, byte> image = new Image<Gray, byte>(imageWidth, imageHeight);
            arrayImage = new double[imageWidth, imageHeight];

            for (int i = 1; i < (imageWidth - 1); i++)
                for (int j = 1; j < (imageHeight - 1); j++)
                    for (int x = -1; x <= 1; x++)
                        for (int y = -1; y <= 1; y++)
                        {
                            Gray color = inputImage[j + y, i + x];
                            arrayImage[i, j] += color.Intensity * mask[y + 1, x + 1];
                        }

            for (int x = 0; x < imageWidth - 1; x++)
                for (int y = 0; y < imageHeight - 1; y++)
                    image[y, x] = new Gray(arrayImage[x, y]);

            return image;
        }

        private Image<Gray, byte> ConvertToGradationCorrection(double[,] arrayLablasian)
        {
            Image<Gray, byte> image = new Image<Gray, byte>(imageWidth, imageHeight);
            double min = double.MaxValue, max = double.MinValue;

            for (int i = 0; i < imageWidth - 1; i++)
                for (int j = 0; j < imageHeight - 1; j++)
                    if (min > arrayLablasian[i, j]) min = arrayLablasian[i, j];

            for (int x = 0; x < imageWidth - 1; x++)
                for (int y = 0; y < imageHeight - 1; y++)
                    arrayLablasian[x, y] = arrayLablasian[x, y] - min;

            for (int i = 0; i < imageWidth - 1; i++)
                for (int j = 0; j < imageHeight - 1; j++)
                    if (max < arrayLablasian[i, j]) max = arrayLablasian[i, j];

            for (int x = 0; x < imageWidth - 1; x++)
                for (int y = 0; y < imageHeight - 1; y++)
                {
                    arrayLablasian[x, y] = 255 * arrayLablasian[x, y] / max;
                    image[y, x] = new Gray(arrayLablasian[x, y]);
                }

            return image;
        }

        private Image<Gray, byte> ConvertToSobel(Image<Gray, byte> inputImage)
        {
            int[,] maskS1 = new int[,] {{ -1, -2, -1 },
                                        {  0,  0,  0 },
                                        {  1,  2,  1 }};

            int[,] maskS2 = new int[,] {{ -1, 0, 1 },
                                        { -2, 0, 2 },
                                        { -1, 0, 1 }};

            double[,] arraySobel1 = new double[imageWidth, imageHeight];
            double[,] arraySobel2 = new double[imageWidth, imageHeight];

            for (int i = 1; i < (imageWidth - 1); i++)
                for (int j = 1; j < (imageHeight - 1); j++)
                    for (int x = -1; x <= 1; x++)
                        for (int y = -1; y <= 1; y++)
                        {
                            Gray color = inputImage[j + y, i + x];
                            arraySobel1[i, j] += color.Intensity * maskS1[y + 1, x + 1];
                            arraySobel2[i, j] += color.Intensity * maskS2[y + 1, x + 1];
                        }

            Image<Gray, byte> image = new Image<Gray, byte>(imageWidth, imageHeight);
            for (int x = 0; x < imageWidth - 1; x++)
                for (int y = 0; y < imageHeight - 1; y++)
                    image[y, x] = new Gray(Math.Abs(arraySobel1[x, y]) + Math.Abs(arraySobel2[x, y]));

            return image;
        }

        private Image<Gray, byte> ConvertToAveragingFiltering(Image<Gray, byte> inputImage)
        {
            Image<Gray, byte> image = new Image<Gray, byte>(imageWidth, imageHeight);
            double[,] arrayImage = new double[imageWidth, imageHeight];

            for (int i = 2; i < (imageWidth - 2); i++)
                for (int j = 2; j < (imageHeight - 2); j++)
                    for (int x = -2; x <= 2; x++)
                        for (int y = -2; y <= 2; y++)
                        {
                            Gray color = inputImage[j + y, i + x];
                            arrayImage[i, j] += color.Intensity;
                        }

            for (int x = 0; x < imageWidth - 1; x++) //Удаление максимума и минимума
                for (int y = 0; y < imageHeight - 1; y++)
                    image[y, x] = new Gray(arrayImage[x, y] / 25);

            return image;
        }

        private Image<Gray, byte> ConvertToGradationCorrection(Image<Gray, byte> inputImage)
        {
            Image<Gray, byte> image = new Image<Gray, byte>(imageWidth, imageHeight);
            for (int y = 0; y < imageHeight; y++)
                for (int x = 0; x < imageWidth; x++)
                    image[y, x] = new Gray(Math.Pow(inputImage[y, x].Intensity / 255, .5) * 255);

            return image;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < pictureBoxes.Length; i++)
            {
                PictureBox pictureBox = new PictureBox
                {
                    Size = new Size(250, 400),
                    Left = 15 + 265 * i,
                    Top = 5,
                    SizeMode = PictureBoxSizeMode.Zoom
                };
                pictureBoxes[i] = pictureBox;

                Label label = new Label
                {
                    AutoSize = true,
                    TextAlign = ContentAlignment.TopCenter,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                labels[i] = label;
                tableLayoutPanel1.Controls.Add(pictureBoxes[i], i, 0);
                tableLayoutPanel1.Controls.Add(labels[i], i, 1);
            }
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            

            inputImage = new Image<Gray, byte>(tbPath.Text);
            imageWidth = inputImage.Cols; imageHeight = inputImage.Rows;

            /* Исходное изображение */
            pictureBoxes[0].Image = inputImage.Bitmap;
            labels[0].Text = "Исходное изображение";

            /* Лапласиан */
            Image<Gray, byte> imageLaplasian = ConvertToLaplasian(inputImage, out double[,] arrayLablasian);
            //panel1.Controls.Add(pictureBoxes[1]); ;

            /*Лапласиан с градационной коррекцией */
            Image<Gray, byte> imageLaplToGradCorr = ConvertToGradationCorrection(arrayLablasian);
            pictureBoxes[1].Image = imageLaplToGradCorr.Bitmap;
            labels[1].Text = "Лапласиан с градационной коррекцией";

            /* Сложение исходного изображения и Лапласиана */
            Image<Gray, byte> imageInputPlusLapl = inputImage + imageLaplasian;
            pictureBoxes[2].Image = imageInputPlusLapl.Bitmap;
            labels[2].Text = "Сложение исходного изображения и Лапласиана";

            /* Собель */
            Image<Gray, byte> imageSobel = ConvertToSobel(inputImage);
            pictureBoxes[3].Image = imageSobel.Bitmap;
            labels[3].Text = "Применение градиентного оператора Собела к исходному изображению";

            /* Однородная усредняющая фильтрация */
            Image<Gray, byte> imageSobToAvrFilt = ConvertToAveragingFiltering(imageSobel);
            pictureBoxes[4].Image = imageSobToAvrFilt.Bitmap;
            labels[4].Text = "Изображение с оперетором Собеля и однородная усредняющая фильтрация";

            /* Перемножение исходного изображениес с Лапласианом и усредняющей фильтрации */
            Image<Gray, byte> imageLaplMultAvrFilt = imageInputPlusLapl.And(imageSobToAvrFilt);
            pictureBoxes[5].Image = imageLaplMultAvrFilt.Bitmap;
            labels[5].Text = "Перемножение Лапласиана и усредняющей фильтрации";

            /* Сложение исходного изображения с изображением выше */
            Image<Gray, byte> imageLaplMultAvrFiltPlusInput = imageLaplMultAvrFilt + inputImage;
            pictureBoxes[6].Image = imageLaplMultAvrFiltPlusInput.Bitmap;
            labels[6].Text = "Сложение исходного изображения и Лапласиана";

            /* Градационная коррекция по степенному закону с изображением выше */
            Image<Gray, byte> outputImage = ConvertToGradationCorrection(imageLaplMultAvrFiltPlusInput);
            pictureBoxes[7].Image = outputImage.Bitmap;
            labels[7].Text = "Результат";
        }
    }
}