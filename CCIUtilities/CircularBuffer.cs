using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCIUtilities
{
    #region CircularBuffer class
    public class CircularBuffer<T> : IEnumerable<T>
    {
        List<Chunk> chunks = new List<Chunk>(10);
        long _bufferLength = 0;
        long _back = 0; //internal index showing where current element 0 of the allocated Buffer is
        long _front = 0; //internal index showing where last element in the allocated buffer is

        #region Properties

        public long MaxSize { get; set; }

        //current size of circular buffer storage
        long _currentSize;
        public long CurrentSize
        {
            get
            {
                return _currentSize;
            }
        }

        //current length of valid data in circular buffer
        public long Length
        {
            get
            {
                return _bufferLength;
            }
        }

        public bool IsFull
        {
            get
            {
                return Length >= MaxSize;
            }
        }

        /// <summary>
        /// Index into allocated buffer; note when one adds an element to the "back" of the buffer the indexing of all previous
        /// elements in the buffer increase by 1
        /// </summary>
        /// <param name="index">index of allocated buffer element to return</param>
        /// <returns>value of element</returns>
        public T this[long index]
        {
            get
            {
                if (indexOK(index))
                {
                    long i = (_back + index) % _currentSize;
                    Chunk c = findChunk(i);
                    return c[i - c.First];
                }
                else
                    throw new IndexOutOfRangeException("Index out of range in CircularBuffer; index = " + index.ToString("0"));
            }

            set
            {
                if (indexOK(index))
                {
                    long i = (_back + index) % _currentSize;
                    Chunk c = findChunk(i);
                    c[i - c.First] = value;
                }
                else
                    throw new IndexOutOfRangeException("Index out of range in CircularBuffer; index = " + index.ToString("0"));
            }
        }
        #endregion

        #region Constructors

        public CircularBuffer(long initialSize, long maxSize)
        {
            if (initialSize <= 0 || maxSize <= 0)
                throw new ArgumentOutOfRangeException("In CircularBuffer constructor: size argument <= 0");
            MaxSize = maxSize;
            allocate(initialSize);
            _currentSize = initialSize;
        }

        public CircularBuffer(long initialSize) : this(initialSize, initialSize) { }
        #endregion

        #region public methods

        public void IncreaseSize()
        {
            long size = _currentSize;
            if (size << 1 > MaxSize) size = MaxSize - size;
            if (_back < _front) //then allocate at the "junction": "easy-peasy"
            {
                allocate(size); //double size of cirular buffer up to max
                _currentSize += size;
                return;
            }
            //next, look for place between _front and _back to add, to avoid having to move elements
            for (int i = 0; i < chunks.Count; i++)
            {
                Chunk c = chunks[i];
                if (c.Last >= _front && c.Last < _back)
                {
                    allocateAfter(i, size);
                    _currentSize += size;
                    return;
                }
            }
            //finally, if none found, allocate at "junction" and move elements into newly allocated Chunk
            long backsideLength = _currentSize - _back;
            Chunk newChunk = allocate(size); //add new Chunk to end of buffer
            _currentSize += size;
            if (backsideLength >= _front) //move front end of allocated buffer back into front of new Chunk
            {
                int fromChunk = 0;
                long fromN = 0;
                int toChunk = chunks.Count - 1;
                long toN = 0;
                for (long i = 0; i < _front; i++)
                {
                    chunks[toChunk].buffer[toN++] = chunks[fromChunk][fromN];
                    chunks[fromChunk].buffer[fromN++] = default(T); //remove reference for garbage collection
                    if (fromN >= chunks[fromChunk].buffer.Length)
                    {
                        fromN = 0;
                        ++fromChunk; //will never "wrap around"
                    }
                    if (toN >= chunks[fromChunk].buffer.Length)
                    {
                        toN = 0;
                        if (++fromChunk >= chunks.Count) fromChunk = 0;
                    }
                }
                _front = chunks.Last().First + _front;
            }
            else //move back end of allocated buffer forward into back of new Chunk
            {
                int fromChunk = chunks.Count - 2;
                long fromN = chunks[fromChunk].buffer.Length;
                int toChunk = chunks.Count - 1;
                long toN = size;
                for (long i = 0; i < backsideLength; i++)
                {
                    chunks[toChunk].buffer[--toN] = chunks[fromChunk].buffer[fromN - 1];
                    chunks[fromChunk].buffer[--fromN] = default(T);
                    if (fromN <= 0)
                    {
                        fromN = chunks[--fromChunk].buffer.Length;
                    }
                    if (toN <= 0)
                    {
                        toN = chunks[--toChunk].buffer.Length;
                    }
                }
                _back = _currentSize - backsideLength;
            }
        }

        public void AddToFront(T newElement)
        {
            if (_front == _back && _bufferLength > 0) IncreaseSize(); //need more space
            Chunk c = findChunk(_front);
            c.buffer[_front - c.First] = newElement;
            if (++_front >= _currentSize) _front = 0; //loop around
            _bufferLength++; ;
        }

        public void AddToBack(T newElement)
        {
            if (_front == _back && _bufferLength > 0) IncreaseSize(); //need more space
            if (--_back < 0) _back = _currentSize - 1;
            Chunk c = findChunk(_back);
            c.buffer[_back - c.First] = newElement;
            _bufferLength++;
        }

        public T RemoveAtFront()
        {
            if (_bufferLength <= 0)
                throw new Exception("In CircularBuffer.RemoveAtFront: attempt to remove from empty buffer");
            T ret = this[_bufferLength - 1];
            this[_bufferLength - 1] = default(T); //remove reference
            if (--_front < 0) _front = _currentSize - 1;
            --_bufferLength;
            return ret;
        }

        public T RemoveAtBack()
        {
            if (_bufferLength <= 0)
                throw new Exception("In CircularBuffer.RemoveAtBack: attempt to remove from empty buffer");
            T ret = this[0];
            this[0] = default(T);
            if (++_back >= _currentSize) _back = 0;
            --_bufferLength;
            return ret;
        }

        public void Clear(bool clearValues = false)
        {
            if (clearValues)
            {
                for (long i = 0; i < Length; i++) this[i] = default(T);
            }
            _back = 0;
            _front = 0;
            _bufferLength = 0;
        }

        public IEnumerable<T> Entries(long From, long Length)
        {
            IEnumerator<T> e = GetEnumerator(From, Length);
            while (e.MoveNext())
                yield return e.Current;
        }

        #endregion

        #region private methods

        Chunk allocate(long size)
        {
            Chunk c;
            T[] buffer = new T[size];
            c.buffer = buffer;
            c.First = _currentSize;
            c.Last = _currentSize + size;
            chunks.Add(c);
            return c;
        }

        void allocateAfter(int after, long size)
        {
            Chunk afterChunk = chunks[after];
            Chunk c = new Chunk();
            T[] buffer = new T[size];
            c.buffer = buffer;
            chunks.Insert(after + 1, c);
            for (int i = after + 1; i < chunks.Count; i++)
            {
                Chunk c1 = chunks[i];
                c1.First += size;
                c1.Last += size;
                chunks[i] = c1;
            }
        }

        bool indexOK(long index)
        {
            return index >= 0 && index < _bufferLength;
        }

        Chunk findChunk(long index)
        {
            return chunks.Find(c => index >= c.First && index < c.Last);
        }
        #endregion

        public IEnumerator<T> GetEnumerator()
        {
            return new CBEnumerator(this);
        }

        internal IEnumerator<T> GetEnumerator(long start, long length)
        {
            return new CBEnumerator(this, start, length);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new CBEnumerator(this);
        }

        internal struct Chunk: IEqualityComparer<Chunk>
        {
            internal long First;
            internal long Last;
            internal T[] buffer;

            internal T this[long i]
            {
                get { return buffer[i]; }
                set { buffer[i] = value; }
            }

            internal long Length { get { return Last - First; } }

            public bool Equals(Chunk x, Chunk y)
            {
                return x.First == y.First;
            }

            public int GetHashCode(Chunk obj)
            {
                return (int)(First & 0xFFFFFFFF);
            }
        }

        public class CBEnumerator : IEnumerator<T>
        {
            CircularBuffer<T> CBuffer;
            Chunk currentChunk; //Chunk containing the current element
            long currentCIndex; //Index within Chunk of current element
            long currentBIndex; //Index within Buffer
            long start;
            long stop;

            public CBEnumerator(CircularBuffer<T> cb) : this(cb, 0, cb._bufferLength) { }

            public CBEnumerator(CircularBuffer<T> cb, long initialOffset, long length)
            {
                CBuffer = cb;
                start = (CBuffer._back + initialOffset) % CBuffer._currentSize;
                currentChunk = CBuffer.findChunk(start);
                currentBIndex = -1;
                currentCIndex = start - currentChunk.First - 1;
                stop = length;
            }

            public T Current
            {
                get { return currentChunk[currentCIndex]; }
            }

            object IEnumerator.Current { get { return Current; } }

            public bool MoveNext()
            {
                if (++currentBIndex >= stop) return false;
                if (++currentCIndex >= currentChunk.Length)
                {
                    currentChunk = CBuffer.findChunk((start + currentBIndex) % CBuffer._currentSize);
                    currentCIndex = 0;
                }
                return true;
            }

            public void Reset()
            {
                currentChunk = CBuffer.findChunk(CBuffer._back);
                currentBIndex = - 1;
                currentCIndex = CBuffer._back - currentChunk.First - 1;
            }

            public void Dispose() { }
        }
    }
    #endregion

    #region DatasetViewer class
    public class DatasetViewer<T>
    {
        static long FirstBufferSize = 1024; //Recommended first CircularBuffer size
        CircularBuffer<T> dataBuffer;
        long _datasetLength; //total number of elements in dataset
        long _maxViewLength; //also equals the maximum size of CircularBuffer
        long _current0 = 0;
        long _currentDataLength = 0;
        long _currentN { get { return _current0 + _currentDataLength; } }

        public long Length { get { return _currentDataLength; } } //length of current View

        public T this[long i] //i is dataset index, may be outside current View, which will be adjusted as needed
        {
            get
            {
                if(IsDatasetIndexInCB(i)) //is datapoint currently in CircularBuffer?
                    return dataBuffer[mapDatasetToCBIndex(i)]; //if so, just return it
                T d = default(T);
                if (IsDatasetIndexInDataset(i)) //is point even in dataset? If not, return default value
                { //if so, ...
                    if (i < _current0) //is datapoint to left of data currently in CircularBuffer?
                    { //if so, fill to left, behind current CB data
                        long limit = _current0 - i; //CB index; this is negative indicating index to left(behind) currnet _back
                        for (int j = 0; j < limit; j++)
                            d = AddOnLeft();
                    }
                    else //datapoint must be to right of data in CB
                    { //if so, fill to right, ahead of current CB data
                        long limit = i - _currentN;
                        for (int j = 0; j <= limit; j++)
                            d = AddOnRight();
                    }
                }
                return d;
            }
        }

        public delegate T AccessDataset(long location); //source of data
        AccessDataset dataPoint;

        public DatasetViewer(AccessDataset data, long dataLength, long bufferLength)
        {
            dataPoint = data;
            _datasetLength = dataLength;
            _maxViewLength = bufferLength;
            dataBuffer = new CircularBuffer<T>(Math.Min(DatasetViewer<T>.FirstBufferSize, bufferLength), Math.Min(bufferLength, dataLength));
        }

        public DatasetViewer(AccessDataset data, long dataLength) : this(data, dataLength, dataLength) { }

        public IEnumerable<T> Dataset(long From, long Length)
        {
            long To = From + Length;
            if (!IsDatasetIndexInDataset(From) || !IsDatasetIndexInDataset(To))
                throw new IndexOutOfRangeException("In DatasetViewer.Dataset iterator: invalid dataset index");
            if (IsDatasetIndexInCB(From)) //then at least From is inside CircularBuffer
            {//so we can read direcly from CircularBuffer for at least some of the data points
                long from = mapDatasetToCBIndex(From);
                long length = Math.Min(Length, dataBuffer.Length - from);
                long remainder = Length - length;
                foreach (T d in dataBuffer.Entries(from, length))
                    yield return d;

                if (remainder > 0) //then To is ouside of current View
                {
                    from = From + length;
                    for (long i = from; i < To; i++)
                        yield return this[i]; //have to do it this way because _left may be changing
                }
            }
            else if (IsDatasetIndexInCB(To)) //then only To is inside CircularBuffer
            {
                yield return this[From++]; //after first one has been read, the View has moved and the new From is ...
                // inside the buffer; To may or may not be anymore
                Length--;
                long from = mapDatasetToCBIndex(From);
                long length = Math.Min(Length, dataBuffer.Length - from);
                long remainder = Length - length;
                foreach (T d in dataBuffer.Entries(from, length))
                    yield return d;

                if (remainder > 0) //then To is ouside of current View
                {
                    from = From + length;
                    for (long i = from; i < To; i++)
                        yield return this[i]; //have to do it this way because _left may be changing
                }
            }
            else //both From and To outside data currently in CircularBuffer
            {
                if (!(
                    (From < _current0 && To < _current0) && (_current0 - To < Math.Min(From + _maxViewLength, _currentN) - _current0) ||
                    (From >= _currentN && To >= _currentN) && (From - _currentN < _currentN - Math.Max(To - _maxViewLength, _current0))
                    ))
                { //so you we don't read too much now; not a "straddle"
                    dataBuffer.Clear();
                    _currentDataLength = 0;
                    _current0 = From;
                }
                for (long i = From; i < To; i++)
                    yield return this[i];
            }
        }

        T AddOnRight()
        {
            if (dataBuffer.IsFull) //full, then need to make space
            { //by removing an element from the back of CB
                dataBuffer.RemoveAtBack();
                _current0++;
                _currentDataLength--;
            }
            T d = dataPoint(_current0 + dataBuffer.Length);
            dataBuffer.AddToFront(d);
            _currentDataLength++;
            return d;
        }

        T AddOnLeft()
        {
            if (dataBuffer.IsFull) //full, then need to make space
            {
                dataBuffer.RemoveAtFront();
                _currentDataLength--;
            }
            T d = dataPoint(--_current0);
            dataBuffer.AddToBack(d);
            _currentDataLength++;
            return d;
        }

        /// <summary>
        /// Map an index into dataset to index into CircularBuffer
        /// </summary>
        /// <param name="n">DatasetIndex: 0 to datasetLength</param>
        /// <returns>CB index</returns>
        long mapDatasetToCBIndex(long n)
        {
            return n - _current0;
        }

        bool IsDatasetIndexInCB(long n)
        {
            return n >= _current0 && n < _current0 + dataBuffer.Length;
        }

        bool IsDatasetIndexInDataset(long n)
        {
            return n >= 0 && n < _datasetLength;
        }
    }
    #endregion
}
