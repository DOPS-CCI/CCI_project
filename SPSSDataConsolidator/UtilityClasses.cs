using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FILMANFileStream;
using CSVStream;
using MATFile;

namespace SPSSDataConsolidator
{
    /// <summary>
    /// Base class for input files
    /// </summary>
    public abstract class FileRecord : INotifyPropertyChanged
    {
        string _path;
        /// <summary>
        /// Directory path and file name to the described file
        /// </summary>
        public string path //path to the file location
        {
            get { return _path; }
            internal set
            {
                if (_path == value) return;
                _path = value;
                Notify("path");
                return;
            }
        }

        /// <summary>
        /// Number of distinct records in this file
        /// </summary>
        abstract public int NumberOfRecords { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void Notify(string p)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
    }

    public interface IFilePointSelector
    {
        /// <summary>
        /// Total number of records associated with this variable selector
        /// </summary>
        int NumberOfRecords { get; }
        /// <summary>
        /// Number of data points selected in this variable selector
        /// </summary>
        int NumberOfDataPoints { get; }
        /// <summary>
        /// Number of files applied to this variable selector; always >= 1
        /// </summary>
        int NumberOfFiles { get; }
        /// <summary>
        /// Is there an error on this variable selector?
        /// </summary>
        bool IsError { get; }
        /// <summary>
        /// Returns the indexed file
        /// </summary>
        /// <param name="i">Zero-based index of the desired FileRecord in this Point Selector</param>
        /// <returns></returns>
        FileRecord this[int i] { get; }
    }

    public class FILMANFileRecord : FileRecord
    {
        public FILMANInputStream stream { get; internal set; }
        public GroupVarDictionary.GroupVarDictionary GVDictionary = null;

        public override int NumberOfRecords
        {
            get { return stream.NRecordSets; }
        }
    }

    public class CSVFileRecord: FileRecord
    {
        public CSVInputStream stream { get; internal set; }

        public override int NumberOfRecords
        {
            get
            {
                return stream.NumberOfRecords;
            }
        }
    }

    public class MATFileRecord: FileRecord
    {
        public MATFileReader stream { get; internal set; }

        public override int NumberOfRecords
        {
            get
            {
                throw new NotImplementedException("MATFileRecord.NumberOfRecords");
            }
        }
    }
}
