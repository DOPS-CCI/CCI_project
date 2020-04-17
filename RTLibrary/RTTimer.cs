using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using CCILibrary;
using CCIUtilities;
using Event;
using EventDictionary;

namespace RTLibrary
{
#region MMTimer interface
    /// <summary>
    /// Defines constants for the multimedia Timer's event types/
    /// </summary>
    public enum RTTimerMode
    {
        /// <summary>
        /// Timer event occurs once.
        /// </summary>
        OneShot,

        /// <summary>
        /// Timer event occurs periodically.
        /// </summary>
        Periodic
    };

    /// <summary>
    /// Represents information about the multimedia Timer's capabilities.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RTTimerCaps
    {
        /// <summary>
        /// Minimum supported period in milliseconds.
        /// </summary>
        public int periodMin;

        /// <summary>
        /// Maximum supported period in milliseconds.
        /// </summary>
        public int periodMax;
    }

    public class RTTimer : IDisposable
    {
        #region Delegates

        // Represents the method that is called by Windows when a timer event occurs.
        private delegate void TimeProc(int id, int msg, int user, int param1, int param2);

        // Represents methods that raise events.
        private delegate void EventRaiser(EventArgs e);

        #endregion

        #region Win32 Multimedia Timer Functions

        // Gets timer capabilities.
        [DllImport("winmm.dll")]
        private static extern int timeGetDevCaps(ref RTTimerCaps caps,
            int sizeOfTimerCaps);

        // Creates and starts the timer.
        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution,
            TimeProc proc, int user, int mode);

        // Stops and destroys the timer.
        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

        // Gets system clock value in msec since startup; wraps around in ~50 days.
        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        static extern uint timeGetTime();

        // set resolution
        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        static extern uint timeBeginPeriod(uint uPeriod);

         // end given resolution regime
        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        static extern uint timeEndPeriod(uint uPeriod);

        // Indicates that the operation was successful.
        private const int TIMERR_NOERROR = 0;

        #endregion

        #region Fields

        // Timer identifier.
        private int _timerID;

        // Timer mode.
        private volatile RTTimerMode mode;

        // Period between timer events in milliseconds.
        private volatile int period;

        // Timer resolution in milliseconds.
        private volatile int resolution;

        // Called by Windows when a timer periodic event occurs.
        private TimeProc timeProcPeriodic;

        // Called by Windows when a timer one shot event occurs.
        private TimeProc timeProcOneShot;

        // Represents the method that raises the Tick event.
        private EventRaiser tickRaiser;

        // Indicates whether or not the timer is running.
        private bool running = false;

        // Indicates whether or not the timer has been disposed.
        private volatile bool disposed = false;

        // The ISynchronizeInvoke object to use for marshaling events.
        private ISynchronizeInvoke synchronizingObject = null;

        // Multimedia timer capabilities.
        private static RTTimerCaps caps;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the Timer has started;
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Occurs when the Timer has stopped;
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Occurs each time the time period elapses
        /// </summary>
        public event EventHandler Tick;

        /// <summary>
        /// Occurs when the timer is disposed
        /// </summary>
        public event EventHandler Disposed;

       #endregion

        #region Construction

        /// <summary>
        /// Initialize class.
        /// </summary>
        static RTTimer()
        {
            // Get multimedia timer capabilities.
            timeGetDevCaps(ref caps, Marshal.SizeOf(caps));
        }

        /// <summary>
        /// Initializes a new instance of the Timer class.
        /// </summary>
        public RTTimer()
        {
            Initialize();
        }

        ~RTTimer()
        {
            Dispose(false);
        }

