using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCIUtilities
{
    public class CircularBuffer<T>
    {
        List<Chunk> chunks = new List<Chunk>(10);
        long _bufferLength = 0;
        long _back = 0; //internal index showing where current element 0 of the allocated Buffer is
        long _front = 0; //internal index showing where last element in the allocated buffer is

        #region Properties

        public long MaxSize { get; set; }

        long _currentSize;
        public long CurrentSize
        {
            get
            {
                return _currentSize;
            }
        }

        /// <summary>
        /// Index into allocated buffer using this; note when one adds an element to the "back" of the buffer the indexing of all previous
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
                    return c.buffer[i - c.first];
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
                    c.buffer[i - c.first] = value;
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
                return;
            }
            //next, look for place between _front and _back to add, to avoid having to move elements
            for (int i = 0; i < chunks.Count; i++)
            {
                Chunk c = chunks[i];
                if (c.last >= _front && c.last < _back)
                {
                    allocateAfter(i, size);
                    _currentSize += size;
                    return;
                }
            }
            //finally, if none found, allocate at "junction" and move elements into newly allocated Chunk
            long backsideLength = _currentSize -_back;
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
                    chunks[toChunk].buffer[toN++] = chunks[fromChunk].buffer[fromN];
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
                _front = chunks.Last().first + _front;
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
            c.buffer[_front - c.first] = newElement;
            if (++_front >= _currentSize) _front = 0; //loop around
            _bufferLength++; ;
        }

        public void AddToBack(T newElement)
        {
            if (_front == _back && _bufferLength > 0) IncreaseSize(); //need more space
            if (--_back < 0) _back = _currentSize - 1;
            Chunk c = findChunk(_back);
            c.buffer[_back - c.first] = newElement;
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
                throw new Exception("In CircularBuffer.RemoveAtBackt: attempt to remove from empty buffer");
            T ret = this[0];
            this[0] = default(T);
            if (++_back >= _currentSize) _back = 0;
            --_bufferLength;
            return ret;
        }
        #endregion

        #region private methods

        Chunk allocate(long size)
        {
            Chunk c;
            T[] buffer = new T[size];
            c.buffer = buffer;
            c.first = _currentSize;
            c.last = _currentSize + size;
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
                c1.first += size;
                c1.last += size;
                chunks[i] = c1;
            }
        }

        bool indexOK(long index)
        {
            return index >= 0 && index < _bufferLength;
        }

        Chunk findChunk(long index)
        {
            return chunks.Find(c => index >= c.first && index < c.last);
        }
        #endregion

        internal struct Chunk
        {
            internal long first;
            internal long last;
            internal T[] buffer;
        }
    }
}
