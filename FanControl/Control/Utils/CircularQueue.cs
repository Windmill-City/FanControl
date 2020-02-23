using System;
using System.Collections;

public class CircularQueue : ICollection
{
    private Object[] _array;
    private int _head;       // First valid element in the queue
    private int _tail;       // Last valid element in the queue
    private int _size;       // Number of elements.
    private bool _isReverse; // is data reversed in the queue
    [NonSerialized]
    private Object _syncRoot;

    private int _version;

    public CircularQueue(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException("capacity", "Negative capacity");

        _array = new Object[capacity];
        _head = 0;
        _tail = 0;
        _size = 0;
        _isReverse = false;
    }

    // Creates a queue with room for capacity objects. When full, the new
    // capacity is set to the old capacity * growFactor.
    //
    public CircularQueue(int capacity, bool isReverse)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException("capacity", "Negative capacity");

        _array = new Object[capacity];
        _isReverse = isReverse;
        _size = 0;
        _head = 0;
        _tail = 0;
    }

    public int Count
    {
        get { return _size; }
    }

    public virtual bool IsSynchronized
    {
        get { return false; }
    }

    public virtual Object SyncRoot
    {
        get
        {
            if (_syncRoot == null)
            {
                System.Threading.Interlocked.CompareExchange(ref _syncRoot, new Object(), null);
            }
            return _syncRoot;
        }
    }

    // Removes all Objects from the queue.
    public virtual void Clear()
    {
        if (_head < _tail)
            Array.Clear(_array, _head, _size);
        else
        {
            Array.Clear(_array, _head, _array.Length - _head);
            Array.Clear(_array, 0, _tail);
        }

        _head = 0;
        _tail = 0;
        _size = 0;
        _version++;
    }

    // CopyTo copies a collection into an Array, starting at a particular
    // index into the array.
    // 
    public virtual void CopyTo(Array array, int index)
    {
        if (array == null)
            throw new ArgumentNullException("array");
        if (array.Rank != 1)
            throw new ArgumentException("Arg_RankMultiDimNotSupported");
        if (index < 0)
            throw new ArgumentOutOfRangeException("index", "ArgumentOutOfRange_Index");
        int arrayLen = array.Length;
        if (arrayLen - index < _size)
            throw new ArgumentException("Argument_InvalidOffLen");

        int numToCopy = _size;
        if (numToCopy == 0)
            return;
        int firstPart = (_array.Length - _head < numToCopy) ? _array.Length - _head : numToCopy;
        Array.Copy(_array, _head, array, index, firstPart);
        numToCopy -= firstPart;
        if (numToCopy > 0)
            Array.Copy(_array, 0, array, index + _array.Length - _head, numToCopy);
    }

    // CopyTo copies a collection into an Array, starting at a particular
    // index into the array.
    // 
    public virtual void CopyTo(Array target, int targetindex, int sourceindex, int numToCopy)
    {
        if (target == null)
            throw new ArgumentNullException("array");
        if (target.Rank != 1)
            throw new ArgumentException("Arg_RankMultiDimNotSupported");
        if (numToCopy < 0)
            throw new ArgumentException("nagetive numToCopy");
        if (targetindex < 0 || sourceindex < 0 || numToCopy + sourceindex > _size)
            throw new ArgumentOutOfRangeException("index", "ArgumentOutOfRange_Index");

        if (numToCopy == 0)
            return;
        int sourcestart = (_head + sourceindex) % _array.Length;
        int firstPart = (_array.Length - sourcestart < numToCopy) ? _array.Length - sourcestart : numToCopy;
        Array.Copy(_array, sourcestart, target, targetindex, firstPart);
        numToCopy -= firstPart;
        if (numToCopy > 0)
            Array.Copy(_array, 0, target, targetindex + _array.Length - sourcestart, numToCopy);
    }

    // Adds obj to the tail of the queue.
    //
    public virtual void Enqueue(Object obj)
    {
        if (_array.Length == 0)
        {
            return;
        }
        if (_size == _array.Length)
        {
            Dequeue();
        }
        if (_isReverse)
        {
            _head = (_head - 1 + _array.Length) % _array.Length;
            _array[_head] = obj;
        }
        else
        {
            _array[_tail] = obj;
            _tail = (_tail + 1) % _array.Length;
        }
        _size++;
        _version++;
    }

    // GetEnumerator returns an IEnumerator over this Queue.  This
    // Enumerator will support removing.
    // 
    public virtual IEnumerator GetEnumerator()
    {
        return new QueueEnumerator(this);
    }

    // Removes the object at the head of the queue and returns it. If the queue
    // is empty, this method simply returns null.
    public virtual Object Dequeue()
    {
        if (Count == 0)
            throw new InvalidOperationException("EmptyQueue");
        Object removed;
        if (_isReverse)
        {
            _tail = (_tail - 1 + _array.Length) % _array.Length;
            removed = _array[_tail];
            _array[_tail] = null;
        }
        else
        {
            removed = _array[_head];
            _head = (_head + 1) % _array.Length;
        }
        _size--;
        _version++;
        return removed;
    }

    // Returns the object at the head of the queue. The object remains in the
    // queue. If the queue is empty, this method throws an 
    // InvalidOperationException.
    public virtual Object Peek()
    {
        if (Count == 0)
            throw new InvalidOperationException("EmptyQueue");

        return _array[_head];
    }

    // Returns true if the queue contains at least one object equal to obj.
    // Equality is determined using obj.Equals().
    //
    // Exceptions: ArgumentNullException if obj == null.
    public virtual bool Contains(Object obj)
    {
        int index = _head;
        int count = _size;

        while (count-- > 0)
        {
            if (obj == null)
            {
                if (_array[index] == null)
                    return true;
            }
            else if (_array[index] != null && _array[index].Equals(obj))
            {
                return true;
            }
            index = (index + 1) % _array.Length;
        }

        return false;
    }

    internal Object GetElement(int i)
    {
        return _array[(_head + i) % _array.Length];
    }

    // Iterates over the objects in the queue, returning an array of the
    // objects in the Queue, or an empty array if the queue is empty.
    // The order of elements in the array is first in to last in, the same
    // order produced by successive calls to Dequeue.
    public virtual Object[] ToArray()
    {
        Object[] arr = new Object[_size];
        if (_size == 0)
            return arr;

        if (_head < _tail)
        {
            Array.Copy(_array, _head, arr, 0, _size);
        }
        else
        {
            Array.Copy(_array, _head, arr, 0, _array.Length - _head);
            Array.Copy(_array, 0, arr, _array.Length - _head, _tail);
        }

        return arr;
    }

    // Implements an enumerator for a Queue.  The enumerator uses the
    // internal version number of the list to ensure that no modifications are
    // made to the list while an enumeration is in progress.
    [Serializable]
    private class QueueEnumerator : IEnumerator, ICloneable
    {
        private CircularQueue _q;
        private int _index;
        private int _version;
        private Object currentElement;

        internal QueueEnumerator(CircularQueue q)
        {
            _q = q;
            _version = _q._version;
            _index = 0;
            currentElement = _q._array;
            if (_q._size == 0)
                _index = -1;
        }

        public Object Clone()
        {
            return MemberwiseClone();
        }

        public virtual bool MoveNext()
        {
            if (_version != _q._version) throw new InvalidOperationException("Failed Version");

            if (_index < 0)
            {
                currentElement = _q._array;
                return false;
            }

            currentElement = _q.GetElement(_index);
            _index++;

            if (_index == _q._size)
                _index = -1;
            return true;
        }

        public virtual Object Current
        {
            get
            {
                if (currentElement == _q._array)
                {
                    if (_index == 0)
                        throw new InvalidOperationException("NotStarted");
                    else
                        throw new InvalidOperationException("Ended");
                }
                return currentElement;
            }
        }

        public virtual void Reset()
        {
            if (_version != _q._version) throw new InvalidOperationException("Failed Version");
            if (_q._size == 0)
                _index = -1;
            else
                _index = 0;
            currentElement = _q._array;
        }
    }
}
