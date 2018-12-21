//  Confidential Source Code Property Toys2Life LLC Colorado 2017
//  www.toys2life.org

using System;
using NAudio.Dsp;

namespace DialogGenerator.Utilities.Model
{
    /// <summary>
    /// Collects data from <see cref="ISoundPlayer"/> and calculates FFT of data
    /// </summary>
    public class SampleAggregator
    {
        #region - fields -

        private Complex[] mChannelData;
        private int mBufferSize;
        private int mBinaryExponentitation;
        private int mChannelDataPosition;

        #endregion

        #region - constructor -

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_bufferSize">buffer size</param>
        public SampleAggregator(int _bufferSize)
        {
            this.mBufferSize = _bufferSize;
            mBinaryExponentitation = (int)Math.Log(_bufferSize, 2);
            mChannelData = new Complex[_bufferSize];
        }

        #endregion

        #region - public methods -

        /// <summary>
        /// Resets position 
        /// </summary>
        public void Clear()
        {
            mChannelDataPosition = 0;
        }
             
        /// <summary>
        /// Add a sample value to the aggregator.
        /// </summary>
        /// <param name="value">The value of the sample.</param>
        public void Add(float leftValue, float rightValue)
        {            
            // Make stored channel data stereo by averaging left and right values.
            mChannelData[mChannelDataPosition].X = (leftValue + rightValue) / 2.0f;
            mChannelData[mChannelDataPosition].Y = 0;
            mChannelDataPosition++;

            if (mChannelDataPosition >= mChannelData.Length)
            {
                mChannelDataPosition = 0;
            }
        }

        /// <summary>
        /// Performs an FFT calculation on the channel data upon request.
        /// </summary>
        /// <param name="fftBuffer">A buffer where the FFT data will be stored.</param>
        public void GetFFTResults(float[] fftBuffer)
        {
            Complex[] _channelDataClone = new Complex[mBufferSize];
            mChannelData.CopyTo(_channelDataClone, 0);
            FastFourierTransform.FFT(true, mBinaryExponentitation, _channelDataClone);

            for (int i = 0; i < _channelDataClone.Length / 2; i++)
            {
                // Calculate actual intensities for the FFT results.
                fftBuffer[i] = (float)Math.Sqrt(_channelDataClone[i].X * _channelDataClone[i].X + _channelDataClone[i].Y * _channelDataClone[i].Y);
            }
        }

        #endregion
    }
}
