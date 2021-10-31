using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Configuration;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using AForge;
using AForge.Imaging;
using AForge.Math;
using FFTW.NET;

namespace Lab4
{

    

    public partial class Form1 : Form
    {
        Image<Bgr, byte> inputImage = null;
        ComplexImage complexImage;
        int imageWidth = 0, imageHeight = 0;
        //int[] intensity;

        Label[] labels = new Label[63];
        PictureBox[] pictureBoxes = new PictureBox[63];

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
                    //inputImage = new Image<Bgr, byte>(openFileDialog1.FileName);
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

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < pictureBoxes.Length; i++)
            {
                
                PictureBox pictureBox = new PictureBox
                {
                    Size = new Size(150, 150),
                    SizeMode = PictureBoxSizeMode.Zoom
                };
                pictureBoxes[i] = pictureBox;

                Label label = new Label
                {
                    AutoSize = true,
                    Font = new Font("Microsoft Sans Serif", 7.25f),
                    TextAlign = ContentAlignment.TopCenter,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                labels[i] = label;
                if (i < 3)
                {
                    tableLayoutPanel1.Controls.Add(pictureBoxes[i], i + 1, 0);
                    tableLayoutPanel1.Controls.Add(labels[i], i + 1, 1);
                }
                else
                {
                    tableLayoutPanel1.Controls.Add(pictureBoxes[i], (i - 3) % 6, (i + 3) / 6 * 2);
                    tableLayoutPanel1.Controls.Add(labels[i], (i - 3) % 6, (i + 3) / 6 * 2 + 1);
                }
            }
        }


		private ComplexImage ComplexImageIdeal(int circleIdeal, bool down)
		{
			//int circleIgeal = 50;
            ComplexImage complexImageIdeal = (ComplexImage)complexImage.Clone();
            for (int u = 0; u < imageWidth; u++)
                for (int v = 0; v < imageHeight; v++)
                    if (Distance(u, v) > circleIdeal && down || Distance(u, v) <= circleIdeal && !down)
                        complexImageIdeal.Data[u, v] *= 0;
			return complexImageIdeal;
		}

        private ComplexImage ComplexImageButterworth(int circleButterworth, bool down)
        {
            int n = 2;
            ComplexImage complexImageButterworth = (ComplexImage)complexImage.Clone();
            for (int i = 0; i < imageWidth; i++)
                for (int j = 0; j < imageHeight; j++)
                    if (down) complexImageButterworth.Data[i, j] *= 1 / (1 + Math.Pow(Distance(i, j) / circleButterworth, 2 * n));
                    else complexImageButterworth.Data[i, j] *= 1 - (1 / (1 + Math.Pow(Distance(i, j) / circleButterworth, 2 * n)));
            return complexImageButterworth;
        }

        private ComplexImage ComplexImageGaussian(int circleGaussian, bool down)
        {
            ComplexImage complexImageGaussian = (ComplexImage)complexImage.Clone();
            for (int i = 0; i < imageWidth; i++)
                for (int j = 0; j < imageHeight; j++)
                    if (down) complexImageGaussian.Data[i, j] *= Math.Exp(-Distance(i, j) / 2 / circleGaussian);
                    else complexImageGaussian.Data[i, j] *= 1 - Math.Exp(-Distance(i, j) / 2 / circleGaussian);
            return complexImageGaussian;
        }

