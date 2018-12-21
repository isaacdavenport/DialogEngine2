//  Confidential Source Code Property Toys2Life LLC Colorado 2017
//  www.toys2life.org

using DialogGenerator.Utilities;
using DialogGenerator.Utilities.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DialogGenerator.UI.Controls
{
    /// <summary>
    /// A spectrum analyzer control for visualizing audio level and frequency data.
    /// </summary>
    [DisplayName("Spectrum Analyzer")]
    [Description("Displays audio level and frequency data.")]
    [ToolboxItem(true)]
    [TemplatePart(Name = "PART_SpectrumCanvas", Type = typeof(Canvas))]
    public class SpectrumAnalyzer : Control
    {
        #region - fields - 

        #region - constants -
        private const int mcScaleFactorLinear = 9;
        private const int mcScaleFactorSqr = 2;
        private const double mcMinDBValue = -90;
        private const double mcMaxDBValue = 0;
        private const double mcDBScale = (mcMaxDBValue - mcMinDBValue);
        private const int mcDefaultUpdateInterval = 25;
        #endregion

        private readonly DispatcherTimer mcAnimationTimer;
        private Canvas mSpectrumCanvas;
        private ISpectrumPlayer mSoundPlayer;
        private readonly List<Shape> mcBarShapes = new List<Shape>();
        private readonly List<Shape> mcPeakShapes = new List<Shape>();
        private double[] mBarHeights;
        private double[] mPeakHeights;
        private float[] mChannelData = new float[2048];
        private float[] channelPeakData;
        private double mBandWidth = 1.0;
        private double mBarWidth = 1;
        private int mMaximumFrequencyIndex = 2047;
        private int mMinimumFrequencyIndex;
        private int[] mBarIndexMax;
        private int[] mBarLogScaleIndexMax;
        #endregion

        #region - dependency properties -

        #region SoundPlayer

        /// <summary>
        /// Identifies the <see cref="SoundPlayer" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty SoundPlayerProperty =
            DependencyProperty.Register("SoundPlayer", typeof(ISoundPlayer), typeof(SpectrumAnalyzer),new PropertyMetadata(null,_onSoundPlayerChanged));

        private static void _onSoundPlayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = d as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
            {
                _spectrumAnalyzer.mSoundPlayer = e.NewValue as ISpectrumPlayer;
                _spectrumAnalyzer.mSoundPlayer.PropertyChanged += _spectrumAnalyzer.soundPlayer_PropertyChanged;
                _spectrumAnalyzer.UpdateBarLayout();
                _spectrumAnalyzer.mcAnimationTimer.Start();
            }
        }

        public ISpectrumPlayer SoundPlayer
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (ISpectrumPlayer)GetValue(SoundPlayerProperty);
            }
            set
            {
                SetValue(SoundPlayerProperty, value);
            }
        }
        #endregion

        #region MaximumFrequency
        /// <summary>
        /// Identifies the <see cref="MaximumFrequency" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty MaximumFrequencyProperty = 
            DependencyProperty.Register("MaximumFrequency", typeof(int), typeof(SpectrumAnalyzer), new UIPropertyMetadata(20000, _onMaximumFrequencyChanged, _onCoerceMaximumFrequency));

        private static object _onCoerceMaximumFrequency(DependencyObject o, object value)
        {
            if (o is SpectrumAnalyzer spectrumAnalyzer)
                return spectrumAnalyzer._onCoerceMaximumFrequency((int)value);
            else
                return value;
        }

        private static void _onMaximumFrequencyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                _spectrumAnalyzer._onMaximumFrequencyChanged((int)e.OldValue, (int)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="MaximumFrequency"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="MaximumFrequency"/></param>
        /// <returns>The adjusted value of <see cref="MaximumFrequency"/></returns>
        protected virtual int _onCoerceMaximumFrequency(int value)
        {
            if ((int)value < MinimumFrequency)
                return MinimumFrequency + 1;
            return value;
        }

        /// <summary>
        /// Called after the <see cref="MaximumFrequency"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="MaximumFrequency"/></param>
        /// <param name="newValue">The new value of <see cref="MaximumFrequency"/></param>
        protected virtual void _onMaximumFrequencyChanged(int oldValue, int newValue)
        {
            UpdateBarLayout();
        }

        /// <summary>
        /// Gets or sets the maximum display frequency (right side) for the spectrum analyzer.
        /// </summary>
        /// <remarks>In usual practice, this value should be somewhere between 0 and half of the maximum sample rate. If using
        /// the maximum sample rate, this would be roughly 22000.</remarks>
        [Category("Common")]
        public int MaximumFrequency
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (int)GetValue(MaximumFrequencyProperty);
            }
            set
            {
                SetValue(MaximumFrequencyProperty, value);
            }
        }
        #endregion

        #region Minimum Frequency
        /// <summary>
        /// Identifies the <see cref="MinimumFrequency" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty MinimumFrequencyProperty = 
            DependencyProperty.Register("MinimumFrequency", typeof(int), typeof(SpectrumAnalyzer), new UIPropertyMetadata(20, _onMinimumFrequencyChanged, _onCoerceMinimumFrequency));

        private static object _onCoerceMinimumFrequency(DependencyObject o, object value)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null)
                return spectrumAnalyzer._onCoerceMinimumFrequency((int)value);
            else
                return value;
        }

        private static void _onMinimumFrequencyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null)
                spectrumAnalyzer._onMinimumFrequencyChanged((int)e.OldValue, (int)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="MinimumFrequency"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="MinimumFrequency"/></param>
        /// <returns>The adjusted value of <see cref="MinimumFrequency"/></returns>
        protected virtual int _onCoerceMinimumFrequency(int value)
        {
            if (value < 0)
                return value = 0;
            CoerceValue(MaximumFrequencyProperty);
            return value;
        }

        /// <summary>
        /// Called after the <see cref="MinimumFrequency"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="MinimumFrequency"/></param>
        /// <param name="newValue">The new value of <see cref="MinimumFrequency"/></param>
        protected virtual void _onMinimumFrequencyChanged(int oldValue, int newValue)
        {
            UpdateBarLayout();
        }

        /// <summary>
        /// Gets or sets the minimum display frequency (left side) for the spectrum analyzer.
        /// </summary>
        [Category("Common")]
        public int MinimumFrequency
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (int)GetValue(MinimumFrequencyProperty);
            }
            set
            {
                SetValue(MinimumFrequencyProperty, value);
            }
        }

        #endregion

        #region BarCount
        /// <summary>
        /// Identifies the <see cref="BarCount" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty BarCountProperty = DependencyProperty.Register("BarCount", typeof(int), typeof(SpectrumAnalyzer), new UIPropertyMetadata(32, _onBarCountChanged, _onCoerceBarCount));

        private static object _onCoerceBarCount(DependencyObject o, object value)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                return _spectrumAnalyzer._onCoerceBarCount((int)value);
            else
                return value;
        }

        private static void _onBarCountChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                _spectrumAnalyzer._onBarCountChanged((int)e.OldValue, (int)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="BarCount"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="BarCount"/></param>
        /// <returns>The adjusted value of <see cref="BarCount"/></returns>
        protected virtual int _onCoerceBarCount(int value)
        {
            value = Math.Max(value, 1);
            return value;
        }

        /// <summary>
        /// Called after the <see cref="BarCount"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="BarCount"/></param>
        /// <param name="newValue">The new value of <see cref="BarCount"/></param>
        protected virtual void _onBarCountChanged(int oldValue, int newValue)
        {
            UpdateBarLayout();
        }

        /// <summary>
        /// Gets or sets the number of bars to show on the sprectrum analyzer.
        /// </summary>
        /// <remarks>A bar's width can be a minimum of 1 pixel. If the BarSpacing and BarCount property result
        /// in the bars being wider than the chart itself, the BarCount will automatically scale down.</remarks>
        [Category("Common")]
        public int BarCount
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (int)GetValue(BarCountProperty);
            }
            set
            {
                SetValue(BarCountProperty, value);
            }
        }
        #endregion

        #region BarSpacing
        /// <summary>
        /// Identifies the <see cref="BarSpacing" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty BarSpacingProperty = 
            DependencyProperty.Register("BarSpacing", typeof(double), typeof(SpectrumAnalyzer), new UIPropertyMetadata(5.0d, _onBarSpacingChanged, _onCoerceBarSpacing));

        private static object _onCoerceBarSpacing(DependencyObject o, object value)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null)
                return spectrumAnalyzer._onCoerceBarSpacing((double)value);
            else
                return value;
        }

        private static void _onBarSpacingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null)
                spectrumAnalyzer._onBarSpacingChanged((double)e.OldValue, (double)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="BarSpacing"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="BarSpacing"/></param>
        /// <returns>The adjusted value of <see cref="BarSpacing"/></returns>
        protected virtual double _onCoerceBarSpacing(double value)
        {
            value = Math.Max(value, 0);
            return value;
        }

        /// <summary>
        /// Called after the <see cref="BarSpacing"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="BarSpacing"/></param>
        /// <param name="newValue">The new value of <see cref="BarSpacing"/></param>
        protected virtual void _onBarSpacingChanged(double oldValue, double newValue)
        {
            UpdateBarLayout();
        }

        /// <summary>
        /// Gets or sets the spacing between the bars.
        /// </summary>
        [Category("Common")]
        public double BarSpacing
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (double)GetValue(BarSpacingProperty);
            }
            set
            {
                SetValue(BarSpacingProperty, value);
            }
        }
        #endregion

        #region PeakFallDelay
        /// <summary>
        /// Identifies the <see cref="PeakFallDelay" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty PeakFallDelayProperty = 
            DependencyProperty.Register("PeakFallDelay", typeof(int), typeof(SpectrumAnalyzer), new UIPropertyMetadata(10, _onPeakFallDelayChanged, _onCoercePeakFallDelay));

        private static object _onCoercePeakFallDelay(DependencyObject o, object value)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                return _spectrumAnalyzer._onCoercePeakFallDelay((int)value);
            else
                return value;
        }

        private static void _onPeakFallDelayChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                _spectrumAnalyzer._onPeakFallDelayChanged((int)e.OldValue, (int)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="PeakFallDelay"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="PeakFallDelay"/></param>
        /// <returns>The adjusted value of <see cref="PeakFallDelay"/></returns>
        protected virtual int _onCoercePeakFallDelay(int value)
        {
            value = Math.Max(value, 0);
            return value;
        }

        /// <summary>
        /// Called after the <see cref="PeakFallDelay"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="PeakFallDelay"/></param>
        /// <param name="newValue">The new value of <see cref="PeakFallDelay"/></param>
        protected virtual void _onPeakFallDelayChanged(int oldValue, int newValue)
        {

        }

        /// <summary>
        /// Gets or sets the delay factor for the peaks falling.
        /// </summary>
        /// <remarks>
        /// The delay is relative to the refresh rate of the chart.
        /// </remarks>
        [Category("Common")]
        public int PeakFallDelay
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (int)GetValue(PeakFallDelayProperty);
            }
            set
            {
                SetValue(PeakFallDelayProperty, value);
            }
        }
        #endregion

        #region IsFrequencyScaleLinear
        /// <summary>
        /// Identifies the <see cref="IsFrequencyScaleLinear" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty IsFrequencyScaleLinearProperty = 
            DependencyProperty.Register("IsFrequencyScaleLinear", typeof(bool), typeof(SpectrumAnalyzer), new UIPropertyMetadata(false, _onIsFrequencyScaleLinearChanged, _onCoerceIsFrequencyScaleLinear));

        private static object _onCoerceIsFrequencyScaleLinear(DependencyObject o, object value)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                return _spectrumAnalyzer._onCoerceIsFrequencyScaleLinear((bool)value);
            else
                return value;
        }

        private static void _onIsFrequencyScaleLinearChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                _spectrumAnalyzer._onIsFrequencyScaleLinearChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="IsFrequencyScaleLinear"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="IsFrequencyScaleLinear"/></param>
        /// <returns>The adjusted value of <see cref="IsFrequencyScaleLinear"/></returns>
        protected virtual bool _onCoerceIsFrequencyScaleLinear(bool value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="IsFrequencyScaleLinear"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="IsFrequencyScaleLinear"/></param>
        /// <param name="newValue">The new value of <see cref="IsFrequencyScaleLinear"/></param>
        protected virtual void _onIsFrequencyScaleLinearChanged(bool oldValue, bool newValue)
        {
            UpdateBarLayout();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the bars are layed out on a linear scale horizontally.
        /// </summary>
        /// <remarks>
        /// If true, the bars will represent frequency buckets on a linear scale (making them all
        /// have equal band widths on the frequency scale). Otherwise, the bars will be layed out
        /// on a logrithmic scale, with each bar having a larger bandwidth than the one previous.
        /// </remarks>
        [Category("Common")]
        public bool IsFrequencyScaleLinear
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (bool)GetValue(IsFrequencyScaleLinearProperty);
            }
            set
            {
                SetValue(IsFrequencyScaleLinearProperty, value);
            }
        }
        #endregion

        #region BarHeightScaling
        /// <summary>
        /// Identifies the <see cref="BarHeightScaling" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty BarHeightScalingProperty = 
            DependencyProperty.Register("BarHeightScaling", typeof(BarHeightScalingStyles), typeof(SpectrumAnalyzer), new UIPropertyMetadata(BarHeightScalingStyles.Decibel, _onBarHeightScalingChanged, _onCoerceBarHeightScaling));

        private static object _onCoerceBarHeightScaling(DependencyObject o, object value)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                return _spectrumAnalyzer._onCoerceBarHeightScaling((BarHeightScalingStyles)value);
            else
                return value;
        }

        private static void _onBarHeightScalingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                _spectrumAnalyzer._onBarHeightScalingChanged((BarHeightScalingStyles)e.OldValue, (BarHeightScalingStyles)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="BarHeightScaling"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="BarHeightScaling"/></param>
        /// <returns>The adjusted value of <see cref="BarHeightScaling"/></returns>
        protected virtual BarHeightScalingStyles _onCoerceBarHeightScaling(BarHeightScalingStyles value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="BarHeightScaling"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="BarHeightScaling"/></param>
        /// <param name="newValue">The new value of <see cref="BarHeightScaling"/></param>
        protected virtual void _onBarHeightScalingChanged(BarHeightScalingStyles oldValue, BarHeightScalingStyles newValue)
        {

        }

        /// <summary>
        /// Gets or sets a value indicating to what scale the bar heights are drawn.
        /// </summary>
        [Category("Common")]
        public BarHeightScalingStyles BarHeightScaling
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (BarHeightScalingStyles)GetValue(BarHeightScalingProperty);
            }
            set
            {
                SetValue(BarHeightScalingProperty, value);
            }
        }
        #endregion

        #region AveragePeaks
        /// <summary>
        /// Identifies the <see cref="AveragePeaks" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty AveragePeaksProperty = 
            DependencyProperty.Register("AveragePeaks", typeof(bool), typeof(SpectrumAnalyzer), new UIPropertyMetadata(false, _onAveragePeaksChanged, _onCoerceAveragePeaks));

        private static object _onCoerceAveragePeaks(DependencyObject o, object value)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                return _spectrumAnalyzer._onCoerceAveragePeaks((bool)value);
            else
                return value;
        }

        private static void _onAveragePeaksChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                _spectrumAnalyzer._onAveragePeaksChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="AveragePeaks"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="AveragePeaks"/></param>
        /// <returns>The adjusted value of <see cref="AveragePeaks"/></returns>
        protected virtual bool _onCoerceAveragePeaks(bool value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="AveragePeaks"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="AveragePeaks"/></param>
        /// <param name="newValue">The new value of <see cref="AveragePeaks"/></param>
        protected virtual void _onAveragePeaksChanged(bool oldValue, bool newValue)
        {

        }

        /// <summary>
        /// Gets or sets a value indicating whether each bar's peak 
        /// value will be averaged with the previous bar's peak.
        /// This creates a smoothing effect on the bars.
        /// </summary>
        [Category("Common")]
        public bool AveragePeaks
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (bool)GetValue(AveragePeaksProperty);
            }
            set
            {
                SetValue(AveragePeaksProperty, value);
            }
        }
        #endregion

        #region BarStyle
        /// <summary>
        /// Identifies the <see cref="BarStyle" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty BarStyleProperty = 
            DependencyProperty.Register("BarStyle", typeof(Style), typeof(SpectrumAnalyzer), new UIPropertyMetadata(null, _onBarStyleChanged, _onCoerceBarStyle));

        private static object _onCoerceBarStyle(DependencyObject o, object value)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                return _spectrumAnalyzer._onCoerceBarStyle((Style)value);
            else
                return value;
        }

        private static void _onBarStyleChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                _spectrumAnalyzer._onBarStyleChanged((Style)e.OldValue, (Style)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="BarStyle"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="BarStyle"/></param>
        /// <returns>The adjusted value of <see cref="BarStyle"/></returns>
        protected virtual Style _onCoerceBarStyle(Style value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="BarStyle"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="BarStyle"/></param>
        /// <param name="newValue">The new value of <see cref="BarStyle"/></param>
        protected virtual void _onBarStyleChanged(Style oldValue, Style newValue)
        {
            UpdateBarLayout();
        }

        /// <summary>
        /// Gets or sets a style with which to draw the bars on the spectrum analyzer.
        /// </summary>
        public Style BarStyle
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (Style)GetValue(BarStyleProperty);
            }
            set
            {
                SetValue(BarStyleProperty, value);
            }
        }
        #endregion

        #region PeakStyle
        /// <summary>
        /// Identifies the <see cref="PeakStyle" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty PeakStyleProperty = DependencyProperty.Register("PeakStyle", typeof(Style), typeof(SpectrumAnalyzer), new UIPropertyMetadata(null, OnPeakStyleChanged, OnCoercePeakStyle));

        private static object OnCoercePeakStyle(DependencyObject o, object value)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null)
                return spectrumAnalyzer.OnCoercePeakStyle((Style)value);
            else
                return value;
        }

        private static void OnPeakStyleChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer spectrumAnalyzer = o as SpectrumAnalyzer;
            if (spectrumAnalyzer != null)
                spectrumAnalyzer.OnPeakStyleChanged((Style)e.OldValue, (Style)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="PeakStyle"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="PeakStyle"/></param>
        /// <returns>The adjusted value of <see cref="PeakStyle"/></returns>
        protected virtual Style OnCoercePeakStyle(Style value)
        {

            return value;
        }

        /// <summary>
        /// Called after the <see cref="PeakStyle"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="PeakStyle"/></param>
        /// <param name="newValue">The new value of <see cref="PeakStyle"/></param>
        protected virtual void OnPeakStyleChanged(Style oldValue, Style newValue)
        {
            UpdateBarLayout();
        }

        /// <summary>
        /// Gets or sets a style with which to draw the falling peaks on the spectrum analyzer.
        /// </summary>
        [Category("Common")]
        public Style PeakStyle
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (Style)GetValue(PeakStyleProperty);
            }
            set
            {
                SetValue(PeakStyleProperty, value);
            }
        }
        #endregion

        #region ActualBarWidth
        /// <summary>
        /// Identifies the <see cref="ActualBarWidth" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty ActualBarWidthProperty = 
            DependencyProperty.Register("ActualBarWidth", typeof(double), typeof(SpectrumAnalyzer), new UIPropertyMetadata(0.0d, _onActualBarWidthChanged, _onCoerceActualBarWidth));

        private static object _onCoerceActualBarWidth(DependencyObject o, object value)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                return _spectrumAnalyzer._onCoerceActualBarWidth((double)value);
            else
                return value;
        }

        private static void _onActualBarWidthChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                _spectrumAnalyzer._onActualBarWidthChanged((double)e.OldValue, (double)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="ActualBarWidth"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="ActualBarWidth"/></param>
        /// <returns>The adjusted value of <see cref="ActualBarWidth"/></returns>
        protected virtual double _onCoerceActualBarWidth(double value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="ActualBarWidth"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="ActualBarWidth"/></param>
        /// <param name="newValue">The new value of <see cref="ActualBarWidth"/></param>
        protected virtual void _onActualBarWidthChanged(double oldValue, double newValue)
        {

        }

        /// <summary>
        /// Gets the actual width that the bars will be drawn at.
        /// </summary>
        public double ActualBarWidth
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (double)GetValue(ActualBarWidthProperty);
            }
            protected set
            {
                SetValue(ActualBarWidthProperty, value);
            }
        }
        #endregion

        #region RefreshRate
        /// <summary>
        /// Identifies the <see cref="RefreshInterval" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty RefreshIntervalProperty = 
            DependencyProperty.Register("RefreshInterval", typeof(int), typeof(SpectrumAnalyzer), new UIPropertyMetadata(mcDefaultUpdateInterval, _onRefreshIntervalChanged, _onCoerceRefreshInterval));

        private static object _onCoerceRefreshInterval(DependencyObject o, object value)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                return _spectrumAnalyzer._onCoerceRefreshInterval((int)value);
            else
                return value;
        }

        private static void _onRefreshIntervalChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                _spectrumAnalyzer._onRefreshIntervalChanged((int)e.OldValue, (int)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="RefreshInterval"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="RefreshInterval"/></param>
        /// <returns>The adjusted value of <see cref="RefreshInterval"/></returns>
        protected virtual int _onCoerceRefreshInterval(int value)
        {
            value = Math.Min(1000, Math.Max(10, value));
            return value;
        }

        /// <summary>
        /// Called after the <see cref="RefreshInterval"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="RefreshInterval"/></param>
        /// <param name="newValue">The new value of <see cref="RefreshInterval"/></param>
        protected virtual void _onRefreshIntervalChanged(int oldValue, int newValue)
        {
            mcAnimationTimer.Interval = TimeSpan.FromMilliseconds(newValue);
        }

        /// <summary>
        /// Gets or sets the refresh interval, in milliseconds, of the Spectrum Analyzer.
        /// </summary>
        /// <remarks>
        /// The valid range of the interval is 10 milliseconds to 1000 milliseconds.
        /// </remarks>
        [Category("Common")]
        public int RefreshInterval
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (int)GetValue(RefreshIntervalProperty);
            }
            set
            {
                SetValue(RefreshIntervalProperty, value);
            }
        }
        #endregion

        #region FFTComplexity
        /// <summary>
        /// Identifies the <see cref="FFTComplexity" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty FFTComplexityProperty = 
            DependencyProperty.Register("FFTComplexity", typeof(FFTDataSize), typeof(SpectrumAnalyzer), new UIPropertyMetadata(FFTDataSize.FFT2048, _onFFTComplexityChanged, _onCoerceFFTComplexity));

        private static object _onCoerceFFTComplexity(DependencyObject o, object value)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                return _spectrumAnalyzer._onCoerceFFTComplexity((FFTDataSize)value);
            else
                return value;
        }

        private static void _onFFTComplexityChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SpectrumAnalyzer _spectrumAnalyzer = o as SpectrumAnalyzer;
            if (_spectrumAnalyzer != null)
                _spectrumAnalyzer._onFFTComplexityChanged((FFTDataSize)e.OldValue, (FFTDataSize)e.NewValue);
        }

        /// <summary>
        /// Coerces the value of <see cref="FFTComplexity"/> when a new value is applied.
        /// </summary>
        /// <param name="value">The value that was set on <see cref="FFTComplexity"/></param>
        /// <returns>The adjusted value of <see cref="FFTComplexity"/></returns>
        protected virtual FFTDataSize _onCoerceFFTComplexity(FFTDataSize value)
        {
            return value;
        }

        /// <summary>
        /// Called after the <see cref="FFTComplexity"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="FFTComplexity"/></param>
        /// <param name="newValue">The new value of <see cref="FFTComplexity"/></param>
        protected virtual void _onFFTComplexityChanged(FFTDataSize _oldValue, FFTDataSize _newValue)
        {
            mChannelData = new float[((int)_newValue / 2)];
        }

        /// <summary>
        /// Gets or sets the complexity of FFT results the Spectrum Analyzer expects. Larger values
        /// will be more accurate at converting time domain data to frequency data, but slower.
        /// </summary>
        [Category("Common")]
        public FFTDataSize FFTComplexity
        {
            // IMPORTANT: To maintain parity between setting a property in XAML and procedural code, do not touch the getter and setter inside this dependency property!
            get
            {
                return (FFTDataSize)GetValue(FFTComplexityProperty);
            }
            set
            {
                SetValue(FFTComplexityProperty, value);
            }
        }
        #endregion

        #endregion

        #region - template overrides -
        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code
        /// or internal processes call System.Windows.FrameworkElement.ApplyTemplate().
        /// </summary>
        public override void OnApplyTemplate()
        {
            mSpectrumCanvas = GetTemplateChild("PART_SpectrumCanvas") as Canvas;
            mSpectrumCanvas.SizeChanged += _spectrumCanvas_SizeChanged;
            UpdateBarLayout();
        }

        /// <summary>
        /// Called whenever the control's template changes. 
        /// </summary>
        /// <param name="oldTemplate">The old template</param>
        /// <param name="newTemplate">The new template</param>
        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);
            if (mSpectrumCanvas != null)
                mSpectrumCanvas.SizeChanged -= _spectrumCanvas_SizeChanged;
        }
        #endregion

        #region - constructor -
        static SpectrumAnalyzer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SpectrumAnalyzer), new FrameworkPropertyMetadata(typeof(SpectrumAnalyzer)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpectrumAnalyzer"/> class.
        /// </summary>
        public SpectrumAnalyzer()
        {
            mcAnimationTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = TimeSpan.FromMilliseconds(mcDefaultUpdateInterval),
            };
            mcAnimationTimer.Tick += _animationTimer_Tick;
        }
        #endregion

        #region - event overrides -
        /// <summary>
        /// When overridden in a derived class, participates in rendering operations that are directed by the layout system. 
        /// The rendering instructions for this element are not used directly when this method is invoked, and are 
        /// instead preserved for later asynchronous use by layout and drawing.
        /// </summary>
        /// <param name="dc">The drawing instructions for a specific element. This context is provided to the layout system.</param>
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            UpdateBarLayout();
            UpdateSpectrum();
        }

        /// <summary>
        /// Raises the SizeChanged event, using the specified information as part of the eventual event data.
        /// </summary>
        /// <param name="sizeInfo">Details of the old and new size involved in the change.</param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateBarLayout();
            UpdateSpectrum();
        }
        #endregion

        #region - event handlers -
        private void soundPlayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsPlaying":
                    if (mSoundPlayer.IsPlaying && !mcAnimationTimer.IsEnabled)
                    {
                        mcAnimationTimer.Start();
                    }
                    break;
                case "IsRecording":
                    if (!mcAnimationTimer.IsEnabled && mSoundPlayer.IsRecording)
                        mcAnimationTimer.Start();
                    break;
            }
        }



        private void _animationTimer_Tick(object sender, EventArgs e)
        {
            UpdateSpectrum();
        }

        private void _spectrumCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBarLayout();
        }
        #endregion

        #region - private functions -
        private void UpdateSpectrum()
        {
            if (mSoundPlayer == null || mSpectrumCanvas == null || mSpectrumCanvas.RenderSize.Width < 1 || mSpectrumCanvas.RenderSize.Height < 1)
                return;

            if (((mSoundPlayer.IsPlaying || mSoundPlayer.IsRecording) &&  !mSoundPlayer.GetFFTData(mChannelData)))
                return;

            UpdateSpectrumShapes();
        }


        private void UpdateSpectrumShapes()
        {
            bool _allZero = true;
            double _fftBucketHeight = 0f;
            double _barHeight = 0f;
            double _lastPeakHeight = 0f;
            double _peakYPos = 0f;
            double height = mSpectrumCanvas.RenderSize.Height;
            int _barIndex = 0;
            double _peakDotHeight = Math.Max(mBarWidth / 2.0f, 1);
            double _barHeightScale = (height - _peakDotHeight);

            for (int i = mMinimumFrequencyIndex; i <= mMaximumFrequencyIndex; i++)
            {
                // If we're paused, keep drawing, but set the current height to 0 so the peaks fall.
                if (!mSoundPlayer.IsPlaying && !mSoundPlayer.IsRecording)
                {
                    _barHeight = 0f;
                }
                else // Draw the maximum value for the bar's band
                {
                    switch (BarHeightScaling)
                    {
                        case BarHeightScalingStyles.Decibel:
                            double dbValue = 20 * Math.Log10((double)mChannelData[i]);
                            _fftBucketHeight = ((dbValue - mcMinDBValue) / mcDBScale) * _barHeightScale;
                            break;
                        case BarHeightScalingStyles.Linear:
                            _fftBucketHeight = (mChannelData[i] * mcScaleFactorLinear) * _barHeightScale;
                            break;
                        case BarHeightScalingStyles.Sqrt:
                            _fftBucketHeight = (((Math.Sqrt((double)mChannelData[i])) * mcScaleFactorSqr) * _barHeightScale);
                            break;
                    }

                    if (_barHeight < _fftBucketHeight)
                        _barHeight = _fftBucketHeight;
                    if (_barHeight < 0f)
                        _barHeight = 0f;
                }

                // If this is the last FFT bucket in the bar's group, draw the bar.
                int currentIndexMax = IsFrequencyScaleLinear ? mBarIndexMax[_barIndex] : mBarLogScaleIndexMax[_barIndex];
                if (i == currentIndexMax)
                {
                    // Peaks can't surpass the height of the control.
                    if (_barHeight > height)
                        _barHeight = height;

                    if (AveragePeaks && _barIndex > 0)
                        _barHeight = (_lastPeakHeight + _barHeight) / 2;

                    _peakYPos = _barHeight;

                    if (channelPeakData[_barIndex] < _peakYPos)
                        channelPeakData[_barIndex] = (float)_peakYPos;
                    else
                        channelPeakData[_barIndex] = (float)(_peakYPos + (PeakFallDelay * channelPeakData[_barIndex])) / ((float)(PeakFallDelay + 1));

                    double xCoord = BarSpacing + (mBarWidth * _barIndex) + (BarSpacing * _barIndex) + 1;

                    mcBarShapes[_barIndex].Margin = new Thickness(xCoord, (height - 1) - _barHeight, 0, 0);
                    mcBarShapes[_barIndex].Height = _barHeight;
                    mcPeakShapes[_barIndex].Margin = new Thickness(xCoord, (height - 1) - channelPeakData[_barIndex] - _peakDotHeight, 0, 0);
                    mcPeakShapes[_barIndex].Height = _peakDotHeight;

                    if (channelPeakData[_barIndex] > 0.05)
                        _allZero = false;

                    _lastPeakHeight = _barHeight;
                    _barHeight = 0f;
                    _barIndex++;
                }
            }

            if (_allZero && (!mSoundPlayer.IsPlaying && !mSoundPlayer.IsRecording))
                mcAnimationTimer.Stop();
        }

        private void UpdateBarLayout()
        {
            if (mSoundPlayer == null || mSpectrumCanvas == null)
                return;

            mBarWidth = Math.Max(((double)(mSpectrumCanvas.RenderSize.Width - (BarSpacing * (BarCount + 1))) / (double)BarCount), 1);
            mMaximumFrequencyIndex = Math.Min(mSoundPlayer.GetFFTFrequencyIndex(MaximumFrequency) + 1, 2047);
            mMinimumFrequencyIndex = Math.Min(mSoundPlayer.GetFFTFrequencyIndex(MinimumFrequency), 2047);
            mBandWidth = Math.Max(((double)(mMaximumFrequencyIndex - mMinimumFrequencyIndex)) / mSpectrumCanvas.RenderSize.Width, 1.0);

            int _actualBarCount;
            if (mBarWidth >= 1.0d)
                _actualBarCount = BarCount;
            else
                _actualBarCount = Math.Max((int)((mSpectrumCanvas.RenderSize.Width - BarSpacing) / (mBarWidth + BarSpacing)), 1);
            channelPeakData = new float[_actualBarCount];

            int _indexCount = mMaximumFrequencyIndex - mMinimumFrequencyIndex;
            int _linearIndexBucketSize = (int)Math.Round((double)_indexCount / (double)_actualBarCount, 0);
            List<int> _maxIndexList = new List<int>();
            List<int> _maxLogScaleIndexList = new List<int>();
            double _maxLog = Math.Log(_actualBarCount, _actualBarCount);
            for (int i = 1; i < _actualBarCount; i++)
            {
                _maxIndexList.Add(mMinimumFrequencyIndex + (i * _linearIndexBucketSize));
                int logIndex = (int)((_maxLog - Math.Log((_actualBarCount + 1) - i, (_actualBarCount + 1))) * _indexCount) + mMinimumFrequencyIndex;
                _maxLogScaleIndexList.Add(logIndex);
            }
            _maxIndexList.Add(mMaximumFrequencyIndex);
            _maxLogScaleIndexList.Add(mMaximumFrequencyIndex);
            mBarIndexMax = _maxIndexList.ToArray();
            mBarLogScaleIndexMax = _maxLogScaleIndexList.ToArray();

            mBarHeights = new double[_actualBarCount];
            mPeakHeights = new double[_actualBarCount];

            mSpectrumCanvas.Children.Clear();
            mcBarShapes.Clear();
            mcPeakShapes.Clear();

            double height = mSpectrumCanvas.RenderSize.Height;
            double _peakDotHeight = Math.Max(mBarWidth / 2.0f, 1);
            for (int i = 0; i < _actualBarCount; i++)
            {
                double _xCoord = BarSpacing + (mBarWidth * i) + (BarSpacing * i) + 1;
                Rectangle _barRectangle = new Rectangle()
                {
                    Margin = new Thickness(_xCoord, height, 0, 0),
                    Width = mBarWidth,
                    Height = 0,
                    Style = BarStyle
                };
                mcBarShapes.Add(_barRectangle);
                Rectangle _peakRectangle = new Rectangle()
                {
                    Margin = new Thickness(_xCoord, height - _peakDotHeight, 0, 0),
                    Width = mBarWidth,
                    Height = _peakDotHeight,
                    Style = PeakStyle
                };
                mcPeakShapes.Add(_peakRectangle);
            }

            foreach (Shape shape in mcBarShapes)
                mSpectrumCanvas.Children.Add(shape);
            foreach (Shape shape in mcPeakShapes)
                mSpectrumCanvas.Children.Add(shape);

            ActualBarWidth = mBarWidth;
        }
        #endregion

        #region - public functions -
        /// <summary>
        /// Register a sound player from which the spectrum analyzer
        /// can get the necessary playback data.
        /// </summary>
        /// <param name="soundPlayer">A sound player that provides spectrum data through the ISpectrumPlayer interface methods.</param>
        public void RegisterSoundPlayer(ISpectrumPlayer _soundPlayer)
        {
            this.mSoundPlayer = _soundPlayer;
            mSoundPlayer.PropertyChanged += soundPlayer_PropertyChanged;
            UpdateBarLayout();
            mcAnimationTimer.Start();
        }
        #endregion

    }
}