        // Initialize timer with default values.
        private void Initialize()
        {
            this.mode = RTTimerMode.Periodic;
            this.period = Capabilities.periodMin;
            this.resolution = 1;

            running = false;

            timeProcPeriodic = new TimeProc(TimerPeriodicEventCallback);
            timeProcOneShot = new TimeProc(TimerOneShotEventCallback);
            tickRaiser = new EventRaiser(OnTick);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// The timer has already been disposed.
        /// </exception>
        /// <exception cref="RTTimerStartException">
        /// The timer failed to start.
        /// </exception>
        public void Start()
        {
            CheckDisposed();
            if (IsRunning) return;

            // If the periodic event callback should be used.
            if (Mode == RTTimerMode.Periodic)
                _timerID = timeSetEvent(Period, Resolution, timeProcPeriodic, 0, (int)RTTimerMode.Periodic);
            else
                _timerID = timeSetEvent(Period, Resolution, timeProcOneShot, 0, (int)RTTimerMode.OneShot);

            // If the timer was created successfully
            if (_timerID != 0)
            {
                running = true;
                if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
                    SynchronizingObject.BeginInvoke(
                        new EventRaiser(OnStarted),
                        new object[] { EventArgs.Empty });
                else
                    OnStarted(EventArgs.Empty);
            }
            else
                throw new RTTimerStartException("Unable to start multimedia Timer.");
        }

        /// <summary>
        /// Stops timer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>
        public void Stop()
        {
            CheckDisposed();
            if (!running) return;

            // Stop and destroy timer.
            int result = timeKillEvent(_timerID);

            Debug.Assert(result == TIMERR_NOERROR);

            running = false;
            if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
                SynchronizingObject.BeginInvoke(
                    new EventRaiser(OnStopped),
                    new object[] { EventArgs.Empty });
            else
                OnStopped(EventArgs.Empty);
        }

        private void StopInternal()
        {
            timeKillEvent(_timerID);
            _timerID = 0;
        }
        #endregion

        #region Callbacks

        // Callback method called by the Win32 multimedia timer when a timer
        // periodic event occurs.
        private void TimerPeriodicEventCallback(int id, int msg, int user, int param1, int param2)
        {
            if (synchronizingObject != null)
            {
                synchronizingObject.BeginInvoke(tickRaiser, new object[] { EventArgs.Empty });
            }
            else
            {
                OnTick(EventArgs.Empty);
            }
        }

        // Callback method called by the Win32 multimedia timer when a timer
        // one shot event occurs.
        private void TimerOneShotEventCallback(int id, int msg, int user, int param1, int param2)
        {
            if (synchronizingObject != null)
            {
                synchronizingObject.BeginInvoke(tickRaiser, new object[] { EventArgs.Empty });
                Stop();
            }
            else
            {
                OnTick(EventArgs.Empty);
                Stop();
            }
        }

        #endregion

        #region Event Raiser Methods

        // Raises the Disposed event.
        private void OnDisposed(EventArgs e)
        {
            EventHandler handler = Disposed;
            if (handler != null)
                handler(this, e);
        }

        // Raises the Started event.
        private void OnStarted(EventArgs e)
        {
            EventHandler handler = Started;
            if (handler != null)
                handler(this, e);
        }

        // Raises the Stopped event.
        private void OnStopped(EventArgs e)
        {
            EventHandler handler = Stopped;
            if (handler != null)
                handler(this, e);
        }

        // Raises the Tick event.
        private void OnTick(EventArgs e)
        {
            EventHandler handler = Tick;
            if (handler != null)
                handler(this, e);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the object used to marshal event-handler calls.
        /// </summary>
        public ISynchronizeInvoke SynchronizingObject
        {
            get
            {
                CheckDisposed();
                return synchronizingObject;
            }
            set
            {
                CheckDisposed();
                synchronizingObject = value;
            }
        }

        /// <summary>
        /// Gets or sets the time between Tick events.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>   
        public int Period
        {
            get
            {
                CheckDisposed();
                return period;
            }
            set
            {
                CheckDisposed();
                if (value < Capabilities.periodMin || value > Capabilities.periodMax)
                    throw new ArgumentOutOfRangeException("Period", value,
                        "Multimedia Timer period out of range.");

                if (period != value)
                {
                    period = value;
//                    if (Resolution > period) resolution = period;
                    if (IsRunning)
                    {
                        Stop();
                        Start();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the timer resolution.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>        
        /// <remarks>
        /// The resolution is in milliseconds. The resolution increases 
        /// with smaller values; a resolution of 0 indicates periodic events 
        /// should occur with the greatest possible accuracy. To reduce system 
        /// overhead, however, you should use the maximum value appropriate 
        /// for your application.
        /// </remarks>
        public int Resolution
        {
            get
            {
                CheckDisposed();
                return resolution;
            }
            set
            {
                CheckDisposed();
                if (value < 0)
                    throw new ArgumentOutOfRangeException("Resolution", value,
                        "Multimedia timer resolution out of range.");

                if (resolution != value)
                {
                    resolution = value;
                    if (IsRunning)
                    {
                        Stop();
                        Start();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the timer mode.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// If the timer has already been disposed.
        /// </exception>
        public RTTimerMode Mode
        {
            get
            {
                CheckDisposed();
                return mode;
            }
            set
            {
                CheckDisposed();
                if (mode != value)
                {
                    mode = value;
                    if (IsRunning)
                    {
                        Stop();
                        Start();
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Timer is running.
        /// </summary>
        public bool IsRunning
        {
            get { return running; }
        }

        /// <summary>
        /// Returns timerID for this RTTimer; == 0 imples timer is invalid
        /// </summary>
        public int RTTimerID
        {
            get { return _timerID; }
        }

        /// <summary>
        /// Gets the timer capabilities.
        /// </summary>
        public static RTTimerCaps Capabilities
        {
            get { return caps; }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Frees timer resources.
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            Dispose(true);
            OnDisposed(EventArgs.Empty);
        }

        private void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException("MultimediaTimer");
        }

        private void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            if (IsRunning) StopInternal();

            if (disposing)
            {
                Disposed = Started = Stopped = Tick = null;
                GC.SuppressFinalize(this);
            }
        }
        #endregion
    }
#endregion

    /// <summary>
    /// The exception that is thrown when a timer fails to start.
    /// </summary>
    public class RTTimerStartException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the TimerStartException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception. 
        /// </param>
        public RTTimerStartException(string message)
            : base(message) { }
    }
/*    public class RTSimpleTimer
    {
        //Callback method that is called by Windows when a timer event occurs.
        internal delegate void TimerCallBack();

        // Creates and starts the timer.
        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution,
            TimeProc proc, int user, int mode);

        // Stops and destroys the timer.
        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

        // Gets system clock value in msec since startup; wraps around in ~50 days.
        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        static extern uint timeGetTime();

        public int StartTimer(int delay, )
        {
            int timerNumber = RTSimpleTimer.timeSetEvent(delay,1,callBack(,0,RTTimerMode.OneShot);
        }
    }

    public class RTTrial
    {

    }

    public abstract class RTEvent
    {
        Dispatcher _mainDispatcher;
        OutputEvent _event;
        private Delegate _execute;
        private object[] _argument;
        public OutputEvent EventRecord //linked OutputEvent record
        {
            get { return _event; }
            set { _event = value; }
        }

        public EventDictionaryEntry EDE
        {
            get
            {
                if (_event != null) return _event.EDE;
                else return null;
            }
        }

        protected void Execute() //execute delagate on main thread, so that GUI is available to it
        {
            _mainDispatcher.BeginInvoke(_execute, DispatcherPriority.Send, _event, _argument);
        }
    }

    public class RTScheduledEvent : RTEvent
    {
        internal void RTTimer_Callback(int timer,
    }*/
}