        private double Distance(int u, int v)
        {
            return Math.Sqrt(Math.Pow(imageWidth / 2 - u, 2) + Math.Pow(imageHeight / 2 - v, 2));
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            /* Первоначальные настройки изображения */
            Bitmap bitmapTemp = new Bitmap(tbPath.Text);
            imageWidth = (int)Math.Pow(2, (int)Math.Log(bitmapTemp.Width, 2));
            imageHeight = (int)Math.Pow(2, (int)Math.Log(bitmapTemp.Height, 2));
            inputImage = new Image<Bgr, byte>(new Bitmap(bitmapTemp, imageWidth, imageHeight));

            /* Исходное изображение */
            pictureBoxes[0].Image = inputImage.Bitmap;
            labels[0].Text = "Исходное изображение";

            /* Оттенки серого*/
            Image<Gray, float> imageGray = new Image<Gray, float>(inputImage.Bitmap);
            //pictureBoxes[1].Image = imageGray.ToBitmap();
            //labels[1].Text = "Оттенки серого";

            /* Спектр изображения */
            complexImage = ComplexImage.FromBitmap(imageGray.ToBitmap());
            complexImage.ForwardFourierTransform();
            pictureBoxes[1].Image = complexImage.ToBitmap();
            labels[1].Text = "Спектор изображения";

            /* Логарифрированный спектор изображения */
            Image<Gray, byte> imageComplexImage = new Image<Gray, byte>(complexImage.ToBitmap());
            for (int i = 0; i < imageWidth; i++)
                for (int j = 0; j < imageHeight; j++)
                    imageComplexImage[j, i] = new Gray(Math.Log(1 + imageComplexImage[j, i].Intensity));
            CvInvoke.Normalize(imageComplexImage, imageComplexImage, 0, 255, NormType.MinMax);
            pictureBoxes[2].Image = imageComplexImage.Bitmap;
            labels[2].Text = "Логарифрированный спектор изображения";

            for (int number = 1; number < 6; number++)
            {
                /* Спектор изображения c идеальным фильтром */
                ComplexImage complexImageIdeal = ComplexImageIdeal(number * 15, true);
                pictureBoxes[3 + (number - 1) * 6].Image = complexImageIdeal.ToBitmap();
                labels[3 + (number - 1) * 6].Text = $"Спектор c радиусом {number * 15}";

                complexImageIdeal.BackwardFourierTransform();
                pictureBoxes[4 + (number - 1) * 6].Image = complexImageIdeal.ToBitmap();
                labels[4 + (number - 1) * 6].Text = "Идеальный фильтр";

                /* Спектор изображения c фильтром Баттервотта */
                ComplexImage complexImageButterworth = ComplexImageButterworth(number * 15, true);
                pictureBoxes[5 + (number - 1) * 6].Image = complexImageButterworth.ToBitmap();
                labels[5 + (number - 1) * 6].Text = $"Спектор c радиусом {number * 15}";

                complexImageButterworth.BackwardFourierTransform();
                pictureBoxes[6 + (number - 1) * 6].Image = complexImageButterworth.ToBitmap();
                labels[6 + (number - 1) * 6].Text = "Фильтр с Баттервоттом";

                /* Спектор изображения c Гауссовским фильтром */
                ComplexImage complexImageGaussian = ComplexImageGaussian(number * 15, true);
                pictureBoxes[7 + (number - 1) * 6].Image = complexImageGaussian.ToBitmap();
                labels[7 + (number - 1) * 6].Text = $"Спектор c радиусом {number * 15}";

                complexImageGaussian.BackwardFourierTransform();
                pictureBoxes[8 + (number - 1) * 6].Image = complexImageGaussian.ToBitmap();
                labels[8 + (number - 1) * 6].Text = "Гауссовский фильтр";

                /* Спектор изображения c идеальным фильтром */
                complexImageIdeal = ComplexImageIdeal(number * 15, false);
                pictureBoxes[33 + (number - 1) * 6].Image = complexImageIdeal.ToBitmap();
                labels[33 + (number - 1) * 6].Text = $"Спектор c радиусом {number * 15}";

                complexImageIdeal.BackwardFourierTransform();
                pictureBoxes[34 + (number - 1) * 6].Image = complexImageIdeal.ToBitmap();
                labels[34 + (number - 1) * 6].Text = "Идеальный фильтр";

                /* Спектор изображения c фильтром Баттервотта */
                complexImageButterworth = ComplexImageButterworth(number * 15, false);
                pictureBoxes[35 + (number - 1) * 6].Image = complexImageButterworth.ToBitmap();
                labels[35 + (number - 1) * 6].Text = $"Спектор c радиусом {number * 15}";

                complexImageButterworth.BackwardFourierTransform();
                pictureBoxes[36 + (number - 1) * 6].Image = complexImageButterworth.ToBitmap();
                labels[36 + (number - 1) * 6].Text = "Фильтр с Баттервоттом";

                /* Спектор изображения c Гауссовским фильтром */
                complexImageGaussian = ComplexImageGaussian(number * 15, false);
                pictureBoxes[37 + (number - 1) * 6].Image = complexImageGaussian.ToBitmap();
                labels[37 + (number - 1) * 6].Text = $"Спектор c радиусом {number * 15}";

                complexImageGaussian.BackwardFourierTransform();
                pictureBoxes[38 + (number - 1) * 6].Image = complexImageGaussian.ToBitmap();
                labels[38 + (number - 1) * 6].Text = "Гауссовский фильтр";
            }
        }
    }
}