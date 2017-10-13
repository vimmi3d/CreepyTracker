using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Microsoft.Samples.Kinect.ColorBasics
{
    public class FrameCounter : INotifyPropertyChanged
    {
        private Stopwatch _stopWatch;
        private uint _framesSinceUpdate = 0;
        private DateTime _nextStatusUpdate = DateTime.MinValue;
        private CpuCounter _cpuCounter;
        private double _fps;
        private double _cpuTime;

        public FrameCounter()
        {
            _stopWatch = new Stopwatch();
            _cpuCounter = new CpuCounter(this);
        }

        public double FramesPerSecond
        {
            get { return _fps; }
            protected set
            {
                if (_fps != value)
                {
                    _fps = value;
                    NotifyPropertyChanged("FramesPerSecond");
                }
            }
        }

        public double CpuTimePerFrame
        {
            get { return _cpuTime; }
            protected set
            {
                if (_cpuTime != value)
                {
                    _cpuTime = value;
                    NotifyPropertyChanged("CpuTimePerFrame");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void DeferFrameCount(TimeSpan timeToWait)
        {
            this._nextStatusUpdate = DateTime.Now + timeToWait;
        }

        public IDisposable Increment()
        {
            this._framesSinceUpdate++;

            if (DateTime.Now >= this._nextStatusUpdate)
            {
                double fps = 0.0;
                double cpuTime = 0.0;

                if (this._stopWatch.IsRunning)
                {
                    this._stopWatch.Stop();
                    fps = this._framesSinceUpdate / this._stopWatch.Elapsed.TotalSeconds;
                    cpuTime = this._cpuTime / fps;
                    this._stopWatch.Reset();
                }

                this._nextStatusUpdate = DateTime.Now + TimeSpan.FromSeconds(1);
                this.FramesPerSecond = fps;
                this.CpuTimePerFrame = cpuTime;
            }

            if (!this._stopWatch.IsRunning)
            {
                this._framesSinceUpdate = 0;
                this._cpuTime = 0;
                this._stopWatch.Start();
            }

            _cpuCounter.Reset();
            return _cpuCounter;
        }

        internal void IncrementCpuTime(double amount)
        {
            _cpuTime += amount;
        }

        public void Reset()
        {
            _nextStatusUpdate = DateTime.MinValue;
            _framesSinceUpdate = 0;
            FramesPerSecond = 0;
            CpuTimePerFrame = 0;
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public class CpuCounter : IDisposable
        {
            FrameCounter _parent;
            Stopwatch _stopWatch;

            internal CpuCounter(FrameCounter parent)
            {
                _parent = parent;
                _stopWatch = new Stopwatch();
            }

            public TimeSpan Elapsed
            {
                get
                {
                    return _stopWatch.Elapsed;
                }
            }

            public void Reset()
            {
                _stopWatch.Reset();
                if (!_stopWatch.IsRunning)
                {
                    _stopWatch.Start();
                }

            }

            public void Dispose()
            {
                _stopWatch.Stop();
                _parent.IncrementCpuTime(_stopWatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
