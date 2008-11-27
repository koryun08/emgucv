using System;
using System.Collections.Generic;
using System.Text;

namespace Emgu.CV.ML.UnitTest
{
   public class UnitTests
   {
      public void TestKNearest()
      {
         int K = 10;
         int trainSampleCount = 100;

         #region Generate the traning data and classes
         Matrix<float> trainData = new Matrix<float>(trainSampleCount, 2);
         Matrix<float> trainClasses = new Matrix<float>(trainSampleCount, 1);

         Image<Bgr, Byte> img = new Image<Bgr, byte>(500, 500);

         Matrix<float> sample = new Matrix<float>(1, 2);

         Matrix<float> trainData1 = trainData.GetRows(0, trainSampleCount >> 1, 1);
         trainData1.SetRandNormal(new MCvScalar(200), new MCvScalar(50));
         Matrix<float> trainData2 = trainData.GetRows(trainSampleCount >> 1, trainSampleCount, 1);
         trainData2.SetRandNormal(new MCvScalar(300), new MCvScalar(50));

         Matrix<float> trainClasses1 = trainClasses.GetRows(0, trainSampleCount >> 1, 1);
         trainClasses1.SetValue(1);
         Matrix<float> trainClasses2 = trainClasses.GetRows(trainSampleCount >> 1, trainSampleCount, 1);
         trainClasses2.SetValue(2);
         #endregion

         using (KNearest knn = new KNearest(trainData.Clone(), trainClasses.Clone(), null, false, K))
         {
            for (int i = 0; i < img.Height; i++)
            {
               for (int j = 0; j < img.Width; j++)
               {
                  sample[0, 0] = j;
                  sample[0, 1] = i;
                  Matrix<float> results, neighborResponses, dist;

                  // estimates the response and get the neighbors' labels
                  float response = knn.FindNearest(sample, K, out results, out neighborResponses, out dist);

                  int accuracy = 0;
                  // compute the number of neighbors representing the majority
                  for (int k = 0; k < K; k++)
                  {
                     if (neighborResponses[0, k] == response)
                        accuracy++;
                  }
                  // highlight the pixel depending on the accuracy (or confidence)
                  img[i, j] =
                  response == 1 ?
                      (accuracy > 5 ? new Bgr(90, 0, 0) : new Bgr(90, 60, 0)) :
                      (accuracy > 5 ? new Bgr(0, 90, 0) : new Bgr(60, 90, 0));
               }
            }
         }

         // display the original training samples
         for (int i = 0; i < (trainSampleCount >> 1); i++)
         {
            Point2D<int> p1 = new Point2D<int>((int)trainData1[i, 0], (int)trainData1[i, 1]);
            img.Draw(new Circle<int>(p1, 2), new Bgr(255, 100, 100), -1);
            Point2D<int> p2 = new Point2D<int>((int)trainData2[i, 0], (int)trainData2[i, 1]);
            img.Draw(new Circle<int>(p2, 2), new Bgr(100, 255, 100), -1);
         }

         Emgu.CV.UI.ImageViewer.Show(img);
      }

      public void TestEM()
      {
         int N = 4; //number of clusters
         int N1 = (int)Math.Sqrt((double)4);

         Bgr[] colors = new Bgr[] { 
            new Bgr(0, 0, 255), 
            new Bgr(0, 255, 0),
            new Bgr(0, 255, 255),
            new Bgr(255, 255, 0)};

         int nSamples = 100;

         Matrix<float> samples = new Matrix<float>(nSamples, 2);
         Matrix<Int32> labels = new Matrix<int>(nSamples, 1);
         Image<Bgr, Byte> img = new Image<Bgr,byte>(500, 500);
         Matrix<float> sample = new Matrix<float>(1, 2);

         using (EM emModel = new EM())
         {
            CvInvoke.cvReshape(samples.Ptr, samples.Ptr, 2, 0);
            for (int i = 0; i < N; i++)
            {
               Matrix<float> rows = samples.GetRows(i * nSamples / N, (i + 1) * nSamples / N, 1);
               double scale = ((i % N1) + 1.0) / (N1 + 1);
               MCvScalar mean = new MCvScalar(scale * img.Width, scale * img.Height);
               MCvScalar sigma = new MCvScalar(30, 30);
               ulong seed = (ulong)DateTime.Now.Ticks;
               CvInvoke.cvRandArr(ref seed, rows.Ptr, Emgu.CV.CvEnum.RAND_TYPE.CV_RAND_NORMAL, mean, sigma);
            }
            CvInvoke.cvReshape(samples.Ptr, samples.Ptr, 1, 0);

            EMParams parameters = new EMParams();
            parameters.Nclusters = N;
            parameters.CovMatType = Emgu.CV.ML.MlEnum.EM_COVARIAN_MATRIX_TYPE.COV_MAT_SPHERICAL;
            parameters.StartStep = Emgu.CV.ML.MlEnum.EM_INIT_STEP_TYPE.START_AUTO_STEP;
            parameters.TermCrit = new MCvTermCriteria(10, 0.1);

            emModel.Train(samples, null, parameters, labels);

            #region Classify every image pixel
            for (int i = 0; i < img.Height; i++)
               for (int j = 0; j < img.Width; j++)
               {
                  sample[0, 0] = (float)i;
                  sample[0, 1] = (float)j;
                  int response = (int) emModel.Predict(sample, null);

                  Bgr color = new Bgr();
                  for (int k = 0; k < color.Coordinate.Length; k++)
                     color.Coordinate[k] = colors[response][k] * 0.5;
                  
                  img.Draw(new Circle<int>(new Point2D<int>(j, i), 1), color, 0);
               }
            #endregion 

            #region draw the clustered samples
            for (int i = 0; i < nSamples; i++)
            {
               img.Draw(new Circle<int>(new Point2D<int>((int)samples[i, 0], (int)samples[i, 1]), 1), colors[labels[i, 0]], 0);
            }
            #endregion 
   
            Emgu.CV.UI.ImageViewer.Show(img);
         }
      }
   }
}