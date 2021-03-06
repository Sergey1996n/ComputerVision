using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab7
{
    public partial class Form1 : Form
    {
        Image<Bgr, byte> inputImage = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnReview_Click_1(object sender, EventArgs e)
        {
            
        }

        private void btnReview_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = openFileDialog1.ShowDialog();

                if (result == DialogResult.OK)
                {
                    inputImage = new Image<Bgr, byte>(openFileDialog1.FileName);
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

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            inputImage = new Image<Bgr, byte>(tbPath.Text);
            //Исходное изображение
            Image<Gray, byte> imageGray = inputImage.Convert<Gray, byte>();
            pictureBox1.Image = imageGray.ToBitmap();

            Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(25, 25), new Point(-1, -1));
            Image<Gray, byte> imageGrad = imageGray.MorphologyEx(MorphOp.Gradient, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

            pictureBox2.Image = imageGrad.ToBitmap();

            var mask = imageGrad.ThresholdBinaryInv(new Gray(32), new Gray(255)); ;
            Mat distanceTransofrm = new Mat();
            CvInvoke.DistanceTransform(mask, distanceTransofrm, null, DistType.L2, 3);
            Image<Gray, byte> markers = distanceTransofrm.ToImage<Gray, byte>();

            pictureBox3.Image = markers.ToBitmap();
            CvInvoke.ConnectedComponents(markers, markers);
            Image<Gray, int> finalMarkers = markers.Convert<Gray, int>();

            CvInvoke.Watershed(inputImage, finalMarkers);

            Image<Gray, byte> boundaries = finalMarkers.Convert(delegate (int x)
            {
                return (byte)(x == -1 ? 255 : 0);
            });

            pictureBox3.Image = (imageGrad + boundaries).ToBitmap();
            pictureBox4.Image = (inputImage + boundaries.Convert<Bgr, byte>()).ToBitmap();
        }


    }
}
